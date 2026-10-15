using System;
using UnityEngine;
using UnityEngine.UI;
// Native row prefab and input components, appended inside the selected organization's panel.
public sealed class GodOrganizationEditor:MonoBehaviour {
 StatsWindow window;object organization;KeyValueField status;InputField nameInput,idInput,traitInput,bonusInput,taxInput,tributeInput;string message="输入ID后点击操作";
 public void Bind(StatsWindow w){var selected=NativeOrganizations.Selected(w);if(!System.Object.ReferenceEquals(selected,organization)){foreach(Transform child in transform){child.gameObject.SetActive(false);UnityEngine.Object.Destroy(child.gameObject);}status=null;}window=w;organization=selected;}
 KeyValueField Row(string label,string value,Action action=null){var prefab=Resources.Load<GameObject>("ui/KeyValueFieldStats");if(prefab==null)throw new Exception("Native row missing");var go=UnityEngine.Object.Instantiate(prefab,transform,false);var row=go.GetComponent<KeyValueField>();row.name_text.text=label;row.value.text=value;if(row.icon!=null)row.icon.gameObject.SetActive(false);var layout=go.GetComponent<LayoutElement>()??go.AddComponent<LayoutElement>();layout.minHeight=28;layout.preferredHeight=28;if(action!=null)NativeEconomyUI.BindAction(row,()=>Run(action));go.SetActive(true);return row;}
 InputField Input(string label,string value,bool number){var row=Row(label,value);var input=NativeCurrencyControls.Input(row,value,number);input.contentType=number?InputField.ContentType.IntegerNumber:InputField.ContentType.Standard;input.characterLimit=64;input.SetTextWithoutNotify(value);return input;}
 void OnDisable(){foreach(var input in new[]{nameInput,idInput,traitInput,bonusInput,taxInput,tributeInput})NativeCurrencyControls.Blur(input);}
 void LateUpdate(){if(window!=null&&!System.Object.ReferenceEquals(organization,NativeOrganizations.Selected(window)))gameObject.SetActive(false);}
 void Run(Action action){if(!gameObject.activeInHierarchy)return;try{if(!System.Object.ReferenceEquals(organization,NativeOrganizations.Selected(window)))throw new Exception("组织已切换，请重新打开编辑器");action();GodWorldRuntime.Flush();}catch(Exception e){while(e.InnerException!=null)e=e.InnerException;message=e.Message;}if(status!=null)status.value.text=message;}
 long Id(){long n;if(!Int64.TryParse(idInput.text,out n)||n<0)throw new Exception("请输入有效ID");return n;}
 int Number(InputField input,int min,int max){int n;if(!Int32.TryParse(input.text,out n)||n<min||n>max)throw new Exception("请输入 "+min+" 至 "+max);return n;}
 void Edit(string action,long id,string value){message=NativeOrganizations.Edit(organization,action,id,value);}
 void OnEnable(){if(status!=null)return;try{var d=NativeOrganizations.Data(organization);if(d==null)return;var kind=organization.GetType().Name;status=Row("组织编辑",message);nameInput=Input("名称",d.name,false);Row("保存名称","应用 ▸",()=>Edit("name",0,nameInput.text));
  bonusInput=Input("手动加成百分比",NativeOrganizations.Manual(organization).ToString(),true);Row("排名加成：自动","同类第一 +15% ▸",()=>{NativeOrganizations.Configure(organization,0,Number(bonusInput,0,25));message="已启用动态竞争";});Row("排名加成：手动","应用0至25% ▸",()=>{NativeOrganizations.Configure(organization,2,Number(bonusInput,0,25));message="已保存手动加成";});Row("排名加成：关闭","关闭 ▸",()=>{NativeOrganizations.Configure(organization,1,0);message="已关闭此组织加成";});
  Row("加成规则","分类生效，个人总加成最多30%");
  idInput=Input(kind=="Alliance"?"目标王国ID":"目标人物ID／归属王国ID","",true);
  if(kind=="Alliance"){Row("加入王国","应用ID ▸",()=>Edit("alliance+",Id(),""));Row("移出王国","应用ID ▸",()=>Edit("alliance-",Id(),""));}
  else {Row("加入成员","应用人物ID ▸",()=>Edit("member",Id(),""));if(kind=="City"||kind=="Kingdom"||kind=="Clan"||kind=="Family")Row("设为领袖","本组织成年成员 ▸",()=>Edit("leader",Id(),""));}
  if(kind=="City")Row("城市归属王国","应用王国ID ▸",()=>Edit("cityKingdom",Id(),""));
  if(kind!="City"&&kind!="Family"&&kind!="Alliance"){traitInput=Input("原生特质ID","",false);Row("添加组织特质","应用 ▸",()=>Edit("trait+",0,traitInput.text));Row("移除组织特质","应用 ▸",()=>Edit("trait-",0,traitInput.text));Row("特质选择","也可直接使用原生特质编辑器");}
  if(kind=="Kingdom"){int local,tribute;d.get("godworld.tax.local",out local,-1);d.get("godworld.tax.tribute",out tribute,-1);taxInput=Input("地方税率%（-1原生）",local.ToString(),true);tributeInput=Input("进贡税率%（-1原生）",tribute.ToString(),true);Row("保存税率","应用 ▸",()=>{int a=Number(taxInput,-1,100),b=Number(tributeInput,-1,100);Edit("taxLocal",a,"");Edit("taxTribute",b,"");});}
  Row("保存世界","打开原生存档 ▸",()=>{NativeLifecycle.SaveGame();message="已打开存档";});
 }catch(Exception e){Debug.LogWarning("GodWorld organization editor: "+e);}}
}
