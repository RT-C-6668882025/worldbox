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
 public static void AttachOrganization(StatsWindow w,Transform target){
  if(target==null)return;var old=target.Find("godworld_organization_editor");if(old!=null){old.GetComponent<GodOrganizationEditor>().Bind(w);old.gameObject.SetActive(!old.gameObject.activeSelf);return;}
  var go=new GameObject("godworld_organization_editor",typeof(RectTransform));go.SetActive(false);go.transform.SetParent(target,false);go.transform.SetAsLastSibling();var layout=go.AddComponent<VerticalLayoutGroup>();layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;layout.spacing=2;go.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;go.AddComponent<GodOrganizationEditor>().Bind(w);go.SetActive(true);
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
 readonly List<KeyValueField> rows=new List<KeyValueField>();
 readonly Dictionary<string,InputField> inputs=new Dictionary<string,InputField>();
 StatsWindow window;object selected;int scope;float nextRefresh;string status="每项独立输入，点击按钮执行";
 public void Bind(StatsWindow w){window=w;if(rows.Count>0)Refresh();}
 void OnEnable(){try{if(rows.Count==0)Create();Refresh();}catch(Exception e){Debug.LogWarning("GodWorld editor UI: "+e);}}
 void Update(){if(Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+1;try{Refresh();}catch(Exception e){Debug.LogWarning("GodWorld editor refresh: "+e);}}
 KeyValueField Add(string label,string value,Action action=null){var prefab=Resources.Load<GameObject>("ui/KeyValueFieldStats");if(prefab==null)throw new Exception("Native currency row prefab missing");var obj=UnityEngine.Object.Instantiate(prefab,transform,false);obj.name="godworld_currency_"+rows.Count;var row=obj.GetComponent<KeyValueField>();var size=obj.GetComponent<LayoutElement>()??obj.AddComponent<LayoutElement>();size.minHeight=36;size.preferredHeight=36;if(row.icon!=null)row.icon.gameObject.SetActive(false);
  foreach(var text in new[]{row.name_text,row.value}){text.supportRichText=false;text.resizeTextForBestFit=true;text.resizeTextMinSize=9;text.resizeTextMaxSize=14;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;}
  row.name_text.text=label;row.value.text=value;NativeEconomyUI.BindAction(row,action==null?null:(UnityEngine.Events.UnityAction)(()=>Run(action)));rows.Add(row);obj.SetActive(true);return row;
 }
 void Amount(string key,string label,Action<double> action){var row=Add(label+"数量","1000");var go=row.value.gameObject;var field=go.AddComponent<InputField>();field.textComponent=row.value;field.text="1000";field.contentType=InputField.ContentType.IntegerNumber;field.lineType=InputField.LineType.SingleLine;field.characterLimit=10;row.value.raycastTarget=true;inputs.Add(key,field);Add(label,"执行 ▸",()=>{double n;if(!CurrencyInput.TryAmount(field.text,out n))throw new Exception("请输入1至2147483647的整数");action(n);});}
 void Create(){Add("双币","");Add("金币余额","");Add("GOD 余额","");Add("当前汇率","");Add("操作状态",status);
  Amount("mintGold","发行金币",n=>Apply(CurrencyScope.Operation.MintGold,n));Amount("burnGold","回收金币",n=>Apply(CurrencyScope.Operation.BurnGold,n));Amount("mintGod","发行 GOD",n=>Apply(CurrencyScope.Operation.MintGod,n));Amount("burnGod","回收 GOD",n=>Apply(CurrencyScope.Operation.BurnGod,n));
  Amount("goldToGod","金币换 GOD",n=>Apply(CurrencyScope.Operation.GoldToGod,n));Amount("godToGold","GOD 换金币",n=>Apply(CurrencyScope.Operation.GodToGold,n));
  Add("作用范围","",()=>{if(window is KingdomWindow){scope=1-scope;status="已切换作用范围";}else status="作用于当前选中对象";});
  Amount("grant","下拨金币",n=>Transfer(true,n));Amount("return","上缴金币",n=>Transfer(false,n));Amount("stimulate","经济投入",n=>{var c=City();var k=Nation();status=NativeLifecycle.Stimulate(c??(k==null?null:k.capital),n)?"已投入现有金币":"金币或资源不足";});
  Add("汇率设置","世界法则中修改");Add("存档","立即保存 ▸",()=>{GodWorldRuntime.Flush();NativeLifecycle.SaveGame();status="已请求原生存档";});
 }
 void Run(Action action){try{if(!System.Object.ReferenceEquals(selected,Selection()))throw new Exception("对象已切换，请重新输入数量");action();}catch(Exception e){status=e.Message;Debug.LogWarning("GodWorld currency action: "+e);}Refresh();}
 Actor Person(){var u=window as UnitWindow;if(u==null)return null;return (Actor)typeof(UnitWindow).GetMethod("get_actor",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(u,null);}
 Kingdom Nation(){var w=window as KingdomWindow;return w!=null?w.getMetaObject():GodWorldRuntime.Nation(City());}
 City City(){var w=window as CityWindow;if(w!=null)return w.getMetaObject();var a=Person();return a==null?null:a.getCity();}
 object Selection(){if(window is UnitWindow)return Person();if(window is KingdomWindow)return Nation();return City();}
 List<City> Cities(){return window is KingdomWindow?CurrencyScope.Cities(null,Nation()):CurrencyScope.Cities(City(),null);}
 static string F(double n){return n.ToString("G12",System.Globalization.CultureInfo.InvariantCulture);}
 void Apply(CurrencyScope.Operation op,double amount){bool ok;var a=Person();if(a!=null){switch(op){case CurrencyScope.Operation.MintGold:ok=NativeLifecycle.IssuePersonal(a,false,amount);break;case CurrencyScope.Operation.BurnGold:ok=NativeLifecycle.IssuePersonal(a,false,-amount);break;case CurrencyScope.Operation.MintGod:ok=NativeLifecycle.IssuePersonal(a,true,amount);break;case CurrencyScope.Operation.BurnGod:ok=NativeLifecycle.IssuePersonal(a,true,-amount);break;default:ok=NativeLifecycle.ExchangePersonal(a,op==CurrencyScope.Operation.GoldToGod,amount);break;}}
  else if(window is KingdomWindow&&scope==0)ok=NativeLifecycle.NationalOperation(Nation(),op,amount);else ok=CurrencyScope.Apply(Cities(),op,amount);
  status=ok?"已完成":"余额或容量不足，未执行";
 }
 void Transfer(bool down,double amount){var a=Person();var c=City();var k=Nation();if(a==null){status=NativeLifecycle.NationalGrant(k,c??(k==null?null:k.capital),amount,down)?"转账完成，总量不变":"转账失败";return;}if(c==null){status="没有所属城市";return;}var d=(ActorData)a.getData();int n=(int)amount;bool ok;if(down){ok=n<=Int32.MaxValue-d.money&&GodWorldRuntime.TryDebitPublic(c,n);if(ok)d.money+=n;}else{ok=d.money>=n&&GodWorldRuntime.CreditPublic(c,n);if(ok)d.money-=n;}status=ok?"转账完成，总量不变":"转账失败";}
 void Refresh(){if(rows.Count==0||window==null)return;var target=Selection();if(!System.Object.ReferenceEquals(selected,target)){selected=target;scope=0;foreach(var field in inputs.Values)field.SetTextWithoutNotify("1000");status="已切换对象，各项数量已重置";}
  var a=Person();var k=Nation();var cities=Cities();bool national=window is KingdomWindow&&scope==0;
  double gold=a!=null?((ActorData)a.getData()).money:national?NativeLifecycle.NationalBalance(k,false):CurrencyScope.Total(cities,false),god=a!=null?NativeLifecycle.PersonalGod(a):national?NativeLifecycle.NationalBalance(k,true):CurrencyScope.Total(cities,true);
  rows[0].value.text=a!=null?"当前人物":national?"独立国库":window is KingdomWindow?"全国城市合计":"当前城市";rows[1].value.text=F(gold);rows[2].value.text=F(god);rows[3].value.text="1 GOD = "+F(GodWorldRuntime.GetRate())+" 金币";rows[4].value.text=status;
  rows[17].value.text=window is KingdomWindow?(scope==0?"独立国库 ▸":"全国城市 ▸"):a!=null?"当前人物":"当前城市";
  rows[18].name_text.text=a!=null?"城市 → 个人数量":"国库 → 城市数量";rows[19].name_text.text=a!=null?"城市 → 个人":"国库 → 城市";rows[20].name_text.text=a!=null?"个人 → 城市数量":"城市 → 国库数量";rows[21].name_text.text=a!=null?"个人 → 城市":"城市 → 国库";
  LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
 }
}
