using System;using System.IO;using System.Linq;using System.Collections.Generic;using Mono.Cecil;
class VerifyNativeImports {
 static IEnumerable<TypeDefinition> Types(IEnumerable<TypeDefinition> types){foreach(var t in types){yield return t;foreach(var n in Types(t.NestedTypes))yield return n;}}
 static string Body(MethodDefinition m){if(!m.HasBody)return "";return string.Join("\n",m.Body.Instructions.Select(i=>i.ToString()))+"\nlocals:"+string.Join(",",m.Body.Variables.Select(v=>v.VariableType.FullName))+"\nexceptions:"+string.Join(",",m.Body.ExceptionHandlers.Select(e=>e.HandlerType+":"+e.TryStart+":"+e.TryEnd+":"+e.HandlerStart+":"+e.HandlerEnd+":"+e.FilterStart+":"+e.CatchType));}
 static void Main(string[] args){int methods=0,imports=0;foreach(var path in Directory.GetFiles(args[1],"*.dll")){
  using(var before=AssemblyDefinition.ReadAssembly(Path.Combine(args[0],Path.GetFileName(path))))using(var after=AssemblyDefinition.ReadAssembly(path)){
   if(before.Name.FullName!=after.Name.FullName)throw new Exception("Assembly identity changed");
   var a=Types(before.MainModule.Types).SelectMany(t=>t.Methods).ToArray();var b=Types(after.MainModule.Types).SelectMany(t=>t.Methods).ToArray();
   if(a.Length!=b.Length)throw new Exception("Method count changed");
   for(int i=0;i<a.Length;i++){if(a[i].FullName!=b[i].FullName||a[i].Attributes!=b[i].Attributes||Body(a[i])!=Body(b[i]))throw new Exception("Managed code changed: "+a[i].FullName);methods++;
    if(a[i].HasPInvokeInfo){var x=a[i].PInvokeInfo;var y=b[i].PInvokeInfo;if(x.EntryPoint!=y.EntryPoint||x.Attributes!=y.Attributes)throw new Exception("Native ABI changed");imports++;}
   }
  }
 }if(methods==0||imports==0)throw new Exception("No repaired core assemblies checked");Console.WriteLine("Preserved "+methods+" core method bodies and "+imports+" native entrypoint signatures");}
}
