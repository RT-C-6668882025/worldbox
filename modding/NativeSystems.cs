using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;

// Adapters for verified native behaviours. No independent simulation loop.
public static class NativeSystems {
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 static readonly FieldInfo LawsField=typeof(MapBox).GetField("world_laws",Flags);
 static readonly FieldInfo BoatTarget=typeof(Actor).GetField("beh_building_target",Flags);
 static readonly Dictionary<Type,FieldInfo> DataFields=new Dictionary<Type,FieldInfo>();
 sealed class Fund {public long gold;public Dictionary<long,long> claims=new Dictionary<long,long>();public int year=Int32.MinValue;}
 static BaseSystemData Data(object o){if(o==null)return null;var type=o.GetType();FieldInfo f;if(!DataFields.TryGetValue(type,out f)){for(var t=type;t!=null&&f==null;t=t.BaseType)f=t.GetField("data",Flags|BindingFlags.DeclaredOnly);DataFields[type]=f;}return f==null?null:f.GetValue(o) as BaseSystemData;}
 static string Text(BaseSystemData d,string key){if(d==null)return "";string v;d.get(key,out v,"");return v;}
 static long Number(BaseSystemData d,string key){if(d==null)return 0;long v;d.get(key,out v,0L);return Math.Max(0,v);}
 static void Increment(BaseSystemData d,string key,long n){if(d==null||n<=0)return;long old=Number(d,key);d.set(key,old>Int64.MaxValue-n?Int64.MaxValue:old+n);}
 static bool Law(string id){var world=World.world;if(world==null||LawsField==null)return true;var laws=LawsField.GetValue(world) as WorldLaws;return laws==null||laws.dict==null||!laws.dict.ContainsKey(id)||laws.isEnabled(id);}
 static bool Diplomacy(){return Law("world_law_diplomacy");}
 static City Capital(Kingdom k){if(k==null)return null;City first=null;foreach(var c in k.cities){if(c==null||!c.isAlive())continue;if(first==null)first=c;if(c.isCapitalCity())return c;}return first;}
 static bool Related(Actor a,Actor b){if(a==null||b==null)return false;if(a.family!=null&&a.family==b.family)return true;var d=(ActorData)a.getData();var other=(ActorData)b.getData();return d.lover==other.id||d.best_friend_id==other.id||other.lover==d.id||other.best_friend_id==d.id;}
 static double Worth(City c){return GodWorldRuntime.GetGold(c);}
 static void Restore(City c,string id,int amount){if(amount<=0)return;if(id=="gold")GodWorldRuntime.CreditPublic(c,amount);else c.addResourcesToRandomStockpile(id,amount);}
 static int Remove(City c,string id,int amount){int before=c.getResourcesAmount(id);c.takeResource(id,amount);return Math.Max(0,before-c.getResourcesAmount(id));}
 static int Add(City c,string id,int amount){int before=c.getResourcesAmount(id);c.addResourcesToRandomStockpile(id,amount);return Math.Max(0,c.getResourcesAmount(id)-before);}
 public static void Earn(Actor actor,int amount){
  if(actor==null||amount<=0)return;var d=(ActorData)actor.getData();int done;d.get("godworld.apprentice",out done,0);
  if(done==0){int familySkill=0;foreach(var parent in actor.getParents())if(parent!=null&&parent!=actor)familySkill=Math.Max(familySkill,NativeProgression.SkillLevel(parent,0));int xp;d.get("god_skill_xp_0",out xp,0);if(xp==0)d.set("god_skill_xp_0",Math.Min(25,familySkill*2));d.set("godworld.apprentice",1);}
  if(actor.citizen_job!=null)d.set("godworld.work.job",actor.citizen_job.id);
  GodWorldRuntime.EarnNativeIncome(actor,IntegrationRules.ConstructionSpeed(amount,NativeProgression.SkillLevel(actor,0)));
 }

