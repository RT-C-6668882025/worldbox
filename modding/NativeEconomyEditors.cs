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
 InputField amountInput;bool godSelected,exchangeOpen;
 StatsWindow window;object selected;float nextRefresh;string status="输入数量，再点发行或回收";
 public void Bind(StatsWindow w){window=w;if(rows.Count>0)Refresh();}
 void OnEnable(){try{if(rows.Count==0)Create();Refresh();}catch(Exception e){Debug.LogWarning("GodWorld editor UI: "+e);}}
 void Update(){if(Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+1;try{Refresh();}catch(Exception e){Debug.LogWarning("GodWorld editor refresh: "+e);}}
 KeyValueField Add(string label,string value,Action action=null){var prefab=Resources.Load<GameObject>("ui/KeyValueFieldStats");if(prefab==null)throw new Exception("Native currency row prefab missing");var obj=UnityEngine.Object.Instantiate(prefab,transform,false);obj.name="godworld_currency_"+rows.Count;var row=obj.GetComponent<KeyValueField>();var size=obj.GetComponent<LayoutElement>()??obj.AddComponent<LayoutElement>();size.minHeight=36;size.preferredHeight=36;if(row.icon!=null)row.icon.gameObject.SetActive(false);
  foreach(var text in new[]{row.name_text,row.value}){text.supportRichText=false;text.resizeTextForBestFit=true;text.resizeTextMinSize=9;text.resizeTextMaxSize=14;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;}
  row.name_text.text=label;row.value.text=value;NativeEconomyUI.BindAction(row,action==null?null:(UnityEngine.Events.UnityAction)(()=>Run(action)));rows.Add(row);obj.SetActive(true);return row;
 }
 void Create(){Add("货币","");Add("切换币种","",()=>{godSelected=!godSelected;status="已选择"+(godSelected?"GOD":"金币");});Add("余额","");var row=Add("自定义数量","1000");amountInput=row.value.gameObject.AddComponent<InputField>();amountInput.textComponent=row.value;amountInput.text="1000";amountInput.contentType=InputField.ContentType.IntegerNumber;amountInput.lineType=InputField.LineType.SingleLine;amountInput.characterLimit=10;row.value.raycastTarget=true;
  Add("发行","发行 ▸",()=>Apply(godSelected?CurrencyScope.Operation.MintGod:CurrencyScope.Operation.MintGold,Amount()));
  Add("回收","回收 ▸",()=>Apply(godSelected?CurrencyScope.Operation.BurnGod:CurrencyScope.Operation.BurnGold,Amount()));
  Add("操作结果",status);Add("兑换","展开 ▸",()=>exchangeOpen=!exchangeOpen);
  Add("", "兑换 ▸",()=>Apply(godSelected?CurrencyScope.Operation.GodToGold:CurrencyScope.Operation.GoldToGod,Amount()));
 }
 double Amount(){double amount;if(!CurrencyInput.TryAmount(amountInput.text,out amount))throw new Exception("请输入1至2147483647的整数");return amount;}
 void Run(Action action){try{if(!System.Object.ReferenceEquals(selected,Selection()))throw new Exception("对象已切换，请重新输入数量");action();}catch(Exception e){status=e.Message;Debug.LogWarning("GodWorld currency action: "+e);}Refresh();}
 Actor Person(){var u=window as UnitWindow;if(u==null)return null;return (Actor)typeof(UnitWindow).GetMethod("get_actor",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(u,null);}
 Kingdom Nation(){var w=window as KingdomWindow;return w!=null?w.getMetaObject():GodWorldRuntime.Nation(City());}
 City City(){var w=window as CityWindow;if(w!=null)return w.getMetaObject();var a=Person();return a==null?null:a.getCity();}
 object Selection(){if(window is UnitWindow)return Person();if(window is KingdomWindow)return Nation();return City();}
 List<City> Cities(){return window is KingdomWindow?CurrencyScope.Cities(null,Nation()):CurrencyScope.Cities(City(),null);}
 static string F(double n){return n.ToString("G12",System.Globalization.CultureInfo.InvariantCulture);}
 void Apply(CurrencyScope.Operation op,double amount){bool ok;var a=Person();if(a!=null){switch(op){case CurrencyScope.Operation.MintGold:ok=NativeLifecycle.IssuePersonal(a,false,amount);break;case CurrencyScope.Operation.BurnGold:ok=NativeLifecycle.IssuePersonal(a,false,-amount);break;case CurrencyScope.Operation.MintGod:ok=NativeLifecycle.IssuePersonal(a,true,amount);break;case CurrencyScope.Operation.BurnGod:ok=NativeLifecycle.IssuePersonal(a,true,-amount);break;default:ok=NativeLifecycle.ExchangePersonal(a,op==CurrencyScope.Operation.GoldToGod,amount);break;}}
  else if(window is KingdomWindow)ok=NativeLifecycle.NationalOperation(Nation(),op,amount);else ok=CurrencyScope.Apply(Cities(),op,amount);
  status=ok?"已完成":"余额或容量不足，未执行";
 }
 void Refresh(){if(rows.Count==0||window==null)return;var target=Selection();if(!System.Object.ReferenceEquals(selected,target)){selected=target;godSelected=false;exchangeOpen=false;amountInput.SetTextWithoutNotify("1000");status="输入数量，再点发行或回收";}
  var a=Person();var k=Nation();var cities=Cities();bool national=window is KingdomWindow;
  double balance=a!=null?(godSelected?NativeLifecycle.PersonalGod(a):((ActorData)a.getData()).money):national?NativeLifecycle.NationalBalance(k,godSelected):CurrencyScope.Total(cities,godSelected);
  rows[0].value.text=a!=null?"当前人物":national?"当前国库":"当前城市";rows[1].value.text=godSelected?"GOD ▸":"金币 ▸";rows[2].value.text=F(balance)+(godSelected?" GOD":" 金币");rows[6].value.text=status;
  rows[7].value.text=exchangeOpen?"收起 ▾":"展开 ▸";rows[8].gameObject.SetActive(exchangeOpen);rows[8].name_text.text=godSelected?"GOD → 金币":"金币 → GOD";rows[8].value.text="兑换 ▸";
  LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
 }
}
