using System;
using System.Collections.Generic;
// Preflight all cities before mutating any wallet. No partial nationwide burn/exchange.
public static class CurrencyScope {
 public enum Operation {MintGold,BurnGold,MintGod,BurnGod,GoldToGod,GodToGold}
 public static List<City> Cities(City city,Kingdom kingdom){
  var result=new List<City>();var seen=new HashSet<long>();
  if(kingdom!=null){foreach(var c in kingdom.cities)if(c!=null&&c.isAlive()&&seen.Add(c.data.id))result.Add(c);}
  else if(city!=null&&city.isAlive())result.Add(city);
  result.Sort((a,b)=>a.data.id.CompareTo(b.data.id));return result;
 }
 public static double Total(List<City> cities,bool god){double n=0;foreach(var c in cities)n+=god?GodWorldRuntime.GetGod(c):GodWorldRuntime.GetGold(c);return n;}
 public static bool Apply(List<City> cities,Operation op,double amount){
  if(cities.Count==0||Double.IsNaN(amount)||Double.IsInfinity(amount)||amount<=0||amount!=Math.Floor(amount))return false;
  bool mint=op==Operation.MintGold||op==Operation.MintGod;
  bool sourceGod=op==Operation.BurnGod||op==Operation.GodToGold;
  var weights=new double[cities.Count];double total=0;
  for(int i=0;i<cities.Count;i++){weights[i]=mint?Math.Max(1,cities[i].getPopulationPeople()):sourceGod?GodWorldRuntime.GetGod(cities[i]):GodWorldRuntime.GetGold(cities[i]);total+=weights[i];}
  if(total<=0||(!mint&&total<amount))return false;
  // Gold inputs/issuance remain integral. GOD also uses whole-unit UI amounts.
  // Largest remainder allocation conserves the user's total exactly.
  var shares=new double[cities.Count];var order=new List<int>();double assigned=0;
  for(int i=0;i<cities.Count;i++){shares[i]=Math.Floor(amount*weights[i]/total);assigned+=shares[i];order.Add(i);}
  order.Sort((a,b)=>{double fa=amount*weights[a]/total-shares[a],fb=amount*weights[b]/total-shares[b];int cmp=fb.CompareTo(fa);return cmp!=0?cmp:a.CompareTo(b);});
  double remaining=amount-assigned;
  foreach(int i in order)if(remaining>=1&&(mint||shares[i]+1<=weights[i])){shares[i]++;remaining--;}
  // Fractional GOD balances may need fractional shares when no city has a whole unit.
  if(!mint&&sourceGod){for(int i=0;i<cities.Count&&remaining>0;i++){double extra=Math.Min(remaining,Math.Max(0,weights[i]-shares[i]));shares[i]+=extra;remaining-=extra;}}
  if(remaining>1e-7)return false;
  double rate=GodWorldRuntime.GetRate();
  for(int i=0;i<cities.Count;i++){
   var c=cities[i];double gold=GodWorldRuntime.GetGold(c),god=GodWorldRuntime.GetGod(c),share=shares[i];
   if(!mint&&share>(sourceGod?god:gold)+1e-9)return false;
   double nextGold=gold+(op==Operation.MintGold?share:op==Operation.GodToGold?share*rate:0);
   double nextGod=god+(op==Operation.MintGod?share:op==Operation.GoldToGod?share/rate:0);
   if(Double.IsInfinity(nextGold)||nextGold>Int32.MaxValue||Double.IsInfinity(nextGod)||Double.IsNaN(nextGod))return false;
  }
  for(int i=0;i<cities.Count;i++)if(shares[i]>0){var c=cities[i];double n=shares[i];switch(op){
   case Operation.MintGold:GodWorldRuntime.Issue(c,false,n);break;
   case Operation.BurnGold:GodWorldRuntime.Issue(c,false,-n);break;
   case Operation.MintGod:GodWorldRuntime.Issue(c,true,n);break;
   case Operation.BurnGod:GodWorldRuntime.Issue(c,true,-n);break;
   case Operation.GoldToGod:GodWorldRuntime.Exchange(c,true,n);break;
   case Operation.GodToGold:GodWorldRuntime.Exchange(c,false,n);break;
  }}return true;
 }
}
