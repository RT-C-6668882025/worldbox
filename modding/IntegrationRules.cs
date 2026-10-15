using System;

// Pure policies shared by native adapters and transaction tests.
public static class IntegrationRules {
 public static double Clamp(double n,double lo,double hi){return Double.IsNaN(n)||Double.IsInfinity(n)?lo:Math.Max(lo,Math.Min(hi,n));}
 public static int Price(int basis,int source,int target,double inflation,bool trusted){
  double scarcity=Clamp((source+10.0)/(target+10.0),.5,2);
  return (int)Math.Ceiling(Clamp(Math.Max(1,basis)*Clamp(inflation,.05,1000000)*scarcity*(trusted?.9:1),1,1000000));
 }
 public static int ProcessingFee(int ingredients,int era,double inflation){return (int)Math.Ceiling(Clamp(Math.Max(1,ingredients)*.05*(1+Math.Max(0,era)*.1)*Clamp(inflation,.05,1000000),1,1000000));}
 public static int ConstructionSpeed(int native,int level){if(native<=0)return native;return (int)Math.Min(Int32.MaxValue,Math.Ceiling(native*(1+Math.Min(100,Math.Max(0,level))*.025)));}
 public static int Wear(int damage,int combat){return damage<=0?damage:Math.Max(1,(int)Math.Ceiling(damage/(1+Math.Min(40,Math.Max(0,combat))*.01)));}
 public static int Learning(int work,double intelligence){return (int)Clamp(Math.Floor(Math.Max(0,work)*(1+Clamp(intelligence,0,100)*.005)),0,25);}
 public static double HeirScore(int governance,double stewardship,bool preferred,bool related){return Math.Max(0,governance)*5+Clamp(stewardship,0,1000)*.2+(preferred?25:0)+(related?5:0);}
 public static int Contribution(double worth,int population){return (int)Math.Min(1000000,Math.Floor(Math.Max(0,Math.Min(worth*.01,Math.Max(1,population*.1)))));}
 public static int RefundShare(long pool,long claim,long total){if(pool<=0||claim<=0||total<=0)return 0;return (int)Math.Min(Int32.MaxValue,Math.Floor((double)pool*Math.Min(claim,total)/total));}

 // Callbacks operate synchronously. Rejected capacity is refunded; a failed
 // settlement reverses accepted goods, so neither side gets a free delivery.
 public static int MoveGoods(int requested,int sourceStock,int targetStock,int capacity,
  Func<int,int> take,Func<int,int> accept,Action<int> undo,Action<int> refund,Func<int,bool> settle){
  int n=Math.Min(Math.Max(0,requested),Math.Min(Math.Max(0,sourceStock),Math.Max(0,capacity-targetStock)));
  if(n==0)return 0;int removed=Math.Min(n,Math.Max(0,take(n)));if(removed==0)return 0;
  int received;
  try{received=Math.Min(removed,Math.Max(0,accept(removed)));}catch{refund(removed);throw;}
  if(received<removed)refund(removed-received);
  if(received==0)return 0;
  if(!settle(received)){undo(received);refund(received);return 0;}
  return received;
 }
}