 // Local public redistribution pays freight, not a second commodity purchase.
 // Foreign dock trade also pays the seller for the delivered commodity.
 public static int TradeGoods(City seller,City buyer,ResourceAsset asset,int requested,Actor carrier){
  if(seller==null||buyer==null||seller==buyer||asset==null||requested<=0||!seller.isAlive()||!buyer.isAlive())return 0;
  var sk=GodWorldRuntime.Nation(seller);var bk=GodWorldRuntime.Nation(buyer);bool foreign=sk!=bk;
  if(foreign&&(!Diplomacy()||sk==null||bk==null||sk.isInWarWith(bk)))return 0;
  int stock=Math.Max(0,seller.getResourcesAmount(asset.id)),target=Math.Max(0,buyer.getResourcesAmount(asset.id));
  if(asset.food&&Law("world_law_hunger"))requested=Math.Min(requested,Math.Max(0,stock-Math.Max(2,seller.getPopulationPeople())));
  int basis=asset.trade_cost>0?asset.trade_cost:asset.money_cost>0?asset.money_cost:asset.loot_value;
  int price=foreign&&asset.id!="gold"?IntegrationRules.Price(basis,stock,target,GodWorldRuntime.State(seller).inflation,Related(seller.leader,buyer.leader)):0;
  int freight=asset.id=="gold"||seller.countBuildings()<2&&buyer.countBuildings()<2?0:1;
  int unit=price+freight;
  if(unit>0)requested=(int)Math.Min(requested,Math.Min(Int32.MaxValue/(long)unit,Math.Floor(Worth(buyer)/unit)));
  if(carrier!=null&&freight>0)requested=Math.Min(requested,Int32.MaxValue-Math.Max(0,((ActorData)carrier.getData()).loot));
  int delivered=IntegrationRules.MoveGoods(requested,stock,target,Int32.MaxValue,
   n=>Remove(seller,asset.id,n),n=>Add(buyer,asset.id,n),n=>Remove(buyer,asset.id,n),n=>Restore(seller,asset.id,n),
   n=>{int cost=n*unit;if(!GodWorldRuntime.TryDebitPublic(buyer,cost))return false;
    GodWorldRuntime.CreditBusiness(seller,n*price+(carrier==null?n*freight:0));
    if(carrier!=null&&freight>0){carrier.addLoot(n*freight);NativeProgression.SkillWork(carrier,1,1);}return true;});
  if(delivered>0){Increment(seller.data,"godworld.goods.exported",delivered);Increment(buyer.data,"godworld.goods.imported",delivered);}
  return delivered;
 }
 public static void Supply(City from,City to,CityStorageSlot slot){if(slot!=null){int n=(int)Math.Min(Int32.MaxValue,slot.asset.supply_give*Math.Max(1,GodWorldRuntime.State(from).logistics));TradeGoods(from,to,slot.asset,n,null);NativeLifecycle.IndustrialTrade(from,to,null);}}
 public static void BoatTrade(Actor boat){
  if(boat==null||BoatTarget==null)return;var dock=BoatTarget.GetValue(boat) as Building;if(dock==null)return;var home=boat.getHomeBuilding();var from=home==null?boat.getCity():home.getCity();var to=dock.getCity();if(from==null||to==null||from==to)return;
  var d=(ActorData)boat.getData();string key=GodWorldRuntime.Year()+":"+to.data.id;
  if(Text(d,"godworld.lastDockTrade")==key)return;if(NativeLifecycle.IndustrialTrade(from,to,boat)>0){d.set("godworld.lastDockTrade",key);return;}
  foreach(var r in AssetManager.resources.list){
   if(r.id=="gold"||r.trade_cost<=0&&r.money_cost<=0&&r.loot_value<=0||from.getResourcesAmount(r.id)<=Math.Max(2,r.trade_bound))continue;
   int n=Math.Min(Math.Max(1,r.trade_give),Math.Max(0,from.getResourcesAmount(r.id)-Math.Max(2,r.trade_bound)));
   if(TradeGoods(from,to,r,n,boat)>0){d.set("godworld.lastDockTrade",key);return;}
  }
 }

