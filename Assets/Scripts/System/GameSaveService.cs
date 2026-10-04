using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[DisallowMultipleComponent]
public sealed class GameSaveService : MonoBehaviour {
    public FourSlotSaveStore Store {get;private set;}
    public bool IsRestoring {get;private set;}
    public string LastError {get;private set;}
    public event Action<bool,string> LoadFinished;
    GameControlManager controls; StoryStateManager story; InventoryManager inventory; FileArchiveManager archive;
    QuestProgress quests; AcquisitionHistory acquisition; SaveAssetRegistry registry;
    GameObject loadingCover;
    void Awake(){
        controls=GetComponent<GameControlManager>();story=GetComponent<StoryStateManager>();inventory=GetComponent<InventoryManager>();archive=GetComponent<FileArchiveManager>();
        quests=GetComponent<QuestProgress>();acquisition=GetComponent<AcquisitionHistory>();
        Store=new FourSlotSaveStore(Path.Combine(Application.persistentDataPath,"Saves"));
    }
    public bool CanSave(out string reason){
        reason="当前无法保存：请在自由行动时保存。";
        if(GamePause.IsPaused&&!GetComponent<GamePause>().SaveAvailableBeforePause)return false;
        if(IsRestoring||!controls||!controls.ActivePlayer||!controls.ActivePlayer.isActiveAndEnabled)return false;
        foreach(ControlLockReason r in Enum.GetValues(typeof(ControlLockReason)))if(r!=ControlLockReason.GlobalPause&&controls.IsLockedBy(r))return false;
        string scene=SceneManager.GetActiveScene().name;if(scene=="EndingSequence")return false;
        var d=GetComponentInChildren<DialogueManager>(true);if(d&&(d.IsDialogueActive()||d.DidDialogueEndThisFrame()))return false;
        if(story.Has(StoryFlag.SchoolChaseStarted)&&!story.Has(StoryFlag.SchoolChaseCompleted))return false;
        foreach(var fog in FindObjectsByType<FogIntroController>())if(fog.IsActive)return false;
        foreach(var rhythm in FindObjectsByType<GuitarRhythmGame>())if(rhythm.IsRunning)return false;
        foreach(var chapter in FindObjectsByType<Scene03Chapter>())if(!chapter.Ready||chapter.Busy)return false;
        foreach(var s in FindObjectsByType<SchoolSceneController>())if(!s.Ready||s.Busy)return false;
        var body=controls.ActivePlayer.GetComponent<Rigidbody2D>();
        if(body&&!controls.ActivePlayer.IsPlanarMovement&&Mathf.Abs(body.linearVelocity.y)>.15f){reason="当前无法保存：请站稳后重试。";return false;}
        if(!GroundedPlacement.IsSupported(controls.ActivePlayer)){reason="当前无法保存：请站稳后重试。";return false;}
        reason=null;return true;
    }
    SaveAssetRegistry Registry => registry?registry:registry=SaveAssetRegistry.Load();
    public GameSaveData Capture(int slot){
        if(!CanSave(out var reason))throw new InvalidOperationException(reason);
        var p=controls.ActivePlayer;var cam=Camera.main;var follow=cam.GetComponent<CameraFollow>();
        if(!follow)throw new InvalidOperationException("当前镜头尚未就绪");
        var data=new GameSaveData{slot=slot,savedUtcTicks=DateTime.UtcNow.Ticks,playSeconds=GameplayClock.Elapsed,scene=SceneManager.GetActiveScene().name,
            actor=controls.AllowsAdultOutfits?"adult":SceneManager.GetActiveScene().name=="Scene03_Childhood"?"child":"youth",
            position=p.transform.position,scale=p.transform.localScale,planar=p.IsPlanarMovement,facing=follow.Facing,
            actorFacing=p.GetComponent<P02ActorPresentation>()?.Direction,
            cameraPosition=cam.transform.position,cameraSize=cam.orthographicSize,cameraX=follow.HorizontalLimits,cameraY=follow.VerticalLimits,followY=follow.FollowsY,cameraOffset=follow.VerticalOffset,
            companions=CompanionSaveState.Capture(),story=story.CaptureSave(),quests=quests.Capture(),firstObtained=acquisition.Capture(),
            lampRaised=GetComponent<OilLampController>()?.IsRaised==true,cameraAvailable=GetComponent<CameraModeController>()?.ContextAvailable==true};
        data.items=inventory.Items.Select(i=>new SavedItem{key=Registry.Key(i),id=i.itemId,displayName=inventory.DisplayName(i),description=inventory.DisplayDescription(i)}).ToArray();
        data.files=archive.Files.Select(f=>new SavedFile{key=Registry.Key(f),id=f.fileId}).ToArray();
        var school=FindAnyObjectByType<SchoolLayoutController>();if(school)data.school=new SavedSchool{present=true,room=school.CurrentRoom,variant=(int)school.Variant,actor=(int)school.Actor,phase=(int)school.Phase};
        var doors=FindAnyObjectByType<ThreeDoorPuzzleController>();if(doors)data.threeDoorProgress=doors.Progress;
        var corridor=FindAnyObjectByType<DistortedCorridorController>();if(corridor){data.distortedJoined=corridor.HasJoined;data.corridorLampStage=corridor.LampStage;}
        data.courtyardLayoutRevision=P03SceneLayoutMigration.Revision;data.childhoodLayoutRevision=P04SceneLayoutMigration.Revision;
        FourSlotSaveStore.Validate(data,slot);return data;
    }
    public bool Save(int slot,bool overwrite,out string error){
        error=null;try{Store.Write(slot,Capture(slot),overwrite);return true;}catch(Exception e){error=e.Message;return false;}
    }
    public bool Load(int slot,out string error){
        error=null;if(IsRestoring){error="正在载入";return false;}
        if(!Store.TryRead(slot,out var data,out error))return false;
        try{ValidateAssets(data);}catch(Exception e){error=e.Message;return false;}
        StartCoroutine(LoadGuarded(data));return true;
    }
    void ValidateAssets(GameSaveData data){
        if(!Application.CanStreamedLevelBeLoaded(data.scene))throw new InvalidDataException("场景不在当前构建中");
        foreach(var i in data.items){var asset=Registry.Resolve<ItemData>(i.key);if(!asset||asset.itemId!=i.id)throw new InvalidDataException("存档物品资源缺失："+i.id);}
        foreach(var f in data.files){var asset=Registry.Resolve<FileData>(f.key);if(!asset||asset.fileId!=f.id)throw new InvalidDataException("存档文件资源缺失："+f.id);}
    }
    IEnumerator LoadGuarded(GameSaveData data){
        IsRestoring=true;LastError=null;GetComponent<GamePause>()?.SetPaused(false);
        if(loadingCover){loadingCover.SetActive(false);Destroy(loadingCover);}
        var root=RuntimePanelUI.Canvas("SaveRestoreCurtain",transform,32700);loadingCover=root.gameObject;
        RuntimePanelUI.Panel("Black",root,Vector2.zero,Vector2.one,Color.black);
        controls.ResetForLoad();controls.AcquireLock(ControlLockReason.SaveRestore,this);
        var sequence=RestoreRoutine(data);
        while(true){bool more=false;object current=null;try{more=sequence.MoveNext();if(more)current=sequence.Current;}catch(Exception e){LastError="载入未完成："+e.Message;}
            if(LastError!=null||!more)break;yield return current;}
        if(LastError==null){controls.ReleaseLock(ControlLockReason.SaveRestore,this);IsRestoring=false;GameplayInput.ConsumeFrame();Destroy(loadingCover);LoadFinished?.Invoke(true,null);}
        else{IsRestoring=false;LoadFinished?.Invoke(false,LastError);Debug.LogError(LastError);ShowRecovery(data.slot);}
    }
    void ShowRecovery(int slot){
        var text=GetComponentInChildren<TMPro.TMP_Text>(true);var font=text?text.font:TMPro.TMP_Settings.defaultFontAsset;
        var root=loadingCover.GetComponent<RectTransform>();
        RuntimePanelUI.Text(LastError+"\n可以重试载入，或重新开始。已有存档会保留。",root,font,new Vector2(.1f,.52f),new Vector2(.9f,.75f),26);
        RuntimePanelUI.Button("重试载入",root,font,new Vector2(.2f,.3f),new Vector2(.47f,.43f),()=>{if(!Load(slot,out var error)){LastError=error;Debug.LogWarning(error);}});
        RuntimePanelUI.Button("重新开始",root,font,new Vector2(.53f,.3f),new Vector2(.8f,.43f),()=>P01SessionRestart.Begin(gameObject));
    }
    IEnumerator RestoreRoutine(GameSaveData data){
        P03SceneLayoutMigration.Apply(data);P04SceneLayoutMigration.Apply(data);
        var dialogue=GetComponentInChildren<DialogueManager>(true);if(dialogue&&dialogue.IsDialogueActive())dialogue.CancelDialogue(dialogue.CurrentDialogue);
        GetComponent<CameraModeController>()?.AbortCapture();GetComponent<OilLampController>()?.LowerLamp();GetComponentInChildren<InventoryUI>(true)?.CloseInventory();
        GetComponent<SchoolChaseSession>()?.ResetForDiskLoad();GetComponent<EndingSession>()?.ResetForDiskLoad();
        story.RestoreSave(data.story,false);quests.Restore(data.quests);acquisition.Restore(data.firstObtained);
        var snapshot=new InventoryManager.SessionSnapshot{items=data.items.Select(i=>Registry.Resolve<ItemData>(i.key)).ToArray(),display=new Dictionary<ItemData,(string name,string description)>()};
        foreach(var i in data.items)snapshot.display[Registry.Resolve<ItemData>(i.key)]=(i.displayName,i.description);
        inventory.RestoreSession(snapshot);archive.RestoreSession(data.files.Select(f=>Registry.Resolve<FileData>(f.key)).ToArray());
        var load=SceneManager.LoadSceneAsync(data.scene);while(!load.isDone)yield return null;
        float deadline=Time.realtimeSinceStartup+25;int quiet=0;
        while(quiet<3){
            bool ready=true;foreach(ControlLockReason r in Enum.GetValues(typeof(ControlLockReason)))if(r!=ControlLockReason.SaveRestore&&controls.IsLockedBy(r))ready=false;
            var child=FindAnyObjectByType<Scene03Chapter>();if(child&&!child.Ready)ready=false;
            var school=FindAnyObjectByType<SchoolSceneController>();if(school&&!school.Ready)ready=false;
            quiet=ready?quiet+1:0;if(Time.realtimeSinceStartup>deadline)throw new InvalidOperationException("场景初始化超时，未放开玩家控制");yield return null;
        }
        if(data.school.present&&story.Has(StoryFlag.SchoolChaseCompleted))FindAnyObjectByType<SchoolChaseController>()?.RestoreCompletedDiskState(data.position);
        var p=controls.ActivePlayer;if(!p)throw new InvalidOperationException("缺少玩家");
        var layout=FindAnyObjectByType<SchoolLayoutController>();
        if(data.school.present){if(!layout||layout.data.Room(data.school.room)==null)throw new InvalidOperationException("学校房间不存在");layout.ApplyVariant((SchoolWorldVariant)data.school.variant);layout.SetNarrative((SchoolActor)data.school.actor,(SchoolPhase)data.school.phase);layout.EnterRoom(data.school.room);}
        var corridor=FindAnyObjectByType<DistortedCorridorController>();if(corridor)corridor.RestoreSave(data.distortedJoined,data.corridorLampStage);
        var doors=FindAnyObjectByType<ThreeDoorPuzzleController>();if(doors)doors.RestoreSave(data.threeDoorProgress);
        p.SetPlanarMovement(data.planar);p.transform.localScale=data.scale;EndingSession.Place(p,data.position);
        p.GetComponent<P02ActorPresentation>()?.RestoreDirection(string.IsNullOrEmpty(data.actorFacing)?(data.facing<0?"left":"right"):data.actorFacing);
        var cam=Camera.main;var follow=cam.GetComponent<CameraFollow>();follow.SetTarget(p.transform);follow.SetFacing(data.facing);
        follow.SetLimits(data.cameraX.x,data.cameraX.y);follow.SetVerticalLimits(data.followY,data.cameraOffset,data.cameraY.x,data.cameraY.y);
        cam.orthographicSize=data.cameraSize;cam.transform.position=data.cameraPosition;RoomPresentationFraming.PrepareCurrentView();
        GetComponent<CameraModeController>().ContextAvailable=data.cameraAvailable;
        // A cold load may return directly to Scene01 without visiting the
        // chapter installer. Restore the owned lamp's runtime dependency only;
        // ownership, exhaustion and use eligibility still come from the save.
        var restoredLamp=GetComponent<OilLampController>();
        if(!restoredLamp){
            var lampItem=snapshot.items.FirstOrDefault(i=>i&&i.itemId=="key_oil_lamp");
            if(lampItem){restoredLamp=gameObject.AddComponent<OilLampController>();restoredLamp.ConfigureItem(lampItem);}
        }
        restoredLamp?.RestoreUseState(data.lampRaised);
        inventory.RestoreSession(snapshot);archive.RestoreSession(data.files.Select(f=>Registry.Resolve<FileData>(f.key)).ToArray());
        quests.Restore(data.quests);acquisition.Restore(data.firstObtained);story.RefreshObservers();CompanionSaveState.Restore(data.companions);
        GameplayClock.Restore(data.playSeconds);Physics2D.SyncTransforms();yield return null;
    }
}
