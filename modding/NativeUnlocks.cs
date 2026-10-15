using System;
using System.Reflection;
// Unlock editor availability without granting free equipment or completing achievements.
public static class NativeUnlocks {
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 public static bool Target(BaseUnlockableAsset asset){return asset is ItemAsset||asset is ActorTrait||asset is SubspeciesTrait||asset is ActorAsset;}
 static void Open(BaseUnlockableAsset asset){if(asset==null||asset.isTemplateAsset())return;asset.unlocked_with_achievement=false;asset.needs_to_be_explored=false;asset.achievement_id="";
  foreach(var name in new[]{"can_be_given","can_be_removed"}){var f=asset.GetType().GetField(name,Flags);if(f!=null&&f.FieldType==typeof(bool))f.SetValue(asset,true);}
  var item=asset as ItemAsset;if(item!=null)item.mod_can_be_given=true;
 }
 public static void Apply(){
  GameProgressData progress=null;if(GameProgress.instance!=null)progress=(GameProgressData)typeof(GameProgress).GetField("data",Flags).GetValue(GameProgress.instance);
  if(progress!=null){if(progress.unlocked_actors==null)progress.unlocked_actors=new System.Collections.Generic.HashSet<string>();if(progress.unlocked_traits_subspecies==null)progress.unlocked_traits_subspecies=new System.Collections.Generic.HashSet<string>();if(progress.unlocked_traits_actor==null)progress.unlocked_traits_actor=new System.Collections.Generic.HashSet<string>();if(progress.unlocked_equipment==null)progress.unlocked_equipment=new System.Collections.Generic.HashSet<string>();}
  if(AssetManager.traits!=null)foreach(var asset in AssetManager.traits.list){Open(asset);if(progress!=null&&!asset.isTemplateAsset())progress.unlocked_traits_actor.Add(asset.id);}
  if(AssetManager.subspecies_traits!=null)foreach(var asset in AssetManager.subspecies_traits.list){Open(asset);if(progress!=null&&!asset.isTemplateAsset())progress.unlocked_traits_subspecies.Add(asset.id);}
  if(AssetManager.actor_library!=null)foreach(var asset in AssetManager.actor_library.list){if(asset.isTemplateAsset())continue;Open(asset);if(!asset.is_boat){asset.can_receive_traits=true;asset.can_edit_traits=true;asset.can_be_inspected=true;}if(progress!=null)progress.unlocked_actors.Add(asset.id);}
  if(AssetManager.items!=null)foreach(var asset in AssetManager.items.list){Open(asset);if(progress!=null&&!asset.isTemplateAsset())progress.unlocked_equipment.Add(asset.id);}
  if(AssetManager.items_modifiers!=null)foreach(var asset in AssetManager.items_modifiers.list)Open(asset);
 }
 public static void EquipmentPools(ItemLibrary library){Apply();foreach(var asset in library.pot_weapon_assets_all)if(!library.pot_weapon_assets_unlocked.Contains(asset))library.pot_weapon_assets_unlocked.Add(asset);
  foreach(var pair in library.pot_equipment_by_groups_all){System.Collections.Generic.List<EquipmentAsset> list;if(!library.pot_equipment_by_groups_unlocked.TryGetValue(pair.Key,out list)){list=new System.Collections.Generic.List<EquipmentAsset>();library.pot_equipment_by_groups_unlocked[pair.Key]=list;}foreach(var asset in pair.Value)if(!list.Contains(asset))list.Add(asset);}
 }
}
