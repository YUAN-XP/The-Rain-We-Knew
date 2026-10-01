using System;
using TMPro;
using UnityEngine;

[DefaultExecutionOrder(-31900), DisallowMultipleComponent]
public sealed class GamePause : MonoBehaviour
{
    public static GamePause Instance {get;private set;}
    public static bool IsPaused => Instance && Instance.paused;
    public event Action ReturnToMenuRequested;
    public bool SaveAvailableBeforePause {get;private set;}
    Rigidbody2D pausedBody; Vector2 pausedVelocity;
    bool paused,confirmReturn; int screen; bool mainMenu; Action confirmed; string message=""; float previousScale; bool previousAudioPause;
    GameObject page; GameControlManager controls; TMP_FontAsset font;
    public bool MenuOpen => page && page.activeSelf;
    void Awake(){Instance=this;controls=GetComponent<GameControlManager>();}
    void Update(){
        if(GetComponent<GameSaveService>()?.IsRestoring==true||controls.IsLockedBy(ControlLockReason.SaveRestore))return;
        if(!GameplayInput.RawKeyDown(KeyCode.Escape))return;
        if(paused){GameplayInput.ConsumeFrame();if(confirmed!=null){confirmed=null;Draw();}else if(confirmReturn){confirmReturn=false;Draw();}else if(screen!=0){screen=0;message="";Draw();}else if(!mainMenu)SetPaused(false);return;}
        var inventory=GetComponentInChildren<InventoryUI>(true);
        if(inventory&&inventory.IsOpen){inventory.CloseInventory();GameplayInput.ConsumeFrame();return;}
        // A local closeable page consumes ESC first. Dialogue / rhythm / chase can pause.
        if(HasCloseablePage())return;
        var cameraTool=GetComponent<CameraModeController>();
        if(cameraTool&&cameraTool.IsRaised&&!cameraTool.IsBusy){cameraTool.Lower();GameplayInput.ConsumeFrame();return;}
        GameplayInput.ConsumeFrame();SetPaused(true);
    }
    bool HasCloseablePage(){
        foreach(var x in FindObjectsByType<DemoCloseupPresentation>())if(x.IsOpen)return true;
        foreach(var x in FindObjectsByType<DeskCloseupController>())if(x.IsOpen)return true;
        foreach(var x in FindObjectsByType<ChildhoodActivityUI>())if(x.IsOpen)return true;
        foreach(var x in FindObjectsByType<PencilDeskCloseup>())if(x.IsOpen)return true;
        foreach(var x in FindObjectsByType<BasketballThrowController>())if(x.IsOpen&&x.CanCancel)return true;
        foreach(var x in FindObjectsByType<SchoolSceneController>())if(x.SecurityPanelOpen)return true;
        foreach(var x in FindObjectsByType<SchoolYouthController>())if(x.ModalOpen)return true;
        foreach(var x in FindObjectsByType<SchoolReadingChair>())if(x.IsOpen)return true;
        // Other school pages own CloseUp/Inventory; their existing ESC handlers run first.
        return controls&&controls.IsLockedBy(ControlLockReason.Inventory);
    }
    public void SetPaused(bool value){
        if(paused==value)return;
        if(value){SaveAvailableBeforePause=GetComponent<GameSaveService>()?.CanSave(out _)==true;pausedBody=controls&&controls.ActivePlayer?controls.ActivePlayer.GetComponent<Rigidbody2D>():null;if(pausedBody)pausedVelocity=pausedBody.linearVelocity;}
        paused=value;GameplayInput.ConsumeFrame();
        UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        if(value){previousScale=Time.timeScale;previousAudioPause=AudioListener.pause;Time.timeScale=0;AudioListener.pause=true;controls?.AcquireLock(ControlLockReason.GlobalPause,this);Draw();}
        else{Time.timeScale=previousScale;AudioListener.pause=previousAudioPause;controls?.ReleaseLock(ControlLockReason.GlobalPause,this);if(pausedBody&&controls&&!controls.IsLocked())pausedBody.linearVelocity=pausedVelocity;pausedBody=null;confirmReturn=false;if(page)page.SetActive(false);}
    }
    void Draw(){
        if(page){page.SetActive(false);Destroy(page);}
        if(!font){var text=GetComponentInChildren<TMP_Text>(true);font=text?text.font:TMP_Settings.defaultFontAsset;}
        var root=RuntimePanelUI.Canvas("GlobalPause",transform,32000);page=root.gameObject;
        RuntimePanelUI.Panel("PauseBackdrop",root,Vector2.zero,Vector2.one,new Color(.045f,.055f,.06f,.72f));
        RuntimePanelUI.Text(mainMenu?"ProjectMystery":screen==1?"保存游戏":screen==2?"载入游戏":"暂停",root,font,new Vector2(.1f,.82f),new Vector2(.9f,.92f),36);
        if(confirmed!=null){
            RuntimePanelUI.Text(message,root,font,new Vector2(.12f,.56f),new Vector2(.88f,.73f),27);
            RuntimePanelUI.Button("取消",root,font,new Vector2(.25f,.35f),new Vector2(.47f,.47f),()=>{confirmed=null;message="";Draw();});
            RuntimePanelUI.Button("确认",root,font,new Vector2(.53f,.35f),new Vector2(.75f,.47f),()=>{var action=confirmed;confirmed=null;action();});
            return;
        }
        if(confirmReturn){
            RuntimePanelUI.Text("返回主菜单？未保存的进度将丢失。",root,font,new Vector2(.1f,.59f),new Vector2(.9f,.74f),30);
            RuntimePanelUI.Button("取消",root,font,new Vector2(.25f,.4f),new Vector2(.47f,.52f),()=>{confirmReturn=false;Draw();});
            RuntimePanelUI.Button("确认返回",root,font,new Vector2(.53f,.4f),new Vector2(.75f,.52f),()=>{mainMenu=true;confirmReturn=false;message="";Draw();ReturnToMenuRequested?.Invoke();});
            return;
        }
        var saves=GetComponent<GameSaveService>();
        if(screen!=0){
            for(int slot=1;slot<=4;slot++){
                int selected=slot;float top=.79f-(slot-1)*.135f;
                bool valid=saves.Store.TryRead(slot,out var data,out var error);
                string label="存档 "+slot+" · "+(valid?new DateTime(data.savedUtcTicks,DateTimeKind.Utc).ToLocalTime().ToString("yyyy-MM-dd HH:mm")+" · "+SceneLabel(data.scene):saves.Store.Exists(slot)?"损坏或不兼容":"空存档");
                RuntimePanelUI.Button(label,root,font,new Vector2(.16f,top-.11f),new Vector2(.84f,top),()=>SelectSlot(selected));
            }
            RuntimePanelUI.Button("返回",root,font,new Vector2(.38f,.08f),new Vector2(.62f,.17f),()=>{screen=0;message="";Draw();});
        }else if(mainMenu){
            RuntimePanelUI.Button("继续游戏",root,font,new Vector2(.32f,.61f),new Vector2(.68f,.71f),()=>{int slot=saves.Store.LatestValidSlot();if(slot==0){message="没有可用存档";Draw();}else LoadSlot(slot);});
            RuntimePanelUI.Button("载入游戏",root,font,new Vector2(.32f,.46f),new Vector2(.68f,.56f),()=>{screen=2;Draw();});
            RuntimePanelUI.Button("新游戏",root,font,new Vector2(.32f,.31f),new Vector2(.68f,.41f),()=>{message="开始新游戏？未保存的进度将丢失，已有存档保留。";confirmed=()=>{SetPaused(false);P01SessionRestart.Begin(gameObject);};Draw();});
        }else{
            RuntimePanelUI.Button("继续游戏",root,font,new Vector2(.32f,.65f),new Vector2(.68f,.75f),()=>SetPaused(false));
            RuntimePanelUI.Button("保存游戏",root,font,new Vector2(.32f,.50f),new Vector2(.68f,.60f),()=>{if(saves.CanSave(out var reason)){screen=1;message="";}else message=reason;Draw();});
            RuntimePanelUI.Button("载入游戏",root,font,new Vector2(.32f,.35f),new Vector2(.68f,.45f),()=>{screen=2;message="";Draw();});
            RuntimePanelUI.Button("返回主菜单",root,font,new Vector2(.32f,.20f),new Vector2(.68f,.30f),()=>{confirmReturn=true;Draw();});
        }
        RuntimePanelUI.Text(message,root,font,new Vector2(.06f,.01f),new Vector2(.94f,.08f),23);
    }
    static string SceneLabel(string scene){switch(scene){case "Scene01_Rain":return "雨夜街道";case "Scene02_Courtyard":return "家属院";case "Scene03_Childhood":return "童年";case "Scene04_School":return "学校";default:return scene;}}
    void SelectSlot(int slot){
        var saves=GetComponent<GameSaveService>();
        if(screen==1){
            if(!saves.CanSave(out var reason)){message=reason;Draw();return;}
            if(saves.Store.Exists(slot)){message="覆盖存档 "+slot+"？原存档将被替换。";confirmed=()=>SaveSlot(slot,true);Draw();}
            else SaveSlot(slot,false);
        }else{
            if(!saves.Store.TryRead(slot,out _,out var error)){message=error;Draw();return;}
            message="载入存档 "+slot+"？未保存的进度将丢失。";confirmed=()=>LoadSlot(slot);Draw();
        }
    }
    void SaveSlot(int slot,bool overwrite){message=GetComponent<GameSaveService>().Save(slot,overwrite,out var error)?"已保存至存档 "+slot:error;Draw();}
    void LoadSlot(int slot){if(!GetComponent<GameSaveService>().Load(slot,out var error)){message=error;Draw();}else{mainMenu=false;screen=0;message="";}}

    void OnApplicationFocus(bool focus){if(!focus&&Application.isPlaying&&GetComponent<GameSaveService>()?.IsRestoring!=true&&!controls.IsLockedBy(ControlLockReason.SaveRestore))SetPaused(true);}
    void OnDestroy(){if(paused){Time.timeScale=previousScale;AudioListener.pause=previousAudioPause;}if(Instance==this)Instance=null;}
}
