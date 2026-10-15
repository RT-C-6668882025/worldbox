using System;using System.IO;using System.Linq;using Mono.Cecil;
class PatchNativeImports {
 static void Main(string[] args){Directory.CreateDirectory(args[1]);var resolver=new DefaultAssemblyResolver();resolver.AddSearchDirectory(args[0]);var parameters=new ReaderParameters{AssemblyResolver=resolver};int changed=0;foreach(var path in Directory.GetFiles(args[0],"*.dll")){
  using(var a=AssemblyDefinition.ReadAssembly(path,parameters)){
   int count=0;foreach(var module in a.MainModule.ModuleReferences){string target=null;switch(module.Name){case "System.Native":case "System.Net.Security.Native":target="libmono-native.so";break;case "MonoPosixHelper":target="libMonoPosixHelper.so";break;case "libmono-btls-shared":target="libmono-btls-shared.so";break;}if(target!=null&&target!=module.Name){module.Name=target;count++;}}
   if(count==0)continue;var output=Path.Combine(args[1],Path.GetFileName(path));a.Write(output);
   using(var check=AssemblyDefinition.ReadAssembly(output)){if(check.MainModule.ModuleReferences.Any(x=>x.Name=="System.Native"||x.Name=="System.Net.Security.Native"||x.Name=="MonoPosixHelper"))throw new Exception("Unresolved native alias: "+path);}
   Console.WriteLine("Native imports rewritten: "+Path.GetFileName(path)+" ("+count+")");changed+=count;
  }
 }if(changed==0)throw new Exception("No native aliases found");Console.WriteLine(changed+" native import tables repaired");}
}
