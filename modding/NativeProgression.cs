using System;
using System.Reflection;
// Completed-action hooks only: production=0, commerce=1, research=2, governance=3, combat=4.
public static class NativeProgression {
 static readonly FieldInfo ActorDataField=typeof(Actor).GetField("data",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
 static readonly MethodInfo AddExperience=typeof(Actor).GetMethod("addExperience",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic,null,new Type[]{typeof(int)},null);
 static BaseSystemData Data(Actor actor) { return actor==null?null:ActorDataField.GetValue(actor) as BaseSystemData; }
 public static int SkillLevel(Actor actor,int role) {
  var data=Data(actor);if(data==null||role<0||role>4)return 0;
  int xp;data.get("god_skill_xp_"+role,out xp,0);return (int)Math.Sqrt(Math.Max(0,xp)/25.0);
 }
 public static void SkillWork(Actor actor,int role,int amount) {
  if(actor==null||!actor.isAlive()||role<0||role>4||amount<=0)return;
  var data=Data(actor);if(data==null)return;int old;string key="god_skill_xp_"+role;
  data.get(key,out old,0);int gain=Math.Min(25,amount);int next=(int)Math.Min(Int32.MaxValue,(long)Math.Max(0,old)+gain);data.set(key,next);GodWorldRuntime.RewardWork(actor,role,gain);
  // Native level and attribute growth is retained; one XP per five real work points.
  int carry;data.get("god_work_level_carry",out carry,0);carry=Math.Max(0,carry)+gain;
  if(carry>=5)AddExperience.Invoke(actor,new object[]{carry/5});data.set("god_work_level_carry",carry%5);
 }
 public static int CraftSkill(Actor actor,int original) { return original+SkillLevel(actor,0)/4; }
 public static void AfterCraft(Actor actor,bool success) { if(success)SkillWork(actor,0,3); }
 public static void AfterReading(Actor actor,int result) { if(result==0)SkillWork(actor,2,3); }
 public static void AfterDelivery(Actor actor,int accepted) { if(accepted>0)SkillWork(actor,0,Math.Min(10,accepted)); }
 public static bool AllowEquipment(Actor actor,EquipmentAsset equipment,City city) {
  if(equipment==null||city==null)return equipment!=null;
  int era=GodWorldRuntime.GetEra(city);string material=equipment.material??"";
  // Confirmed shipped material IDs; retain cost/payment/equipment-value checks downstream.
  if(material=="wood")return era>=1;
  if(material=="stone")return era>=1;
  if(material=="copper"||material=="bronze"||material=="iron"||material=="silver")return era>=3;
  if(material=="steel"||material=="mythril"||material=="adamantine")return era>=4;
  return true; // Unknown native assets remain original rather than inventing compatibility.
 }
}
