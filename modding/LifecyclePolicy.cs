using System;
// Shared deterministic rules; no hidden issuance or population deletion.
public static class LifecyclePolicy {
 public static double Bound(double x,double lo,double hi){return Double.IsNaN(x)||Double.IsInfinity(x)?lo:Math.Max(lo,Math.Min(hi,x));}
 public static int Wage(int era,double price){return (int)Math.Ceiling(Bound((1+Math.Max(0,era)) * Bound(price,.05,1000000),1,1000000));}
 public static int Mobilize(int adults,int eligible,bool rebel,bool war,int level,double funds,double military,double price,double crisis){
  adults=Math.Max(0,adults);eligible=Math.Min(adults,Math.Max(0,eligible));if(eligible==0)return 0;
  if(rebel)return Math.Min(eligible,(int)Math.Floor(adults*.92));
  int demand=(int)Math.Ceiling(adults*(war?Math.Min(.85,.15+Math.Max(0,level)*.06):.08));
  int available=(int)Math.Min(eligible,Math.Floor(Math.Max(0,funds+military)/Math.Max(1,price)));
  if(crisis>=75)available=(int)Math.Floor(available*.5);
  return Math.Min(eligible,Math.Min(demand,available));
 }
 public static double Stress(double old,double wages,double army,double publicService,double food,double jobless,bool monetized){
  double financial=monetized?(1-Bound(wages,0,1))*20+(1-Bound(army,0,1))*16+(1-Bound(publicService,0,1))*12+Bound(jobless,0,1)*6:0;
  double target=Bound(financial+(1-Bound(food,0,1))*32,0,100);
  return Bound(old*.65+target*.35,0,100);
 }
 public static double HoldingCapacity(int prod,int trade,int science,int governance,int combat){return 100+(Math.Max(0,prod)+Math.Max(0,trade)+Math.Max(0,science)+Math.Max(0,governance)+Math.Max(0,combat))*100.0;}
 public static double Depreciation(double capital,double service,double crisis,bool war){return Math.Max(0,capital)*(.005+(1-Bound(service,0,1))*.03+Bound(crisis,0,100)*.0003+(war?.01:0));}
 public static int DeployThreshold(int target,int current,bool defensive){if(target<=0||current<=0)return 0;return defensive?1:Math.Max(1,(int)Math.Min(8,Math.Ceiling(target*.35)));}
 public static int CrisisLoyalty(double crisis,int years){return (int)Math.Min(180,Math.Max(0,crisis)*1.5+Math.Min(15,Math.Max(0,years))*2);}
}
