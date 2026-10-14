using System;

public sealed class GodWallet {
 public double gold, god;
}
public sealed class GodEconomyState {
 public GodWallet treasury=new GodWallet(), households=new GodWallet(), business=new GodWallet();
 public double infrastructure, knowledge, capitalInvested, resources, years, warYears, satisfaction=100, loyalty=100, inflation=1, previousMoney, previousOutput;
 public int era, warLevel;
 public double lastBuildPaid,lastResearchPaid,lastArmyPaid,lastOutput,lastProfit;
 public double production, commerce, research, governance, combat;
}
public static class GodEconomy {
 public static readonly string[] EraNames={"原始","石器","农耕","金属","蒸汽","工业","电力","信息","现代"};
 public static double goldPerGod=1;
 static bool Valid(double x){return !Double.IsNaN(x)&&!Double.IsInfinity(x)&&x>=0;}
 static double Sum(double a,double b){double x=a+b;return Valid(x)?x:a;}
 static double Clamp(double x,double lo,double hi){return Math.Max(lo,Math.Min(hi,x));}
 public static bool Issue(GodWallet w,bool god,double amount){if(w==null||!Valid(amount))return false;double old=god?w.god:w.gold,next=old+amount;if(!Valid(old)||!Valid(next))return false;if(god)w.god=next;else w.gold=next;return true;}
 public static bool Burn(GodWallet w,bool god,double amount){if(w==null||!Valid(amount))return false;double old=god?w.god:w.gold;if(!Valid(old)||old<amount)return false;if(god)w.god=old-amount;else w.gold=old-amount;return true;}
 public static bool SetRate(double rate){if(!Valid(rate)||rate==0)return false;goldPerGod=rate;return true;}
 public static bool Exchange(GodWallet w,bool fromGod,double amount){if(w==null||!Valid(amount)||!Valid(goldPerGod)||goldPerGod==0)return false;double input=fromGod?w.god:w.gold,output=fromGod?w.gold:w.god,converted=fromGod?amount*goldPerGod:amount/goldPerGod,next=output+converted;if(!Valid(input)||!Valid(output)||input<amount||!Valid(converted)||!Valid(next))return false;if(fromGod){w.god=input-amount;w.gold=next;}else{w.gold=input-amount;w.god=next;}return true;}
 public static bool Transfer(GodWallet from,GodWallet to,double amount){if(from==null||to==null||!Valid(amount)||!Valid(from.gold)||!Valid(to.gold)||from.gold<amount||!Valid(to.gold+amount))return false;if(Object.ReferenceEquals(from,to))return true;from.gold-=amount;to.gold+=amount;return true;}
 static double Pay(GodWallet from,GodWallet to,double desired){if(desired>from.gold&&from.god>0&&goldPerGod>0){double convert=Math.Min(from.god,(desired-from.gold)/goldPerGod);Exchange(from,true,convert);}double amount=Math.Min(from.gold,Math.Max(0,desired));return Transfer(from,to,amount)?amount:0;}
 public static double TotalGold(GodEconomyState s){return s.treasury.gold+s.households.gold+s.business.gold;}
 // Resources is real city stock, supplied by the native adapter. Output is goods,
 // never newly minted currency; all income below is payment by another wallet.
 public static void Tick(GodEconomyState s,int population,int soldiers,bool atWar,double kingAggression){
  if(s==null||population<0||soldiers<0||!Valid(s.resources)||!Valid(s.infrastructure)||!Valid(s.knowledge)||!Valid(s.treasury.gold)||!Valid(s.households.gold)||!Valid(s.business.gold))return;
  s.years=Sum(s.years,1);s.lastBuildPaid=s.lastResearchPaid=s.lastArmyPaid=s.lastOutput=s.lastProfit=0;
  if(population==0)return;
  double workers=Math.Max(0,population-soldiers),resourceBudget=s.resources;double price=Valid(s.inflation)&&s.inflation>0?s.inflation:1;price=Clamp(price,.05,1000000);
  double buildUnits=Math.Min(resourceBudget,workers*.08/(1+s.infrastructure*.01));
  double targetBuild=buildUnits*(2+s.era)*price;
  s.lastBuildPaid=Pay(s.treasury,s.business,targetBuild);
  double actualBuild=targetBuild>0?buildUnits*s.lastBuildPaid/targetBuild:0;
  s.infrastructure=Sum(s.infrastructure,actualBuild);s.capitalInvested=Sum(s.capitalInvested,s.lastBuildPaid);resourceBudget-=actualBuild;
  double researchDemand=workers*.15*(1+s.infrastructure*.02)*price;
  s.lastResearchPaid=Pay(s.treasury,s.households,researchDemand);
  double education=researchDemand>0?s.lastResearchPaid/researchDemand:0;
  s.knowledge=Sum(s.knowledge,education*workers*.025*(1+s.research*.01));
  if(atWar){s.warYears=Sum(s.warYears,1);s.warLevel=(int)Math.Min(int.MaxValue,1+Math.Floor(s.warYears/3));}
  else{s.warYears=0;s.warLevel=0;}
  double armyCost=soldiers*(.3+s.era*.12)*(1+s.warLevel*.2)*(1+Clamp(kingAggression,0,100)/200)*price;
  s.lastArmyPaid=Pay(s.treasury,s.households,armyCost);
  double armyCoverage=armyCost>0?s.lastArmyPaid/armyCost:1;
  // Infrastructure thresholds create successive acceleration as production matures.
  double phase=1+Math.Floor(Math.Log(1+s.infrastructure)/Math.Log(2))*.22;
  double potential=workers*.1*phase*(1+s.production*.01)*(1+s.era*.15);
  s.lastOutput=Math.Min(resourceBudget,potential);resourceBudget-=s.lastOutput;
  double salesDemand=s.lastOutput*(1+s.commerce*.005)*price;
  double sales=Pay(s.households,s.business,salesDemand);
  double wages=Pay(s.business,s.households,sales*.55);
  double taxes=Pay(s.business,s.treasury,sales*.15);
  s.lastProfit=Math.Max(0,sales-wages-taxes);
  double reinvestUnits=Math.Min(resourceBudget,s.lastProfit*.3/((2+s.era)*price));
  double reinvestPaid=Pay(s.business,s.households,reinvestUnits*(2+s.era)*price);
  double actualReinvest=reinvestPaid/((2+s.era)*price);resourceBudget-=actualReinvest;
  s.infrastructure=Sum(s.infrastructure,actualReinvest);s.capitalInvested=Sum(s.capitalInvested,reinvestPaid);s.resources=Math.Max(0,resourceBudget);
  s.production=Sum(s.production,workers>0?s.lastOutput/workers*.08:0);
  s.commerce=Sum(s.commerce,sales>0?.05:0);s.research=Sum(s.research,education*.08);
  s.governance=Sum(s.governance,taxes>0?.03:0);s.combat=Sum(s.combat,atWar?armyCoverage*.08:0);
  double burden=atWar?(1+s.warLevel*.4)*(1+(1-armyCoverage)):0;
  s.satisfaction=Clamp(s.satisfaction+(sales>0?.25:-.15)-burden,0,100);
  s.loyalty=Clamp(s.loyalty+(s.satisfaction-50)*.01-burden*.4,0,100);
  while(s.era<EraNames.Length-1){int next=s.era+1;double scale=Math.Pow(2,next);if(s.infrastructure<scale*2||s.knowledge<scale||s.production<next*.1||s.resources<next)break;s.era=next;}
  double money=TotalGold(s)+(s.treasury.god+s.households.god+s.business.god)*goldPerGod;if(Valid(money)&&s.previousMoney>0&&s.previousOutput>0&&s.lastOutput>0){double nextPrice=(money/s.previousMoney)/(s.lastOutput/s.previousOutput);if(Valid(nextPrice)&&nextPrice>0)s.inflation=Clamp(price*(1+.1*(nextPrice-1)),.05,1000000);}
  s.previousMoney=money;s.previousOutput=s.lastOutput;
 }
}
