#!/usr/bin/env python3
import argparse,hashlib,io,json,pathlib,shutil,subprocess,zipfile,struct,os
ROOT=pathlib.Path(__file__).resolve().parent
SOURCE_SHA='c39817efa21e539fe61f318dc1ad4050b424c6120ab8ed384d94148f869c4bdc'
def sha(p):return hashlib.sha256(pathlib.Path(p).read_bytes()).hexdigest()
def run(*a):subprocess.run([str(x) for x in a],check=True)
def main():
 ap=argparse.ArgumentParser();ap.add_argument('--apk',type=pathlib.Path,required=True);ap.add_argument('--tools',type=pathlib.Path,required=True);ap.add_argument('--output',type=pathlib.Path,required=True);a=ap.parse_args();apk=a.apk.resolve();tools=a.tools.resolve();out=a.output.resolve();out.mkdir(parents=True,exist_ok=True)
 if sha(apk)!=SOURCE_SHA:raise SystemExit('Source hash mismatch; re-analysis required')
 for n,p in json.loads((ROOT/'toolchain.json').read_text()).items():
  if sha(tools/n)!=p['sha256']:raise SystemExit('Tool hash mismatch: '+n)
 managed=out/'managed';managed.mkdir(exist_ok=True)
 with zipfile.ZipFile(apk) as z:
  with zipfile.ZipFile(io.BytesIO(z.read('assets/Mono/Managed.zip'))) as m:m.extractall(managed)
 dotnet=tools/'dotnet/dotnet';csc=tools/'dotnet/sdk/8.0.100/Roslyn/bincore/csc.dll';cecil=tools/'cecil/lib/netstandard2.0/Mono.Cecil.dll';refs=list((tools/'dotnet/packs/Microsoft.NETCore.App.Ref/8.0.0/ref/net8.0').glob('*.dll'))
 def compile(n,src,ref,exe=True):
  rsp=out/(n+'.rsp');rsp.write_text('\n'.join(['-nologo','-nostdlib+','-target:'+('exe' if exe else 'library'),'-out:'+str(out/(n+'.dll'))]+['-r:'+str(p) for p in ref]+[str(ROOT/s) for s in src]));run(dotnet,csc,'@'+str(rsp))
  if exe:(out/(n+'.runtimeconfig.json')).write_text(json.dumps({'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':'8.0.0'}}}))
 compile('NativeContentUnlocks',['NativeContentUnlocks.cs'],list(managed.glob('*.dll')),False)
 shutil.copy(cecil,out/'Mono.Cecil.dll');compile('Patch',['Patch.cs'],refs+[cecil]);run(dotnet,out/'Patch.dll',managed/'Assembly-CSharp.dll',out/'NativeContentUnlocks.dll',managed,out/'Assembly-CSharp.patched.dll',out/'patch-log.txt')
 assert (out/'Assembly-CSharp.patched.dll').stat().st_size>4000000
 compile('Verify',['Verify.cs'],refs+[cecil]);run(dotnet,out/'Verify.dll',managed/'Assembly-CSharp.dll',out/'Assembly-CSharp.patched.dll',out/'NativeContentUnlocks.dll',managed)
 smoke=out/'smoke';smoke.mkdir(exist_ok=True)
 for p in managed.glob('*.dll'):
  if p.name!='mscorlib.dll':shutil.copy(p,smoke/p.name)
 shutil.copy(out/'Assembly-CSharp.patched.dll',smoke/'Assembly-CSharp.dll');shutil.copy(out/'NativeContentUnlocks.dll',smoke/'NativeContentUnlocks.dll')
 compile('Smoke',['Smoke.cs'],refs+[smoke/'Assembly-CSharp.dll',smoke/'NativeContentUnlocks.dll'])
 shutil.copy(out/'Smoke.dll',smoke/'Smoke.dll');shutil.copy(out/'Smoke.runtimeconfig.json',smoke/'Smoke.runtimeconfig.json');run(dotnet,smoke/'Smoke.dll')
 nested=io.BytesIO()
 with zipfile.ZipFile(apk) as z:
  with zipfile.ZipFile(io.BytesIO(z.read('assets/Mono/Managed.zip'))) as m,zipfile.ZipFile(nested,'w') as w:
   for entry in m.infolist():w.writestr(entry,(out/'Assembly-CSharp.patched.dll').read_bytes() if entry.filename=='Assembly-CSharp.dll' else m.read(entry))
   w.writestr('NativeContentUnlocks.dll',(out/'NativeContentUnlocks.dll').read_bytes(),compress_type=zipfile.ZIP_DEFLATED)
  unsigned=out/'unsigned.apk'
  with zipfile.ZipFile(unsigned,'w') as w:
   for entry in z.infolist():
    if entry.filename.startswith('META-INF/') and (entry.filename.endswith(('.SF','.RSA','.DSA','.EC')) or entry.filename=='META-INF/MANIFEST.MF'):continue
    data=nested.getvalue() if entry.filename=='assets/Mono/Managed.zip' else z.read(entry)
    entry.extra=b''
    if entry.compress_type==zipfile.ZIP_STORED:
     align=16384 if entry.filename.endswith('.so') else 4
     pad=(-(w.fp.tell()+30+len(entry.filename.encode())))%align
     while pad and pad<6:pad+=align
     if pad:entry.extra=struct.pack('<HHH',0xd935,pad-4,align)+bytes(pad-6)
    w.writestr(entry,data)
 key=out/'test-signing.p12'
 if not key.exists():run('keytool','-genkeypair','-alias','worldbox-test','-keyalg','RSA','-keysize','2048','-validity','10000','-keystore',key,'-storepass','android','-keypass','android','-dname','CN=Worldbox Native Unlock Test','-storetype','PKCS12')
 run('java','-jar',tools/'ecj.jar','-17','-cp',tools/'apksig.jar','-d',out,ROOT/'SignApk.java')
 signed=out/'worldbox-native-unlocks-test.apk';run('java','-cp',str(tools/'apksig.jar')+':'+str(out),'SignApk',unsigned,signed,key);run('java','-cp',str(tools/'apksig.jar')+':'+str(out),'SignApk','verify',signed)
 with zipfile.ZipFile(apk) as src,zipfile.ZipFile(signed) as dst:
  assert dst.testzip() is None
  changed=[n for n in src.namelist() if not n.startswith('META-INF/') and src.read(n)!=dst.read(n)]
  assert changed==['assets/Mono/Managed.zip'],changed
  with zipfile.ZipFile(io.BytesIO(dst.read(changed[0]))) as m:assert hashlib.sha256(m.read('NativeContentUnlocks.dll')).hexdigest()==sha(out/'NativeContentUnlocks.dll')
 receipt={'source_sha256':sha(apk),'apk_sha256':sha(signed),'changed_apk_payloads':changed,'android_startup_tested':False,'release_status':'prerelease; Android device testing required'}
 (out/'build-receipt.json').write_text(json.dumps(receipt,indent=2));print(json.dumps(receipt,indent=2))
if __name__=='__main__':main()
