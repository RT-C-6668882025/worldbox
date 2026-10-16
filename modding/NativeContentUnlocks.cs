using System;
using System.Collections.Generic;
// No UI, Update hooks, economy code or saved-game rewriting.
public static class NativeContentUnlocks {
 public static int ArmyCapacity(City city) {
  return city==null || city.status==null ? 0 : Math.Max(0,city.status.population_adults);
 }
 public static bool Target(BaseUnlockableAsset a) {
  return a!=null && (a is ActorAsset || a is BaseAugmentationAsset && !(a is WorldLawAsset));
 }
 static void Open(BaseUnlockableAsset a) {
  if(a==null || String.IsNullOrEmpty(a.id) || a.id.StartsWith("$") || a.id.StartsWith("_"))return;
  a.unlocked_with_achievement=false;a.needs_to_be_explored=false;
  var b=a as BaseAugmentationAsset;
  if(b!=null){b.can_be_given=true;b.can_be_removed=true;}
  var item=a as ItemAsset;if(item!=null)item.mod_can_be_given=true;
  var actor=a as ActorAsset;
  if(actor!=null && !actor.is_boat){actor.can_edit_traits=true;actor.can_receive_traits=true;actor.can_be_inspected=true;}
 }
 public static void AssetsReady() {
  foreach(var a in AssetManager.traits.list)Open(a);
  foreach(var a in AssetManager.subspecies_traits.list)Open(a);
  foreach(var a in AssetManager.culture_traits.list)Open(a);
  foreach(var a in AssetManager.language_traits.list)Open(a);
  foreach(var a in AssetManager.clan_traits.list)Open(a);
  foreach(var a in AssetManager.kingdoms_traits.list)Open(a);
  foreach(var a in AssetManager.religion_traits.list)Open(a);
  foreach(var a in AssetManager.gene_library.list)Open(a);
  foreach(var a in AssetManager.phenotype_library.list)Open(a);
  foreach(var a in AssetManager.actor_library.list)Open(a);
  foreach(var a in AssetManager.items.list)Open(a);
  foreach(var a in AssetManager.items_modifiers.list)Open(a);
  foreach(var a in AssetManager.plots_library.list)Open(a);
  EquipmentPools(AssetManager.items);
 }
 public static void EquipmentPools(ItemLibrary library) {
  if(library==null)return;
  if(library.pot_weapon_assets_all!=null && library.pot_weapon_assets_unlocked!=null)
   foreach(var a in library.pot_weapon_assets_all)if(a!=null && !library.pot_weapon_assets_unlocked.Contains(a))library.pot_weapon_assets_unlocked.Add(a);
  if(library.pot_equipment_by_groups_all!=null && library.pot_equipment_by_groups_unlocked!=null)
   foreach(var pair in library.pot_equipment_by_groups_all){
    if(pair.Key==null || pair.Value==null)continue;
    List<EquipmentAsset> list;
    if(!library.pot_equipment_by_groups_unlocked.TryGetValue(pair.Key,out list)||list==null){list=new List<EquipmentAsset>();library.pot_equipment_by_groups_unlocked[pair.Key]=list;}
    foreach(var a in pair.Value)if(a!=null && !list.Contains(a))list.Add(a);
   }
 }
}
