using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[DefaultExecutionOrder(-31900), DisallowMultipleComponent]
public sealed class GamePause : MonoBehaviour {
 public static GamePause Instance {get;private set;}
 public static bool IsPaused=>Instance&&(Instance.paused||Instance.freezeOwners.Count>0);
 public event Action ReturnToMenuRequested;
 public bool SaveAvailableBeforePause {get;private set;}
 public bool MenuOpen=>page&&page.activeSelf;
 public bool IsMainMenu=>mainMenu&&MenuOpen;
 readonly HashSet<UnityEngine.Object> freezeOwners=new HashSet<UnityEngine.Object>();
 bool paused,frozen,mainMenu;int screen;Action confirmed;string message="";float previousScale;bool previousAudioPause;
 GameObject page;GameControlManager controls;Rigidbody2D pausedBody;Vector2 pausedVelocity;
 void Awake(){Instance=this;controls=GetComponent<GameControlManager>();}
 public void Freeze(UnityEngine.Object owner){if(!owner)return;freezeOwners.Add(owner);ApplyFreeze();}
 public void Thaw(UnityEngine.Object owner){freezeOwners.Remove(owner);ApplyFreeze();}
 void ApplyFreeze(){bool value=paused||freezeOwners.Count>0;if(value==frozen)return;frozen=value;
  if(value){previousScale=Time.timeScale;previousAudioPause=AudioListener.pause;Time.timeScale=0;AudioListener.pause=true;}
  else {Time.timeScale=previousScale;AudioListener.pause=previousAudioPause;}
 }
 void Update(){if(freezeOwners.RemoveWhere(v=>!v)>0)ApplyFreeze();
  if(GetComponent<GameSaveService>()?.IsRestoring==true||controls.IsLockedBy(ControlLockReason.SaveRestore)||GameplayInput.FrameConsumed)return;
  if(!GameplayInput.RawKeyDown(KeyCode.Escape))return;
  if(GetComponent<P07AcquisitionPresentation>()?.IsOpen==true)return;
  if(paused){GameplayInput.ConsumeFrame();if(confirmed!=null){confirmed=null;message="";Draw();}else if(screen!=0){screen=0;message="";Draw();}else if(!mainMenu)SetPaused(false);return;}
  var inventory=GetComponentInChildren<InventoryUI>(true);if(inventory&&inventory.IsOpen){inventory.CloseInventory();GameplayInput.ConsumeFrame();return;}
  if(HasCloseablePage())return;
  var cameraTool=GetComponent<CameraModeController>();if(cameraTool&&cameraTool.IsRaised&&!cameraTool.IsBusy){cameraTool.Lower();GameplayInput.ConsumeFrame();return;}
  GameplayInput.ConsumeFrame();SetPaused(true);
 }
    bool HasCloseablePage(){
        foreach(var x in FindObjectsByType<DemoCloseupPresentation>())if(x.IsOpen)return true;
        foreach(var x in FindObjectsByType<DeskCloseupController>())if(x.IsOpen)return true;
        foreach(var x in FindObjectsByType<ChildhoodActivityUI>())if(x.IsOpen)return true;
        foreach(var x in FindObjectsByType<PencilDeskCloseup>())if(x.IsOpen)return true;
        foreach(var x in FindObjectsByType<BasketballThrowController>())if(x.IsOpen&&x.CanCancel)return true;
        foreach(var x in FindObjectsByType<SchoolSceneController>())if(x.ModalOpen)return true;
        foreach(var x in FindObjectsByType<SchoolYouthController>())if(x.ModalOpen)return true;
        foreach(var x in FindObjectsByType<SchoolYouthSecurity>())if(x.IsOpen)return true;
        foreach(var x in FindObjectsByType<SchoolReadingChair>())if(x.IsOpen)return true;
        // Other school pages own CloseUp/Inventory; their existing ESC handlers run first.
        return controls&&controls.IsLockedBy(ControlLockReason.Inventory);
    }
 public void ShowMainMenu(){mainMenu=true;confirmed=null;screen=0;message="";SetPaused(true);Draw();ReturnToMenuRequested?.Invoke();}
 public void SetPaused(bool value){if(paused==value)return;
  if(value){SaveAvailableBeforePause=GetComponent<GameSaveService>()?.CanSave(out _)==true;pausedBody=controls&&controls.ActivePlayer?controls.ActivePlayer.GetComponent<Rigidbody2D>():null;if(pausedBody)pausedVelocity=pausedBody.linearVelocity;}
  paused=value;GameplayInput.ConsumeFrame();UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
  if(value){controls?.AcquireLock(ControlLockReason.GlobalPause,this);ApplyFreeze();Draw();}
  else{controls?.ReleaseLock(ControlLockReason.GlobalPause,this);ApplyFreeze();if(pausedBody&&controls&&!controls.IsLocked())pausedBody.linearVelocity=pausedVelocity;pausedBody=null;if(page)page.SetActive(false);}
 }
 void Confirm(string text,Action action){message=text;confirmed=action;Draw();}
 public void StartNew(){confirmed=null;mainMenu=false;SetPaused(false);P01SessionRestart.Begin(gameObject);}
 public void OpenSettings(){screen=3;Draw();}
 void Draw(){if(page){page.SetActive(false);Destroy(page);}var root=RuntimePanelUI.Canvas("P07GlobalMenu",transform,32000);page=root.gameObject;
  if(mainMenu)P07UI.Background(root);else RuntimePanelUI.Panel("PauseDim",root,Vector2.zero,Vector2.one,new Color(.02f,.025f,.035f,.76f));
  if(confirmed!=null){RuntimePanelUI.Panel("ConfirmDim",root,Vector2.zero,Vector2.one,new Color(0,0,0,.45f));P07UI.PaperPanel(root,new Vector2(.23f,.32f),new Vector2(.77f,.67f));P07UI.Text(message,root,new Vector2(.27f,.47f),new Vector2(.73f,.61f),30,true);
   P07UI.Button("取消",root,new Vector2(.28f,.37f),new Vector2(.47f,.44f),()=>{confirmed=null;message="";Draw();},true);
   P07UI.Button("确认",root,new Vector2(.53f,.37f),new Vector2(.72f,.44f),()=>{var a=confirmed;confirmed=null;a?.Invoke();},true);return;}
  if(screen==3){var r=RuntimePanelUI.Rect("SharedSettings",root,Vector2.zero,Vector2.one);r.gameObject.AddComponent<P07SettingsPanel>().Build(()=>{screen=0;Draw();});return;}
  var saves=GetComponent<GameSaveService>();
  if(screen==1||screen==2){P07UI.Text(screen==1?"保存游戏":"读取游戏",root,new Vector2(.09f,.80f),new Vector2(.88f,.9f),38);
   for(int slot=1;slot<=4;slot++){int selected=slot;int col=(slot-1)%2,row=(slot-1)/2;var min=new Vector2(.10f+col*.415f,.46f-row*.27f);var max=min+new Vector2(.385f,.22f);
    bool valid=saves.Store.TryRead(slot,out var data,out _);string label=valid?(string.IsNullOrEmpty(QuestProgress.Label(data.quests.currentEvent))?"自由探索":QuestProgress.Label(data.quests.currentEvent))+"\n┄┄┄┄┄┄┄┄┄┄┄┄┄┄\n"+new DateTime(data.savedUtcTicks,DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm")+"     "+TimeSpan.FromSeconds(data.playSeconds).ToString(@"hh\:mm\:ss"):(saves.Store.Exists(slot)?"损坏或不兼容\n此存档无法读取":"空");
    P07UI.PaperPanel(root,min,max);var b=P07UI.Button("",root,min,max,()=>SelectSlot(selected),true);b.name="SaveSlot"+slot;b.interactable=screen==1||valid;
    if(valid){string task=string.IsNullOrEmpty(QuestProgress.Label(data.quests.currentEvent))?"自由探索":QuestProgress.Label(data.quests.currentEvent);
     P07UI.Text(task,b.transform,new Vector2(.05f,.56f),new Vector2(.96f,.90f),26,true);
     var dash=P07UI.Text("- - - - - - - - - - - - - - - - - - - - -",b.transform,new Vector2(.05f,.44f),new Vector2(.96f,.55f),20,true);
     P07UI.Text(new DateTime(data.savedUtcTicks,DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm"),b.transform,new Vector2(.05f,.10f),new Vector2(.63f,.37f),22,true);
     var played=P07UI.Text(TimeSpan.FromSeconds(data.playSeconds).ToString(@"hh\:mm\:ss"),b.transform,new Vector2(.64f,.10f),new Vector2(.95f,.37f),22,true);played.alignment=TextAlignmentOptions.Right;
    }else{var t=P07UI.Text(label,b.transform,new Vector2(.05f,.08f),new Vector2(.95f,.92f),28,true);t.alignment=TextAlignmentOptions.Center;}
    P07UI.Text("存档 "+slot,root,min+new Vector2(0,.225f),max+new Vector2(0,.065f),20);
   }P07UI.Back(root,()=>{screen=0;message="";Draw();});
  }else{var data=Resources.Load<P07PresentationData>("P07Presentation");P07UI.Text(mainMenu?data?data.gameTitle:"ProjectMystery":"暂停",root,new Vector2(.09f,.75f),new Vector2(.49f,.9f),mainMenu?57:42);
   var actions=new List<(string,Action)>();
   if(mainMenu){bool saved=saves.Store.LatestValidSlot()>0;if(saved){actions.Add(("继续游戏",()=>LoadSlot(saves.Store.LatestValidSlot())));actions.Add(("读取游戏",()=>{screen=2;Draw();}));}
    actions.Add((saved?"开始新游戏":"开始游戏",()=>{if(saved)Confirm("开始新游戏？未保存的进度将丢失，已有存档保留。",StartNew);else StartNew();}));actions.Add(("设置",OpenSettings));actions.Add(("退出游戏",Quit));
   }else{actions.Add(("继续游戏",()=>SetPaused(false)));actions.Add(("保存游戏",()=>{if(saves.CanSave(out var reason)){screen=1;message="";}else message=reason;Draw();}));actions.Add(("设置",OpenSettings));actions.Add(("退出至游戏主菜单",()=>Confirm("此操作会丢失未保存游戏内容，是否继续？",ShowMainMenu)));actions.Add(("退出游戏",()=>Confirm("此操作会丢失未保存游戏内容，是否继续？",Quit)));}
   for(int i=0;i<actions.Count;i++){var a=actions[i];float y=.12f+(actions.Count-1-i)*.085f;P07UI.Button(a.Item1,root,new Vector2(.09f,y),new Vector2(.355f,y+.063f),a.Item2);}
  }P07UI.Text(message,root,new Vector2(.09f,.01f),new Vector2(.94f,.07f),23);
 }
 void SelectSlot(int slot){var saves=GetComponent<GameSaveService>();if(screen==1){if(!saves.CanSave(out var reason)){message=reason;Draw();return;}if(saves.Store.Exists(slot))Confirm("覆盖存档 "+slot+"？原存档将被替换。",()=>SaveSlot(slot,true));else SaveSlot(slot,false);}
  else {if(!saves.Store.TryRead(slot,out _,out var error)){message=error;Draw();return;}Confirm("载入存档 "+slot+"？未保存的进度将丢失。",()=>LoadSlot(slot));}}
 void SaveSlot(int slot,bool overwrite){message=GetComponent<GameSaveService>().Save(slot,overwrite,out var error)?"已保存至存档 "+slot:error;Draw();}
 void LoadSlot(int slot){if(!GetComponent<GameSaveService>().Load(slot,out var error)){message=error;Draw();}else{mainMenu=false;screen=0;message="";}}
 public static void Quit(){GameplayInput.ConsumeFrame();
 #if UNITY_EDITOR
 Debug.Log("P07 quit requested; stopping Play Mode preserves the editor.");UnityEditor.EditorApplication.isPlaying=false;
 #else
 Application.Quit();
 #endif
 }
 void OnApplicationFocus(bool focus){if(!focus&&Application.isPlaying&&GetComponent<GameSaveService>()?.IsRestoring!=true&&!controls.IsLockedBy(ControlLockReason.SaveRestore))SetPaused(true);}
 void OnDestroy(){if(frozen){Time.timeScale=previousScale;AudioListener.pause=previousAudioPause;}if(Instance==this)Instance=null;}
}
