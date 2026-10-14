using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;
// Appended to native pooled stats lists; no new canvas, overlay, or input system.
public static class NativeEconomyUI {
 class OwnedClick {public UnityAction callback;}
 static readonly System.Runtime.CompilerServices.ConditionalWeakTable<KeyValueField,OwnedClick> clicks=new System.Runtime.CompilerServices.ConditionalWeakTable<KeyValueField,OwnedClick>();
 public static bool TryClick(KeyValueField row){if(row==null)return false;OwnedClick owner;if(!clicks.TryGetValue(row,out owner)||owner.callback==null||row.on_click_value!=owner.callback)return false;owner.callback();return true;}
 static readonly double[] amounts={100,1000,10000,100000,1000000};
 static int index=1;
 static readonly MethodInfo showRow=typeof(StatsWindow).GetMethod("showStatRow",BindingFlags.Instance|BindingFlags.NonPublic,null,new Type[]{typeof(string),typeof(object),typeof(string),typeof(MetaType),typeof(long),typeof(bool),typeof(string),typeof(string),typeof(TooltipDataGetter),typeof(bool)},null);
 static readonly MethodInfo actorGetter=typeof(UnitWindow).GetMethod("get_actor",BindingFlags.Instance|BindingFlags.NonPublic);
 public static void ShowCity(CityWindow w){if(w!=null)Append(w,w.getMetaObject());}
 public static void ShowKingdom(KingdomWindow w){if(w!=null){Kingdom k=w.getMetaObject();if(k!=null)Append(w,k.capital);}}
 public static void ShowUnit(UnitWindow w){if(w==null || actorGetter==null)return;try{Actor a=(Actor)actorGetter.Invoke(w,null);if(a!=null)Row(w,"个人能力",GodWorldRuntime.DescribeActor(a),null);}catch(Exception e){Debug.LogWarning("GodWorld individual UI: "+e.Message);}}
 public static void ShowFamily(FamilyWindow w){if(w==null)return;try{Family f=w.getMetaObject();if(f!=null)Row(w,"家族财富",GodWorldRuntime.DescribeFamily(f),null);}catch(Exception e){Debug.LogWarning("GodWorld family UI: "+e.Message);}}
 static void Append(StatsWindow w,City c){if(c==null || showRow==null)return;try{
  Row(w,"金币",Fmt(GodWorldRuntime.GetGold(c)),null);
  Row(w,"god",Fmt(GodWorldRuntime.GetGod(c)),null);
  Row(w,"发展阶段",GodWorldRuntime.Describe(c),null);
  Row(w,"操作数量",Fmt(amounts[index])+" ▸",()=>{index=(index+1)%amounts.Length;});
  Row(w,"发行金币","+ "+Fmt(amounts[index]),()=>GodWorldRuntime.Issue(c,false,amounts[index]));
  Row(w,"回收金币","− "+Fmt(amounts[index]),()=>GodWorldRuntime.Issue(c,false,-amounts[index]));
  Row(w,"发行 god","+ "+Fmt(amounts[index]),()=>GodWorldRuntime.Issue(c,true,amounts[index]));
  Row(w,"回收 god","− "+Fmt(amounts[index]),()=>GodWorldRuntime.Issue(c,true,-amounts[index]));
  Row(w,"金币 → god",Fmt(amounts[index])+" 金币",()=>GodWorldRuntime.Exchange(c,true,amounts[index]));
  Row(w,"god → 金币",Fmt(amounts[index])+" god",()=>GodWorldRuntime.Exchange(c,false,amounts[index]));
  Row(w,"汇率","1 god = "+Fmt(GodWorldRuntime.GetRate())+" 金币",null);
  Row(w,"提高汇率","× 1.25",()=>GodWorldRuntime.SetRate(GodWorldRuntime.GetRate()*1.25));
  Row(w,"降低汇率","÷ 1.25",()=>GodWorldRuntime.SetRate(GodWorldRuntime.GetRate()/1.25));
 }catch(Exception e){Debug.LogWarning("GodWorld currency UI: "+e.Message);}}
 static string Fmt(double n){return n.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture);}
 static void Row(StatsWindow w,string label,string value,Action action){
  KeyValueField row=(KeyValueField)showRow.Invoke(w,new object[]{label,value,null,default(MetaType),0L,false,null,null,null,false});
  // Native row pools reuse objects: replace callbacks every refresh, including read-only rows.
  row.on_click_value=action==null?null:(UnityAction)(()=>{try{action();w.updateStats();}catch(Exception e){Debug.LogWarning("GodWorld currency action: "+e.Message);}});
  OwnedClick owner;if(!clicks.TryGetValue(row,out owner)){owner=new OwnedClick();clicks.Add(row,owner);}owner.callback=row.on_click_value;
 }
}
