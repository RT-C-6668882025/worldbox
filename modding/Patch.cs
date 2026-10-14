using System;using System.Linq;using System.Collections.Generic;using Mono.Cecil;using Mono.Cecil.Cil;
class Patch {
 static ModuleDefinition module,helper;static readonly HashSet<MethodDefinition> changed=new HashSet<MethodDefinition>();
 static TypeDefinition T(string name){return module.Types.Single(x=>x.Name==name);}
 static MethodDefinition M(string type,string name){return T(type).Methods.Single(x=>x.Name==name);}
 static MethodReference H(string type,string name){return module.ImportReference(helper.Types.Single(x=>x.Name==type).Methods.Single(x=>x.Name==name));}
 static Instruction Arg(int n){return Instruction.Create(n==0?OpCodes.Ldarg_0:n==1?OpCodes.Ldarg_1:n==2?OpCodes.Ldarg_2:OpCodes.Ldarg_3);}
 static Instruction Call(string type,string name){return Instruction.Create(OpCodes.Call,H(type,name));}
 static void Prefix(MethodDefinition m,params Instruction[] code){var il=m.Body.GetILProcessor();var first=m.Body.Instructions.First();foreach(var i in code)il.InsertBefore(first,i);changed.Add(m);}
 static void After(ILProcessor il,Instruction at,IEnumerable<Instruction> code){foreach(var i in code){il.InsertAfter(at,i);at=i;}}
 // Preserve branch targets: the existing ret instruction becomes the hook's first instruction.
 static void End(MethodDefinition m,Func<Instruction[]> factory){var il=m.Body.GetILProcessor();foreach(var r in m.Body.Instructions.Where(x=>x.OpCode==OpCodes.Ret).ToArray()){r.OpCode=OpCodes.Nop;r.Operand=null;After(il,r,factory().Concat(new[]{Instruction.Create(OpCodes.Ret)}));}changed.Add(m);}
 static void Result(MethodDefinition m,Func<VariableDefinition,Instruction[]> factory){var il=m.Body.GetILProcessor();var local=new VariableDefinition(m.ReturnType);m.Body.Variables.Add(local);m.Body.InitLocals=true;foreach(var r in m.Body.Instructions.Where(x=>x.OpCode==OpCodes.Ret).ToArray()){r.OpCode=OpCodes.Stloc;r.Operand=local;After(il,r,factory(local).Concat(new[]{Instruction.Create(OpCodes.Ret)}));}changed.Add(m);}
 static void Constant(MethodDefinition m,int value){m.Body.Instructions.Clear();m.Body.ExceptionHandlers.Clear();m.Body.Variables.Clear();m.Body.Instructions.Add(Instruction.Create(OpCodes.Ldc_I4,value));m.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));changed.Add(m);}
 static void Expand(MethodDefinition method){foreach(var i in method.Body.Instructions){switch(i.OpCode.Code){case Code.Br_S:i.OpCode=OpCodes.Br;break;case Code.Brfalse_S:i.OpCode=OpCodes.Brfalse;break;case Code.Brtrue_S:i.OpCode=OpCodes.Brtrue;break;case Code.Beq_S:i.OpCode=OpCodes.Beq;break;case Code.Bne_Un_S:i.OpCode=OpCodes.Bne_Un;break;case Code.Bge_S:i.OpCode=OpCodes.Bge;break;case Code.Bge_Un_S:i.OpCode=OpCodes.Bge_Un;break;case Code.Bgt_S:i.OpCode=OpCodes.Bgt;break;case Code.Bgt_Un_S:i.OpCode=OpCodes.Bgt_Un;break;case Code.Ble_S:i.OpCode=OpCodes.Ble;break;case Code.Ble_Un_S:i.OpCode=OpCodes.Ble_Un;break;case Code.Blt_S:i.OpCode=OpCodes.Blt;break;case Code.Blt_Un_S:i.OpCode=OpCodes.Blt_Un;break;case Code.Leave_S:i.OpCode=OpCodes.Leave;break;}}method.Body.MaxStackSize=Math.Max(16,method.Body.MaxStackSize);}
 static void Main(string[] args){var resolver=new DefaultAssemblyResolver();resolver.AddSearchDirectory(args[2]);var a=AssemblyDefinition.ReadAssembly(args[0],new ReaderParameters{AssemblyResolver=resolver});module=a.MainModule;helper=AssemblyDefinition.ReadAssembly(args[1]).MainModule;
  if(module.AssemblyReferences.Any(x=>x.Name=="RebellionRules"))throw new Exception("Input already patched: use pristine source APK.");
  var city=T("City");var kingdom=city.Fields.Single(x=>x.Name=="kingdom");
  End(M("City","updateCityStatus"),()=>new[]{Arg(0),Arg(0),Instruction.Create(OpCodes.Ldfld,kingdom),Call("RebellionRules","ApplyManpower"),Arg(0),Instruction.Create(OpCodes.Ldfld,city.Fields.Single(x=>x.Name=="status")),Instruction.Create(OpCodes.Ldc_I4,Int32.MaxValue),Instruction.Create(OpCodes.Stfld,T("CityStatus").Fields.Single(x=>x.Name=="maximum_items"))});
  var possible=M("CityBehCheckCitizenTasks","getPossibleWarriors");var il=possible.Body.GetILProcessor();var original=possible.Body.Instructions.First();var count=new VariableDefinition(module.TypeSystem.Int32);possible.Body.Variables.Add(count);possible.Body.InitLocals=true;
  Prefix(possible,Arg(1),Arg(1),Instruction.Create(OpCodes.Ldfld,kingdom),Call("RebellionRules","GetManpower"),Instruction.Create(OpCodes.Stloc,count),Instruction.Create(OpCodes.Ldloc,count),Instruction.Create(OpCodes.Ldc_I4_0),Instruction.Create(OpCodes.Blt,original),Instruction.Create(OpCodes.Ldloc,count),Instruction.Create(OpCodes.Ret));
  Result(possible,n=>new[]{Arg(1),Arg(1),Instruction.Create(OpCodes.Ldfld,kingdom),Instruction.Create(OpCodes.Ldloc,n),Call("GodWorldRuntime","WarManpower")});
  Prefix(M("War","update"),Arg(0),Call("RebellionRules","CheckDefeat"));
  var capture=M("City","finishCapture");var was=new VariableDefinition(module.TypeSystem.Boolean);capture.Body.Variables.Add(was);capture.Body.InitLocals=true;
  Prefix(capture,Arg(0),Arg(1),Call("RebellionRules","CaptureCapital"),Instruction.Create(OpCodes.Stloc,was));
  il=capture.Body.GetILProcessor();var join=capture.Body.Instructions.Single(x=>x.Operand is MethodReference&&((MethodReference)x.Operand).Name=="joinAnotherKingdom");After(il,join,new[]{Arg(0),Instruction.Create(OpCodes.Ldloc,was),Arg(0),Instruction.Create(OpCodes.Ldfld,kingdom),Call("RebellionRules","AfterCapture")});changed.Add(capture);
  Prefix(M("City","update"),Arg(0),Arg(1),Call("GodWorldRuntime","BeginCityUpdate"));
  End(M("City","update"),()=>new[]{Arg(0),Arg(1),Call("GodWorldRuntime","TickCity")});
  Result(M("City","getResourcesAmount"),n=>new[]{Arg(0),Arg(1),Instruction.Create(OpCodes.Ldloc,n),Call("GodWorldRuntime","ResourceTotal")});
  Prefix(M("City","hasEnoughResourcesFor"),Arg(0),Arg(1),Call("GodWorldRuntime","EnsureBuildLiquidity"));
  Result(M("City","hasEnoughResourcesFor"),n=>new[]{Arg(0),Arg(1),Instruction.Create(OpCodes.Ldloc,n),Call("GodWorldRuntime","CanFundCost")});
  Prefix(M("City","spendResourcesForBuildingAsset"),Arg(0),Arg(1),Call("GodWorldRuntime","PayConstruction"));
  foreach(var sum in M("City","getResourcesAmount").Body.Instructions.Where(x=>x.OpCode==OpCodes.Add).ToArray()){sum.OpCode=OpCodes.Call;sum.Operand=H("GodWorldRuntime","SaturatingResourceSum");}
  var take=M("City","takeResource");Prefix(take,Arg(0),Arg(1),Arg(2),Call("GodWorldRuntime","DrainReserve"),Instruction.Create(OpCodes.Starg,take.Parameters[1]));
  foreach(var name in new[]{"getLoyalty","getCachedLoyalty"})Result(M("City",name),n=>new[]{Arg(0),Instruction.Create(OpCodes.Ldloc,n),Call("GodWorldRuntime","Loyalty")});
  var leader=M("City","setLeader");Prefix(leader,Arg(0),Arg(1),Call("GodWorldRuntime","ChooseLeader"),Instruction.Create(OpCodes.Starg,leader.Parameters[0]));
  foreach(var pair in new[]{new[]{"CityWindow","ShowCity"},new[]{"KingdomWindow","ShowKingdom"},new[]{"UnitWindow","ShowUnit"},new[]{"FamilyWindow","ShowFamily"}})End(M(pair[0],"showStatsRows"),()=>new[]{Arg(0),Call("NativeEconomyUI",pair[1])});
  var click=M("KeyValueField","<Awake>b__16_2");original=click.Body.Instructions.First();Prefix(click,Arg(0),Call("NativeEconomyUI","TryClick"),Instruction.Create(OpCodes.Brfalse,original),Instruction.Create(OpCodes.Ret));
  Prefix(M("SavedMap","create"),Call("GodWorldRuntime","Flush"));
  var craft=M("ItemCrafting","craftItem");Prefix(craft,Arg(0),Arg(3),Call("NativeProgression","CraftSkill"),Instruction.Create(OpCodes.Starg,craft.Parameters[3]));
  Result(craft,n=>new[]{Arg(0),Instruction.Create(OpCodes.Ldloc,n),Call("NativeProgression","AfterCraft"),Instruction.Create(OpCodes.Ldloc,n)});
  var check=M("ItemCrafting","hasEnoughResourcesToCraft");original=check.Body.Instructions.First();Prefix(check,Arg(0),Arg(1),Arg(2),Call("NativeProgression","AllowEquipment"),Instruction.Create(OpCodes.Brtrue,original),Instruction.Create(OpCodes.Ldc_I4_0),Instruction.Create(OpCodes.Ret));
  var delivery=M("Actor","giveInventoryResourcesToCity");il=delivery.Body.GetILProcessor();foreach(var call in delivery.Body.Instructions.Where(x=>x.Operand is MethodReference&&((MethodReference)x.Operand).Name=="addResourcesToRandomStockpile").ToArray()){il.InsertBefore(call,Arg(0));call.OpCode=OpCodes.Call;call.Operand=H("GodWorldRuntime","Deliver");}changed.Add(delivery);
  End(M("Actor","increaseKills"),()=>new[]{Arg(0),Call("GodWorldRuntime","CombatWork")});
  var salary=M("Actor","addMoney");Prefix(salary,Arg(0),Arg(1),Call("GodWorldRuntime","PayNativeSalary"),Instruction.Create(OpCodes.Starg,salary.Parameters[0]));
  var spend=M("Actor","spendMoney");Prefix(spend,Arg(0),Arg(1),Call("GodWorldRuntime","RecordSpend"),Instruction.Create(OpCodes.Starg,spend.Parameters[0]));
  Prefix(M("Actor","die"),Arg(0),Call("GodWorldRuntime","BeforeDeath"));
  var change=M("CityResources","change");Prefix(change,Arg(0),Arg(1),Arg(2),Call("GodWorldRuntime","SafeResourceDelta"),Instruction.Create(OpCodes.Starg,change.Parameters[1]));
  End(M("ResourceLibrary","post_init"),()=>new[]{Arg(0),Call("GodWorldRuntime","UnlockResources")});
  Constant(M("City","hasReachedWorldLawLimit"),0);Constant(M("Family","isFull"),0);Constant(M("Actor","hasReachedOffspringLimit"),0);Constant(M("Actor","getMaxPossibleLevel"),Int32.MaxValue);Constant(M("City","getLimitOfBuildingsType"),Int32.MaxValue);
  var map=M("MapSizeLibrary","isSizeValid");map.Body.Instructions.Clear();map.Body.ExceptionHandlers.Clear();map.Body.Variables.Clear();map.Body.Instructions.Add(Arg(0));map.Body.Instructions.Add(Call("GodWorldRuntime","MapAllowed"));map.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));changed.Add(map);
  foreach(var method in changed)Expand(method);a.Write(args[3]);Console.WriteLine("Patched "+changed.Count+" native methods.");
 }
}
