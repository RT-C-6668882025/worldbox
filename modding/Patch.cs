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
  Prefix(M("LoadingScreen","Awake"),Arg(0),Call("BootDiagnostics","Begin"));
  End(M("WorldLawsWindow","OnEnable"),()=>new[]{Arg(0),Call("NativeWorldRate","Attach")});
  End(M("LoadingScreen","Update"),()=>new[]{Arg(0),Call("BootDiagnostics","Paint")});
  foreach(var instruction in M("SmoothLoader","doActions").Body.Instructions.Where(i=>i.Operand is MethodReference&&((MethodReference)i.Operand).DeclaringType.Name=="MapLoaderAction"&&((MethodReference)i.Operand).Name=="Invoke").ToArray()){instruction.OpCode=OpCodes.Call;instruction.Operand=H("BootDiagnostics","Run");changed.Add(M("SmoothLoader","doActions"));}
  foreach(var name in new[]{"isUnlocked","isAvailable","isUnlockedByPlayer"}){var gate=M("BaseUnlockableAsset",name);var start=gate.Body.Instructions.First();Prefix(gate,Arg(0),Call("NativeUnlocks","Target"),Instruction.Create(OpCodes.Brfalse,start),Instruction.Create(OpCodes.Ldc_I4_1),Instruction.Create(OpCodes.Ret));}
  // initLibs returns only after all post_init AND linkAssets passes have completed.
  End(M("AssetManager","initLibs"),()=>new[]{Call("NativeUnlocks","AssetsReady")});
  foreach(var name in new[]{"create","loadData"})End(M("GameProgress",name),()=>new[]{Call("NativeUnlocks","Apply")});
  End(M("ItemLibrary","fillUnlockedPools"),()=>new[]{Arg(0),Call("NativeUnlocks","EquipmentPools")});
  End(M("MapBox","updateFinish"),()=>new[]{Call("NativeOrganizations","Tick")});
  foreach(var type in new[]{"CityWindow","KingdomWindow","FamilyWindow","ClanWindow","AllianceWindow","CultureWindow","ReligionWindow","LanguageWindow","SubspeciesWindow"})End(M(type,"showStatsRows"),()=>new[]{Arg(0),Call("NativeOrganizations","Show")});
  Result(M("Kingdom","getTaxRateLocal"),n=>new[]{Arg(0),Instruction.Create(OpCodes.Ldloc,n),Call("NativeOrganizations","TaxLocal")});
  Result(M("Kingdom","getTaxRateTribute"),n=>new[]{Arg(0),Instruction.Create(OpCodes.Ldloc,n),Call("NativeOrganizations","TaxTribute")});
  var city=T("City");var kingdom=city.Fields.Single(x=>x.Name=="kingdom");
  End(M("City","updateCityStatus"),()=>new[]{Arg(0),Arg(0),Instruction.Create(OpCodes.Ldfld,kingdom),Call("RebellionRules","ApplyManpower"),Arg(0),Instruction.Create(OpCodes.Ldfld,city.Fields.Single(x=>x.Name=="status")),Instruction.Create(OpCodes.Ldc_I4,Int32.MaxValue),Instruction.Create(OpCodes.Stfld,T("CityStatus").Fields.Single(x=>x.Name=="maximum_items"))});
  // The native task allocator has a >15-adult gate. Rebuild military jobs after
  // its civilian allocation, so the actual job slots and recruitment agree.
  End(M("CityBehCheckCitizenTasks","execute"),()=>new[]{Arg(1),Call("NativeLifecycle","MilitaryJobs")});
  End(M("City","updateCityStatus"),()=>new[]{Arg(0),Call("NativeLifecycle","MilitaryJobs")});
  var possible=M("CityBehCheckCitizenTasks","getPossibleWarriors");possible.Body.Instructions.Clear();possible.Body.ExceptionHandlers.Clear();possible.Body.Variables.Clear();foreach(var i in new[]{Arg(1),Call("NativeLifecycle","Target"),Instruction.Create(OpCodes.Ret)})possible.Body.Instructions.Add(i);changed.Add(possible);
  var il=possible.Body.GetILProcessor();Instruction original;
  foreach(var pair in new[]{new[]{"checkCanMakeWarrior","CanRecruit"},new[]{"checkIfWarriorStillOk","CanRemain"}}){var m=M("City",pair[0]);m.Body.Instructions.Clear();m.Body.ExceptionHandlers.Clear();m.Body.Variables.Clear();foreach(var i in new[]{Arg(0),Arg(1),Call("NativeLifecycle",pair[1]),Instruction.Create(OpCodes.Ret)})m.Body.Instructions.Add(i);changed.Add(m);}
  var deploy=M("City","isOkToSendArmy");deploy.Body.Instructions.Clear();deploy.Body.ExceptionHandlers.Clear();deploy.Body.Variables.Clear();foreach(var i in new[]{Arg(0),Call("NativeLifecycle","CanDeploy"),Instruction.Create(OpCodes.Ret)})deploy.Body.Instructions.Add(i);changed.Add(deploy);
  Result(M("City","tryToMakeWarrior"),n=>new[]{Arg(0),Instruction.Create(OpCodes.Ldloc,n),Call("NativeLifecycle","RecruitResult")});
  Result(M("DiplomacyHelpers","isWarNeeded"),n=>new[]{Arg(0),Instruction.Create(OpCodes.Ldloc,n),Call("NativeLifecycle","WarNeeded")});
  End(M("War","newWar"),()=>new[]{Arg(0),Call("NativeLifecycle","WarCreated")});
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
  End(M("StatsWindow","OnEnable"),()=>new[]{Arg(0),Call("NativeEconomyEditors","Attach")});
  var click=M("KeyValueField","<Awake>b__16_2");original=click.Body.Instructions.First();Prefix(click,Arg(0),Call("NativeEconomyUI","TryClick"),Instruction.Create(OpCodes.Brfalse,original),Instruction.Create(OpCodes.Ret));
  Prefix(M("SavedMap","create"),Call("GodWorldRuntime","Flush"));
  var craft=M("ItemCrafting","craftItem");Prefix(craft,Arg(0),Arg(3),Call("NativeProgression","CraftSkill"),Instruction.Create(OpCodes.Starg,craft.Parameters[3]));
  Result(craft,n=>new[]{Arg(0),Instruction.Create(OpCodes.Ldloc,n),Call("NativeProgression","AfterCraft"),Instruction.Create(OpCodes.Ldloc,n)});
  Result(M("Building","calculateMainSprite"),n=>new[]{Arg(0),Instruction.Create(OpCodes.Ldloc,n),Call("IndustrialAppearance","SpriteFor")});
  End(M("ItemLibrary","init"),()=>new[]{Arg(0),Call("NativeLifecycle","InitWeapons")});
  End(M("ItemLibrary","post_init"),()=>new[]{Arg(0),Call("NativeLifecycle","WeaponPaths")});
  il=craft.Body.GetILProcessor();foreach(var c in craft.Body.Instructions.Where(x=>x.Operand is MethodReference&&((MethodReference)x.Operand).Name=="generateItem").ToArray())After(il,c,new[]{Instruction.Create(OpCodes.Dup),Instruction.Create(OpCodes.Ldarg,craft.Parameters[4]),Call("NativeLifecycle","EquipmentMade")});changed.Add(craft);
  var shot=M("Actor","tryToAttack");original=shot.Body.Instructions.First();Prefix(shot,Arg(0),Call("NativeLifecycle","AmmoReady"),Instruction.Create(OpCodes.Brtrue,original),Instruction.Create(OpCodes.Ldc_I4_0),Instruction.Create(OpCodes.Ret));Result(shot,n=>new[]{Arg(0),Instruction.Create(OpCodes.Ldloc,n),Call("NativeLifecycle","Shot")});
  var check=M("ItemCrafting","hasEnoughResourcesToCraft");original=check.Body.Instructions.First();Prefix(check,Arg(0),Arg(1),Arg(2),Call("NativeProgression","AllowEquipment"),Instruction.Create(OpCodes.Brtrue,original),Instruction.Create(OpCodes.Ldc_I4_0),Instruction.Create(OpCodes.Ret));
  var delivery=M("Actor","giveInventoryResourcesToCity");il=delivery.Body.GetILProcessor();foreach(var call in delivery.Body.Instructions.Where(x=>x.Operand is MethodReference&&((MethodReference)x.Operand).Name=="addResourcesToRandomStockpile").ToArray()){il.InsertBefore(call,Arg(0));call.OpCode=OpCodes.Call;call.Operand=H("GodWorldRuntime","Deliver");}changed.Add(delivery);
  End(M("Actor","increaseKills"),()=>new[]{Arg(0),Call("GodWorldRuntime","CombatWork")});
  // Fund production only; settlement, tax transfers, gifts and stolen money
  // retain the native addMoney/addLoot transfer path without another charge.
  foreach(var pair in new[]{new[]{"BehClaimZoneForCityActorBorder","tryClaimZone"},new[]{"BehGenerateLootFromHouse","execute"},new[]{"BehBuildTarget","execute"},new[]{"BehCityActorCreateRoad","execute"},new[]{"BehCityActorFertilizeCrop","execute"},new[]{"BehGetResourcesFromMine","execute"},new[]{"BehMakeFarm","execute"},new[]{"BehPlantCrops","execute"},new[]{"BehRemoveRuins","execute"},new[]{"BehThrowResources","execute"}}){
   var earning=M(pair[0],pair[1]);var calls=earning.Body.Instructions.Where(x=>x.Operand is MethodReference&&((MethodReference)x.Operand).DeclaringType.Name=="Actor"&&((MethodReference)x.Operand).Name=="addLoot").ToArray();if(calls.Length==0)throw new Exception("Missing native income event "+earning.FullName);
   foreach(var c in calls){c.OpCode=OpCodes.Call;c.Operand=H("NativeSystems","Earn");}changed.Add(earning);
  }
  foreach(var pair in new[]{new[]{"ItemCrafting","craftItem","SpendCommerce"},new[]{"BehTryToEatCityFood","eatFood","SpendCommerce"},new[]{"BehRepairEquipment","execute","SpendCommerce"},new[]{"BehLaunchFireworks","execute","SpendCommerce"},new[]{"PlotManager","newPlot","SpendPolitics"}}){
   var purchase=M(pair[0],pair[1]);var calls=purchase.Body.Instructions.Where(x=>x.Operand is MethodReference&&((MethodReference)x.Operand).DeclaringType.Name=="Actor"&&((MethodReference)x.Operand).Name=="spendMoney").ToArray();if(calls.Length==0)throw new Exception("Missing native expense "+purchase.FullName);
   foreach(var c in calls){c.OpCode=OpCodes.Call;c.Operand=H("GodWorldRuntime",pair[2]);}changed.Add(purchase);
  }
  foreach(var pair in new[]{new[]{"ItemCrafting","hasEnoughResourcesToCraft"},new[]{"BehTryToEatCityFood","execute"},new[]{"BehRepairEquipment","execute"},new[]{"BehLaunchFireworks","execute"},new[]{"PlotManager","newPlot"}}){var m=M(pair[0],pair[1]);foreach(var c in m.Body.Instructions.Where(x=>x.Operand is MethodReference&&((MethodReference)x.Operand).DeclaringType.Name=="Actor"&&((MethodReference)x.Operand).Name=="hasEnoughMoney").ToArray()){c.OpCode=OpCodes.Call;c.Operand=H("NativeLifecycle","CanAfford");}changed.Add(m);}
  End(M("CityWindow","showStatsRows"),()=>new[]{Arg(0),Call("NativeLifecycle","ShowCity")});
  End(M("KingdomWindow","showStatsRows"),()=>new[]{Arg(0),Call("NativeLifecycle","ShowKingdom")});
  var gift=M("BehFinishTalk","makeGift");il=gift.Body.GetILProcessor();var giftAmount=gift.Body.Instructions.Single(x=>x.Operand is MethodReference&&((MethodReference)x.Operand).Name=="getMoneyForGift");After(il,giftAmount,new[]{Arg(1),Arg(2),Call("GodWorldRuntime","PrepareGift")});changed.Add(gift);
  // Preserve native bounds while removing the old 99,999-personal-coin cap.
  foreach(var name in new[]{"addMoney","addLoot"}){var credit=M("Actor",name);foreach(var i in credit.Body.Instructions){if(i.OpCode==OpCodes.Add){i.OpCode=OpCodes.Call;i.Operand=H("GodWorldRuntime","SaturatingResourceSum");}else if(i.OpCode==OpCodes.Ldc_I4&&(int)i.Operand==99999)i.Operand=Int32.MaxValue;}changed.Add(credit);}
  foreach(var type in new[]{"MetaObject`1","Alliance"}){var sum=M(type,"countTotalMoney");foreach(var i in sum.Body.Instructions.Where(i=>i.OpCode==OpCodes.Add).ToArray()){i.OpCode=OpCodes.Call;i.Operand=H("GodWorldRuntime","SaturatingResourceSum");}changed.Add(sum);}
  Result(M("BehFinishReading","execute"),n=>new[]{Arg(1),Instruction.Create(OpCodes.Ldloc,n),Call("NativeProgression","AfterReading"),Instruction.Create(OpCodes.Ldloc,n)});
  Result(M("Actor","getConstructionSpeed"),n=>new[]{Arg(0),Instruction.Create(OpCodes.Ldloc,n),Call("NativeSystems","Construction")});
  End(M("City","updateCityStatus"),()=>new[]{Arg(0),Call("NativeSystems","CityYear")});
  var produce=M("CityBehProduceResources","tryToProduce");original=produce.Body.Instructions.First();Prefix(produce,Arg(1),Arg(2),Call("NativeSystems","CanProcess"),Instruction.Create(OpCodes.Brtrue,original),Instruction.Create(OpCodes.Ldc_I4_0),Instruction.Create(OpCodes.Ret));
  foreach(var c in produce.Body.Instructions.Where(x=>x.Operand is MethodReference&&((MethodReference)x.Operand).Name=="addResourcesToRandomStockpile").ToArray()){c.OpCode=OpCodes.Call;c.Operand=H("NativeSystems","Produced");}changed.Add(produce);
  var supply=M("CityBehSupplyKingdomCities","shareResource");supply.Body.Instructions.Clear();supply.Body.ExceptionHandlers.Clear();supply.Body.Variables.Clear();foreach(var c in new[]{Arg(1),Arg(2),Arg(3),Call("NativeSystems","Supply"),Instruction.Create(OpCodes.Ret)})supply.Body.Instructions.Add(c);changed.Add(supply);
  var trade=M("BehBoatMakeTrade","execute");trade.Body.Instructions.Clear();trade.Body.ExceptionHandlers.Clear();trade.Body.Variables.Clear();foreach(var c in new[]{Arg(1),Call("NativeSystems","BoatTrade"),Instruction.Create(OpCodes.Ldc_I4_0),Instruction.Create(OpCodes.Ret)})trade.Body.Instructions.Add(c);changed.Add(trade);
  Result(M("SuccessionTool","getKingFromRoyalClan"),n=>new[]{Arg(0),Arg(1),Instruction.Create(OpCodes.Ldloc,n),Call("NativeSystems","RoyalHeir")});
  Result(M("SuccessionTool","getKingFromLeaders"),n=>new[]{Arg(0),Instruction.Create(OpCodes.Ldloc,n),Call("NativeSystems","LeaderHeir")});
  End(M("Kingdom","setKing"),()=>new[]{Arg(0),Arg(1),Call("NativeSystems","Crowned")});
  foreach(var name in new[]{"getLoyalty","getCachedLoyalty"})Result(M("City",name),n=>new[]{Arg(0),Instruction.Create(OpCodes.Ldloc,n),Call("NativeSystems","Loyalty")});
  End(M("Alliance","update"),()=>new[]{Arg(0),Call("NativeSystems","UpdateAlliance")});
  Prefix(M("Alliance","leave"),Arg(0),Arg(1),Call("NativeSystems","LeaveAlliance"));
  Prefix(M("Alliance","dissolve"),Arg(0),Call("NativeSystems","DissolveAlliance"));
  foreach(var pair in new[]{new[]{"setUnitHasIt","ItemOwner"},new[]{"setInCityStorage","ItemStorage"}})End(M("Item",pair[0]),()=>new[]{Arg(0),Arg(1),Call("NativeSystems",pair[1])});
  End(M("Item","reforge"),()=>new[]{Arg(0),Call("NativeSystems","Reforged")});
  var wear=M("Item","getDamaged");Prefix(wear,Arg(1),Arg(0),Call("NativeSystems","Wear"),Instruction.Create(OpCodes.Starg,wear.Parameters[0]));
  var repair=M("BehRepairEquipment","execute");foreach(var c in repair.Body.Instructions.Where(x=>x.Operand is MethodReference&&((MethodReference)x.Operand).Name=="fullRepair").ToArray()){c.OpCode=OpCodes.Call;c.Operand=H("NativeSystems","Repair");}changed.Add(repair);
  End(M("Subspecies","genesChangedEvent"),()=>new[]{Arg(0),Call("NativeSystems","GenomeChanged")});
  foreach(var pair in new[]{new[]{"CityWindow","ShowCity"},new[]{"KingdomWindow","ShowKingdom"},new[]{"AllianceWindow","ShowAlliance"},new[]{"ItemWindow","ShowItem"},new[]{"SubspeciesWindow","ShowSubspecies"}})End(M(pair[0],"showStatsRows"),()=>new[]{Arg(0),Call("NativeSystems",pair[1])});
  Prefix(M("Actor","die"),Arg(0),Call("GodWorldRuntime","BeforeDeath"));
  var change=M("CityResources","change");Prefix(change,Arg(0),Arg(1),Arg(2),Call("GodWorldRuntime","SafeResourceDelta"),Instruction.Create(OpCodes.Starg,change.Parameters[1]));
  End(M("ResourceLibrary","post_init"),()=>new[]{Arg(0),Call("GodWorldRuntime","UnlockResources")});
  Constant(M("City","hasReachedWorldLawLimit"),0);Constant(M("Family","isFull"),0);Constant(M("Actor","hasReachedOffspringLimit"),0);Constant(M("Actor","getMaxPossibleLevel"),Int32.MaxValue);Constant(M("City","getLimitOfBuildingsType"),Int32.MaxValue);
  var map=M("MapSizeLibrary","isSizeValid");map.Body.Instructions.Clear();map.Body.ExceptionHandlers.Clear();map.Body.Variables.Clear();map.Body.Instructions.Add(Arg(0));map.Body.Instructions.Add(Call("GodWorldRuntime","MapAllowed"));map.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));changed.Add(map);
  foreach(var method in changed)Expand(method);a.Write(args[3]);Console.WriteLine("Patched "+changed.Count+" native methods.");
 }
}

