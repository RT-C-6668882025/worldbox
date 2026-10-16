"""Compare ECMA-335 verification against the original game's existing unsafe IL."""
import collections,re,shutil,subprocess,pathlib,json

def verify(dotnet,verifier,managed,patched,helper,out):
 out=pathlib.Path(out);folder=out/'type-verification';folder.mkdir(exist_ok=True)
 native=folder/'Assembly-CSharp.dll';extension=folder/'RebellionRules.dll';shutil.copyfile(patched,native);shutil.copyfile(helper,extension)
 common=[p for p in pathlib.Path(managed).glob('*.dll') if p.name!='Assembly-CSharp.dll']
 def run(name,inputs,main_ref,extra):
  args=[str(dotnet),str(verifier)]+[str(p) for p in inputs]
  for p in common+[main_ref]+extra:args+=['-r',str(p)]
  result=subprocess.run(args,stdout=subprocess.PIPE,stderr=subprocess.STDOUT,text=True)
  (folder/(name+'.log')).write_text(result.stdout)
  if result.returncode not in (0,2):raise RuntimeError('ILVerify did not complete: '+name+' exit '+str(result.returncode))
  return [re.sub(r'\[[^\]]+\.dll : ', '[ASSEMBLY : ',line) for line in result.stdout.splitlines() if line.startswith('[IL]: Error')]
 original=pathlib.Path(managed)/'Assembly-CSharp.dll';baseline=run('original',[original],original,[]);actual=run('patched',[native,extension],native,[extension])
 extra=list((collections.Counter(actual)-collections.Counter(baseline)).elements())
 if extra:raise RuntimeError('New typed IL errors:\n'+'\n'.join(extra))
 print('Typed IL verified: no new errors; original unsafe-IL baseline '+str(len(baseline)))
 return {'no_new_typed_il_errors':True,'original_unsafe_il_baseline':len(baseline)}
