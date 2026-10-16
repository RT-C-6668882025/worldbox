using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using UnityEngine;

// Runtime effects use native work, citizens, jobs, wars and save objects.
public static class NativeLifecycle {
 const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 sealed class Scan {public Actor[] actors;public int cursor,year,living,working,adults;public int[] sectors=new int[4];public double coins,gods,pending;public double[] nativeAttributes=new double[5];}
 sealed class NationFunds {public GodWallet wallet=new GodWallet();public int year=Int32.MinValue;}
 static readonly Dictionary<City,Scan> scans=new Dictionary<City,Scan>();
 sealed class TargetCache {public float until;public int target;}
 static readonly Dictionary<City,TargetCache> targets=new Dictionary<City,TargetCache>();
 static readonly FieldInfo MapField=typeof(MapBox).GetField("map_stats",Flags),LawsField=typeof(MapBox).GetField("world_laws",Flags),BuildingAssetField=typeof(Building).GetField("asset",Flags),TotalTasksField=typeof(CitizenJobs).GetField("_total_tasks",Flags);
 static readonly MethodInfo WeaponMethod=typeof(Actor).GetMethod("getWeaponAsset",Flags),RevoltMethod=typeof(City).GetMethod("makeOwnKingdom",Flags),StartWarMethod=typeof(DiplomacyManager).GetMethod("startWar",Flags);
 static object mapIdentity;
 static BaseSystemData Data(Actor a){return (ActorData)a.getData();}
 static double Read(BaseSystemData d,string key){string raw;d.get(key,out raw,"0");double n;return Double.TryParse(raw,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out n)&&!Double.IsNaN(n)&&!Double.IsInfinity(n)?Math.Max(0,n):0;}
 static void Write(BaseSystemData d,string key,double n){d.set(key,Math.Max(0,n).ToString("R",System.Globalization.CultureInfo.InvariantCulture));}
 static int Integer(BaseSystemData d,string key,int fallback=0){int n;d.get(key,out n,fallback);return n;}
 public static bool Law(string id){var f=LawsField;var laws=f==null||World.world==null?null:f.GetValue(World.world) as WorldLaws;return laws==null||laws.dict==null||!laws.dict.ContainsKey(id)||laws.isEnabled(id);}
 static void Reset(){var f=MapField;object identity=World.world==null?null:f.GetValue(World.world);if(!System.Object.ReferenceEquals(identity,mapIdentity)){scans.Clear();targets.Clear();mapIdentity=identity;}}
 public static double PersonalGod(Actor a){return a==null?0:Read(Data(a),"godworld.personal.god");}
 public static double GodCapacity(Actor a){return a==null?0:LifecyclePolicy.HoldingCapacity(NativeProgression.Ability(a,0),NativeProgression.Ability(a,1),NativeProgression.Ability(a,2),NativeProgression.Ability(a,3),NativeProgression.Ability(a,4));}
 public static bool IssuePersonal(Actor a,bool god,double delta){
  if(a==null||!a.isAlive()||Double.IsNaN(delta)||Double.IsInfinity(delta))return false;var d=(ActorData)a.getData();
  if(god){double next=PersonalGod(a)+delta;if(next<0||next>GodCapacity(a))return false;Write(d,"godworld.personal.god",next);return true;}
  if(delta!=Math.Floor(delta)||d.money+delta<0||d.money+delta>Int32.MaxValue)return false;d.money+=(int)delta;return true;
 }
 public static bool ExchangePersonal(Actor a,bool goldToGod,double amount){
  if(a==null||amount<=0||Double.IsNaN(amount)||Double.IsInfinity(amount))return false;var d=(ActorData)a.getData();double rate=GodWorldRuntime.GetRate(),g=PersonalGod(a);
  if(goldToGod){if(amount!=Math.Floor(amount)||d.money<amount||g+amount/rate>GodCapacity(a))return false;d.money-=(int)amount;Write(d,"godworld.personal.god",g+amount/rate);}
  else{double gain=amount*rate;if(g<amount||gain>Int32.MaxValue-d.money)return false;int whole=(int)Math.Floor(gain);if(whole==0)return false;Write(d,"godworld.personal.god",g-whole/rate);d.money+=whole;}return true;
 }
 public static int Quote(int amount,Actor a){if(amount<=0)return 0;var s=a==null?null:GodWorldRuntime.State(a.getCity());return (int)Math.Ceiling(LifecyclePolicy.Bound(amount*(s==null?1:s.inflation),1,1000000));}
 public static bool CanAfford(Actor a,int amount){if(a==null)return false;int n=Quote(amount,a);var d=(ActorData)a.getData();return d.money>=n;}
 public static int LiquidPayment(Actor a,int requested){return Math.Min(Quote(requested,a),Math.Max(0,((ActorData)a.getData()).money));}
 public static void RecordWork(Actor a,int wanted,int paid){
  if(a==null||wanted<=0)return;wanted=(int)Math.Min(wanted,Int32.MaxValue-(long)Math.Max(0,((ActorData)a.getData()).loot));if(wanted<=0)return;var c=a.getCity();if(c==null)return;var s=GodWorldRuntime.State(c);var d=Data(a);s.wageRequested+=wanted;s.wageFunded+=paid;
  if(paid>0){d.set("godworld.employed.year",GodWorldRuntime.Year());int sector=a.citizen_job==null?0:Sector(a.citizen_job.id);d.set("godworld.employer.sector",sector);}
  double owed=Math.Max(0,wanted-paid);if(owed>0){Write(d,"godworld.wage.owed",Math.Min(Int32.MaxValue,Read(d,"godworld.wage.owed")+owed));s.wageArrears+=owed;}
  GodWorldRuntime.Dirty(c);
 }
 static int Sector(string job){if(job==null)return 0;return job.Contains("farm")||job.Contains("gather")?0:job.Contains("mine")||job.Contains("wood")?1:job.Contains("build")||job.Contains("road")?2:3;}
 public static void Delivered(City c,int accepted){if(c==null||accepted<=0)return;var s=GodWorldRuntime.State(c);s.nativeOutput+=accepted;GodWorldRuntime.Dirty(c);}
 public static void StartYear(City city,int year){
  Reset();if(city==null)return;int done=Integer(city.data,"godworld.payroll.scan.year",Int32.MinValue);if(done>=year||scans.ContainsKey(city))return;
  city.data.set("godworld.payroll.pending",year);scans[city]=new Scan{actors=city.units.Where(a=>a!=null&&a.isAlive()).ToArray(),year=year};
 }
 public static bool Scanning(City c){Reset();return scans.ContainsKey(c);}
 public static void Step(City city){
  Reset();if(city!=null&&!scans.ContainsKey(city)){int pending=Integer(city.data,"godworld.payroll.pending",Int32.MinValue);if(pending!=Int32.MinValue)StartYear(city,pending);}
  Scan scan;if(city==null||!scans.TryGetValue(city,out scan))return;var s=GodWorldRuntime.State(city);int stop=Math.Min(scan.actors.Length,scan.cursor+64);
  for(;scan.cursor<stop;scan.cursor++){
   var a=scan.actors[scan.cursor];if(a==null||!a.isAlive()||a.getCity()!=city)continue;var d=(ActorData)a.getData();scan.living++;for(int role=0;role<5;role++)scan.nativeAttributes[role]+=NativeProgression.Ability(a,role);if(!a.isBaby()){scan.adults++;if(Integer(d,"godworld.employed.year",Int32.MinValue)>=scan.year-1){scan.working++;int sector=Integer(d,"godworld.employer.sector");scan.sectors[Math.Max(0,Math.Min(3,sector))]++;}}
   if(Integer(d,"godworld.payroll.year",Int32.MinValue)<scan.year){
    d.set("godworld.payroll.year",scan.year);
    int debt=(int)Math.Min(Int32.MaxValue,Read(d,"godworld.wage.owed"));if(debt>0){int repaid=GodWorldRuntime.FundNativeIncome(a,debt);a.addLoot(repaid);Write(d,"godworld.wage.owed",debt-repaid);s.wageArrears=Math.Max(0,s.wageArrears-repaid);}
    if(a.is_profession_warrior){
     int due=LifecyclePolicy.Wage(s.era,s.inflation);double old=Read(d,"godworld.army.owed"),want=Math.Min(Int32.MaxValue,old+due);
     int pay=(int)Math.Min(Math.Min(Math.Floor(s.military.gold),want),Int32.MaxValue-(long)Math.Max(0,d.money));s.military.gold-=pay;d.money+=pay;
     double owed=want-pay;Write(d,"godworld.army.owed",owed);s.armyArrears=Math.Max(0,s.armyArrears+due-pay);
     int misses=owed>=due&&s.military.gold<due?Integer(d,"godworld.army.misses")+1:0;d.set("godworld.army.misses",misses);
     if(misses>=2){a.changeHappiness("godworld_unpaid",-Math.Min(20,misses*3));if(!a.isKing()&&!a.isCityLeader()&&(d.id+scan.year)%3==0)a.stopBeingWarrior();}
    }
    int pressure=(int)Math.Min(25,s.crisis*.25);a.changeHappiness("godworld_economy",pressure>0?-pressure:1);
    // Fund civic leadership from the maintenance provider account, never mint it.
    if(a==city.leader){int desired=LifecyclePolicy.Wage(s.era,s.inflation);int pay=(int)Math.Min(Math.Min(desired,Math.Floor(s.business.gold)),Int32.MaxValue-(long)Math.Max(0,d.money));s.business.gold-=pay;d.money+=pay;}
   }
   scan.coins+=Math.Max(0,d.money);scan.pending+=Math.Max(0,d.loot);scan.gods+=PersonalGod(a);
  }
  if(scan.cursor<scan.actors.Length)return;
  s.residentMoney=scan.coins;s.pendingIncome=scan.pending;s.circulatingGold=scan.coins+scan.pending;s.circulatingGod=scan.gods;s.employed=scan.working;s.sectorWorkers=scan.sectors;s.unemployment=scan.adults>0?1-scan.working/(double)scan.adults:0;
  if(scan.living>0){s.production=scan.nativeAttributes[0]/scan.living;s.commerce=scan.nativeAttributes[1]/scan.living;s.research=scan.nativeAttributes[2]/scan.living;s.governance=scan.nativeAttributes[3]/scan.living;s.combat=scan.nativeAttributes[4]/scan.living;}
  s.foodCoverage=!Law("world_law_hunger")?1:Math.Min(1,city.getTotalFood()/(double)Math.Max(1,city.getPopulationPeople()));
  city.data.set("godworld.payroll.scan.year",scan.year);city.data.set("godworld.payroll.pending",Int32.MinValue);scans.Remove(city);GodWorldRuntime.Dirty(city);FinishYear(city,scan.year);
 }
 static NationFunds Nation(Kingdom k){string text;k.data.get("godworld.national.funds",out text,"");try{var n=text.Length==0?new NationFunds():JsonConvert.DeserializeObject<NationFunds>(text);return n??new NationFunds();}catch{return new NationFunds();}}
 static void SaveNation(Kingdom k,NationFunds n){k.data.set("godworld.national.funds",JsonConvert.SerializeObject(n));}
 public static double NationalBalance(Kingdom k,bool god){if(k==null)return 0;var n=Nation(k);return god?n.wallet.god:n.wallet.gold;}
 public static bool NationalOperation(Kingdom k,CurrencyScope.Operation op,double amount){
  if(k==null||amount<=0||Double.IsNaN(amount)||Double.IsInfinity(amount))return false;var n=Nation(k);bool ok;
  GodEconomy.SetRate(GodWorldRuntime.GetRate());switch(op){case CurrencyScope.Operation.MintGold:ok=GodEconomy.Issue(n.wallet,false,amount);break;case CurrencyScope.Operation.MintGod:ok=GodEconomy.Issue(n.wallet,true,amount);break;case CurrencyScope.Operation.BurnGold:ok=GodEconomy.Burn(n.wallet,false,amount);break;case CurrencyScope.Operation.BurnGod:ok=GodEconomy.Burn(n.wallet,true,amount);break;case CurrencyScope.Operation.GoldToGod:ok=GodEconomy.Exchange(n.wallet,false,amount);break;default:ok=GodEconomy.Exchange(n.wallet,true,amount);break;}if(ok)SaveNation(k,n);return ok;
 }
 public static bool NationalGrant(Kingdom k,City c,double requested,bool toCity){
  if(k==null||c==null||GodWorldRuntime.Nation(c)!=k||requested<=0||requested!=Math.Floor(requested))return false;var n=Nation(k);
  if(toCity){if(n.wallet.gold<requested||requested>Int32.MaxValue-GodWorldRuntime.GetGold(c))return false;if(!GodWorldRuntime.CreditPublic(c,(int)requested))return false;n.wallet.gold-=requested;}
  else{if(requested>Int32.MaxValue||n.wallet.gold+requested>9e15||!GodWorldRuntime.TryDebitPublic(c,(int)requested))return false;n.wallet.gold+=requested;}
  SaveNation(k,n);return true;
 }
 static void NationalYear(City city,int year){
  var k=GodWorldRuntime.Nation(city);if(k==null||k.capital!=city)return;var n=Nation(k);if(n.year>=year)return;n.year=year;
  foreach(var c in k.cities.ToArray()){
   if(c==null||!c.isAlive())continue;double reserve=c.getPopulationPeople()*LifecyclePolicy.Wage(GodWorldRuntime.GetEra(c),GodWorldRuntime.State(c).inflation)*2.0;
   int tax=(int)Math.Min(1000000,Math.Floor(Math.Max(0,GodWorldRuntime.GetGold(c)-reserve)*.02));if(tax>0&&n.wallet.gold+tax<=9e15&&GodWorldRuntime.TryDebitPublic(c,tax))n.wallet.gold+=tax;
  }
  foreach(var c in k.cities.OrderBy(c=>c==null?0:-GodWorldRuntime.State(c).crisis).ToArray()){
   if(c==null||!c.isAlive())continue;double gap=Math.Max(0,c.getPopulationPeople()*LifecyclePolicy.Wage(GodWorldRuntime.GetEra(c),GodWorldRuntime.State(c).inflation)-GodWorldRuntime.GetGold(c));
   int aid=(int)Math.Min(Math.Min(Math.Floor(n.wallet.gold),Math.Ceiling(gap)),Int32.MaxValue-GodWorldRuntime.GetGold(c));if(aid>0&&GodWorldRuntime.CreditPublic(c,aid))n.wallet.gold-=aid;
  }SaveNation(k,n);
 }
 static void FinishYear(City c,int year){
  var s=GodWorldRuntime.State(c);if(s.crisis>=50&&s.stressYears>=3){if(s.laboratory>0)s.laboratory=0;else if(s.powerPlant>0)s.powerPlant=0;else if(s.factory>0)s.factory=0;else s.steamPlant=0;}Civilization(c);NationalYear(c,year);
  int last=Integer(c.data,"godworld.revolt.year",Int32.MinValue);var k=GodWorldRuntime.Nation(c);
  if(s.crisis<35||s.stressYears<3||k==null||RebellionRules.IsRebel(k)||!Law("world_law_rebellions")||!Law("world_law_diplomacy")||c.getPopulationPeople()<=5||(last!=Int32.MinValue&&year-last<8))return;
  Actor leader=c.leader; if(leader==null||leader.isKing())leader=c.units.FirstOrDefault(a=>a!=null&&a.isAlive()&&!a.isBaby()&&!a.isKing());if(leader==null)return;
  var revolt=(Kingdom)RevoltMethod.Invoke(c,new object[]{leader,true,false});if(revolt==null||revolt==k)return;var war=(War)StartWarMethod.Invoke(World.world.diplomacy,new object[]{k,revolt,WarTypeLibrary.rebellion,true});
  if(war!=null){war.data.set("godworld.rebel.originCity",c.data.id);war.data.set("godworld.rebel.originVersion",2);c.data.set("godworld.revolt.year",year);s.crisis*=.65;MilitaryJobs(c);}
 }
 public static int Loyalty(City c,int original){var s=GodWorldRuntime.State(c);return s==null?original:Math.Max(-1000,original-LifecyclePolicy.CrisisLoyalty(s.crisis,s.stressYears));}
 public static int Target(City c){Reset();if(c==null)return 0;TargetCache cached;if(targets.TryGetValue(c,out cached)&&Time.unscaledTime<cached.until)return cached.target;int value=ComputeTarget(c);targets[c]=new TargetCache{until=Time.unscaledTime+2,target=value};return value;}
 static int ComputeTarget(City c){
  if(c==null||!Law("world_law_civ_army"))return 0;var k=GodWorldRuntime.Nation(c);if(k==null)return 0;var s=GodWorldRuntime.State(c);bool rebel=RebellionRules.IsRebel(k),war=k.getWars(false).Any(w=>!w.hasEnded());
  int eligible=0;foreach(var a in c.units){if(a==null||!a.isAlive()||a.isBaby()||a.isKing()||a.isCityLeader())continue;if(!rebel&&c.culture!=null&&(a.isSexFemale()&&c.culture.hasTrait("conscription_male_only")||a.isSexMale()&&c.culture.hasTrait("conscription_female_only")))continue;eligible++;}
  int target=LifecyclePolicy.Mobilize(c.status.population_adults,eligible,rebel,war,s.warLevel,GodWorldRuntime.GetGold(c),s.military.gold,LifecyclePolicy.Wage(s.era,s.inflation),s.crisis);return s.stressYears<2?Math.Max(target,Math.Min(eligible,c.countWarriors())):target;
 }
 public static void MilitaryJobs(City c){
  if(c==null)return;int target=Target(c);var jobs=c.jobs;var attacker=CitizenJobLibrary.attacker;
  // Allocate the military first, then fit civilian positions in the remaining workforce.
  int free=Math.Max(0,c.status.population_adults-target),left=free;
  foreach(var pair in jobs.jobs.ToArray()){if(pair.Key==attacker)continue;int requested=Math.Max(0,pair.Value);bool commercial=Sector(pair.Key.id)!=0;var state=GodWorldRuntime.State(c);if(commercial&&state.monetized!=0)requested=Math.Min(requested,Math.Max(1,(int)Math.Min(Int32.MaxValue,state.firmCapacity)));int retain=Math.Min(requested,left);if(retain==0)jobs.jobs.Remove(pair.Key);else jobs.jobs[pair.Key]=retain;left-=retain;}
  if(target>0)jobs.jobs[attacker]=target;else jobs.jobs.Remove(attacker);TotalTasksField.SetValue(jobs,(int)Math.Min(Int32.MaxValue,jobs.jobs.Values.Sum(x=>(long)Math.Max(0,x))));c.status.warrior_slots=target;
 }
 public static bool CanRecruit(City c,Actor a){if(c==null||a==null||!a.isAlive()||a.isBaby()||a.isKing()||a.isCityLeader()||a.is_profession_warrior)return false;MilitaryJobs(c);if(c.countWarriors()>=c.status.warrior_slots)return false;bool rebel=RebellionRules.IsRebel(GodWorldRuntime.Nation(c));return rebel||c.culture==null||!(a.isSexFemale()&&c.culture.hasTrait("conscription_male_only")||a.isSexMale()&&c.culture.hasTrait("conscription_female_only"));}
 public static bool CanRemain(City c,Actor a){if(c==null||a==null)return false;int target=Target(c);c.status.warrior_slots=target;bool keep=c.countWarriors()<=target;if(!keep&&!a.isKing()&&!a.isCityLeader())a.stopBeingWarrior();return keep;}
 public static bool CanDeploy(City c){if(c==null||!c.hasArmy())return false;int current=c.army.countUnits();var k=GodWorldRuntime.Nation(c);int target=Target(c);return current>=LifecyclePolicy.DeployThreshold(target,current,k!=null&&RebellionRules.IsRebel(k))&&current>0&&target>0;}
 public static void AfterRecruit(City c){if(c==null)return;var f=typeof(City).GetField("_timer_warrior",Flags);double speed=(RebellionRules.IsRebel(GodWorldRuntime.Nation(c))?8:1+Math.Min(5,GodWorldRuntime.State(c).warLevel*.25))*(1+NativeOrganizations.CityBonus(c,"combat"));f.SetValue(c,Math.Max(.25f,(float)f.GetValue(c)/(float)speed));}
 public static bool WarNeeded(Kingdom k,bool original){if(original||k==null||!Law("world_law_diplomacy")||k.getWars(false).Any(w=>!w.hasEnded()))return original;int done=Integer(k.data,"godworld.lastFiscalWar",Int32.MinValue);if(done!=Int32.MinValue&&GodWorldRuntime.Year()-done<8)return false;return k.cities.Any(c=>c!=null&&GodWorldRuntime.State(c).crisis>=45)&&k.countTotalWarriors()>0;}
 public static void WarCreated(War w){if(w==null)return;var a=w.getMainAttacker();if(a!=null)a.data.set("godworld.lastFiscalWar",GodWorldRuntime.Year());if(w.getAsset().rebellion){var rebel=w.getMainDefender();if(rebel!=null&&rebel.capital!=null){w.data.set("godworld.rebel.originCity",rebel.capital.data.id);w.data.set("godworld.rebel.originVersion",2);}}}

