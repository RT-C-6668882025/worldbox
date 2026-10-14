using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
class Patch {
 static void Main(string[] args) {
  var resolver=new DefaultAssemblyResolver(); resolver.AddSearchDirectory(args[2]);
  var a=AssemblyDefinition.ReadAssembly(args[0],new ReaderParameters {AssemblyResolver=resolver});
  var m=a.MainModule;
  var h=AssemblyDefinition.ReadAssembly(args[1]); var helpers=h.MainModule.Types.Single(x=>x.Name=="RebellionRules");
  Func<string,MethodReference> H=n=>m.ImportReference(helpers.Methods.Single(x=>x.Name==n));
  var city=m.Types.Single(x=>x.Name=="City"); var kingdom=city.Fields.Single(x=>x.Name=="kingdom");
  var status=city.Methods.Single(x=>x.Name=="updateCityStatus");
  var il=status.Body.GetILProcessor();var ret=status.Body.Instructions.Last();
  foreach(var x in new[]{Instruction.Create(OpCodes.Ldarg_0),Instruction.Create(OpCodes.Ldarg_0),Instruction.Create(OpCodes.Ldfld,kingdom),Instruction.Create(OpCodes.Call,H("ApplyManpower"))}) il.InsertBefore(ret,x);
  var possible=m.Types.Single(x=>x.Name=="CityBehCheckCitizenTasks").Methods.Single(x=>x.Name=="getPossibleWarriors");
  il=possible.Body.GetILProcessor(); var original=possible.Body.Instructions.First();
  var count=new VariableDefinition(m.TypeSystem.Int32);possible.Body.Variables.Add(count);possible.Body.InitLocals=true;
  foreach(var x in new[]{Instruction.Create(OpCodes.Ldarg_1),Instruction.Create(OpCodes.Ldarg_1),Instruction.Create(OpCodes.Ldfld,kingdom),Instruction.Create(OpCodes.Call,H("GetManpower")),Instruction.Create(OpCodes.Stloc,count),Instruction.Create(OpCodes.Ldloc,count),Instruction.Create(OpCodes.Ldc_I4_0),Instruction.Create(OpCodes.Blt,original),Instruction.Create(OpCodes.Ldloc,count),Instruction.Create(OpCodes.Ret)})il.InsertBefore(original,x);
  var war=m.Types.Single(x=>x.Name=="War").Methods.Single(x=>x.Name=="update");il=war.Body.GetILProcessor();original=war.Body.Instructions.First();il.InsertBefore(original,Instruction.Create(OpCodes.Ldarg_0));il.InsertBefore(original,Instruction.Create(OpCodes.Call,H("CheckDefeat")));
  var capture=city.Methods.Single(x=>x.Name=="finishCapture");il=capture.Body.GetILProcessor();original=capture.Body.Instructions.First();
  var was=new VariableDefinition(m.TypeSystem.Boolean);capture.Body.Variables.Add(was);capture.Body.InitLocals=true;
  foreach(var x in new[]{Instruction.Create(OpCodes.Ldarg_0),Instruction.Create(OpCodes.Ldarg_1),Instruction.Create(OpCodes.Call,H("CaptureCapital")),Instruction.Create(OpCodes.Stloc,was)})il.InsertBefore(original,x);
  var join=capture.Body.Instructions.Single(x=>x.Operand is MethodReference && ((MethodReference)x.Operand).Name=="joinAnotherKingdom");var next=join.Next;
  foreach(var x in new[]{Instruction.Create(OpCodes.Ldarg_0),Instruction.Create(OpCodes.Ldloc,was),Instruction.Create(OpCodes.Ldarg_0),Instruction.Create(OpCodes.Ldfld,kingdom),Instruction.Create(OpCodes.Call,H("AfterCapture"))})il.InsertBefore(next,x);
  foreach(var method in new[]{status,possible,war,capture}) method.Body.MaxStackSize=Math.Max(method.Body.MaxStackSize,8);
  a.Write(args[3]);Console.WriteLine("Patched four methods.");
 }
}
