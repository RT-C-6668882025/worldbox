using System;
public static class EconomyModelTests {
 static int checks;
 static void Check(bool b,string name){if(!b)throw new Exception(name);checks++;}
 static bool Near(double a,double b){return Math.Abs(a-b)<1e-7*Math.Max(1,Math.Abs(a));}
 static GodEconomyState State(){var s=new GodEconomyState();s.resources=100000;s.treasury.gold=100000;s.households.gold=10000;return s;}
 public static void Main(){
 var w=new GodWallet();Check(GodEconomy.Issue(w,false,100),"issue gold");Check(GodEconomy.Issue(w,true,10),"issue god");Check(GodEconomy.SetRate(2),"rate");double eq=w.gold+w.god*2;Check(GodEconomy.Exchange(w,true,5)&&Near(eq,w.gold+w.god*2),"exchange conserved");Check(GodEconomy.Exchange(w,false,10)&&Near(eq,w.gold+w.god*2),"reverse conserved");double before=w.gold;Check(!GodEconomy.Exchange(w,false,1000)&&w.gold==before,"atomic insufficient");Check(!GodEconomy.Issue(w,false,double.NaN)&&!GodEconomy.SetRate(0),"invalid finite");Check(GodEconomy.Burn(w,true,1),"burn");
 var s=State();double total=GodEconomy.TotalGold(s),r=s.resources;for(int i=0;i<50;i++)GodEconomy.Tick(s,100,10,false,50);Check(Near(total,GodEconomy.TotalGold(s)),"annual conservation");Check(s.infrastructure>0&&s.knowledge>0&&s.resources<r&&s.era>0,"funded resources development");
 var empty=new GodEconomyState();empty.treasury.gold=1e12;GodEconomy.Tick(empty,100,0,false,0);Check(empty.infrastructure==0&&empty.lastOutput==0&&empty.era==0,"money not instant development");
 var noFunds=new GodEconomyState();noFunds.resources=1000;GodEconomy.Tick(noFunds,100,0,false,0);Check(noFunds.infrastructure==0,"construction requires funds");
 var war=State();double satisfaction=war.satisfaction;for(int i=0;i<12;i++)GodEconomy.Tick(war,100,92,true,100);Check(war.warLevel>=4&&war.satisfaction<satisfaction&&war.lastArmyPaid>0,"war escalation political cost");GodEconomy.Tick(war,100,0,false,0);Check(war.warLevel==0&&war.warYears==0,"peace reset");
 var overflow=new GodWallet();overflow.gold=double.MaxValue;Check(!GodEconomy.Issue(overflow,false,double.MaxValue)&&overflow.gold==double.MaxValue,"overflow atomic");
 var dual=new GodEconomyState();dual.resources=1000;dual.treasury.god=1000;GodEconomy.SetRate(2);double worth=GodEconomy.TotalGold(dual)+(dual.treasury.god+dual.households.god+dual.business.god)*2;GodEconomy.Tick(dual,100,10,true,20);Check(dual.lastBuildPaid>0&&dual.lastArmyPaid>0,"god settles production and military costs");Check(Near(worth,GodEconomy.TotalGold(dual)+(dual.treasury.god+dual.households.god+dual.business.god)*2),"dual-currency annual conservation");var shock=State();GodEconomy.SetRate(1);GodEconomy.Tick(shock,100,0,false,0);double price=shock.inflation;shock.treasury.god=1e6;GodEconomy.Tick(shock,100,0,false,0);Check(shock.inflation>price,"god issuance changes price pressure");price=shock.inflation;GodEconomy.SetRate(2);GodEconomy.Tick(shock,100,0,false,0);Check(shock.inflation>price,"rate revalues god in monetary pressure");
 Console.WriteLine(checks+" economic checks passed");
 }
}