 static int Remove(City c,string id,int n){int before=c.getResourcesAmount(id);c.takeResource(id,n);return Math.Max(0,before-c.getResourcesAmount(id));}
 static bool Materials(City c,int wood,int stone,int metal){return c.getResourcesAmount("wood")>=wood&&c.getResourcesAmount("stone")>=stone&&c.getResourcesAmount("common_metals")>=metal;}
 static bool Invest(City c,int wood,int stone,int metal,int cost){if(!Materials(c,wood,stone,metal)||!GodWorldRuntime.TryDebitPublic(c,cost))return false;Remove(c,"wood",wood);Remove(c,"stone",stone);Remove(c,"common_metals",metal);GodWorldRuntime.CreditBusiness(c,cost);return true;}
 static bool FactoryPay(City c,int batches){return batches<=0||GodWorldRuntime.TryDebitPublic(c,batches*LifecyclePolicy.Wage(GodWorldRuntime.GetEra(c),GodWorldRuntime.State(c).inflation));}
 static void Manufacture(City c,int batches,Action<int> consume,Action<int> produce){
  if(batches<=0)return;var s=GodWorldRuntime.State(c);int fee=LifecyclePolicy.Wage(s.era,s.inflation);int count=(int)Math.Min(batches,Math.Floor((GodWorldRuntime.GetGold(c))/fee));if(count<=0||!FactoryPay(c,count))return;
  consume(count);produce(count);GodWorldRuntime.CreditBusiness(c,count*fee);s.nativeOutput+=count;
  var crew=c.units.Where(a=>a!=null&&a.isAlive()&&!a.isBaby()&&!a.is_profession_warrior&&!a.isKing()&&!a.isCityLeader()).Take(Math.Min(count,Math.Max(1,(int)Math.Min(1000000,s.firmCapacity)))).ToArray();foreach(var actor in crew){int paid=GodWorldRuntime.FundNativeIncome(actor,fee);RecordWork(actor,fee,paid);actor.addLoot(paid);Data(actor).set("godworld.employer.sector",3);}
 }
 public static void Civilization(City c){
  var s=GodWorldRuntime.State(c);int era=GodWorldRuntime.GetEra(c);int wage=LifecyclePolicy.Wage(era,s.inflation);int workers=Math.Max(0,c.status.population_adults-c.countWarriors());int units=Math.Max(0,(int)Math.Min(Math.Floor(s.firmCapacity),workers));
  // Facilities are persistent upgrades attached to actual native workshops/mines.
  bool workshop=c.buildings.Any(b=>b!=null&&b.isAlive()&&!b.isUnderConstruction()&&!b.isState(BuildingState.Ruins)&&(((BuildingAsset)BuildingAssetField.GetValue(b)).type=="type_blacksmith"||((BuildingAsset)BuildingAssetField.GetValue(b)).type=="type_mine"||((BuildingAsset)BuildingAssetField.GetValue(b)).type=="type_windmill"));
  if(!workshop||units==0)return;
  if(era>=4&&s.steamPlant==0&&Invest(c,20,10,10,wage*20))s.steamPlant=1;
  if(era>=5&&s.factory==0&&s.steel>=10&&Invest(c,10,20,20,wage*30)){s.steel-=10;s.factory=1;}
  if(era>=6&&s.powerPlant==0&&s.machinery>=5&&Invest(c,10,20,20,wage*40)){s.machinery-=5;s.powerPlant=1;}
  if(era>=7&&s.laboratory==0&&s.components>=5&&Invest(c,5,10,10,wage*50)){s.components-=5;s.laboratory=1;}
  // Power is a per-year service: unused capacity expires, fuel is consumed.
  s.power=0;
  units=NativeOrganizations.Scale(units,NativeOrganizations.CityBonus(c,"production"));
  int n=Math.Min(units,c.getResourcesAmount("wood")/2);
  if(era>=3)Manufacture(c,n,x=>Remove(c,"wood",x*2),x=>s.coal+=x);
  n=(int)Math.Min(units,Math.Min(s.coal,c.getResourcesAmount("common_metals")/2));
  if(era>=4&&s.steamPlant>0)Manufacture(c,n,x=>{s.coal-=x;Remove(c,"common_metals",x*2);},x=>s.steel+=x);
  n=(int)Math.Min(units,s.steel/2);
  if(era>=5&&s.factory>0)Manufacture(c,n,x=>s.steel-=x*2,x=>s.machinery+=x);
  n=(int)Math.Min(units,s.coal);
  if(era>=6&&s.powerPlant>0)Manufacture(c,n,x=>s.coal-=x,x=>s.power+=x*8);
  n=(int)Math.Min(units,Math.Min(s.steel,Math.Min(s.machinery,s.power/2)));
  if(era>=6&&s.factory>0)Manufacture(c,n,x=>{s.steel-=x;s.machinery-=x;s.power-=x*2;},x=>s.components+=x);
  if(era>=7&&s.laboratory>0&&s.components>0&&s.power>0){double use=Math.Min(units,Math.Min(s.components,s.power));s.components-=use;s.power-=use;s.knowledge+=use*.5*(1+NativeOrganizations.CityBonus(c,"research"));}
  n=(int)Math.Min(units,s.steel);if(era>=5&&s.factory>0)Manufacture(c,n,x=>s.steel-=x,x=>s.ammunition+=x*5);
  // Real investment creates capacity; idle/unfunded firms shrink in the annual model.
  if(s.business.gold>wage*units*4&&Materials(c,2,1,1)){double spend=Math.Min(s.business.gold*.1,wage*Math.Max(1,units));s.business.gold-=spend;s.households.gold+=spend;Remove(c,"wood",2);Remove(c,"stone",1);Remove(c,"common_metals",1);s.firmCapacity+=Math.Max(1,spend/wage*.1);s.investmentPaid=spend;}
  s.logistics=1+Math.Min(4,s.machinery/Math.Max(1,workers))+(s.powerPlant>0&&s.power>0?1:0);IndustrialAppearance.Assign(c,s);GodWorldRuntime.Dirty(c);
 }
 static double Stock(GodEconomyState s,int id){switch(id){case 0:return s.coal;case 1:return s.steel;case 2:return s.machinery;case 3:return s.components;default:return s.ammunition;}}
 static void Stock(GodEconomyState s,int id,double value){switch(id){case 0:s.coal=value;break;case 1:s.steel=value;break;case 2:s.machinery=value;break;case 3:s.components=value;break;default:s.ammunition=value;break;}}
 public static int IndustrialTrade(City from,City to,Actor carrier){
  if(from==null||to==null||from==to||!from.isAlive()||!to.isAlive())return 0;var fk=GodWorldRuntime.Nation(from);var tk=GodWorldRuntime.Nation(to);bool foreign=fk!=tk;if(foreign&&(!Law("world_law_diplomacy")||fk==null||tk==null||fk.isInWarWith(tk)))return 0;
  var seller=GodWorldRuntime.State(from);var buyer=GodWorldRuntime.State(to);int total=0;
  for(int id=0;id<5;id++){double stock=Stock(seller,id),target=Stock(buyer,id);int reserve=Math.Max(2,(int)Math.Min(1000000,seller.firmCapacity));int units=(int)Math.Min(10,Math.Min(Math.Floor(stock-reserve),Math.Max(0,reserve-target)));if(units<=0)continue;
   int price=(int)Math.Ceiling((id+1)*LifecyclePolicy.Bound(seller.inflation,.05,1000000));int fee=1;int n=(int)Math.Min(units,Math.Floor((GodWorldRuntime.GetGold(to))/(price+fee)));if(n<=0||!GodWorldRuntime.TryDebitPublic(to,n*(price+fee)))continue;
   Stock(seller,id,stock-n);Stock(buyer,id,target+n);GodWorldRuntime.CreditBusiness(from,n*(price+fee));total+=n;GodWorldRuntime.Dirty(from);GodWorldRuntime.Dirty(to);
  }return total;
 }
 public static bool EraReady(City c,int next){var s=GodWorldRuntime.State(c);switch(next){case 4:return s.nativeOutput+s.lastOutput>0;case 5:return s.steamPlant>0&&(s.steel>=2||s.factory>0);case 6:return s.factory>0&&(s.machinery>=1||s.powerPlant>0);case 7:return s.powerPlant>0&&(s.components>=1||s.laboratory>0);case 8:return s.laboratory>0&&s.knowledge>=512;default:return true;}}
 public static bool Stimulate(City c,double amount){if(c==null||amount<=0||amount!=Math.Floor(amount)||amount>Int32.MaxValue||!Materials(c,1,1,0)||!GodWorldRuntime.TryDebitPublic(c,(int)amount))return false;var s=GodWorldRuntime.State(c);s.households.gold+=amount;Remove(c,"wood",1);Remove(c,"stone",1);s.firmCapacity+=Math.Log(1+amount)*.2;GodWorldRuntime.Dirty(c);return true;}
 public static void SaveGame(){ScrollWindow.showWindow("save_world_confirm");}
 public static void InitWeapons(ItemLibrary library){
  string[] ids={"godworld_industrial_gun","godworld_electric_gun","godworld_modern_gun"};string[] originals={"shotgun","alien_blaster","alien_blaster"};
  for(int i=0;i<ids.Length;i++){if(library.has(ids[i]))continue;var original=library.get(originals[i]);if(original==null)continue;var asset=library.clone(ids[i],originals[i]);asset.base_stats=(BaseStats)original.base_stats.Clone();asset.unlocked_with_achievement=false;asset.needs_to_be_explored=false;asset.achievement_id="";asset.material="steel";asset.equipment_value=200+i*100;asset.cost_gold=30+i*20;asset.cost_coins_resources=0;asset.cost_resource_id_1="common_metals";asset.cost_resource_1=5+i*3;asset.cost_resource_id_2="wood";asset.cost_resource_2=2;asset.base_stats["damage"]=20+i*15;asset.base_stats["range"]=12+i*6;}
 }
 public static void WeaponPaths(ItemLibrary library){if(library==null)return;string[] ids={"godworld_industrial_gun","godworld_electric_gun","godworld_modern_gun"};string[] originals={"shotgun","alien_blaster","alien_blaster"};for(int i=0;i<ids.Length;i++){var asset=library.get(ids[i]);var original=library.get(originals[i]);if(asset==null||original==null)continue;asset.path_gameplay_sprite=original.path_gameplay_sprite;asset.path_icon=original.path_icon;}}
 public static bool ModernEquipment(City c,EquipmentAsset asset){if(asset==null||!asset.id.StartsWith("godworld_"))return true;var s=GodWorldRuntime.State(c);if(s==null||s.steel<2||s.machinery<1)return false;return asset.id=="godworld_industrial_gun"?s.factory>0:asset.id=="godworld_electric_gun"?s.powerPlant>0&&s.components>=1:s.laboratory>0&&s.components>=2;}
 public static void EquipmentMade(Item item,City c){if(item==null||c==null)return;var a=item.getAsset();if(a==null||!a.id.StartsWith("godworld_"))return;var s=GodWorldRuntime.State(c);s.steel=Math.Max(0,s.steel-2);s.machinery=Math.Max(0,s.machinery-1);if(a.id!="godworld_industrial_gun")s.components=Math.Max(0,s.components-(a.id=="godworld_modern_gun"?2:1));GodWorldRuntime.Dirty(c);}
 static EquipmentAsset Weapon(Actor a){return a==null?null:(EquipmentAsset)WeaponMethod.Invoke(a,null);}
 public static bool AmmoReady(Actor a){var w=Weapon(a);if(w==null||!w.id.StartsWith("godworld_"))return true;var s=GodWorldRuntime.State(a.getCity());return s!=null&&(w.id=="godworld_industrial_gun"?s.ammunition>=1:s.power>=1);}
 public static bool Shot(Actor a,bool success){if(!success)return false;var w=Weapon(a);if(w==null||!w.id.StartsWith("godworld_"))return true;var c=a.getCity();var s=GodWorldRuntime.State(c);if(s!=null){if(w.id=="godworld_industrial_gun")s.ammunition=Math.Max(0,s.ammunition-1);else s.power=Math.Max(0,s.power-1);GodWorldRuntime.Dirty(c);}return true;}
 public static bool RecruitResult(City c,bool ok){if(ok)AfterRecruit(c);return ok;}
 public static void ShowCity(CityWindow w){var c=w.getMetaObject();if(c==null)return;var s=GodWorldRuntime.State(c);NativeEconomyUI.Extra(w,"经济状态",GodWorldRuntime.Describe(c));NativeEconomyUI.Extra(w,"社会压力",s.crisis.ToString("0")+" · 持续 "+s.stressYears+" 年");NativeEconomyUI.Extra(w,"欠薪 / 欠饷",s.wageArrears.ToString("0")+" / "+s.armyArrears.ToString("0"));NativeEconomyUI.Extra(w,"就业与岗位",s.employed+" 人 · 企业容量 "+s.firmCapacity.ToString("0"));NativeEconomyUI.Extra(w,"产业就业","农业 "+s.sectorWorkers[0]+" · 采掘 "+s.sectorWorkers[1]+" · 建设 "+s.sectorWorkers[2]+" · 加工服务 "+s.sectorWorkers[3]);NativeEconomyUI.Extra(w,"军队动员",c.countWarriors()+" / "+Target(c));NativeEconomyUI.Extra(w,"工业库存","燃料 "+s.coal.ToString("0")+" · 钢 "+s.steel.ToString("0")+" · 机械 "+s.machinery.ToString("0"));NativeEconomyUI.Extra(w,"电力与技术","电 "+s.power.ToString("0")+" · 元件 "+s.components.ToString("0")+" · 弹药 "+s.ammunition.ToString("0"));NativeEconomyUI.Extra(w,"设施阶段","蒸汽 "+s.steamPlant+" · 工厂 "+s.factory+" · 电站 "+s.powerPlant+" · 实验室 "+s.laboratory);NativeEconomyUI.Extra(w,"居民持币（最近年结）",s.residentMoney.ToString("0")+" 金币 / "+s.circulatingGod.ToString("0.##")+" GOD");NativeEconomyUI.Extra(w,"待结算劳动收入",s.pendingIncome.ToString("0")+" 金币");}
 public static void ShowKingdom(KingdomWindow w){var k=w.getMetaObject();if(k==null)return;NativeEconomyUI.Extra(w,"国家财政",NationalBalance(k,false).ToString("0")+" 金币 / "+NationalBalance(k,true).ToString("0.##")+" GOD");NativeEconomyUI.Extra(w,"公共财政说明","国库与城市分账 · 城市结余2%入国库 · 按需拨款");}
}
