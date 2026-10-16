using ai.behaviours;using System;using System.Reflection;using System.Runtime.CompilerServices;using System.Collections;
class Smoke {
 static int count;static void Assert(bool b,string n){if(!b)throw new Exception(n);count++;}
 static object Fresh(Type t)=>RuntimeHelpers.GetUninitializedObject(t);
 static void Main(){
  GameProgress.instance=null;
  foreach(var name in new[]{"ActorAsset","ActorTrait","SubspeciesTrait","CultureTrait","LanguageTrait","ClanTrait","KingdomTrait","ReligionTrait","GeneAsset","PhenotypeAsset","EquipmentAsset","ItemModAsset","PlotAsset"}){
   var t=typeof(ActorAsset).Assembly.GetType(name,true);var a=(BaseUnlockableAsset)Fresh(t);a.id="smoke_"+name;a.needs_to_be_explored=true;a.unlocked_with_achievement=true;
   Assert(NativeContentUnlocks.Target(a),name+" target");Assert(a.isAvailable(),name+" available without progress");Assert(a.isUnlocked(),name+" unlocked");Assert(a.isUnlockedByPlayer(),name+" player unlocked");
  }
  Assert(!NativeContentUnlocks.Target(null),"null excluded");Assert(!NativeContentUnlocks.Target((BaseUnlockableAsset)Fresh(typeof(WorldLawAsset))),"world law excluded");
  var open=typeof(NativeContentUnlocks).GetMethod("Open",BindingFlags.NonPublic|BindingFlags.Static);
  var animal=(ActorAsset)Fresh(typeof(ActorAsset));animal.id="animal";open.Invoke(null,new object[]{animal});Assert(animal.can_edit_traits&&animal.can_receive_traits&&animal.can_be_inspected,"animal editing enabled");
  var boat=(ActorAsset)Fresh(typeof(ActorAsset));boat.id="boat";boat.is_boat=true;open.Invoke(null,new object[]{boat});Assert(!boat.can_edit_traits&&!boat.can_receive_traits,"boat compatibility retained");
  var mod=(ItemModAsset)Fresh(typeof(ItemModAsset));mod.id="effect";open.Invoke(null,new object[]{mod});Assert(mod.can_be_given&&mod.can_be_removed&&mod.mod_can_be_given,"equipment effects editable");
  NativeContentUnlocks.EquipmentPools(null);count++;
  Assert(NativeContentUnlocks.ArmyCapacity(null)==0,"null city capacity");
  var city=(City)Fresh(typeof(City));Assert(NativeContentUnlocks.ArmyCapacity(city)==0,"uninitialized city capacity");typeof(City).GetField("status").SetValue(city,new CityStatus());
  var tasks=(CityBehCheckCitizenTasks)Fresh(typeof(CityBehCheckCitizenTasks));
  var possible=typeof(CityBehCheckCitizenTasks).GetMethod("getPossibleWarriors",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
  foreach(var adults in new[]{0,1,16,100,10000,Int32.MaxValue}){
   city.status.population_adults=adults;city.status.warrior_slots=0;city.status.warriors_current=adults;
   Assert(city.getMaxWarriors()==adults,"army displayed capacity "+adults);
   Assert((int)possible.Invoke(tasks,new object[]{city})==adults,"army recruitment capacity "+adults);
   Assert(!(bool)typeof(City).GetMethod("isArmyFull",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).Invoke(city,null),"old slot cap bypassed "+adults);
   Assert(!(bool)typeof(City).GetMethod("isArmyOverLimit",BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.Public).Invoke(city,null),"no slot-based demobilization "+adults);
  }
  city.status.population_adults=-1;Assert(city.getMaxWarriors()==0,"negative transient population clamped");
  Console.WriteLine("PASS: "+count+" checks executing actual patched game types under .NET; this is not Android/Unity startup.");
 }
}
