using System;
public static class RebellionRules {
 public static bool IsRebel(Kingdom kingdom) {
  if (kingdom == null) return false;
  foreach (War war in kingdom.getWars(false))
   if (!war.hasEnded() && war.getAsset().rebellion && war.isMainAttacker(kingdom)) return true;
  return false;
 }
 public static int GetManpower(City city, Kingdom kingdom) {
  return IsRebel(kingdom) ? (int)Math.Floor(city.status.population_adults * 0.92) : -1;
 }
 public static void ApplyManpower(City city, Kingdom kingdom) {
  if (city == null || !IsRebel(kingdom)) return;
  city.status.warrior_slots = (int)Math.Floor(city.status.population_adults * 0.92);
 }
 class Origin {public City city;}
 static readonly System.Runtime.CompilerServices.ConditionalWeakTable<War,Origin> origins=new System.Runtime.CompilerServices.ConditionalWeakTable<War,Origin>();
 public static void CheckDefeat(War war) {
  if(war.hasEnded()||!war.getAsset().rebellion)return;
  var attacker=war.getMainAttacker();if(attacker==null)return;Origin origin;
  if(!origins.TryGetValue(war,out origin)){
   long id;war.data.get("godworld.rebel.originCity",out id,0L);City city=null;
   if(id==0)city=attacker.capital;else foreach(var candidate in World.world.cities.list)if(candidate.data.id==id){city=candidate;break;}
   if(city==null){war.lostWar(attacker);return;}war.data.set("godworld.rebel.originCity",city.data.id);origin=new Origin{city=city};origins.Add(war,origin);
  }
  if(!origin.city.isAlive()||origin.city.data.kingdomID!=attacker.data.id||origin.city.getPopulationPeople()<=5)war.lostWar(attacker);
 }
 public static bool CaptureCapital(City city, Kingdom attacker) {
  return city.isCapitalCity() && IsRebel(attacker);
 }
 public static void AfterCapture(City city, bool wasCapital, Kingdom kingdom) {
  if (wasCapital && kingdom != null && IsRebel(kingdom)) kingdom.setCapital(city);
 }
}
