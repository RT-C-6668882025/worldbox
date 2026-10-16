using System;using System.Linq;using System.Collections.Generic;using Mono.Cecil;using Mono.Cecil.Cil;
class Patch {
 static ModuleDefinition m;static List<string> log=new List<string>();
 static MethodDefinition M(string t,string n)=>m.Types.Single(x=>x.Name==t).Methods.Single(x=>x.Name==n);
 static void Main(string[] a){
 var r=new DefaultAssemblyResolver();r.AddSearchDirectory(a[2]);
 m=ModuleDefinition.ReadModule(a[0],new ReaderParameters{AssemblyResolver=r});
 var helper=ModuleDefinition.ReadModule(a[1],new ReaderParameters{AssemblyResolver=r});
 MethodReference H(string n)=>m.ImportReference(helper.Types.Single(t=>t.Name=="NativeContentUnlocks").Methods.Single(f=>f.Name==n));
 foreach(var n in new[]{"isUnlocked","isAvailable","isUnlockedByPlayer"}){
  var f=M("BaseUnlockableAsset",n);var il=f.Body.GetILProcessor();var first=f.Body.Instructions[0];
  foreach(var i in new[]{Instruction.Create(OpCodes.Ldarg_0),Instruction.Create(OpCodes.Call,H("Target")),Instruction.Create(OpCodes.Brfalse,first),Instruction.Create(OpCodes.Ldc_I4_1),Instruction.Create(OpCodes.Ret)})il.InsertBefore(first,i);
  log.Add("gate "+f.FullName);
 }
 // Asset mutation happens only once all native libraries have initialized and linked.
 var init=M("AssetManager","initLibs");foreach(var ret in init.Body.Instructions.Where(i=>i.OpCode==OpCodes.Ret).ToArray()){ret.OpCode=OpCodes.Call;ret.Operand=H("AssetsReady");init.Body.GetILProcessor().InsertAfter(ret,Instruction.Create(OpCodes.Ret));}
 log.Add("post-init "+init.FullName);
 var pools=M("ItemLibrary","fillUnlockedPools");foreach(var ret in pools.Body.Instructions.Where(i=>i.OpCode==OpCodes.Ret).ToArray()){
  ret.OpCode=OpCodes.Ldarg_0;var il=pools.Body.GetILProcessor();var call=Instruction.Create(OpCodes.Call,H("EquipmentPools"));il.InsertAfter(ret,call);il.InsertAfter(call,Instruction.Create(OpCodes.Ret));
 }log.Add("pools "+pools.FullName);
 // Only the three original content-edit paths; unrelated premium/UI behavior is preserved.
 foreach(var pair in new[]{new[]{"AugmentationsEditor`7","editorButtonClick"},new[]{"GeneEditor","clickGeneAssetAction"},new[]{"LocusElement","OnDrop"}}){
  var f=M(pair[0],pair[1]);int count=0;
  foreach(var i in f.Body.Instructions)if(i.Operand is MethodReference mr&&mr.DeclaringType.Name=="Config"&&mr.Name=="get_hasPremium"){i.OpCode=OpCodes.Ldc_I4_1;i.Operand=null;count++;}
  if(count!=1)throw new Exception("Unexpected premium gate count "+f.FullName+":"+count);log.Add("editor "+f.FullName);
 }
 // Remove population-ratio/leader caps without infinite sentinel values.
 foreach(var pair in new[]{new[]{"City","getMaxWarriors"},new[]{"CityBehCheckCitizenTasks","getPossibleWarriors"}}){
  var f=M(pair[0],pair[1]);f.Body=new MethodBody(f);var il=f.Body.GetILProcessor();
  il.Append(Instruction.Create(pair[0]=="City"?OpCodes.Ldarg_0:OpCodes.Ldarg_1));
  il.Append(Instruction.Create(OpCodes.Call,H("ArmyCapacity")));il.Append(Instruction.Create(OpCodes.Ret));log.Add("army capacity "+f.FullName);
 }
 foreach(var name in new[]{"isArmyFull","isArmyOverLimit"}){
  var f=M("City",name);f.Body=new MethodBody(f);f.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4_0));f.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));log.Add("army gate "+f.FullName);
 }
 m.Write(a[3]);System.IO.File.WriteAllLines(a[4],log);Console.WriteLine(String.Join("\n",log));
 }
}
