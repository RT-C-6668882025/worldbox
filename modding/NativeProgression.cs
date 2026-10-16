using System;
// All ability inputs are native stats, including native trait effects. No parallel skill XP.
public static class NativeProgression {
 public static int Ability(Actor actor,int role){if(actor==null||role<0||role>4)return 0;string id=role==1?"diplomacy":role==2?"intelligence":role==4?"warfare":"stewardship";double n=GodWorldRuntime.Attribute(actor,id);return Double.IsNaN(n)||Double.IsInfinity(n)?0:(int)Math.Min(20,Math.Max(0,n)/5);}
 public static void CompletedWork(Actor actor,int role,int amount){if(actor==null||!actor.isAlive()||role<0||role>4||amount<=0)return;GodWorldRuntime.RewardWork(actor,role,amount);}
 public static int CraftSkill(Actor actor,int original){return NativeOrganizations.Scale(original,NativeOrganizations.Bonus(actor,"production"));}
 public static void AfterCraft(Actor actor,bool success){if(success)CompletedWork(actor,0,3);}
 public static void AfterReading(Actor actor,int result){if(result==0)CompletedWork(actor,2,NativeOrganizations.ResearchWork(actor,20));}
 public static void AfterDelivery(Actor actor,int accepted){if(accepted>0)CompletedWork(actor,0,Math.Min(10,accepted));}
 public static bool AllowEquipment(Actor actor,EquipmentAsset equipment,City city){return equipment!=null&&(city==null||NativeLifecycle.ModernEquipment(city,equipment));}
}
