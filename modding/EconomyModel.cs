using System;

public sealed class GodWallet {
 public double gold, god;
}
public sealed class GodEconomyState {
 // households is a legacy name for funded payroll escrow, not residents' cash.
 public GodWallet treasury=new GodWallet(), households=new GodWallet(), business=new GodWallet();
 public GodWallet education=new GodWallet(), military=new GodWallet(), politics=new GodWallet();
 public double salesReceipts, wagesPaid, circulatingGold, circulatingGod, readingWork, practiceWork;
 public double infrastructure, knowledge, capitalInvested, resources, years, warYears, satisfaction=100, loyalty=100, inflation=1, previousMoney, previousOutput;
 public int era, warLevel;
 public double residentMoney, pendingIncome;
 public double nativeOutput, wageRequested, wageFunded, wageArrears, armyArrears, crisis, serviceCoverage=1, wageCoverage=1, armyCoverage=1, foodCoverage=1, unemployment;
 public int stressYears, employed, monetized;public int[] sectorWorkers=new int[4];
 public double firmCapacity=4, maintenancePaid, investmentPaid, fiscalPaid, capitalLoss;
 public double coal, steel, machinery, power, components, ammunition;
 public int steamPlant, factory, powerPlant, laboratory;
 public double logistics;
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
 static double Pay(GodWallet from,GodWallet to,double desired){double amount=Math.Min(from.gold,Math.Max(0,desired));return Transfer(from,to,amount)?amount:0;}
 public static double TotalGold(GodEconomyState s){return s.treasury.gold+s.households.gold+s.business.gold+s.education.gold+s.military.gold+s.politics.gold;}
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
  s.salesReceipts=Sum(s.salesReceipts,s.lastBuildPaid);
  double actualBuild=targetBuild>0?buildUnits*s.lastBuildPaid/targetBuild:0;
  s.infrastructure=Sum(s.infrastructure,actualBuild);s.capitalInvested=Sum(s.capitalInvested,s.lastBuildPaid);resourceBudget-=actualBuild;
  double researchDemand=workers*.15*(1+s.infrastructure*.02)*price;
  // Reserve only the missing budget; unused education/army funds stay available.
  s.lastResearchPaid=Pay(s.treasury,s.education,Math.Max(0,researchDemand-s.education.gold));
  double education=Math.Min(1,Math.Max(0,s.readingWork)/Math.Max(1,workers*.1));
  // Practice allows pre-literate societies to develop without inventing idle XP.
  s.knowledge=Sum(s.knowledge,(Math.Max(0,s.readingWork)*.025+Math.Max(0,s.practiceWork)*.01)*(1+s.research*.01));
  s.readingWork=s.practiceWork=0;
  if(atWar){s.warYears=Sum(s.warYears,1);s.warLevel=(int)Math.Min(int.MaxValue,1+Math.Floor(s.warYears/3));}
  else{s.warYears=0;s.warLevel=0;}
  double armyCost=(double)soldiers*LifecyclePolicy.Wage(s.era,price);
  s.lastArmyPaid=Pay(s.treasury,s.military,Math.Max(0,armyCost-s.military.gold));
  double armyCoverage=armyCost>0?Math.Min(1,s.military.gold/armyCost):1;s.armyCoverage=armyCoverage;
  // Only measured native deliveries/processing are economic output. No second
  // resource destruction or fictitious commodity production at year settlement.
  s.lastOutput=Math.Max(0,s.nativeOutput);s.nativeOutput=0;
  // Real native purchases and funded work supply these counters. No second
  // annual purchase, wage, or fixed-rate tax is synthesized here.
  double sales=Math.Max(0,s.salesReceipts),wages=Math.Max(0,s.wagesPaid);
  s.lastProfit=Math.Max(0,sales-wages);
  s.salesReceipts=s.wagesPaid=0;
  double reinvestUnits=Math.Min(resourceBudget,s.lastProfit*.3/((2+s.era)*price));
  double reinvestPaid=Pay(s.business,s.households,reinvestUnits*(2+s.era)*price);
  double actualReinvest=reinvestPaid/((2+s.era)*price);resourceBudget-=actualReinvest;
  s.firmCapacity=Sum(s.firmCapacity,actualReinvest*.25);s.infrastructure=Sum(s.infrastructure,actualReinvest);s.capitalInvested=Sum(s.capitalInvested,reinvestPaid);s.resources=Math.Max(0,resourceBudget);
  s.production=Sum(s.production,workers>0?s.lastOutput/workers*.08:0);
  s.commerce=Sum(s.commerce,sales>0?.05:0);s.research=Sum(s.research,education*.08);
  s.combat=Sum(s.combat,atWar?armyCoverage*.08:0);
  s.wageCoverage=s.wageRequested>0?Clamp(s.wageFunded/s.wageRequested,0,1):(s.wageArrears>0?0:1);
  if(TotalGold(s)+s.circulatingGold+s.treasury.god*goldPerGod>0||population>=40)s.monetized=1;
  double serviceNeed=population*(.08+s.era*.04)*price;
  s.fiscalPaid=Pay(s.treasury,s.politics,Math.Max(0,serviceNeed-s.politics.gold));
  s.serviceCoverage=serviceNeed>0?Clamp(s.politics.gold/serviceNeed,0,1):1;
  // Existing civic providers perform maintenance, funded by the political budget.
  s.maintenancePaid=Pay(s.politics,s.business,Math.Min(serviceNeed,s.politics.gold));
  bool deficit=s.foodCoverage<.5||s.monetized!=0&&(s.wageCoverage<.5||armyCoverage<.5||s.serviceCoverage<.5);
  s.stressYears=deficit?Math.Min(1000,s.stressYears+1):Math.Max(0,s.stressYears-1);
  s.crisis=LifecyclePolicy.Stress(s.crisis,s.wageCoverage,armyCoverage,s.serviceCoverage,s.foodCoverage,s.unemployment,s.monetized!=0);
  if(deficit&&s.stressYears>3)s.crisis=Clamp(s.crisis+Math.Min(20,s.stressYears-3),0,100);
  s.capitalLoss=LifecyclePolicy.Depreciation(s.infrastructure,s.serviceCoverage,s.crisis,atWar);
  s.infrastructure=Math.Max(0,s.infrastructure-s.capitalLoss);
  s.firmCapacity=Math.Max(1,s.firmCapacity*(1-(s.crisis>=50?.04:.005)));
  s.wageRequested=s.wageFunded=0;
  double burden=atWar?(1+s.warLevel*.4)*(1+(1-armyCoverage)):0;
  s.satisfaction=Clamp(s.satisfaction+(sales>0?.8:-.15)-burden-s.crisis*.12,0,100);
  s.loyalty=Clamp(s.loyalty+(s.satisfaction-50)*.04-burden*.4-s.crisis*.08,0,100);
  while(s.era<EraNames.Length-1){int next=s.era+1;if(next>=5&&s.steamPlant==0||next>=6&&s.factory==0||next>=7&&s.powerPlant==0||next>=8&&s.laboratory==0)break;double scale=Math.Pow(2,next);if(s.infrastructure<scale*2||s.knowledge<scale||s.production<next*.1||s.resources<next)break;s.era=next;}
  double money=TotalGold(s)+s.circulatingGold+(s.treasury.god+s.households.god+s.business.god+s.education.god+s.military.god+s.politics.god+s.circulatingGod)*goldPerGod;if(Valid(money)&&s.previousMoney>0){double outputChange=s.previousOutput>0&&s.lastOutput>0?s.lastOutput/s.previousOutput:1;double nextPrice=(money/s.previousMoney)/outputChange;if(Valid(nextPrice)&&nextPrice>0)s.inflation=Clamp(price*(1+.1*(nextPrice-1)),.05,1000000);}
  s.previousMoney=money;s.previousOutput=s.lastOutput;
 }
}
