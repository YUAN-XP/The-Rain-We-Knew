using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class P07SettingsPanel:MonoBehaviour {
 public static int[] SupportedSizes=>new[]{3840,2160,2560,1440,1920,1080,1600,900,1366,768,1280,720};
 public int Tab {get;private set;}public bool DropdownOpen=>dropdown;GameObject content,dropdown;Action back;TMP_Text resolution;RectTransform selectedTab;
 public static void ApplySaved(){AudioListener.volume=Mathf.Clamp01(PlayerPrefs.GetFloat("P07.MasterVolume",1));int w=PlayerPrefs.GetInt("P07.Width",0),h=PlayerPrefs.GetInt("P07.Height",0);if(w>=640&&h>=360&&!Application.isEditor)Screen.SetResolution(w,h,FullScreenMode.Windowed);}
 public void Build(Action returnAction){back=returnAction;P07UI.PaperPanel(transform,new Vector2(.055f,.07f),new Vector2(.945f,.93f));P07UI.Text("设置",transform,new Vector2(.10f,.79f),new Vector2(.26f,.89f),40,true);
  foreach(var a in new[]{("音频",0),("画面",1),("制作名单",2),("语言",3)}){int tab=a.Item2;var b=P07UI.Button(a.Item1,transform,new Vector2(.10f,.64f-tab*.13f),new Vector2(.27f,.71f-tab*.13f),()=>Select(tab),true);b.GetComponentInChildren<TMP_Text>().color=P07UI.Ink;}
  var divide=RuntimePanelUI.Rect("InkDivider",transform,new Vector2(.30f,.26f),new Vector2(.302f,.79f)).gameObject.AddComponent<P07InkGraphic>();divide.color=P07UI.Ink;divide.raycastTarget=false;
  selectedTab=RuntimePanelUI.Rect("SelectedTabUnderline",transform,new Vector2(.10f,.63f),new Vector2(.26f,.635f));var line=selectedTab.gameObject.AddComponent<P07InkGraphic>();line.line=true;line.color=P07UI.Ink;line.raycastTarget=false;
  P07UI.Back(transform,back,true);Select(0);
 }
 static void DisposePanel(GameObject panel){if(!panel)return;panel.SetActive(false);if(Application.isPlaying)Destroy(panel);else DestroyImmediate(panel);}
 void Select(int index){Tab=index;selectedTab.anchorMin=new Vector2(.10f,.63f-index*.13f);selectedTab.anchorMax=new Vector2(.26f,.635f-index*.13f);DisposePanel(content);dropdown=null;content=RuntimePanelUI.Rect("SettingsContent",transform,new Vector2(.34f,.20f),new Vector2(.89f,.76f)).gameObject;
  if(index==0){P07UI.Text("全局音量",content.transform,new Vector2(.03f,.68f),new Vector2(.9f,.8f),34,true);
   var r=RuntimePanelUI.Rect("MasterVolume",content.transform,new Vector2(.06f,.48f),new Vector2(.92f,.53f));var slider=r.gameObject.AddComponent<Slider>();slider.minValue=0;slider.maxValue=1;
   RuntimePanelUI.Panel("Track",r,Vector2.zero,Vector2.one,new Color(.42f,.45f,.43f));var fill=RuntimePanelUI.Panel("Fill",r,Vector2.zero,Vector2.one,new Color(.1f,.15f,.17f));slider.fillRect=fill.rectTransform;
   var handle=RuntimePanelUI.Panel("Handle",r,new Vector2(0,.5f),new Vector2(0,.5f),P07UI.Ink);handle.rectTransform.sizeDelta=new Vector2(15,35);slider.handleRect=handle.rectTransform;slider.targetGraphic=handle;slider.direction=Slider.Direction.LeftToRight;slider.SetValueWithoutNotify(AudioListener.volume);
   var value=P07UI.Text(Mathf.RoundToInt(AudioListener.volume*100)+"%",content.transform,new Vector2(.06f,.25f),new Vector2(.92f,.39f),27,true);
   slider.onValueChanged.AddListener(v=>{AudioListener.volume=v;PlayerPrefs.SetFloat("P07.MasterVolume",v);PlayerPrefs.Save();value.text=Mathf.RoundToInt(v*100)+"%";});
  }else if(index==1){P07UI.Text("分辨率",content.transform,new Vector2(.03f,.68f),new Vector2(.9f,.8f),34,true);
   var b=P07UI.Button(Screen.width+" × "+Screen.height,content.transform,new Vector2(.03f,.49f),new Vector2(.9f,.63f),ToggleResolution,true);resolution=b.GetComponentInChildren<TMP_Text>();resolution.color=P07UI.Ink;var arrow=RuntimePanelUI.Rect("ResolutionArrow",b.transform,new Vector2(.85f,.24f),new Vector2(.96f,.76f)).gameObject.AddComponent<P07InkSymbol>();arrow.arrow=true;arrow.color=P07UI.Ink;arrow.raycastTarget=false;
   P07UI.Text("滚轮或拖动右侧滑条查看更多",content.transform,new Vector2(.03f,.01f),new Vector2(.97f,.09f),20,true);
  }else if(index==3){P07UI.Text("游戏语言",content.transform,new Vector2(.03f,.68f),new Vector2(.95f,.82f),34,true);
   P07UI.Text("当前语言："+(GameLocalization.Current==GameLanguage.SimplifiedChinese?"简体中文":"English"),content.transform,new Vector2(.03f,.61f),new Vector2(.96f,.67f),22,true);
   var zh=P07UI.Button("简体中文",content.transform,new Vector2(.03f,.46f),new Vector2(.96f,.59f),()=>ChooseLanguage(GameLanguage.SimplifiedChinese),true);zh.name="LanguageChinese";zh.interactable=GameLocalization.Current!=GameLanguage.SimplifiedChinese;
   var en=P07UI.Button("English",content.transform,new Vector2(.03f,.25f),new Vector2(.96f,.38f),()=>ChooseLanguage(GameLanguage.English),true);en.name="LanguageEnglish";en.interactable=GameLocalization.Current!=GameLanguage.English;
   P07UI.Text("选择后立即生效，并在下次启动时保留。",content.transform,new Vector2(.03f,.02f),new Vector2(.96f,.17f),24,true);
  }else{var data=Resources.Load<P07PresentationData>("P07Presentation");P07UI.Text(data?data.credits:"",content.transform,new Vector2(.03f,.15f),new Vector2(.96f,.78f),30,true);}
 }
 void ChooseLanguage(GameLanguage language){GameLocalization.SetLanguage(language);Select(3);}
 public static Vector2Int[] Resolutions(){var modes=Screen.resolutions.Select(v=>new Vector2Int(v.width,v.height)).ToList();modes.Add(new Vector2Int(Screen.width,Screen.height));int mw=Screen.currentResolution.width,mh=Screen.currentResolution.height;
  foreach(var s in new[]{new Vector2Int(3840,2160),new Vector2Int(3440,1440),new Vector2Int(2560,1600),new Vector2Int(2560,1440),new Vector2Int(1920,1200),new Vector2Int(1920,1080),new Vector2Int(1600,900),new Vector2Int(1366,768),new Vector2Int(1280,720)})if(s.x<=mw&&s.y<=mh)modes.Add(s);
  return modes.Where(s=>s.x>=640&&s.y>=360).Distinct().OrderByDescending(s=>s.x*s.y).ThenByDescending(s=>s.x).ToArray();}
 public void ToggleResolution(){if(dropdown){Destroy(dropdown);dropdown=null;return;}var modes=Resolutions();var r=RuntimePanelUI.Rect("TwoExtraResolutionRows",content.transform,new Vector2(.03f,.47f),new Vector2(.94f,.47f));r.pivot=new Vector2(.5f,1);r.sizeDelta=new Vector2(0,140);dropdown=r.gameObject;
  var scroll=r.gameObject.AddComponent<ScrollRect>();scroll.horizontal=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=34;
  var view=RuntimePanelUI.Rect("Viewport",r,Vector2.zero,new Vector2(.94f,1));view.gameObject.AddComponent<RectMask2D>();scroll.viewport=view;
  var list=RuntimePanelUI.Rect("Options",view,new Vector2(0,1),Vector2.one);list.pivot=new Vector2(.5f,1);float row=70;list.sizeDelta=new Vector2(0,modes.Length*row);scroll.content=list;
  for(int i=0;i<modes.Length;i++){var mode=modes[i];var b=P07UI.Button(mode.x+" × "+mode.y,list,new Vector2(0,1),Vector2.one,()=>{Screen.SetResolution(mode.x,mode.y,FullScreenMode.Windowed);PlayerPrefs.SetInt("P07.Width",mode.x);PlayerPrefs.SetInt("P07.Height",mode.y);PlayerPrefs.Save();resolution.text=mode.x+" × "+mode.y;Destroy(dropdown);dropdown=null;},true);var br=(RectTransform)b.transform;br.pivot=new Vector2(.5f,1);br.sizeDelta=new Vector2(0,row);br.anchoredPosition=new Vector2(0,-i*row);b.GetComponentInChildren<TMP_Text>().color=P07UI.Ink;}
  var sr=RuntimePanelUI.Rect("Scrollbar",r,new Vector2(.96f,0),Vector2.one);var bg=sr.gameObject.AddComponent<Image>();bg.color=new Color(.5f,.5f,.45f,.3f);var bar=sr.gameObject.AddComponent<Scrollbar>();var h=RuntimePanelUI.Panel("Handle",sr,Vector2.zero,Vector2.one,P07UI.Ink);bar.handleRect=h.rectTransform;bar.targetGraphic=h;bar.direction=Scrollbar.Direction.BottomToTop;scroll.verticalScrollbar=bar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.Permanent;Canvas.ForceUpdateCanvases();scroll.verticalNormalizedPosition=1;
 }
}
