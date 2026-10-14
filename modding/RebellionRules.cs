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
 public static void CheckDefeat(War war) {
  if (!war.hasEnded() && war.getAsset().rebellion && war.countAttackersPopulation() <= 5)
   war.lostWar(war.getMainAttacker());
 }
 public static bool CaptureCapital(City city, Kingdom attacker) {
  return city.isCapitalCity() && IsRebel(attacker);
 }
 public static void AfterCapture(City city, bool wasCapital, Kingdom kingdom) {
  if (wasCapital && kingdom != null && IsRebel(kingdom)) kingdom.setCapital(city);
 }
}
