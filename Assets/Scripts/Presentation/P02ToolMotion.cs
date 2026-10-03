using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Serializes draw/hold/stow; controller inventory and story APIs retain authority.</summary>
[DisallowMultipleComponent, DefaultExecutionOrder(-60)]
public sealed class P02ToolMotion : MonoBehaviour
{
    public enum Tool { None,Camera,Lamp }
    public enum Motion { Stowed,Drawing,Holding,Stowing }
    public Tool Current { get; private set; }
    public Tool Desired { get; private set; }
    public Motion State { get; private set; }
    public float Blend { get; private set; }
    public bool CameraReady => Current==Tool.Camera&&State==Motion.Holding;
    public bool LampVisible => Current==Tool.Lamp&&Blend>.01f;
    GameControlManager controls;
    CameraModeController cameraTool;
    OilLampController lamp;
    PlayerController activePlayer;
    P02ActorPresentation actor;
    SpriteRenderer prop;
    Sprite heldCameraLeft,heldCameraRight;
    bool restoreLatch;
    public static P02ToolMotion CurrentOwner => PersistentRuntime.Instance?PersistentRuntime.Instance.GetComponent<P02ToolMotion>():null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;Attach();}
    static void Loaded(Scene scene,LoadSceneMode mode)=>Attach();
    static void Attach(){if(PersistentRuntime.Instance&&!PersistentRuntime.Instance.GetComponent<P02ToolMotion>())PersistentRuntime.Instance.gameObject.AddComponent<P02ToolMotion>();}
    void Awake(){Resolve();SceneManager.activeSceneChanged+=SceneChanged;}
    void Resolve(){controls=GetComponent<GameControlManager>();cameraTool=GetComponent<CameraModeController>();lamp=GetComponent<OilLampController>();}
    bool CanReadInput()
    {
        if(!controls||!actor||!actor.OwnsBody||cameraTool&&cameraTool.IsBusy)return false;
        foreach(ControlLockReason reason in Enum.GetValues(typeof(ControlLockReason)))
            if(reason!=ControlLockReason.Camera&&reason!=ControlLockReason.ToolMotion&&controls.IsLockedBy(reason))return false;
        var dialogue=GetComponentInChildren<DialogueManager>(true);
        return !dialogue||!dialogue.IsDialogueActive()&&!dialogue.DidDialogueEndThisFrame();
    }
    public bool Request(Tool requested)
    {
        if(!CanReadInput())return false;
        if(requested==Tool.Lamp&&(!lamp||!lamp.CanUseLamp)){if(lamp&&lamp.Owned&&!lamp.StoryUsable)lamp.ShowExhausted();return false;}
        if(requested==Tool.Camera&&(!cameraTool||!cameraTool.CanBeginPresentation))return false;
        Desired=requested;
        return true;
    }
    void Update()
    {
        // SchoolRuntimeInstaller adds the authored lamp after the initial Scene01
        // runtime. Reacquire it when it becomes available rather than keeping an
        // Awake-time null reference for the rest of the game.
        if(!controls||!cameraTool||!lamp)Resolve();if(!controls)return;
        if(activePlayer!=controls.ActivePlayer){Clear(true);activePlayer=controls.ActivePlayer;actor=activePlayer?activePlayer.GetComponent<P02ActorPresentation>():null;}
        if(!actor&&activePlayer)actor=activePlayer.GetComponent<P02ActorPresentation>();
        bool restoring=controls.IsLockedBy(ControlLockReason.SaveRestore)||controls.IsLockedBy(ControlLockReason.SceneTransition);
        // Same-scene fades (stairs, fog and rooms) retain the controller-owned
        // lamp state. Hide/re-adopt presentation without cancelling its gameplay
        // state. Disk restore and real scene changes still use their original cleanup.
        if(restoring){if(!restoreLatch)Clear(controls.IsLockedBy(ControlLockReason.SaveRestore));restoreLatch=true;return;}
        restoreLatch=false;
        if(GameplayClock.Delta<=0)return;
        // Public checkpoint/cutscene calls are adopted; never replay their narrative effect.
        if(State==Motion.Stowed){
            if(cameraTool&&cameraTool.IsRaised){Current=Desired=Tool.Camera;State=Motion.Holding;Blend=1;}
            else if(lamp&&lamp.IsRaised){Current=Desired=Tool.Lamp;State=Motion.Holding;Blend=1;}
        }
        if(CanReadInput()){
            if(GameplayInput.GetKeyDown(KeyCode.R))Request(Desired==Tool.Lamp?Tool.None:Tool.Lamp);
            else if(GameplayInput.GetKeyDown(KeyCode.Q))Request(Desired==Tool.Camera?Tool.None:Tool.Camera);
        }
        if(cameraTool&&cameraTool.IsBusy)return;
        if(State==Motion.Holding&&Current==Tool.Camera&&!cameraTool.IsRaised)Desired=Tool.None;
        if(State==Motion.Holding&&Current==Tool.Lamp&&!lamp.IsRaised)Desired=Tool.None;
        if(State==Motion.Stowed&&Desired!=Tool.None){Current=Desired;State=Motion.Drawing;Lock();}
        if(State==Motion.Drawing){
            if(Desired!=Current){State=Motion.Stowing;LowerCurrent();}
            else {Blend=Mathf.MoveTowards(Blend,1,GameplayClock.Delta/.46f);
                if(Blend>=1){Unlock();bool ok=Current==Tool.Camera?cameraTool.Raise():lamp.RaiseLamp();
                    if(ok)State=Motion.Holding;else{Desired=Tool.None;State=Motion.Stowing;Lock();}}}
        }
        if(State==Motion.Holding&&Desired!=Current){State=Motion.Stowing;Lock();LowerCurrent();}
        if(State==Motion.Stowing){Blend=Mathf.MoveTowards(Blend,0,GameplayClock.Delta/.34f);
            if(Blend<=0){Current=Tool.None;State=Motion.Stowed;Unlock();}}
        if(actor){actor.ToolBlend=Blend;actor.ToolKind=Current==Tool.Camera?"camera":Current==Tool.Lamp?"lamp":null;}
    }
    void LateUpdate()
    {
        if(!actor||!actor.OwnsBody||Current==Tool.None||Blend<=.32f){if(prop)prop.enabled=false;return;}
        var rig=actor.Rig;var hand=Current==Tool.Camera?rig.HandCamera:rig.HandLamp;
        if(!prop){prop=new GameObject("P02_OwnedTool").AddComponent<SpriteRenderer>();}
        prop.transform.SetParent(rig.transform,false);
        if(Current==Tool.Camera){
            if(!heldCameraLeft||!heldCameraRight)foreach(var sprite in Resources.LoadAll<Sprite>("P02HeldCamera/hand_camera_PV18")){
                if(sprite.name=="camera_left")heldCameraLeft=sprite;
                if(sprite.name=="camera_right")heldCameraRight=sprite;
            }
            prop.sprite=actor.Direction.Contains("left")?heldCameraLeft:heldCameraRight;
        }else prop.sprite=DemoArtCatalog.Find(lamp&&lamp.IsIlluminating?"SPR_KEY_ITEMS_CORE_V02:oil_lamp_on":"SPR_KEY_ITEMS_CORE_V02:oil_lamp_off");
        prop.enabled=prop.sprite;
        if(!prop.sprite)return;
        float sign=actor.Direction.Contains("left")?-1:1;
        float height=(Current==Tool.Camera?.24f:.34f)*rig.targetHeight/1.8f;
        prop.transform.rotation=Quaternion.identity;
        float unit=height/prop.sprite.bounds.size.y;
        prop.transform.localScale=Vector3.one*unit;
        // Catalog item sprites use their own pivots. Attach the painted centre
        // to the hand, instead of accidentally lifting a bottom-pivot camera.
        prop.transform.position=Current==Tool.Camera?
            hand.position+new Vector3(0,.035f,0)*rig.targetHeight/1.8f:
            hand.position+new Vector3(sign*.025f,-.09f,0)*rig.targetHeight/1.8f-prop.sprite.bounds.center*unit;
        prop.flipX=false;prop.sortingOrder=26;prop.color=new Color(1,1,1,Mathf.Clamp01((Blend-.32f)/.16f));
    }
    void LowerCurrent(){if(Current==Tool.Camera&&cameraTool)cameraTool.Lower();if(Current==Tool.Lamp&&lamp)lamp.LowerLamp();}
    void Lock(){if(controls)controls.AcquireLock(ControlLockReason.ToolMotion,this);}
    void Unlock(){if(controls)controls.ReleaseLock(ControlLockReason.ToolMotion,this);}
    public void Clear(bool cancelControllers)
    {
        if(cancelControllers){if(cameraTool)cameraTool.AbortCapture();if(lamp)lamp.LowerLamp();}
        Unlock();Current=Desired=Tool.None;State=Motion.Stowed;Blend=0;
        if(actor){actor.ToolBlend=0;actor.ToolKind=null;}if(prop)prop.enabled=false;
    }
    void SceneChanged(Scene oldScene,Scene newScene)=>Clear(true);
    void OnDisable()=>Clear(true);
    void OnDestroy(){SceneManager.activeSceneChanged-=SceneChanged;if(prop)Destroy(prop.gameObject);}
}
