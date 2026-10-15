using System;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public static class NativeEconomyEditors {
 public static void Attach(StatsWindow window){
  try{bool kingdom=window is KingdomWindow;if(!kingdom&&!(window is CityWindow))return;
   string name=kingdom?"content_traits_editor":"content_resources";Transform parent=null;
   foreach(var t in window.GetComponentsInChildren<Transform>(true))if(t.name==name){parent=t;break;}
   if(parent!=null)AttachAt(window,parent);
  }catch(Exception e){Debug.LogWarning("GodWorld editor attach: "+e);}
 }
 public static void AttachAt(StatsWindow w,Transform parent){
  var found=parent.Find("godworld_currency_editor");if(found!=null){found.GetComponent<GodCurrencyEditorPanel>().Bind(w);return;}
  var go=new GameObject("godworld_currency_editor",typeof(RectTransform));go.SetActive(false);go.transform.SetParent(parent,false);go.transform.SetAsFirstSibling();
  var layout=go.AddComponent<VerticalLayoutGroup>();layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;layout.spacing=2;
  var fit=go.AddComponent<ContentSizeFitter>();fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
  go.AddComponent<GodCurrencyEditorPanel>().Bind(w);go.SetActive(true);
 }
}
public sealed class GodCurrencyEditorPanel:MonoBehaviour {
 static readonly double[] amounts={100,1000,10000,100000,1000000};
 readonly List<KeyValueField> rows=new List<KeyValueField>();StatsWindow window;double amount=1000;int scope;float nextRefresh;string status="点击右侧数值操作";InputField amountInput,rateInput;
 public void Bind(StatsWindow w){if(window!=w){scope=0;window=w;}if(rows.Count>0)Refresh();}
 void OnEnable(){try{if(rows.Count==0)Create();Refresh();}catch(Exception e){Debug.LogWarning("GodWorld editor UI: "+e);}}
 void Update(){if(Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+1;try{Refresh();}catch(Exception e){Debug.LogWarning("GodWorld editor refresh: "+e);}}
 InputField Input(KeyValueField row,string value,bool integral){
  var field=row.value.gameObject.AddComponent<InputField>();field.textComponent=row.value;field.text=value;field.contentType=integral?InputField.ContentType.IntegerNumber:InputField.ContentType.DecimalNumber;
  field.lineType=InputField.LineType.SingleLine;field.characterLimit=24;row.value.raycastTarget=true;return field;
 }
 void Create(){var prefab=Resources.Load<GameObject>("ui/KeyValueFieldStats");if(prefab==null)throw new Exception("Native currency row prefab missing");
  for(int i=0;i<20;i++){var obj=UnityEngine.Object.Instantiate(prefab,transform,false);obj.name="godworld_currency_"+i;var row=obj.GetComponent<KeyValueField>();var size=obj.GetComponent<LayoutElement>()??obj.AddComponent<LayoutElement>();size.minHeight=26;size.preferredHeight=26;if(row.icon!=null)row.icon.gameObject.SetActive(false);rows.Add(row);obj.SetActive(true);}
  amountInput=Input(rows[3],"1000",true);amountInput.onEndEdit.AddListener(text=>{double n;if(Double.TryParse(text,out n)&&n>0&&n<=Int32.MaxValue&&n==Math.Floor(n)){amount=n;status="操作数量已修改";}else status="请输入1到2147483647的整数";Refresh();});
  rateInput=Input(rows[10],F(GodWorldRuntime.GetRate()),false);rateInput.onEndEdit.AddListener(text=>{double n;if(Double.TryParse(text,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out n)&&n>0&&!Double.IsInfinity(n)){GodWorldRuntime.SetRate(n);status="汇率已修改";}else status="汇率必须大于0";Refresh();});
 }
 Actor Person(){var u=window as UnitWindow;if(u==null)return null;return (Actor)typeof(UnitWindow).GetMethod("get_actor",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(u,null);}
 Kingdom Nation(){var kw=window as KingdomWindow;if(kw!=null)return kw.getMetaObject();var c=City();return GodWorldRuntime.Nation(c);}
 City City(){var cw=window as CityWindow;if(cw!=null)return cw.getMetaObject();var a=Person();return a==null?null:a.getCity();}
 List<City> Cities(){return window is KingdomWindow?CurrencyScope.Cities(null,Nation()):CurrencyScope.Cities(City(),null);}
 static string F(double n){return n.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture);}
 void Row(int i,string label,string value,Action action){var r=rows[i];r.name_text.text=label;if(i!=3&&i!=10)r.value.text=value;NativeEconomyUI.BindAction(r,action==null?null:(UnityEngine.Events.UnityAction)(()=>{try{action();Refresh();}catch(Exception e){status="操作失败";Debug.LogWarning("GodWorld editor action: "+e);Refresh();}}));}
 void Apply(CurrencyScope.Operation op){bool ok;var a=Person();
  if(a!=null){switch(op){case CurrencyScope.Operation.MintGold:ok=NativeLifecycle.IssuePersonal(a,false,amount);break;case CurrencyScope.Operation.BurnGold:ok=NativeLifecycle.IssuePersonal(a,false,-amount);break;case CurrencyScope.Operation.MintGod:ok=NativeLifecycle.IssuePersonal(a,true,amount);break;case CurrencyScope.Operation.BurnGod:ok=NativeLifecycle.IssuePersonal(a,true,-amount);break;default:ok=NativeLifecycle.ExchangePersonal(a,op==CurrencyScope.Operation.GoldToGod,amount);break;}}
  else if(window is KingdomWindow&&scope==0)ok=NativeLifecycle.NationalOperation(Nation(),op,amount);else ok=CurrencyScope.Apply(Cities(),op,amount);
  status=ok?"已完成":"余额不足、持币容量不足或超出上限";
 }
 void PersonalTransfer(bool fromCity){var a=Person();var c=City();if(a==null||c==null){status="没有所属城市";return;}var d=(ActorData)a.getData();int n=(int)amount;bool ok;
  if(fromCity){ok=n<=Int32.MaxValue-d.money&&GodWorldRuntime.TryDebitPublic(c,n);if(ok)d.money+=n;}
  else{ok=d.money>=n&&GodWorldRuntime.CreditPublic(c,n);if(ok)d.money-=n;}status=ok?"转账完成，总量不变":"转账失败";
 }
 void Refresh(){if(rows.Count==0||window==null)return;var a=Person();var k=Nation();var c=City();var cities=Cities();bool national=window is KingdomWindow&&scope==0;
  double gold=a!=null?((ActorData)a.getData()).money:national?NativeLifecycle.NationalBalance(k,false):CurrencyScope.Total(cities,false);
  double god=a!=null?NativeLifecycle.PersonalGod(a):national?NativeLifecycle.NationalBalance(k,true):CurrencyScope.Total(cities,true);
  Row(0,a!=null?"双币 · 个人":national?"双币 · 独立国库":window is KingdomWindow?"双币 · 全国城市":"双币 · 本村镇",status,null);
  Row(1,"金币",F(gold),null);Row(2,"GOD",F(god),null);Row(3,"输入操作数量","",null);
  Row(4,"发行金币","+ "+F(amount),()=>Apply(CurrencyScope.Operation.MintGold));Row(5,"回收金币","− "+F(amount),()=>Apply(CurrencyScope.Operation.BurnGold));
  Row(6,"发行 GOD","+ "+F(amount),()=>Apply(CurrencyScope.Operation.MintGod));Row(7,"回收 GOD","− "+F(amount),()=>Apply(CurrencyScope.Operation.BurnGod));
  Row(8,"金币 → GOD",F(amount)+" 金币",()=>Apply(CurrencyScope.Operation.GoldToGod));Row(9,"GOD → 金币",F(amount)+" GOD",()=>Apply(CurrencyScope.Operation.GodToGold));
  Row(10,"输入汇率：1 GOD =","",null);if(!rateInput.isFocused)rateInput.text=F(GodWorldRuntime.GetRate());if(!amountInput.isFocused)amountInput.text=F(amount);
  Row(11,"提高汇率","× 1.25",()=>GodWorldRuntime.SetRate(GodWorldRuntime.GetRate()*1.25));Row(12,"降低汇率","÷ 1.25",()=>GodWorldRuntime.SetRate(GodWorldRuntime.GetRate()/1.25));
  Row(13,"作用范围",a!=null?"当前人物 · 汇率全世界共用":window is KingdomWindow?(scope==0?"独立国库 ▸":"全国城市 ▸"):"当前村镇",window is KingdomWindow?(Action)(()=>scope=1-scope):null);
  Row(14,a!=null?"城市 → 个人":"国库 → 当前城市",F(amount)+" 金币",()=>{if(a!=null)PersonalTransfer(true);else status=NativeLifecycle.NationalGrant(k,c??(k==null?null:k.capital),amount,true)?"拨款完成":"拨款失败";});
  Row(15,a!=null?"个人 → 城市":"当前城市 → 国库",F(amount)+" 金币",()=>{if(a!=null)PersonalTransfer(false);else status=NativeLifecycle.NationalGrant(k,c??(k==null?null:k.capital),amount,false)?"上缴完成":"上缴失败";});
  Row(16,"快速数量","切换档位 ▸",()=>{int index=Array.IndexOf(amounts,amount);amount=amounts[(index+1)%amounts.Length];});
  Row(17,"经济加速","投入现有资金 ▸",()=>{var target=c??(k==null?null:k.capital);status=NativeLifecycle.Stimulate(target,amount)?"已投入，仍需资源和生产":"资金或资源不足";});
  Row(18,a!=null?"个人 GOD 容量":"操作说明",a!=null?F(NativeLifecycle.GodCapacity(a)):"发行增加总量；拨款仅转移",null);
  Row(19,"存档","立即保存 ▸",()=>{GodWorldRuntime.Flush();NativeLifecycle.SaveGame();status="已请求原生存档";});
  LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
 }
}
