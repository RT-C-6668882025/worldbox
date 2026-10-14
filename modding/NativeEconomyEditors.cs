using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
// Reuses native editor/resource content and native KeyValueFieldStats prefab.
public static class NativeEconomyEditors {
 public static void Attach(StatsWindow window){
  try{
   bool kingdom=window is KingdomWindow;if(!kingdom&&!(window is CityWindow))return;
   Transform parent=null;string name=kingdom?"content_traits_editor":"content_resources";
   foreach(var t in window.GetComponentsInChildren<Transform>(true))if(t.name==name){parent=t;break;}
   if(parent==null){Debug.LogWarning("GodWorld editor content missing: "+name);return;}
   var existing=parent.Find("godworld_currency_editor");
   if(existing!=null){existing.GetComponent<GodCurrencyEditorPanel>().Bind(window);return;}
   var go=new GameObject("godworld_currency_editor",typeof(RectTransform));
   go.SetActive(false);go.transform.SetParent(parent,false);go.transform.SetAsFirstSibling();
   var layout=go.AddComponent<VerticalLayoutGroup>();layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;layout.spacing=2;
   var fit=go.AddComponent<ContentSizeFitter>();fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
   var panel=go.AddComponent<GodCurrencyEditorPanel>();panel.Bind(window);go.SetActive(true);
  }catch(Exception e){Debug.LogWarning("GodWorld editor attach: "+e);}
 }
}
public sealed class GodCurrencyEditorPanel:MonoBehaviour {
 static readonly double[] amounts={100,1000,10000,100000,1000000};
 readonly List<KeyValueField> rows=new List<KeyValueField>();StatsWindow window;int amountIndex=1;float nextRefresh;string status="点击右侧数值操作";
 public void Bind(StatsWindow w){window=w;if(rows.Count>0)Refresh();}
 void OnEnable(){try{if(rows.Count==0)Create();Refresh();}catch(Exception e){Debug.LogWarning("GodWorld editor UI: "+e);}}
 void Update(){if(Time.unscaledTime<nextRefresh)return;nextRefresh=Time.unscaledTime+1;try{Refresh();}catch(Exception e){Debug.LogWarning("GodWorld editor refresh: "+e);}}
 void Create(){
  var prefab=Resources.Load<GameObject>("ui/KeyValueFieldStats");if(prefab==null)throw new Exception("Native currency row prefab missing");
  for(int i=0;i<14;i++){
   var obj=UnityEngine.Object.Instantiate(prefab,transform,false);obj.name="godworld_currency_"+i;
   var row=obj.GetComponent<KeyValueField>();if(row==null)throw new Exception("Native currency row component missing");
   var size=obj.GetComponent<LayoutElement>();if(size==null)size=obj.AddComponent<LayoutElement>();size.minHeight=22;size.preferredHeight=22;
   if(row.icon!=null)row.icon.gameObject.SetActive(false);rows.Add(row);obj.SetActive(true);
  }
 }
 List<City> Scope(){var kw=window as KingdomWindow;if(kw!=null)return CurrencyScope.Cities(null,kw.getMetaObject());var cw=window as CityWindow;return CurrencyScope.Cities(cw==null?null:cw.getMetaObject(),null);}
 static string F(double n){return n.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture);}
 void Row(int i,string label,string value,Action action){var r=rows[i];r.name_text.text=label;r.value.text=value;NativeEconomyUI.BindAction(r,action==null?null:(UnityEngine.Events.UnityAction)(()=>{try{action();Refresh();}catch(Exception e){status="操作失败";Debug.LogWarning("GodWorld editor action: "+e);Refresh();}}));}
 void Apply(CurrencyScope.Operation op){status=CurrencyScope.Apply(Scope(),op,amounts[amountIndex])?"已完成":"余额不足或超出上限";}
 void Refresh(){if(rows.Count==0||window==null)return;var cities=Scope();double amount=amounts[amountIndex];
  Row(0,window is KingdomWindow?"双币 · 全国":"双币 · 本村镇",status,null);
  Row(1,"金币",F(CurrencyScope.Total(cities,false)),null);Row(2,"god",F(CurrencyScope.Total(cities,true)),null);
  Row(3,"操作数量",F(amount)+" ▸",()=>{amountIndex=(amountIndex+1)%amounts.Length;});
  Row(4,"发行金币","+ "+F(amount),()=>Apply(CurrencyScope.Operation.MintGold));
  Row(5,"回收金币","− "+F(amount),()=>Apply(CurrencyScope.Operation.BurnGold));
  Row(6,"发行 god","+ "+F(amount),()=>Apply(CurrencyScope.Operation.MintGod));
  Row(7,"回收 god","− "+F(amount),()=>Apply(CurrencyScope.Operation.BurnGod));
  Row(8,"金币 → god",F(amount)+" 金币",()=>Apply(CurrencyScope.Operation.GoldToGod));
  Row(9,"god → 金币",F(amount)+" god",()=>Apply(CurrencyScope.Operation.GodToGold));
  Row(10,"汇率","1 god = "+F(GodWorldRuntime.GetRate())+" 金币",null);
  Row(11,"提高汇率","× 1.25",()=>GodWorldRuntime.SetRate(GodWorldRuntime.GetRate()*1.25));
  Row(12,"降低汇率","÷ 1.25",()=>GodWorldRuntime.SetRate(GodWorldRuntime.GetRate()/1.25));
  Row(13,"作用范围",window is KingdomWindow?cities.Count+" 城市 · 发行按人口分配":"当前村镇 · 汇率全世界共用",null);
  LayoutRebuilder.MarkLayoutForRebuild((RectTransform)transform);
 }
}
