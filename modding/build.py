#!/usr/bin/env python3
"""Pinned, reproducible Linux x64 APK patch build. Does not change client configuration."""
import argparse, hashlib, io, json, pathlib, shutil, subprocess, zipfile
ROOT=pathlib.Path(__file__).resolve().parent
SOURCE_SHA='c39817efa21e539fe61f318dc1ad4050b424c6120ab8ed384d94148f869c4bdc'
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for chunk in iter(lambda:f.read(1048576),b''):h.update(chunk)
 return h.hexdigest()
def run(*cmd):subprocess.run([str(s) for s in cmd],check=True)
def main():
 ap=argparse.ArgumentParser();ap.add_argument('--apk',type=pathlib.Path,required=True);ap.add_argument('--tools',type=pathlib.Path,required=True);ap.add_argument('--output',type=pathlib.Path,required=True);ap.add_argument('--keystore',type=pathlib.Path);ap.add_argument('--fetch-tools',action='store_true');args=ap.parse_args()
 apk=args.apk.resolve();tools=args.tools.resolve();out=args.output.resolve();tools.mkdir(parents=True,exist_ok=True);out.mkdir(parents=True,exist_ok=True)
 if sha(apk)!=SOURCE_SHA:raise SystemExit('APK differs from verified source. Reinspect its native API before patching.')
 pins=json.loads((ROOT/'toolchain.json').read_text())
 if args.fetch_tools:
  for name,pin in pins.items():
   dest=tools/name
   if not dest.exists() or sha(dest)!=pin['sha256']:run('curl','-fL','--max-time','120','-o',dest,pin['url'])
   if sha(dest)!=pin['sha256']:raise SystemExit('Tool hash mismatch: '+name)
  if not (tools/'dotnet/dotnet').exists():
   (tools/'dotnet').mkdir(exist_ok=True);run('tar','--no-same-owner','-xzf',tools/'dotnet.tar.gz','-C',tools/'dotnet')
  with zipfile.ZipFile(tools/'cecil.nupkg') as z:z.extract('lib/netstandard2.0/Mono.Cecil.dll',tools/'cecil')
 dotnet=tools/'dotnet/dotnet';sdk=tools/'dotnet/sdk/8.0.100';csc=sdk/'Roslyn/bincore/csc.dll';cecil=tools/'cecil/lib/netstandard2.0/Mono.Cecil.dll'
 for name in ['cecil.nupkg','ecj.jar','apksig.jar']:
  if not (tools/name).exists() or sha(tools/name)!=pins[name]['sha256']:raise SystemExit('Missing or unverified tool '+name+' (use --fetch-tools).')
 managed=out/'managed';managed.mkdir(exist_ok=True)
 with zipfile.ZipFile(apk) as z:
  with zipfile.ZipFile(io.BytesIO(z.read('assets/Mono/Managed.zip'))) as m:m.extractall(managed)
 refs=list((tools/'dotnet/packs/Microsoft.NETCore.App.Ref/8.0.0/ref/net8.0').glob('*.dll'))
 runtime={'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':'8.0.0'}}}
 def compile(name,sources,reference,exe=True):
  rsp=out/(name+'.rsp');rsp.write_text('\n'.join(['-nostdlib+','-target:'+('exe' if exe else 'library'),'-out:'+str(out/(name+'.dll'))]+['-r:'+str(r) for r in reference]+[str(ROOT/s) for s in sources]));run(dotnet,csc,'@'+str(rsp))
  if exe:(out/(name+'.runtimeconfig.json')).write_text(json.dumps(runtime))
 compile('RebellionRules',['RebellionRules.cs','EconomyModel.cs','NativeProgression.cs','NativeEconomyUI.cs','GodWorldRuntime.cs'],list(managed.glob('*.dll')),False)
 shutil.copy(cecil,out/'Mono.Cecil.dll');compile('Patch',['Patch.cs'],refs+[cecil]);run(dotnet,out/'Patch.dll',managed/'Assembly-CSharp.dll',out/'RebellionRules.dll',managed,out/'Assembly-CSharp.patched.dll')
 compile('VerifyIL',['VerifyIL.cs'],refs+[cecil]);run(dotnet,out/'VerifyIL.dll',out/'Assembly-CSharp.patched.dll',managed)
 shutil.copy(managed/'Newtonsoft.Json.dll',out/'Newtonsoft.Json.dll')
 compile('EconomyTests',['EconomyModel.cs','EconomyModelTests.cs'],refs);run(dotnet,out/'EconomyTests.dll')
 compile('RuntimeTests',['RebellionRules.cs','EconomyModel.cs','NativeProgression.cs','NativeEconomyUI.cs','GodWorldRuntime.cs','RuntimeTests.cs'],refs+[out/'Newtonsoft.Json.dll']);run(dotnet,out/'RuntimeTests.dll')
 unsigned=out/'worldbox-economy-native-unsigned.apk';run('python3',ROOT/'repack.py',apk,out/'Assembly-CSharp.patched.dll',out/'RebellionRules.dll',unsigned)
 key=(args.keystore or out/'worldbox-test-signing.p12').resolve()
 if not key.exists():run('keytool','-genkeypair','-alias','worldbox-test','-keyalg','RSA','-keysize','2048','-validity','10000','-keystore',key,'-storepass','android','-keypass','android','-dname','CN=Worldbox Local Test','-storetype','PKCS12')
 run('java','-jar',tools/'ecj.jar','-17','-cp',tools/'apksig.jar','-d',out,ROOT/'SignApk.java')
 signed=out/'worldbox-economy-native-alpha.apk';classpath=str(tools/'apksig.jar')+':'+str(out)
 run('java','-cp',classpath,'SignApk',unsigned,signed,key);run('java','-cp',classpath,'SignApk','verify',signed)
 receipt={'source_sha256':SOURCE_SHA,'output_sha256':sha(signed),'output':str(signed),'helper_sha256':sha(out/'RebellionRules.dll'),'tests':{'economic':19,'native_adapter_standins':27,'hook_stack_graphs':28},'android_runtime_tested':False}
 (out/'build-receipt.json').write_text(json.dumps(receipt,indent=2));print(json.dumps(receipt,indent=2))
if __name__=='__main__':main()
