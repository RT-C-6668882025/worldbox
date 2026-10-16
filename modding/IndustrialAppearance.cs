using System;
using System.Collections.Generic;
using UnityEngine;
// Native-sized pixel sprites: original collision, placement, animation and render
// pipeline remain intact. Only funded individual buildings receive an upgrade.
public static class IndustrialAppearance {
 static readonly Dictionary<string,Sprite> cache=new Dictionary<string,Sprite>();
 static Color32 C(byte r,byte g,byte b){return new Color32(r,g,b,255);}
 static readonly Color32 outline=C(42,42,49),wall=C(162,158,143),side=C(112,111,111),roof=C(88,101,112),light=C(203,211,198),glass=C(100,174,188),dark=C(50,73,88),brick=C(123,77,61),gold=C(224,190,97);
 static void Rect(Color32[] p,int x,int y,int w,int h,Color32 c){for(int j=Math.Max(0,y);j<Math.Min(48,y+h);j++)for(int i=Math.Max(0,x);i<Math.Min(40,x+w);i++)p[j*40+i]=c;}
 static void Window(Color32[] p,int x,int y){Rect(p,x,y,3,4,dark);Rect(p,x,y+1,2,3,glass);Rect(p,x,y,3,1,light);}
 static Color32[] Pixels(int tier){var p=new Color32[40*48];Rect(p,3,3,34,4,outline);Rect(p,5,6,30,18,outline);Rect(p,6,7,28,16,wall);Rect(p,27,7,7,16,side);Rect(p,7,23,26,3,roof);Rect(p,9,26,22,2,light);
  for(int x=9;x<26;x+=7)Window(p,x,13);Rect(p,19,7,5,7,dark);Rect(p,20,8,3,5,brick);
  if(tier<=2){Rect(p,28,25,5,16,outline);Rect(p,29,26,3,14,brick);Rect(p,27,39,7,3,light);Rect(p,11,24,4,10,outline);Rect(p,12,25,2,8,side);Rect(p,10,32,6,2,roof);Rect(p,6,25,4,2,light);if(tier==2){Rect(p,9,27,14,2,roof);for(int x=8;x<23;x+=5){Rect(p,x,24,3,3,glass);Rect(p,x,27,2,2,light);}Rect(p,4,10,3,8,brick);}}
  if(tier==3){Rect(p,8,27,17,9,outline);Rect(p,9,28,15,7,glass);for(int x=10;x<24;x+=4)Rect(p,x,28,1,7,dark);Rect(p,9,31,15,1,light);Rect(p,29,25,2,17,outline);Rect(p,26,39,8,1,dark);Rect(p,27,35,6,1,dark);Rect(p,30,25,1,17,gold);}
  if(tier==4){Rect(p,12,25,16,12,outline);Rect(p,13,26,14,10,light);for(int x=15;x<26;x+=5)Window(p,x,29);Rect(p,17,37,6,1,side);Rect(p,19,38,2,4,dark);Rect(p,14,40,10,2,glass);Rect(p,29,24,4,3,glass);}
  if(tier>=5){Rect(p,8,7,24,33,outline);Rect(p,9,8,22,31,light);Rect(p,27,8,4,31,side);Rect(p,7,39,26,3,roof);Rect(p,10,42,20,2,light);for(int y=13;y<37;y+=7)for(int x=11;x<27;x+=6)Window(p,x,y);Rect(p,18,8,5,5,dark);if(tier>=6){Rect(p,13,43,12,2,glass);for(int x=14;x<25;x+=4)Rect(p,x,43,1,2,dark);}}
  return p;
 }
 public static Sprite SpriteFor(Building b,Sprite original){if(b==null||original==null||!b.isAlive()||b.isUnderConstruction()||b.isState(BuildingState.Ruins)||b.isState(BuildingState.Removed)||b.isAbandoned())return original;int tier;((BaseSystemData)b.getData()).get("godworld.facility.tier",out tier,0);if(tier<1||tier>6)return original;
  int w=(int)original.rect.width,h=(int)original.rect.height;if(w<=0||h<=0)return original;string key=tier+":"+w+":"+h+":"+original.pixelsPerUnit+":"+original.pivot;Sprite result;if(cache.TryGetValue(key,out result))return result;
  var source=Pixels(tier);var resized=new Color32[w*h];for(int y=0;y<h;y++)for(int x=0;x<w;x++)resized[y*w+x]=source[Math.Min(47,y*48/h)*40+Math.Min(39,x*40/w)];
  var texture=new Texture2D(w,h,TextureFormat.RGBA32,false);texture.name="godworld_facility_"+tier;texture.filterMode=FilterMode.Point;texture.wrapMode=TextureWrapMode.Clamp;texture.SetPixels32(resized);texture.Apply(false,false);
  result=Sprite.Create(texture,new Rect(0,0,w,h),new Vector2(original.pivot.x/w,original.pivot.y/h),original.pixelsPerUnit);result.name=texture.name;cache[key]=result;return result;
 }
 public static void Assign(City c,GodEconomyState s){
  if(c==null)return;int stage=s.laboratory>0?4:s.powerPlant>0?3:s.factory>0?2:s.steamPlant>0?1:0;if(stage==0)return;
  var field=typeof(Building).GetField("asset",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic);
  Building workshop=null;foreach(var b in c.buildings){if(b==null||!b.isAlive()||b.isUnderConstruction())continue;var asset=(BuildingAsset)field.GetValue(b);if(asset.type=="type_mine"||asset.type=="type_blacksmith"||asset.type=="type_windmill"){workshop=b;break;}}
  if(workshop!=null)((BaseSystemData)workshop.getData()).set("godworld.facility.tier",stage);
  if(s.powerPlant==0||s.power<=0)return;
  foreach(var b in c.buildings){if(b==null||!b.isAlive()||b.isUnderConstruction()||b==workshop)continue;var asset=(BuildingAsset)field.GetValue(b);if(!asset.hasHousingSlots())continue;var data=(BaseSystemData)b.getData();int old;data.get("godworld.facility.tier",out old,0);int next=s.laboratory>0?6:5;if(old>=next)continue;
   int wage=LifecyclePolicy.Wage(s.era,s.inflation);if(c.getResourcesAmount("stone")<3||c.getResourcesAmount("common_metals")<1||!GodWorldRuntime.TryDebitPublic(c,wage*8))break;
   c.takeResource("stone",3);c.takeResource("common_metals",1);GodWorldRuntime.CreditBusiness(c,wage*8);data.set("godworld.facility.tier",next);break;
  }
 }
}