 static int ProcessingCost(ResourceAsset r,City c){long count=0;foreach(var id in r.ingredients??new string[0])count+=Math.Max(0,r.ingredients_amount);return IntegrationRules.ProcessingFee((int)Math.Min(Int32.MaxValue,count),GodWorldRuntime.GetEra(c),GodWorldRuntime.State(c).inflation);}
 public static bool CanProcess(ResourceAsset r,City c){
  if(r==null||c==null)return false;if(r.id=="gold")return true;
  if(!c.hasSpaceForResourceInStockpile(r))return false;
  long gold=0;foreach(var id in r.ingredients??new string[0]){if(id=="gold")gold+=Math.Max(0,r.ingredients_amount);else if(c.getResourcesAmount(id)<Math.Max(0,r.ingredients_amount))return false;}
  long cost=gold+ProcessingCost(r,c);return cost<=Int32.MaxValue&&GodWorldRuntime.EnsurePublicLiquidity(c,(int)cost);
 }
 public static int Produced(City c,string id,int amount){
  var r=AssetManager.resources.get(id);int received=Add(c,id,amount);
  if(received==0){if(r!=null)foreach(var ingredient in r.ingredients??new string[0])Restore(c,ingredient,Math.Max(0,r.ingredients_amount));return 0;}
  if(id!="gold"&&r!=null){int fee=ProcessingCost(r,c);if(!GodWorldRuntime.TryDebitPublic(c,fee)){Remove(c,id,received);foreach(var ingredient in r.ingredients??new string[0])Restore(c,ingredient,Math.Max(0,r.ingredients_amount));return 0;}GodWorldRuntime.CreditBusiness(c,fee);}
  Increment(c.data,"godworld.goods.processed",received);NativeLifecycle.Delivered(c,received);return received;
 }
 public static int Construction(Actor a,int original){int speed=IntegrationRules.ConstructionSpeed(original,NativeProgression.SkillLevel(a,0));var s=a==null?null:GodWorldRuntime.State(a.getCity());return speed<=0||s==null?speed:(int)Math.Min(Int32.MaxValue,Math.Ceiling(speed*(1+Math.Min(2,s.machinery*.01)+(s.power>0?.25:0))));}

 static Fund ReadFund(Alliance a){try{string raw=Text(Data(a),"godworld.alliance.fund");var f=raw.Length==0?new Fund():JsonConvert.DeserializeObject<Fund>(raw);if(f==null||f.gold<0||f.gold>Int32.MaxValue||f.claims==null)return new Fund();return f;}catch{return new Fund();}}
 static void SaveFund(Alliance a,Fund f){Data(a).set("godworld.alliance.fund",JsonConvert.SerializeObject(f));}
 static SaveCustomData WorldReserve(){var world=World.world;if(world==null)return null;var f=typeof(MapBox).GetField("map_stats",Flags);var map=f.GetValue(world) as MapStats;if(map==null)return null;if(map.custom_data==null)map.custom_data=new SaveCustomData();return map.custom_data;}
 public static void UpdateAlliance(Alliance a){
  if(a==null||!Diplomacy())return;var f=ReadFund(a);int year=GodWorldRuntime.Year();if(year<=f.year)return;f.year=year;SaveFund(a,f);
  var members=a.kingdoms_list.Where(k=>k!=null&&a.hasKingdom(k)).ToArray();
  if(members.Length<2)return;
  var reserve=WorldReserve();long held=Number(reserve,"godworld.unclaimedAllianceGold");long recover=Math.Min(held,Int32.MaxValue-f.gold);if(recover>0){reserve.set("godworld.unclaimedAllianceGold",held-recover);f.gold+=recover;SaveFund(a,f);}
  foreach(var k in members){var c=Capital(k);if(c==null)continue;int pay=IntegrationRules.Contribution(Worth(c),c.getPopulationPeople());pay=(int)Math.Min(pay,Int32.MaxValue-f.gold);if(pay<=0||!GodWorldRuntime.TryDebitPublic(c,pay))continue;f.gold+=pay;long old;f.claims.TryGetValue(k.data.id,out old);f.claims[k.data.id]=Math.Min(Int64.MaxValue-pay,old)+pay;SaveFund(a,f);}
  // Aid is redistribution of the existing fund, not newly issued currency.
  foreach(var k in members.OrderBy(k=>{var c=Capital(k);return c==null?Double.MaxValue:Worth(c)/Math.Max(1,c.getPopulationPeople());})){
   var c=Capital(k);if(c==null||f.gold==0)continue;bool war=k.hasEnemies();double gap=Math.Max(0,c.getPopulationPeople()*(war?2:1)-Worth(c));
   int aid=(int)Math.Min(Math.Min(f.gold,Math.Ceiling(gap)),Int32.MaxValue-GodWorldRuntime.GetGold(c));
   if(aid>0&&GodWorldRuntime.CreditPublic(c,aid)){f.gold-=aid;Increment(c.data,"godworld.alliance.aid",aid);SaveFund(a,f);}
  }
 }
 public static void LeaveAlliance(Alliance a,Kingdom k){
  if(a==null||k==null||!a.hasKingdom(k))return;var f=ReadFund(a);long claim;if(!f.claims.TryGetValue(k.data.id,out claim))return;
  long total=0;foreach(var n in f.claims.Values)total=total>Int64.MaxValue-n?Int64.MaxValue:total+n;
  int refund=IntegrationRules.RefundShare(f.gold,claim,total),left=refund;
  foreach(var c in k.cities){if(c==null||!c.isAlive())continue;int n=(int)Math.Min(left,Int32.MaxValue-GodWorldRuntime.GetGold(c));if(n>0&&GodWorldRuntime.CreditPublic(c,n)){f.gold-=n;left-=n;}if(left==0)break;}
  // A full treasury must not lose its refund or leave a claim on other members.
  if(left>0){var reserve=WorldReserve();if(reserve!=null){Increment(reserve,"godworld.allianceRefund."+k.data.id,left);f.gold-=left;}else{SaveFund(a,f);return;}}
  f.claims.Remove(k.data.id);SaveFund(a,f);
 }
 public static void DissolveAlliance(Alliance a){if(a==null)return;foreach(var k in a.kingdoms_list.ToArray())LeaveAlliance(a,k);var f=ReadFund(a);if(f.gold>0){var reserve=WorldReserve();if(reserve!=null){Increment(reserve,"godworld.unclaimedAllianceGold",f.gold);f.gold=0;SaveFund(a,f);}}}
 public static void CityYear(City city){
  if(city==null)return;int year=GodWorldRuntime.Year(),done;city.data.get("godworld.systems.year",out done,Int32.MinValue);if(done>=year)return;city.data.set("godworld.systems.year",year);
  var k=GodWorldRuntime.Nation(city);if(k==null)return;var reserve=WorldReserve();string key="godworld.allianceRefund."+k.data.id;long held=Number(reserve,key);int pay=(int)Math.Min(held,Int32.MaxValue-GodWorldRuntime.GetGold(city));if(pay>0&&GodWorldRuntime.CreditPublic(city,pay))reserve.set(key,held-pay);
 }

