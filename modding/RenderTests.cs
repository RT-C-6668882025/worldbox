using System;using System.IO;using System.Linq;
namespace UnityEngine {
 public struct Color32 {public byte r,g,b,a;public Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;}}
 public struct Rect {public float width,height;public Rect(float x,float y,float w,float h){width=w;height=h;}}
 public struct Vector2{public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
 public enum TextureFormat{RGBA32}public enum FilterMode{Point}public enum TextureWrapMode{Clamp}
 public class Texture2D{public string name;public FilterMode filterMode;public TextureWrapMode wrapMode;public Color32[] pixels;public bool readable=true;public Texture2D(int w,int h,TextureFormat f,bool b){}public void SetPixels32(Color32[] p){pixels=p;}public void Apply(bool a,bool b){if(b)readable=false;}}
 public class Sprite {public Rect rect;public Vector2 pivot;public float pixelsPerUnit;public string name;public Texture2D texture;public static Sprite Create(Texture2D t,Rect r,Vector2 p,float units){return new Sprite{texture=t,rect=r,pivot=new Vector2(p.x*r.width,p.y*r.height),pixelsPerUnit=units};}}
}
class RenderTests {
 static void Main(string[] args){string dir=args[0];Directory.CreateDirectory(dir);int checks=0;var old=new UnityEngine.Sprite{rect=new UnityEngine.Rect(0,0,40,48),pivot=new UnityEngine.Vector2(20,7),pixelsPerUnit=1};var b=new Building();string last="";
 for(int tier=1;tier<=6;tier++){b.data.set("godworld.facility.tier",tier);var sprite=IndustrialAppearance.SpriteFor(b,old);if(sprite==old||sprite.rect.width!=40||sprite.rect.height!=48||sprite.pivot.x!=20||sprite.pivot.y!=7)throw new Exception("Native sprite geometry changed");checks++;
 if(!sprite.texture.readable)throw new Exception("Native recoloring requires readable texture pixels");checks++;int opaque=sprite.texture.pixels.Count(p=>p.a>0);if(opaque<200||opaque>=1920)throw new Exception("Invalid transparent silhouette");checks++;
 string signature=String.Join(",",sprite.texture.pixels.Select(p=>p.r+":"+p.g+":"+p.b+":"+p.a));if(signature==last)throw new Exception("Facility tiers have identical art");last=signature;checks++;
 if(IndustrialAppearance.SpriteFor(b,old)!=sprite)throw new Exception("Sprite cache missed");checks++;
 using(var f=File.Create(Path.Combine(dir,"facility-"+tier+".pam"))){var header=System.Text.Encoding.ASCII.GetBytes("P7\nWIDTH 40\nHEIGHT 48\nDEPTH 4\nMAXVAL 255\nTUPLTYPE RGB_ALPHA\nENDHDR\n");f.Write(header,0,header.Length);for(int y=47;y>=0;y--)for(int x=0;x<40;x++){var p=sprite.texture.pixels[y*40+x];f.WriteByte(p.r);f.WriteByte(p.g);f.WriteByte(p.b);f.WriteByte(p.a);}}
 }
 b.data.set("godworld.facility.tier",0);if(IndustrialAppearance.SpriteFor(b,old)!=old)throw new Exception("Unfunded native building altered");checks++;b.data.set("godworld.facility.tier",4);b.state=BuildingState.Ruins;if(IndustrialAppearance.SpriteFor(b,old)!=old)throw new Exception("Native ruins were hidden");checks++;b.state=BuildingState.Normal;b.abandoned=true;if(IndustrialAppearance.SpriteFor(b,old)!=old)throw new Exception("Abandoned native graphics were hidden");checks++;Console.WriteLine(checks+" headless render checks passed");}
}
