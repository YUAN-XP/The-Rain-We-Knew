
using System.Collections;
using TMPro;
using UnityEngine;
[DisallowMultipleComponent]
public sealed class P07QuestDisplay:MonoBehaviour {
 QuestProgress quest;GameControlManager controls;GameObject page;TMP_Text text;P07InkGraphic strike;Coroutine change;string shown="";bool finishing;CanvasGroup iconOpacity;RectTransform taskRect;
 void Start(){quest=GetComponent<QuestProgress>();controls=GetComponent<GameControlManager>();var root=RuntimePanelUI.Canvas("P07QuestHUD",transform,145);taskRect=RuntimePanelUI.Rect("UpperBlackBarTask",root,new Vector2(.04f,.958f),new Vector2(.04f,.958f));taskRect.pivot=new Vector2(0,.5f);taskRect.sizeDelta=new Vector2(250,48);page=taskRect.gameObject;
  P07UI.PaperPanel(page.transform,Vector2.zero,Vector2.one);var icon=RuntimePanelUI.Rect("TaskInkSymbol",page.transform,new Vector2(0,.5f),new Vector2(0,.5f));icon.pivot=new Vector2(0,.5f);icon.anchoredPosition=new Vector2(14,0);icon.sizeDelta=new Vector2(26,30);iconOpacity=icon.gameObject.AddComponent<CanvasGroup>();var symbol=icon.gameObject.AddComponent<P07InkSymbol>();symbol.color=P07UI.Ink;symbol.raycastTarget=false;
  text=P07UI.Text("",page.transform,Vector2.zero,Vector2.one,23,true);text.rectTransform.offsetMin=new Vector2(52,4);text.rectTransform.offsetMax=new Vector2(-14,-4);
  var line=RuntimePanelUI.Rect("PencilCompletionStrike",text.transform,new Vector2(0,.5f),new Vector2(0,.5f));line.pivot=new Vector2(0,.5f);line.sizeDelta=new Vector2(100,6);strike=line.gameObject.AddComponent<P07InkGraphic>();strike.line=true;strike.color=P07UI.Ink;strike.raycastTarget=false;strike.SetProgress(0);quest.Changed+=Changed;GameLocalization.LanguageChanged+=Fit;Changed();}
 void Fit(){if(!text)return;float width=Mathf.Clamp(text.GetPreferredValues(GameLocalization.Display(shown)).x,45,960);taskRect.sizeDelta=new Vector2(width+68,48);strike.rectTransform.sizeDelta=new Vector2(width,6);}
 void Changed(){finishing=quest.CurrentEvent==(int)QuestEvent.BoardingTriggered&&!string.IsNullOrEmpty(shown);if(change!=null)StopCoroutine(change);if(GetComponent<GameSaveService>().IsRestoring){shown=quest.TaskName;strike.SetProgress(0);text.text=shown;Fit();iconOpacity.alpha=1;text.maxVisibleCharacters=int.MaxValue;change=null;finishing=false;}else change=StartCoroutine(Animate(quest.TaskName));}
 float DisplayDelta=>!finishing&&controls&&controls.IsLocked()?0:GameplayClock.Delta;
 IEnumerator Animate(string next){if(!string.IsNullOrEmpty(shown)&&next!=shown){for(float t=0;t<.45f;t+=DisplayDelta){strike.SetProgress(t/.45f);yield return null;}strike.SetProgress(1);for(float t=0;t<.3f;t+=DisplayDelta)yield return null;}
  shown=next;strike.SetProgress(0);text.text=next;Fit();iconOpacity.alpha=0;text.maxVisibleCharacters=0;
  if(!string.IsNullOrEmpty(next)){for(float t=0;t<.18f;t+=DisplayDelta){iconOpacity.alpha=t/.18f;yield return null;}iconOpacity.alpha=1;for(float t=0;t<.6f;t+=DisplayDelta){text.maxVisibleCharacters=Mathf.CeilToInt(GameLocalization.VisibleLength(next)*t/.6f);yield return null;}}
  text.maxVisibleCharacters=int.MaxValue;change=null;finishing=false;
 }
 void Update(){if(page)page.SetActive(!string.IsNullOrEmpty(shown)&&!GamePause.IsPaused&&(!controls.IsLocked()||finishing&&!controls.IsLockedBy(ControlLockReason.Dialogue)&&!controls.IsLockedBy(ControlLockReason.Inventory)&&!controls.IsLockedBy(ControlLockReason.CloseUp)&&!controls.IsLockedBy(ControlLockReason.SaveRestore)));}
 void OnDestroy(){if(quest)quest.Changed-=Changed;GameLocalization.LanguageChanged-=Fit;}
}
