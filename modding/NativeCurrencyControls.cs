using System;using UnityEngine;using UnityEngine.UI;
// Native Value objects already contain Button (Selectable). InputField must own a separate object.
public static class NativeCurrencyControls {
 public static void Fit(Transform panel,Transform parent){var rect=(RectTransform)panel;var container=parent as RectTransform;float width=container==null?210:container.rect.width;if(width<100)width=210;rect.sizeDelta=new Vector2(width,0);var size=panel.GetComponent<LayoutElement>()??panel.gameObject.AddComponent<LayoutElement>();size.minWidth=width;size.preferredWidth=width;size.flexibleWidth=1;}
 public static InputField Input(KeyValueField row,string value,bool integral){
  var template=row.value;var parent=template.transform.parent;template.gameObject.SetActive(false);
  var go=new GameObject("godworld_input",typeof(RectTransform));go.transform.SetParent(parent,false);var rect=(RectTransform)go.transform;rect.anchorMin=new Vector2(.48f,0);rect.anchorMax=new Vector2(1,1);rect.offsetMin=Vector2.zero;rect.offsetMax=new Vector2(-2,0);
  var background=go.AddComponent<Image>();background.color=new Color(.10f,.12f,.09f,.9f);background.raycastTarget=true;
  var textGo=new GameObject("Text",typeof(RectTransform));textGo.transform.SetParent(go.transform,false);var textRect=(RectTransform)textGo.transform;textRect.anchorMin=Vector2.zero;textRect.anchorMax=new Vector2(1,1);textRect.offsetMin=new Vector2(2,1);textRect.offsetMax=new Vector2(-2,-1);
  var text=textGo.AddComponent<Text>();text.font=template.font;text.fontSize=8;text.fontStyle=template.fontStyle;text.color=template.color;text.alignment=TextAnchor.MiddleRight;text.supportRichText=false;text.raycastTarget=false;text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Truncate;
  var input=go.AddComponent<InputField>();input.targetGraphic=background;input.textComponent=text;input.contentType=integral?InputField.ContentType.IntegerNumber:InputField.ContentType.DecimalNumber;input.lineType=InputField.LineType.SingleLine;input.characterLimit=integral?10:24;input.interactable=true;input.text=value;return input;
 }
}