 static double CandidateScore(Actor a,Actor preferred,Actor previous){return IntegrationRules.HeirScore(NativeProgression.SkillLevel(a,3),GodWorldRuntime.Attribute(a,"stewardship"),a==preferred,Related(a,previous));}
 public static Actor RoyalHeir(Kingdom k,Actor previous,Actor original){
  if(k==null||original==null||original.clan==null)return original;Actor best=original;double score=CandidateScore(original,original,previous);
  foreach(var a in original.clan.units){if(a==null||a==previous||!original.clan.fitToRule(a,k))continue;double s=CandidateScore(a,original,previous);if(s>score){score=s;best=a;}}
  return best;
 }
 public static Actor LeaderHeir(Kingdom k,Actor original){if(k==null||original==null)return original;Actor best=original;double score=CandidateScore(original,original,null);foreach(var c in k.cities){var a=c.leader;if(a==null||!a.isUnitFitToRule())continue;double s=CandidateScore(a,original,null);if(s>score){score=s;best=a;}}return best;}
 public static void Crowned(Kingdom k,Actor a){if(k==null||a==null||k.king!=a)return;long id=((ActorData)a.getData()).id;if(Number(k.data,"godworld.crown.actor")==id)return;k.data.set("godworld.crown.actor",id);k.data.set("godworld.crown.year",GodWorldRuntime.Year());k.data.set("godworld.crown.governance",NativeProgression.SkillLevel(a,3));}
 public static int Loyalty(City c,int original){var k=GodWorldRuntime.Nation(c);if(k==null||k.king==null)return original;int bonus=Math.Min(10,NativeProgression.SkillLevel(k.king,3))+ (Related(c.leader,k.king)?2:0);return (int)Math.Min(Int32.MaxValue,(long)original+bonus);}

