using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-31950)]
public sealed class P07AcquisitionPresentation:MonoBehaviour {
 struct Gain {public string id;public bool first;}
 readonly Queue<Gain> gains=new Queue<Gain>();readonly Queue<string> toasts=new Queue<string>();
 AcquisitionHistory history;GameControlManager controls;GamePause pause;GameObject page,toastRoot;Image shade,fileImage;TMP_Text title,body,toastText;P07InkGraphic toastInk;
 FileData currentFile;CanvasGroup artwork;bool reading;float openAge,toastAge;string toastValue;
 public bool IsOpen=>page&&page.activeSelf;
 public bool HasPendingPresentation=>IsOpen||gains.Count>0||toasts.Count>0||!string.IsNullOrEmpty(toastValue);
 void Awake(){history=GetComponent<AcquisitionHistory>();controls=GetComponent<GameControlManager>();pause=GetComponent<GamePause>();history.Obtained+=Gained;}
 void Gained(string id,bool first){if(GetComponent<GameSaveService>()?.IsRestoring==true)return;gains.Enqueue(new Gain{id=id,first=first});}
 void Update(){if(pause.MenuOpen)return;
  if(IsOpen){openAge+=Time.unscaledDeltaTime;shade.color=new Color(0,0,0,Mathf.Lerp(0,currentFile ? .78f : 1f,Mathf.Clamp01(openAge/.4f)));
   if(artwork)artwork.alpha=Mathf.Clamp01((openAge-.4f)/.25f);
   if(openAge>.65f&&!GameplayInput.FrameConsumed){if(GameplayInput.RawKeyDown(KeyCode.Escape)){GameplayInput.ConsumeFrame();Close();return;}if(currentFile&&!reading&&GameplayInput.RawKeyDown(KeyCode.E)){GameplayInput.ConsumeFrame();ReadFile();}}return;}
  if(GamePause.IsPaused||controls.IsLocked()||GetComponent<GameSaveService>().IsRestoring)return;
  if(gains.Count>0){var gain=gains.Dequeue();string name="";Sprite sprite=null;currentFile=null;
   if(gain.id.StartsWith("item:")){var item=GetComponent<InventoryManager>().Items.FirstOrDefault(x=>x&&gain.id=="item:"+x.itemId);if(!item)return;name=GetComponent<InventoryManager>().DisplayName(item);sprite=item.icon;
    toasts.Enqueue(name+"已放入背包");if(Combinable(false))toasts.Enqueue("有物品待组合");if(gain.first)Open(name,GetComponent<InventoryManager>().DisplayDescription(item),sprite);
   }else if(gain.id.StartsWith("file:")){currentFile=GetComponent<FileArchiveManager>().Files.FirstOrDefault(x=>x&&gain.id=="file:"+x.fileId);if(!currentFile)return;name=currentFile.fileName;sprite=currentFile.icon;toasts.Enqueue(name+"已放入背包");if(Combinable(true))toasts.Enqueue("有文件待组合");if(gain.first)Open(name,currentFile.summary,sprite);
   }else if(gain.id.StartsWith("outfit:")){toasts.Enqueue("服装已放入背包");}
  }
  if(IsOpen)return;
  if(string.IsNullOrEmpty(toastValue)&&toasts.Count>0)BeginToast(toasts.Dequeue());
  if(!string.IsNullOrEmpty(toastValue)){toastAge+=Time.unscaledDeltaTime;toastText.maxVisibleCharacters=Mathf.CeilToInt(GameLocalization.VisibleLength(toastValue)*Mathf.Clamp01(toastAge/.6f));toastInk.SetProgress(Mathf.Clamp01(toastAge/.4f));if(toastAge>3.2f){toastValue=null;toastRoot.SetActive(false);}}
 }
 bool Combinable(bool file){var cm=GetComponent<CombinationManager>();var inv=GetComponent<InventoryManager>();var files=GetComponent<FileArchiveManager>();if(!cm)return false;
  // Query the original candidate rules; never award, remove or combine anything here.
  return cm.Candidates(Array.Empty<ItemData>()).Any(r=>{
   if((file?r.resultFile==null:r.resultFile!=null)||r.materials.Select(m=>m.item.itemId).Distinct().Count()!=r.materials.Length)return false;
   if(r.materials.Any(m=>inv.Items.Count(i=>i==m.item)!=m.count||inv.Items.Count(i=>i&&i.itemId==m.item.itemId)!=m.count))return false;
   if(!r.resultFile&&!r.resultItem&&!r.retainedResult)return false;
   if(r.retainedResult&&!r.materials.Any(m=>m.item==r.retainedResult&&!m.consume))return false;
   if(r.resultFile&&(string.IsNullOrWhiteSpace(r.resultFile.fileId)||files.HasFile(r.resultFile)))return false;
   if(r.resultItem&&inv.HasItemById(r.resultItem.itemId))return false;
   var lamp=GetComponent<OilLampController>();return !r.restoreLamp||lamp&&lamp.Owned&&r.retainedResult;
  });
 }
 void Open(string name,string description,Sprite sprite){page=RuntimePanelUI.Canvas("FirstAcquisitionCloseup",transform,31000).gameObject;shade=RuntimePanelUI.Panel("EnvironmentFade",page.transform,Vector2.zero,Vector2.one,Color.clear);
  var content=RuntimePanelUI.Rect("AcquisitionContent",page.transform,Vector2.zero,Vector2.one);artwork=content.gameObject.AddComponent<CanvasGroup>();artwork.alpha=0;
  fileImage=P07UI.Sprite("AcquiredArtwork",content,currentFile?new Vector2(.29f,.23f):new Vector2(.48f,.17f),currentFile?new Vector2(.71f,.87f):new Vector2(.88f,.82f),sprite);
  title=P07UI.Text(name,content,new Vector2(.09f,.69f),new Vector2(.43f,.8f),40);body=P07UI.Text(description,content,new Vector2(.09f,.36f),new Vector2(.43f,.63f),28);
  if(currentFile){title.rectTransform.anchorMin=new Vector2(.11f,.86f);title.rectTransform.anchorMax=new Vector2(.8f,.94f);body.gameObject.SetActive(false);P07UI.Text("[E] 阅读",content,new Vector2(.10f,.09f),new Vector2(.4f,.15f),25);}
  P07UI.Button("[Esc] 关闭",content,new Vector2(.77f,.09f),new Vector2(.94f,.15f),Close);
  openAge=0;reading=false;GameplayInput.ConsumeFrame();controls.AcquireLock(ControlLockReason.CloseUp,this);pause.Freeze(this);
 }
 void ReadFile(){reading=true;fileImage.color=new Color(.28f,.28f,.28f,.75f);body.gameObject.SetActive(true);body.text=currentFile.content;body.rectTransform.anchorMin=new Vector2(.21f,.23f);body.rectTransform.anchorMax=new Vector2(.79f,.78f);body.alignment=TextAlignmentOptions.TopLeft;body.fontSize=28;body.fontSizeMin=18;body.color=P07UI.Paper;
  foreach(var t in page.GetComponentsInChildren<TMP_Text>())if(t.text=="[E] 阅读")t.gameObject.SetActive(false);
 }
 public void Close(){GameplayInput.ConsumeFrame();controls.ReleaseLock(ControlLockReason.CloseUp,this);pause.Thaw(this);if(page){page.SetActive(false);Destroy(page);}page=null;currentFile=null;reading=false;}
 void BeginToast(string value){if(!toastRoot){var c=RuntimePanelUI.Canvas("AcquisitionToasts",transform,150);toastRoot=RuntimePanelUI.Rect("TopRightToast",c,new Vector2(.63f,.932f),new Vector2(.965f,.98f)).gameObject;toastInk=toastRoot.AddComponent<P07InkGraphic>();toastInk.color=P07UI.Paper;toastInk.raycastTarget=false;toastText=P07UI.Text("",toastRoot.transform,new Vector2(.04f,.02f),new Vector2(.98f,.98f),23,true);}
  toastRoot.SetActive(true);toastValue=value;toastText.text=value;toastAge=0;toastText.maxVisibleCharacters=0;float width=Mathf.Clamp(toastText.GetPreferredValues(GameLocalization.Display(value)).x+50,140,535);var r=(RectTransform)toastRoot.transform;r.anchorMin=r.anchorMax=new Vector2(.965f,.966f);r.pivot=new Vector2(1,.5f);r.sizeDelta=new Vector2(width,40);}
 public void ResetPresentation(){gains.Clear();toasts.Clear();toastValue=null;if(toastRoot)toastRoot.SetActive(false);if(IsOpen)Close();}
 void OnDestroy(){if(history)history.Obtained-=Gained;if(pause)pause.Thaw(this);if(controls)controls.ReleaseLock(ControlLockReason.CloseUp,this);}
}
