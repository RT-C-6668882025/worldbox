using System;
class IntegrationRulesTests {
 static int checks;
 static void Check(bool ok,string name){if(!ok)throw new Exception(name);checks++;Console.WriteLine("PASS "+name);}
 static void Main(){
  Check(IntegrationRules.Price(10,10,10,1,false)==10,"native price baseline");
  Check(IntegrationRules.Price(10,100,0,1,false)>10,"import scarcity changes price");
  Check(IntegrationRules.Price(10,10,10,1,true)==9,"trusted leaders change price without a rebate mint");
  Check(IntegrationRules.Price(Int32.MaxValue,Int32.MaxValue,0,Double.PositiveInfinity,false)<=1000000,"price is finite and bounded");
  Check(IntegrationRules.Price(0,0,0,0,false)>=1,"zero-price commodity cannot underflow");
  Check(IntegrationRules.ProcessingFee(100,3,2)>IntegrationRules.ProcessingFee(100,0,1),"material era and price affect processing cost");
  Check(IntegrationRules.ProcessingFee(0,0,1)==1,"processing has a minimum funded service cost");
  Check(IntegrationRules.ConstructionSpeed(0,100)==0,"native zero construction speed stays blocked");
  Check(IntegrationRules.ConstructionSpeed(4,20)>4,"work skill adds to genetic construction speed");
  Check(IntegrationRules.ConstructionSpeed(Int32.MaxValue,100)==Int32.MaxValue,"construction multiplication saturates");
  Check(IntegrationRules.Wear(0,40)==0,"native zero wear remains zero");
  Check(IntegrationRules.Wear(100,40)<100,"experienced warrior reduces real equipment wear");
  Check(IntegrationRules.Wear(1,40)==1,"skill cannot make equipment immortal");
  Check(IntegrationRules.Wear(Int32.MaxValue,40)>0,"equipment wear cannot overflow negative");
  Check(IntegrationRules.Learning(3,10)==3,"ordinary intelligence does not double tiny work rewards");
  Check(IntegrationRules.Learning(10,100)>10,"native intelligence affects actual learning");
  Check(IntegrationRules.Learning(100,100)==25,"learning remains bounded per event");
  Check(IntegrationRules.Learning(0,100)==0,"idle event cannot award learning");
  Check(IntegrationRules.HeirScore(0,10,true,false)>IntegrationRules.HeirScore(0,10,false,false),"native heir preference is retained");
  Check(IntegrationRules.HeirScore(10,10,false,false)>IntegrationRules.HeirScore(0,10,true,false),"large governance advantage can affect eligible succession");
  Check(IntegrationRules.HeirScore(1,10,false,true)>IntegrationRules.HeirScore(1,10,false,false),"family relation participates in governance");
  Check(IntegrationRules.Contribution(1000,100)==10,"alliance contribution is bounded by member wealth");
  Check(IntegrationRules.Contribution(0,100)==0,"empty member cannot create alliance money");
  Check(IntegrationRules.Contribution(1e20,Int32.MaxValue)<=1000000,"alliance contribution cap prevents overflow");
  Check(IntegrationRules.RefundShare(100,20,100)==20,"leaving member gets proportional remaining fund");
  Check(IntegrationRules.RefundShare(25,20,100)==5,"spent alliance funds are not refunded twice");
  Check(IntegrationRules.RefundShare(100,0,0)==0,"empty contribution cannot claim a fund");
  int source=100,target=0,buyer=100,seller=0;
  Func<int,int> take=n=>{source-=n;return n;};Func<int,int> accept=n=>{target+=n;return n;};Action<int> undo=n=>target-=n;Action<int> refund=n=>source+=n;
  Func<int,bool> settle=n=>{int cost=n*3;if(buyer<cost)return false;buyer-=cost;seller+=cost;return true;};
  int received=IntegrationRules.MoveGoods(5,source,target,100,take,accept,undo,refund,settle);
  Check(received==5&&source==95&&target==5,"delivery moves actual goods");
  Check(buyer==85&&seller==15&&buyer+seller==100,"trade payment conserves money");
  source=100;target=0;buyer=100;seller=0;
  received=IntegrationRules.MoveGoods(5,source,target,100,take,n=>{target+=2;return 2;},undo,refund,settle);
  Check(received==2&&source==98&&target==2,"partial warehouse acceptance refunds rejected goods");
  Check(buyer==94&&seller==6,"partial delivery charges only received units");
  source=100;target=0;buyer=0;seller=0;
  received=IntegrationRules.MoveGoods(5,source,target,100,take,accept,undo,refund,settle);
  Check(received==0&&source==100&&target==0,"failed settlement reverses delivery");
  Check(buyer==0&&seller==0,"failed settlement never creates seller income");
  source=100;target=100;int calls=0;
  received=IntegrationRules.MoveGoods(5,source,target,100,n=>{calls++;return n;},accept,undo,refund,settle);
  Check(received==0&&calls==0,"full destination does not debit source");
  source=2;target=0;buyer=100;seller=0;
  received=IntegrationRules.MoveGoods(100,source,target,100,take,accept,undo,refund,settle);
  Check(received==2&&source==0&&target==2,"requested delivery cannot exceed stock");
  source=100;target=99;buyer=100;seller=0;
  received=IntegrationRules.MoveGoods(100,source,target,100,take,accept,undo,refund,settle);
  Check(received==1&&target==100&&source==99,"destination representation bounds delivery");
  source=100;target=0;bool thrown=false;
  try{IntegrationRules.MoveGoods(5,source,target,100,take,n=>{throw new Exception("warehouse unavailable");},undo,refund,settle);}catch{thrown=true;}
  Check(thrown&&source==100&&target==0,"warehouse exception refunds removed goods");
  source=100;target=0;buyer=100;seller=0;
  received=IntegrationRules.MoveGoods(5,source,target,100,n=>0,accept,undo,refund,settle);
  Check(received==0&&buyer==100&&seller==0,"failed source withdrawal does not charge buyer");
  received=IntegrationRules.MoveGoods(-1,source,target,100,take,accept,undo,refund,settle);
  Check(received==0&&source==100,"negative delivery is rejected");
  Console.WriteLine(checks+" native integration policy checks passed");
 }
}
