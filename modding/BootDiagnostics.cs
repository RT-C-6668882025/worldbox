using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using UnityEngine;
// Local-only boot diagnostics: no networking and no world/save mutation.
public static class BootDiagnostics {
 static bool installed;static string stage="LoadingScreen.Awake",error="",file="";static DateTime lastWrite=DateTime.MinValue;static LoadingScreen screen;
 static readonly FieldInfo LastAction=typeof(SmoothLoader).GetField("_last_action",BindingFlags.Static|BindingFlags.NonPublic);
 public static void Begin(LoadingScreen loading){screen=loading;if(installed)return;installed=true;Application.logMessageReceived+=Logged;try{RuntimeNativeLibraries.Initialize();}catch(Exception e){Logged(e.ToString(),"Native runtime library mapping",LogType.Exception);throw;}try{RuntimeNativeLibraries.CheckFileAccess();file=Path.Combine(Application.persistentDataPath,"godworld-startup.log");File.WriteAllText(file,"GodWorld 0.4.5 diagnostic boot\n"+DateTime.UtcNow.ToString("O")+"\n");}catch(Exception e){file="";Logged(e.ToString(),"Filesystem preflight",LogType.Exception);throw;}Write("BEGIN LoadingScreen.Awake");}
 static void Write(string value){if(file.Length==0)return;try{if(File.Exists(file)&&new FileInfo(file).Length>131072)return;File.AppendAllText(file,DateTime.UtcNow.ToString("O")+" "+value+"\n");}catch{}}
 static void Logged(string message,string trace,LogType type){if(type!=LogType.Exception&&type!=LogType.Error&&type!=LogType.Assert)return;if(error.Length>0)return;error=message+"\n"+trace;if(error.Length>3500)error=error.Substring(0,3500);Write("ERROR "+stage+"\n"+error);Paint(screen);}
 public static void Run(MapLoaderAction action){var last=LastAction==null?null:LastAction.GetValue(null);var id=last==null?null:last.GetType().GetField("id",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);stage=id==null?"unnamed loader action":Convert.ToString(id.GetValue(last));Write("START "+stage);try{action();Write("DONE "+stage);}catch(Exception e){error=e.ToString();Write("FAILED "+stage+"\n"+error);Paint(screen);throw;}}
 public static void Paint(LoadingScreen loading){if(loading==null)return;try{var text=loading.loadingHelperText??loading.percents;if(text==null)return;bool failed=error.Length>0;text.gameObject.SetActive(true);var rect=text.rectTransform;rect.SetParent(loading.transform,false);rect.SetAsLastSibling();rect.anchorMin=new Vector2(.03f,.05f);rect.anchorMax=new Vector2(.97f,.72f);rect.offsetMin=Vector2.zero;rect.offsetMax=Vector2.zero;text.supportRichText=false;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Overflow;text.alignment=TextAnchor.UpperLeft;text.fontSize=failed?14:16;text.color=failed?new Color(1,.45f,.3f,1):Color.white;text.text="GODWorld 0.4.5 诊断\n阶段："+stage+"\n"+RuntimeNativeLibraries.Status+"\n"+(failed?error:"若卡住，请截取此处的阶段文字");if(DateTime.UtcNow-lastWrite>TimeSpan.FromSeconds(10)){lastWrite=DateTime.UtcNow;Write("WAIT "+stage);}}catch{}}
}

// Run before any System.IO operation: Android installs these helpers beside libmonosgen.
public static class RuntimeNativeLibraries {
 static bool ready;public static string Status="native bootstrap not started";
 [DllImport("libdl.so",CallingConvention=CallingConvention.Cdecl)]static extern IntPtr dlopen(string name,int flags);
 [DllImport("libdl.so",CallingConvention=CallingConvention.Cdecl)]static extern IntPtr dlerror();
 [DllImport("libdl.so",CallingConvention=CallingConvention.Cdecl)]static extern IntPtr dlsym(IntPtr library,string symbol);
 [DllImport("libmonosgen-2.0.so",CallingConvention=CallingConvention.Cdecl)]static extern IntPtr mono_get_corlib();
 static IntPtr nativeHandle;
 [DllImport("libmonosgen-2.0.so",CallingConvention=CallingConvention.Cdecl)]
 static extern void mono_dllmap_insert(IntPtr image,[MarshalAs(UnmanagedType.LPStr)] string source,string sourceEntry,[MarshalAs(UnmanagedType.LPStr)] string target,string targetEntry);
 public static void Initialize(){
  if(ready)return;Status="platform="+Application.platform;
  if(Application.platform!=RuntimePlatform.Android){Status+="; native mapping skipped";return;}
  string directory;
  using(var player=new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
  using(var activity=player.GetStatic<AndroidJavaObject>("currentActivity"))
  using(var info=activity.Call<AndroidJavaObject>("getApplicationInfo"))directory=info.Get<string>("nativeLibraryDir");
  if(String.IsNullOrEmpty(directory))throw new InvalidOperationException("Android nativeLibraryDir is empty");
  Status+="; dir="+directory;
  nativeHandle=dlopen(directory+"/libmono-native.so",2|256);
  if(nativeHandle==IntPtr.Zero)throw new DllNotFoundException("dlopen libmono-native.so: "+Marshal.PtrToStringAnsi(dlerror()));
  if(dlsym(nativeHandle,"SystemNative_LChflagsCanSetHiddenFlag")==IntPtr.Zero)throw new EntryPointNotFoundException("libmono-native.so: SystemNative_LChflagsCanSetHiddenFlag");
  Status+="; dlopen + symbol OK";
  Map("System.Native",directory,"libmono-native.so");
  mono_dllmap_insert(mono_get_corlib(),"System.Native",null,directory+"/libmono-native.so",null);
  Map("System.Net.Security.Native",directory,"libmono-native.so");
  Map("MonoPosixHelper",directory,"libMonoPosixHelper.so");
  Map("libmono-btls-shared",directory,"libmono-btls-shared.so");
  ready=true;Status+="; corlib mapping OK";
 }
 public static void CheckFileAccess(){File.Exists(Application.persistentDataPath+"/godworld-startup.log");Status+="; File.Exists OK";}
 static void Map(string source,string directory,string file){mono_dllmap_insert(IntPtr.Zero,source,null,directory+"/"+file,null);}
}