 static void ItemEvent(Item item,string type,string owner,string name){
  if(item==null)return;var data=Data(item);string signature=type+":"+owner;if(Text(data,"godworld.item.last")==signature)return;
  data.set("godworld.item.last",signature);Increment(data,"godworld.item.transfers",1);
  string label=String.IsNullOrEmpty(name)?"未命名":name.Replace('\n',' ');if(label.Length>64)label=label.Substring(0,64);
  string line=GodWorldRuntime.Year()+" · "+type+"："+label,history=Text(data,"godworld.item.history");var lines=(history.Length==0?line:history+"\n"+line).Split('\n');data.set("godworld.item.history",String.Join("\n",lines.Skip(Math.Max(0,lines.Length-8)).ToArray()));
 }
 public static void ItemOwner(Item item,Actor actor){if(actor!=null){var d=(ActorData)actor.getData();ItemEvent(item,"持有人",d.id.ToString(),d.name);}}
 public static void ItemStorage(Item item,City city){if(city!=null)ItemEvent(item,"城市",city.data.id.ToString(),city.data.name);}
 public static void Repaired(Item item){if(item==null)return;Increment(Data(item),"godworld.item.repairs",1);var a=item.getActor();if(a!=null)NativeProgression.SkillWork(a,0,1);}
 public static void Repair(Item item){if(item==null)return;bool needed=item.needRepair();item.fullRepair();if(needed)Repaired(item);}
 public static void Reforged(Item item){if(item==null)return;Increment(Data(item),"godworld.item.reforges",1);var a=item.getActor();if(a!=null)NativeProgression.SkillWork(a,0,2);}
 public static int Wear(int damage,Item item){if(item==null)return damage;return IntegrationRules.Wear(damage,NativeProgression.SkillLevel(item.getActor(),4));}
 public static void GenomeChanged(Subspecies s){if(s!=null)Increment(Data(s),"godworld.genome.revisions",1);}

 static object Selected(StatsWindow window){var m=window.GetType().GetMethod("get_meta_object",Flags);return m==null?null:m.Invoke(window,null);}
 public static void ShowAlliance(AllianceWindow w){var a=Selected(w) as Alliance;if(a==null)return;var f=ReadFund(a);NativeEconomyUI.Extra(w,"共同金币",f.gold.ToString());NativeEconomyUI.Extra(w,"共同财政",Diplomacy()?"按年缴纳 · 贫困/战时援助 · 退出结算":"外交法则已关闭，暂停缴纳及援助");}
 public static void ShowItem(ItemWindow w){var i=Selected(w) as Item;if(i==null)return;var d=Data(i);NativeEconomyUI.Extra(w,"经济履历","转移 "+Number(d,"godworld.item.transfers")+" · 维修 "+Number(d,"godworld.item.repairs")+" · 重铸 "+Number(d,"godworld.item.reforges"));NativeEconomyUI.Extra(w,"流转记录",Text(d,"godworld.item.history"));}
 public static void ShowSubspecies(SubspeciesWindow w){var s=Selected(w) as Subspecies;if(s==null)return;NativeEconomyUI.Extra(w,"人口与生产","第 "+s.getGeneration()+" 代 · 代谢 "+s.getMetabolicRate()+" · "+(s.needs_food&&Law("world_law_hunger")?"原生食物需求生效":"无饥饿需求"));}
 public static void ShowCity(CityWindow w){var c=w.getMetaObject();if(c==null)return;var s=GodWorldRuntime.State(c);NativeEconomyUI.Extra(w,"资金分账","商 "+s.business.gold.ToString("0")+" · 工资 "+s.households.gold.ToString("0")+" · 教 "+s.education.gold.ToString("0")+" · 军 "+s.military.gold.ToString("0")+" · 政 "+s.politics.gold.ToString("0"));NativeEconomyUI.Extra(w,"生产与运输","加工 "+Number(c.data,"godworld.goods.processed")+" · 出 "+Number(c.data,"godworld.goods.exported")+" · 入 "+Number(c.data,"godworld.goods.imported"));}
 public static void ShowKingdom(KingdomWindow w){var k=w.getMetaObject();if(k!=null)NativeEconomyUI.Extra(w,"联盟待退金币",Number(WorldReserve(),"godworld.allianceRefund."+k.data.id).ToString());}
}
