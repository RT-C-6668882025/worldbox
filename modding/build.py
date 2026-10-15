#!/usr/bin/env python3
"""Pinned, reproducible Linux x64 APK patch build. Does not change client configuration."""
import argparse, hashlib, io, json, pathlib, shutil, subprocess, zipfile, os, fcntl, re, uuid, tempfile
ROOT=pathlib.Path(__file__).resolve().parent
SOURCE_SHA='c39817efa21e539fe61f318dc1ad4050b424c6120ab8ed384d94148f869c4bdc'
def sha(p):
 h=hashlib.sha256()
 with p.open('rb') as f:
  for chunk in iter(lambda:f.read(1048576),b''):h.update(chunk)
 return h.hexdigest()
def run(*cmd):
 result=subprocess.run([str(s) for s in cmd],stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True);print(result.stdout,end="");result.check_returncode();return result.stdout
def main():
 ap=argparse.ArgumentParser();ap.add_argument('--apk',type=pathlib.Path,required=True);ap.add_argument('--tools',type=pathlib.Path,required=True);ap.add_argument('--output',type=pathlib.Path,required=True);ap.add_argument('--keystore',type=pathlib.Path);ap.add_argument('--fetch-tools',action='store_true');args=ap.parse_args()
 apk=args.apk.resolve();tools=args.tools.resolve();out=args.output.resolve();tools.mkdir(parents=True,exist_ok=True);out.mkdir(parents=True,exist_ok=True)
 lock=(out/'build.lock').open('w')
 try:fcntl.flock(lock,fcntl.LOCK_EX|fcntl.LOCK_NB)
 except BlockingIOError:raise SystemExit('Another build owns this output directory; use a different output path.')
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
 for name in ['cecil.nupkg','ecj.jar','apksig.jar','ilverify.nupkg']:
  if not (tools/name).exists() or sha(tools/name)!=pins[name]['sha256']:raise SystemExit('Missing or unverified tool '+name+' (use --fetch-tools).')
 managed=out/'managed';managed.mkdir(exist_ok=True)
 with zipfile.ZipFile(apk) as z:
  with zipfile.ZipFile(io.BytesIO(z.read('assets/Mono/Managed.zip'))) as m:m.extractall(managed)
 refs=list((tools/'dotnet/packs/Microsoft.NETCore.App.Ref/8.0.0/ref/net8.0').glob('*.dll'))
 runtime={'runtimeOptions':{'tfm':'net8.0','framework':{'name':'Microsoft.NETCore.App','version':'8.0.0'}}}
 def compile(name,sources,reference,exe=True):
  rsp=out/(name+'.rsp');rsp.write_text('\n'.join(['-nostdlib+','-target:'+('exe' if exe else 'library'),'-out:'+str(out/(name+'.dll'))]+['-r:'+str(r) for r in reference]+[str(ROOT/s) for s in sources]));run(dotnet,csc,'@'+str(rsp))
  if exe:(out/(name+'.runtimeconfig.json')).write_text(json.dumps(runtime))
 compile('RebellionRules',['RebellionRules.cs','EconomyModel.cs','LifecyclePolicy.cs','NativeLifecycle.cs','NativeUnlocks.cs','BootDiagnostics.cs','NativeOrganizationEditor.cs','IndustrialAppearance.cs','IntegrationRules.cs','NativeSystems.cs','NativeProgression.cs','NativeOrganizations.cs','NativeEconomyUI.cs','NativeEconomyEditors.cs','NativeCurrencyControls.cs','NativeWorldRate.cs','CurrencyInput.cs','CurrencyScope.cs','GodWorldRuntime.cs'],list(managed.glob('*.dll')),False)
 shutil.copy(cecil,out/'Mono.Cecil.dll');compile('PatchNativeImports',['PatchNativeImports.cs'],refs+[cecil]);run(dotnet,out/'PatchNativeImports.dll',managed,out/'native-imports');compile('VerifyNativeImports',['VerifyNativeImports.cs'],refs+[cecil]);native_import_log=run(dotnet,out/'VerifyNativeImports.dll',managed,out/'native-imports');compile('Patch',['Patch.cs'],refs+[cecil]);run(dotnet,out/'Patch.dll',managed/'Assembly-CSharp.dll',out/'RebellionRules.dll',managed,out/'Assembly-CSharp.patched.dll')
 compile('VerifyIL',['VerifyIL.cs'],refs+[cecil]);verify_log=run(dotnet,out/'VerifyIL.dll',out/'Assembly-CSharp.patched.dll',managed)
 with zipfile.ZipFile(tools/'ilverify.nupkg') as z:z.extractall(out/'ilverify')
 from verify_types import verify
 typed_validation=verify(dotnet,out/'ilverify/tools/net8.0/any/ILVerify.dll',managed,out/'Assembly-CSharp.patched.dll',out/'RebellionRules.dll',out)
 shutil.copy(managed/'Newtonsoft.Json.dll',out/'Newtonsoft.Json.dll')
 compile('EconomyTests',['EconomyModel.cs','LifecyclePolicy.cs','EconomyModelTests.cs'],refs);economy_log=run(dotnet,out/'EconomyTests.dll')
 compile('IntegrationTests',['IntegrationRules.cs','IntegrationRulesTests.cs'],refs);integration_log=run(dotnet,out/'IntegrationTests.dll')
 compile('RuntimeTests',['RebellionRules.cs','EconomyModel.cs','LifecyclePolicy.cs','NativeLifecycle.cs','IntegrationRules.cs','NativeProgression.cs','NativeOrganizations.cs','NativeEconomyUI.cs','CurrencyScope.cs','GodWorldRuntime.cs','RuntimeTests.cs','OrganizationTests.cs'],refs+[out/'Newtonsoft.Json.dll']);runtime_log=run(dotnet,out/'RuntimeTests.dll')
 compile('CurrencyPanelTests',['NativeEconomyEditors.cs','NativeCurrencyControls.cs','NativeWorldRate.cs','CurrencyInput.cs','CurrencyPanelTests.cs'],refs);currency_log=run(dotnet,out/'CurrencyPanelTests.dll')
 compile('BootDiagnosticsTests',['BootDiagnostics.cs','BootDiagnosticsTests.cs'],refs);boot_log=run(dotnet,out/'BootDiagnosticsTests.dll')
 compile('UnlockTests',['NativeUnlocks.cs','UnlockTests.cs'],refs);unlock_log=run(dotnet,out/'UnlockTests.dll')
 render_sources=['RebellionRules.cs','EconomyModel.cs','LifecyclePolicy.cs','NativeLifecycle.cs','IntegrationRules.cs','NativeProgression.cs','NativeOrganizations.cs','NativeEconomyUI.cs','CurrencyScope.cs','GodWorldRuntime.cs','RuntimeTests.cs','OrganizationTests.cs','IndustrialAppearance.cs','RenderTests.cs']
 render_rsp=out/'RenderTests.rsp';render_rsp.write_text('\n'.join(['-nostdlib+','-target:exe','-define:RENDER_TESTS','-main:RenderTests','-out:'+str(out/'RenderTests.dll')]+['-r:'+str(r) for r in refs+[out/'Newtonsoft.Json.dll']]+[str(ROOT/s) for s in render_sources]));run(dotnet,csc,'@'+str(render_rsp));(out/'RenderTests.runtimeconfig.json').write_text(json.dumps(runtime));render_log=run(dotnet,out/'RenderTests.dll',out/'sprite-preview')
 unsigned=out/'worldbox-economy-native-unsigned.apk';run('python3',ROOT/'repack.py',apk,out/'Assembly-CSharp.patched.dll',out/'RebellionRules.dll',unsigned,out/'native-imports')
 key=(args.keystore or out/'worldbox-test-signing.p12').resolve()
 if not key.exists():run('keytool','-genkeypair','-alias','worldbox-test','-keyalg','RSA','-keysize','2048','-validity','10000','-keystore',key,'-storepass','android','-keypass','android','-dname','CN=Worldbox Local Test','-storetype','PKCS12')
 run('java','-jar',tools/'ecj.jar','-17','-cp',tools/'apksig.jar','-d',out,ROOT/'SignApk.java')
 signed=out/'worldbox-economy-native-alpha.apk';signing_dir=tempfile.TemporaryDirectory(prefix='worldbox-sign-');temporary=pathlib.Path(signing_dir.name)/'signed.apk';classpath=str(tools/'apksig.jar')+':'+str(out)
 temporary.unlink(missing_ok=True)
 run('java','-cp',classpath,'SignApk',unsigned,temporary,key);run('java','-cp',classpath,'SignApk','verify',temporary)
 with zipfile.ZipFile(temporary) as check:
  if check.testzip() is not None:raise SystemExit('Signed APK has a corrupt ZIP member.')
  with zipfile.ZipFile(io.BytesIO(check.read('assets/Mono/Managed.zip'))) as nested:
   if hashlib.sha256(nested.read('RebellionRules.dll')).hexdigest()!=sha(out/'RebellionRules.dll'):raise SystemExit('Embedded helper differs from the verified build.')
 from runtime_cache import verify_runtime
 from native_support import verify as verify_native_support
 native_support=verify_native_support(temporary,apk,out/'native-imports')
 runtime_id=verify_runtime(temporary,apk,(out/'Assembly-CSharp.patched.dll').read_bytes(),(out/'RebellionRules.dll').read_bytes())
 staged=out/("verified-"+uuid.uuid4().hex+".apk")
 with temporary.open("rb") as source,staged.open("wb") as target:
  shutil.copyfileobj(source,target,1024*1024);target.flush();os.fsync(target.fileno())
 if sha(staged)!=sha(temporary):raise SystemExit("Signed APK transfer hash mismatch.")
 os.replace(staged,signed);signing_dir.cleanup()
 receipt={'core_native_import_repair':{'preserved_method_bodies':int(re.search(r'Preserved (\d+)',native_import_log)[1]),'preserved_native_entrypoint_signatures':int(re.search(r'and (\d+) native',native_import_log)[1]),'embedded_assemblies':{p.name:sha(p) for p in (out/'native-imports').glob('*.dll')}},'android_native_helpers':native_support,'typed_il_validation':typed_validation,'runtime_cache_id':runtime_id,'unity_extension_assembly_registered':True,'source_sha256':SOURCE_SHA,'output_sha256':sha(signed),'output':str(signed),'helper_sha256':sha(out/'RebellionRules.dll'),'tests':{'economic':int(re.search(r'(\d+) economic checks passed',economy_log)[1]),'native_adapter_standins':int(re.search(r'(\d+) native-adapter checks passed',runtime_log)[1]),'integration_policy':int(re.search(r'(\d+) native integration policy checks passed',integration_log)[1]),'hook_stack_graphs':int(re.search(r'Verified (\d+) hooked',verify_log)[1]),'unlock':int(re.search(r'(\d+) unlock checks passed',unlock_log)[1]),'currency_panel':int(re.search(r'(\d+) currency panel checks passed',currency_log)[1]),'boot_diagnostics':int(re.search(r'(\d+) boot diagnostic checks passed',boot_log)[1]),'headless_render':int(re.search(r'(\d+) headless render checks passed',render_log)[1]),'native_money_routing':True,'native_systems_routing':True},'android_runtime_tested':False}
 (out/'build-receipt.json').write_text(json.dumps(receipt,indent=2));print(json.dumps(receipt,indent=2))
if __name__=='__main__':main()

