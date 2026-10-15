using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
// Availability is pure; bulk writes wait until every native library has linked.
public static class NativeUnlocks {
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static readonly HashSet<string> Types=new HashSet<string>{"ItemAsset","EquipmentAsset","ItemModAsset","ActorTrait","SubspeciesTrait","ActorAsset","CultureTrait","LanguageTrait","ClanTrait","KingdomTrait","ReligionTrait"};
 static bool ready,busy;public static string LastError="";
 static bool Real(BaseUnlockableAsset a){return a!=null&&!String.IsNullOrEmpty(a.id)&&!a.id.StartsWith("$")&&!a.id.StartsWith("_");}
 public static bool Target(BaseUnlockableAsset asset){if(!Real(asset))return false;for(var t=asset.GetType();t!=null;t=t.BaseType)if(Types.Contains(t.Name))return true;return false;}
 static FieldInfo Field(Type type,string name){for(var t=type;t!=null;t=t.BaseType){var f=t.GetField(name,Flags|BindingFlags.DeclaredOnly);if(f!=null)return f;}return null;}
 static void Failure(Exception error){string message=error.GetType().Name+": "+error.Message;if(LastError!=message){LastError=message;UnityEngine.Debug.LogWarning("GodWorld unlock skipped safely: "+message);}}
 static void Open(BaseUnlockableAsset asset){asset.unlocked_with_achievement=false;asset.needs_to_be_explored=false;asset.achievement_id="";foreach(var name in new[]{"can_be_given","can_be_removed"}){var f=Field(asset.GetType(),name);if(f!=null&&f.FieldType==typeof(bool))f.SetValue(asset,true);}var item=asset as ItemAsset;if(item!=null)item.mod_can_be_given=true;var actor=asset as ActorAsset;if(actor!=null&&!actor.is_boat){actor.can_receive_traits=true;actor.can_edit_traits=true;actor.can_be_inspected=true;}}
 static void Library(string name,string progressName,GameProgressData progress){try{var f=typeof(AssetManager).GetField(name,BindingFlags.Static|BindingFlags.Public);var library=f==null?null:f.GetValue(null);if(library==null)return;var listField=Field(library.GetType(),"list");var list=listField==null?null:listField.GetValue(library) as IEnumerable;if(list==null)return;HashSet<string> unlocked=null;if(progress!=null&&progressName!=null){var p=Field(typeof(GameProgressData),progressName);if(p!=null){unlocked=p.GetValue(progress) as HashSet<string>;if(unlocked==null){unlocked=new HashSet<string>();p.SetValue(progress,unlocked);}}}foreach(var value in list){var asset=value as BaseUnlockableAsset;if(!Real(asset))continue;Open(asset);if(unlocked!=null)unlocked.Add(asset.id);}}catch(Exception e){Failure(e);}}
 public static void AssetsReady(){ready=true;Apply();EquipmentPools(AssetManager.items);}
 public static void Apply(){if(!ready||busy)return;busy=true;try{GameProgressData progress=null;var instance=GameProgress.instance;if(instance!=null){var f=Field(typeof(GameProgress),"data");if(f!=null)progress=f.GetValue(instance) as GameProgressData;}
  foreach(var pair in new[]{new[]{"traits","unlocked_traits_actor"},new[]{"subspecies_traits","unlocked_traits_subspecies"},new[]{"actor_library","unlocked_actors"},new[]{"items","unlocked_equipment"},new[]{"items_modifiers",null},new[]{"culture_traits","unlocked_traits_culture"},new[]{"language_traits","unlocked_traits_language"},new[]{"clan_traits","unlocked_traits_clan"},new[]{"kingdoms_traits","unlocked_traits_kingdom"},new[]{"religion_traits","unlocked_traits_religion"}})Library(pair[0],pair[1],progress);
 }catch(Exception e){Failure(e);}finally{busy=false;}}
 public static void EquipmentPools(ItemLibrary library){if(!ready||library==null)return;try{if(library.pot_weapon_assets_all!=null&&library.pot_weapon_assets_unlocked!=null)foreach(var asset in library.pot_weapon_assets_all)if(Real(asset)&&!library.pot_weapon_assets_unlocked.Contains(asset))library.pot_weapon_assets_unlocked.Add(asset);
  if(library.pot_equipment_by_groups_all!=null&&library.pot_equipment_by_groups_unlocked!=null)foreach(var pair in library.pot_equipment_by_groups_all){if(pair.Key==null||pair.Value==null)continue;List<EquipmentAsset> list;if(!library.pot_equipment_by_groups_unlocked.TryGetValue(pair.Key,out list)||list==null){list=new List<EquipmentAsset>();library.pot_equipment_by_groups_unlocked[pair.Key]=list;}foreach(var asset in pair.Value)if(Real(asset)&&!list.Contains(asset))list.Add(asset);}
 }catch(Exception e){Failure(e);}}
}
