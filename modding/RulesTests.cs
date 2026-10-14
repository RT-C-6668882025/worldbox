using System;
using System.Collections.Generic;
public class Kingdom { public List<War> wars=new List<War>(); public City capital; public IEnumerable<War> getWars(bool unused){return wars;} public void setCapital(City c){capital=c;} }
public class WarTypeAsset {public bool rebellion;}
public class War {public bool ended; public int population; public Kingdom attacker; public WarTypeAsset asset=new WarTypeAsset();public bool hasEnded(){return ended;}public WarTypeAsset getAsset(){return asset;}public bool isMainAttacker(Kingdom k){return k==attacker;}public int countAttackersPopulation(){return population;}public Kingdom getMainAttacker(){return attacker;}public void lostWar(Kingdom k){ended=true;}}
public class CityStatus { public int population_adults,warrior_slots; }
public class City {public CityStatus status=new CityStatus();public bool capital;public bool isCapitalCity(){return capital;}}
class RulesTests {
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);}
 static void Main(){
  var k=new Kingdom();var w=new War{attacker=k,population=6};w.asset.rebellion=true;k.wars.Add(w);var c=new City();c.status.population_adults=100;
  Check(RebellionRules.GetManpower(c,k)==92,"100 adults -> 92 slots");
  RebellionRules.CheckDefeat(w);Check(!w.ended,"6 people continue");w.population=5;RebellionRules.CheckDefeat(w);Check(w.ended,"5 people end");
  Check(RebellionRules.GetManpower(c,k)==-1,"ended rebellion uses original rule");w.ended=false;w.asset.rebellion=false;RebellionRules.CheckDefeat(w);Check(!w.ended,"ordinary war remains unchanged");
  Check(RebellionRules.GetManpower(c,k)==-1,"ordinary army uses original rule");w.asset.rebellion=true;c.capital=true;
  Check(RebellionRules.CaptureCapital(c,k),"rebel captures capital");RebellionRules.AfterCapture(c,true,k);Check(k.capital==c,"captured city becomes capital");
  var other=new Kingdom();Check(!RebellionRules.IsRebel(other),"unrelated kingdom unchanged");
 }
}
