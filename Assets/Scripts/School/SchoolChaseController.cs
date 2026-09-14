using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed partial class SchoolChaseController : MonoBehaviour, ISchoolOfficeExitHandler
{
    public SchoolSceneController school;
    public SchoolChaseCaptureContext capture;
    public DialogueData openingDialogue;
    public SchoolHomeData homeData;
    [Header("Alpha chase parameters; actual held-input timing is a separate test")]
    [Min(.1f)] public float enemySpeed=6.3f;
    [Min(.1f)] public float finalEnemySpeed=.85f;
    [Min(.1f)] public float catchDistance=.7f;
    [Min(1)] public float safeSpawnDistance=8f;
    public Vector2 randomInterval=new Vector2(3.5f,5f);
    [Min(.05f)] public float doorwayGrace=.55f;
    [Header("Fixed threat locations in the existing RoomIDs")]
    public float officeThreatX=49f, readingThreatX=208.8f, roofMidpointX=40f, thirdLeftThreatX=24f;
    [Min(0)] public float openingCameraPan=6.8f;
    public SchoolChaseStep Step {get;private set;}
    public SchoolChaseRoute Route {get;private set;}
    public bool IsActive => Step!=SchoolChaseStep.Dormant&&Step!=SchoolChaseStep.Returned;
    public bool IsEscaping => Step==SchoolChaseStep.Escaping;
    public bool CanUseCamera => IsEscaping&&!doorPending&&!school.SecurityPanelOpen;
    public bool IsPreviewing {get;private set;}
    public bool EnemyActive => enemy!=null&&enemy.activeSelf;
    public string EnemyRoom {get;private set;}
    public Vector2 EnemyPosition => enemy!=null?(Vector2)enemy.transform.position:Vector2.zero;
    public float RandomRemaining=>randomRemaining;
    public int DeathCount {get;private set;}
    public int RunId=>session!=null?session.Generation:0;
    public string LastSwitchRefusal {get;private set;}="";
    public SchoolChaseSession Session=>session;
    SchoolLayoutController Layout=>school.layout;
    GameControlManager Controls=>school.Controls;
    StoryStateManager Story=>school.Story;
    CameraModeController cameraTool;
    OilLampController lamp;
    SchoolChaseSession session;
    PlayerController adult;
    ScreenFadeController fade;
    GameObject enemy,deathPanel,previewRoot;
    Sprite enemySprite;
    readonly Queue<SchoolPortalDefinition> enemyDoors=new Queue<SchoolPortalDefinition>();
    readonly Dictionary<SpriteRenderer,bool> hiddenSprites=new Dictionary<SpriteRenderer,bool>();
    readonly Dictionary<Canvas,bool> hiddenCanvases=new Dictionary<Canvas,bool>();
    System.Random random=new System.Random();
    DialogueData ownedDialogue;
    bool ownsSequence,ready,doorPending,roofCrossed,retryArmed,openingCompleted;
    float randomRemaining,grace,enemyDoorDelay,previousRoofX;
    int inputAfterFrame;
    string pendingPortal;
    RoomTransitionInteractable activeTransition;
    CameraFollow openingFollow;
    bool openingFollowWasEnabled;

    public bool HasSessionToRestore => PersistentRuntime.Instance!=null&&PersistentRuntime.Instance.GetComponent<SchoolChaseSession>()!=null
        && PersistentRuntime.Instance.GetComponent<SchoolChaseSession>().HasCheckpoint
        && !PersistentRuntime.Instance.GetComponent<StoryStateManager>().Has(StoryFlag.SchoolReturnedHome);
    public void Initialize()
    {
        if(ready)return;
        var runtime=PersistentRuntime.Instance;
        if(runtime==null||school==null||!school.Ready||capture==null||openingDialogue==null) {Debug.LogWarning("School chase needs its configured school, context and dialogue.",this);return;}
        session=runtime.GetComponent<SchoolChaseSession>()??runtime.gameObject.AddComponent<SchoolChaseSession>();
        cameraTool=runtime.GetComponent<CameraModeController>();lamp=runtime.GetComponent<OilLampController>();fade=FindAnyObjectByType<ScreenFadeController>();
        adult=runtime.GetComponentsInChildren<AdultOutfitController>(true).Select(x=>x.GetComponentInParent<PlayerController>(true)).FirstOrDefault();
        if(adult==null||cameraTool==null||lamp==null||fade==null){Debug.LogWarning("School chase could not resolve the original adult and tools.",this);return;}
        capture.chase=this;
        enemy=new GameObject("XiangYan_ChaseActor");enemy.transform.SetParent(transform,false);
        enemySprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,Texture2D.whiteTexture.width,Texture2D.whiteTexture.height),Vector2.one*.5f,Texture2D.whiteTexture.width);
        MakeEnemyPart("Body",Vector2.zero,new Vector2(.75f,1.55f),new Color(.17f,.13f,.19f),20);
        MakeEnemyPart("Head",new Vector2(0,.89f),new Vector2(.53f,.5f),new Color(.5f,.37f,.4f),21);
        MakeEnemyPart("Eyes",new Vector2(0,.92f),new Vector2(.39f,.07f),new Color(.96f,.36f,.3f),22);
        enemy.SetActive(false);ready=true;InitializeSecurity();
        var weather=GetComponent<SchoolChaseWeather>()??gameObject.AddComponent<SchoolChaseWeather>();weather.chase=this;
        var canvas=RuntimePanelUI.Canvas("SchoolChaseDeath",transform,245);
        deathPanel=canvas.gameObject;RuntimePanelUI.Panel("Black",canvas,Vector2.zero,Vector2.one,Color.black);
        RuntimePanelUI.Text("你的意识消散了",canvas,school.font,new Vector2(.2f,.43f),new Vector2(.8f,.59f),40);
        RuntimePanelUI.Text("[E] 重试",canvas,school.font,new Vector2(.3f,.28f),new Vector2(.7f,.37f),27);deathPanel.SetActive(false);
    }
    void MakeEnemyPart(string name,Vector2 position,Vector2 size,Color color,int order)
    {
        var g=new GameObject(name);g.transform.SetParent(enemy.transform,false);g.transform.localPosition=position;g.transform.localScale=size;
        var r=g.AddComponent<SpriteRenderer>();r.sprite=enemySprite;r.color=color;r.sortingOrder=order;
    }
    public bool TryExit(SchoolYouthController flow)
    {
        if(!ready)Initialize();
        if(!ready||flow!=school.youthFlow||Step!=SchoolChaseStep.Dormant||!flow.IsYouthMemory||Layout.CurrentRoom!="StaffOffice"
            ||!Story.Has(StoryFlag.SchoolEvidenceSubmitted)||!Story.Has(StoryFlag.SchoolOfficeHandoffReady)||Controls.IsLocked()||flow.Busy)return false;
        var checkpoint=SchoolChaseCheckpoint.Capture(school,SchoolCheckpointReason.Office,SchoolChaseRoute.None);
        var portal=FindPortal("StaffOffice_Door_BtoA");
        if(checkpoint==null||portal==null)return false;
        session.Store(checkpoint);session.AdvanceGeneration();Step=SchoolChaseStep.Opening;Route=SchoolChaseRoute.OfficePressure;
        openingCompleted=false;roofCrossed=false;doorPending=true;pendingPortal=portal.portalId;
        // The original transition moves the youth. Its Arrived callback then
        // starts adult handover; never substitute hardcoded world coordinates.
        activeTransition=portal.transition;portal.transition.Interact();return true;
    }
    public bool HandlePortal(SchoolPortal portal)
    {
        if(!IsActive)return false;
        if((!IsEscaping&&Step!=SchoolChaseStep.Exploring)||doorPending||!CanPass(portal.Definition)){return true;}
        RecordDoor(portal.Definition);portal.transition.Interact();return true;
    }
    public bool CanPass(SchoolPortalDefinition d)
    {
        if(d==null)return false;
        if(d.sourceRoom=="StaffOffice"||d.targetRoom=="StaffOffice"||d.targetRoom=="MaleToilet"||d.targetRoom=="EquipmentRoom")return false;
        if(!IsEscaping&&Step!=SchoolChaseStep.Exploring)return false;
        return (Layout.Variant==SchoolWorldVariant.Memory?d.memory:d.otherworld)==SchoolPortalGate.Open;
    }
    public bool HandleLegacy(SchoolInteractable source)
    {
        if(!IsActive)return false;
        if(Step==SchoolChaseStep.Exploring&&!doorPending){
            if(source.action==SchoolAction.FogReturn){BeginHomeReturn();return true;}
            if(source.action==SchoolAction.SecurityDoor){
                var d=Layout.data.Portal(source.destinationRoomId=="SecurityRoom"?"Security_Door_AtoB":"Security_Door_BtoA");
                RecordDoor(d);source.transition.Interact();return true;
            }
            if(source.action==SchoolAction.Investigation)return false;
        }
        if(!IsEscaping||doorPending)return true;
        if(source.action==SchoolAction.SecurityDoor&&Layout.CurrentRoom=="Floor01"&&Layout.Variant==SchoolWorldVariant.Otherworld)
        {securityDoor=source;school.ShowSecurityPanel();}
        return true;
    }
    SchoolInteractable securityDoor;
    public bool TrySecurityCode(string code)
    {
        if(!IsEscaping||Layout.Actor!=SchoolActor.Adult||Layout.Variant!=SchoolWorldVariant.Otherworld
            ||!Story.Has(StoryFlag.SchoolSecurityClueSeen)||code!="4268"||securityDoor==null)return false;
        var d=Layout.data.Portal("Security_Door_AtoB");
        // Pause is owned before dismissing the password UI; no vulnerable gap.
        RecordDoor(d);school.CloseModal();securityDoor.transition.Interact();return true;
    }
    void RecordDoor(SchoolPortalDefinition d)
    {
        doorPending=true;pendingPortal=d.id;
        activeTransition=FindPortal(d.id)?.transition;
        if(activeTransition==null)activeTransition=Layout.GetComponentsInChildren<SchoolInteractable>(true)
            .FirstOrDefault(i=>i.gameObject.activeInHierarchy&&i.action==SchoolAction.SecurityDoor&&i.destinationRoomId==d.targetRoom)?.transition;
        if(EnemyActive)enemyDoors.Enqueue(d);
    }
    public void PortalArrived(string id)
    {
        if(!IsActive||!doorPending||pendingPortal!=id)return;
        doorPending=false;pendingPortal=null;activeTransition=null;grace=doorwayGrace;
        if(Step==SchoolChaseStep.Exploring){SaveCompletedResume();return;}
        if(Step==SchoolChaseStep.Opening&&id=="StaffOffice_Door_BtoA") {StartCoroutine(OpenChase());return;}
        if(!IsEscaping)return;
        if(id=="Stair_F03_Roof_Right_AtoB"&&Route==SchoolChaseRoute.OfficePressure) {
            Route=SchoolChaseRoute.RoofCrossing;roofCrossed=false;previousRoofX=Controls.ActivePlayer.transform.position.x;ClearEnemy();
        }
        else if(id=="Stair_F03_Roof_Left_BtoA"&&Route==SchoolChaseRoute.RoofCrossing&&roofCrossed) {
            Route=SchoolChaseRoute.ThirdFloorLeft;ClearEnemy();
        }
        else if(id=="Stair_F02_F03_Left_BtoA"&&Route==SchoolChaseRoute.ThirdFloorLeft) {
            Route=SchoolChaseRoute.LowerEscape;ResetRandom();
        }
        else if(id=="Security_Door_AtoB"&&Layout.CurrentRoom=="SecurityRoom") {
            // No RoofVisited lock here: shortcut prevention must work in the
            // actual route, not strand a bypassing player at the final door.
            Step=SchoolChaseStep.SecurityArrival;ClearEnemy();cameraTool.SetCaptureContext(null);cameraTool.ContextAvailable=false;
            StartCoroutine(CaptureSecurityArrival());
        }
        else if(Route==SchoolChaseRoute.OfficePressure&&Layout.CurrentRoom=="ReadingPassage") {
            ClearEnemy();EnsureFixedThreat();
        }
    }
    IEnumerator CaptureSecurityArrival()
    {
        float timeout=Time.realtimeSinceStartup+2;
        while(Time.realtimeSinceStartup<timeout) {
            yield return new WaitForFixedUpdate();
            var bodyCollider=Controls.ActivePlayer.GetComponent<Collider2D>();
            if(bodyCollider!=null&&SchoolChaseGeometry.Ground(Layout,Layout.Variant,Layout.CurrentRoom,Controls.ActivePlayer.transform.position.x,out float ground)
                &&Mathf.Abs(bodyCollider.bounds.min.y-ground)<.055f)break;
        }
        if(Step!=SchoolChaseStep.SecurityArrival)yield break;
        var settled=Controls.ActivePlayer.GetComponent<Collider2D>();
        if(settled==null||!SchoolChaseGeometry.Ground(Layout,Layout.Variant,Layout.CurrentRoom,Controls.ActivePlayer.transform.position.x,out float floor)
            ||Mathf.Abs(settled.bounds.min.y-floor)>=.055f){Notice("安保室落点尚未就绪。",4);Die();yield break;}
        Story.Set(StoryFlag.SchoolSecurityEntered);
        var cp=SchoolChaseCheckpoint.Capture(school,SchoolCheckpointReason.Security,Route);
        if(cp!=null)session.Store(cp);
        OnSecurityReady();
    }
    partial void OnSecurityReady();
    IEnumerator OpenChase()
    {
        OwnSequence();cameraTool.AbortCapture();lamp.LowerLamp();
        yield return fade.FadeThroughBlack(()=>SetActor(SchoolActor.Adult,Controls.ActivePlayer.transform.position),this);
        Layout.SetNarrative(SchoolActor.Adult,SchoolPhase.Chase);Story.Set(StoryFlag.SchoolChaseStarted);
        SetHorrorPresentation(true);
        // Left approach can be seen during dialogue but has no capture clock.
        Spawn("Floor03Right",officeThreatX,false);
        var shot=Camera.main;var follow=shot.GetComponent<CameraFollow>();
        openingFollow=follow;openingFollowWasEnabled=follow.enabled;follow.enabled=false;
        shot.transform.position=new Vector3(Mathf.Clamp(shot.transform.position.x-openingCameraPan,follow.HorizontalLimits.x,follow.HorizontalLimits.y),shot.transform.position.y,shot.transform.position.z);
        yield return Say(openingDialogue);
        if(!openingCompleted){Die();yield break;}
        Notice("相机似乎有反应...",3);
        yield return new WaitForSecondsRealtime(.8f);
        while(Input.GetKey(KeyCode.E)||Input.GetKey(KeyCode.Space))yield return null;
        inputAfterFrame=Time.frameCount+1;grace=doorwayGrace;Step=SchoolChaseStep.Escaping;
        cameraTool.ContextAvailable=true;cameraTool.SetCaptureContext(capture);RestoreOpeningCamera();follow.SnapToTarget();ReleaseSequence();
    }
    IEnumerator Say(DialogueData data)
    {
        openingCompleted=false;if(data==null)yield break;
        while(school.Dialogue.IsDialogueActive())yield return null;
        int count=school.Dialogue.CompletedCount;ownedDialogue=data;school.Dialogue.StartDialogue(data);yield return null;
        while(school.Dialogue.IsDialogueActive())yield return null;
        openingCompleted=school.Dialogue.CompletedCount>count&&school.Dialogue.LastCompletedDialogue==data;ownedDialogue=null;yield return null;
    }
    public void SetActor(SchoolActor actor,Vector2 position)
    {
        var target=actor==SchoolActor.Adult?adult:school.youth;
        var other=actor==SchoolActor.Adult?school.youth:adult;
        // On a school reload the authored inactive youth has not run Awake.
        // An inactive actor already cannot move; do not call its Rigidbody API
        // until it has been activated and initialized.
        if(other.gameObject.activeInHierarchy)other.SetMovementEnabled(false);
        other.gameObject.SetActive(false);
        target.gameObject.SetActive(true);target.transform.position=position;
        var b=target.GetComponent<Rigidbody2D>();if(b!=null){b.position=position;b.linearVelocity=Vector2.zero;b.angularVelocity=0;}
        target.GetComponent<PlayerInteraction>()?.BindRuntime(PersistentRuntime.Instance.GetComponentInChildren<InteractionUI>(true),school.Dialogue);
        Controls.BindPlayer(target,actor==SchoolActor.Adult);Layout.EnterRoom(Layout.CurrentRoom);Physics2D.SyncTransforms();
    }
    public bool Paused => Controls==null||doorPending||school.SecurityPanelOpen||Time.timeScale<=0
        ||Controls.IsLockedBy(ControlLockReason.Inventory)||Controls.IsLockedBy(ControlLockReason.CloseUp)
        ||Controls.IsLockedBy(ControlLockReason.Dialogue)||Controls.IsLockedBy(ControlLockReason.Interaction)
        ||Controls.IsLockedBy(ControlLockReason.SceneTransition)||Controls.IsLockedBy(ControlLockReason.SchoolSequence)
        ||cameraTool!=null&&cameraTool.IsBusy;
    void Update()
    {
        if(!ready)return;
        if(Step==SchoolChaseStep.Opening&&EnemyActive&&ownedDialogue!=null)
            MoveEnemyTowards(officeThreatX+1.4f,.6f*Time.unscaledDeltaTime);
        if(Step==SchoolChaseStep.Dead){
            if(!Input.GetKey(KeyCode.E))retryArmed=true;
            if(retryArmed&&Input.GetKeyDown(KeyCode.E)&&Time.frameCount>inputAfterFrame)StartCoroutine(RestoreCheckpoint());return;
        }
        TickSecurity();
        if(!IsEscaping||Time.frameCount<=inputAfterFrame||Paused)return;
        float dt=Time.unscaledDeltaTime;grace=Mathf.Max(0,grace-dt);
        if(Route==SchoolChaseRoute.RoofCrossing&&Layout.CurrentRoom=="Roof") {
            float x=Controls.ActivePlayer.transform.position.x;
            // A continuous right-to-left crossing in the actual roof RoomID.
            // Merely loading/teleporting into the left half is not traversal.
            if(previousRoofX>roofMidpointX&&x<=roofMidpointX&&previousRoofX-x<=Mathf.Max(1,20*dt))roofCrossed=true;
            previousRoofX=x;
        }
        EnsureFixedThreat();
        if(!EnemyActive&&Route==SchoolChaseRoute.LowerEscape){randomRemaining-=dt;if(randomRemaining<=0)TryRandomSpawn();}
        TickEnemy(dt);
    }
    partial void TickSecurity();
    void EnsureFixedThreat()
    {
        if(EnemyActive||!IsEscaping)return;
        if(Route==SchoolChaseRoute.OfficePressure&&Layout.Variant==SchoolWorldVariant.Memory) {
            if(Layout.CurrentRoom=="Floor03Right")Spawn(Layout.CurrentRoom,officeThreatX,true);
            else if(Layout.CurrentRoom=="ReadingPassage")Spawn(Layout.CurrentRoom,readingThreatX,true);
        }
        else if(Route==SchoolChaseRoute.RoofCrossing&&roofCrossed&&Layout.CurrentRoom=="Roof")
            Spawn("Roof",Mathf.Min(77,Controls.ActivePlayer.transform.position.x+safeSpawnDistance+1),true);
        else if(Route==SchoolChaseRoute.ThirdFloorLeft&&Layout.CurrentRoom=="Floor03Left"&&Layout.Variant==SchoolWorldVariant.Memory)
            Spawn("Floor03Left",thirdLeftThreatX,true);
    }
    bool Spawn(string room,float x,bool safe)
    {
        if(EnemyActive||!SchoolChaseGeometry.Ground(Layout,Layout.Variant,room,x,out float ground))return false;
        Vector2 p=new Vector2(x,ground+.8f);Vector2 actor=Controls.ActivePlayer.transform.position;
        if(!SchoolChaseGeometry.Clear(Layout,Layout.Variant,room,p,new Vector2(.3f,.7f)))return false;
        if(safe&&(Mathf.Abs(x-actor.x)<safeSpawnDistance||!SchoolChaseGeometry.Segment(Layout,Layout.Variant,room,p,actor)))return false;
        EnemyRoom=room;enemy.transform.position=p;enemy.SetActive(true);enemyDoorDelay=0;return true;
    }
    void TryRandomSpawn()
    {
        float x=Controls.ActivePlayer.transform.position.x;bool left=random.Next(2)==0;
        foreach(float side in left?new[]{-1f,1f}:new[]{1f,-1f})
            if(Spawn(Layout.CurrentRoom,x+side*(safeSpawnDistance+1f+(float)random.NextDouble()*2),true)){ResetRandom();return;}
        randomRemaining=.4f; // no safe ground/reachable side; do not pile up requests
    }
    void ResetRandom(){randomRemaining=Mathf.Lerp(randomInterval.x,randomInterval.y,(float)random.NextDouble());}
    void TickEnemy(float dt)
    {
        if(!EnemyActive)return;
        if(enemyDoorDelay>0){enemyDoorDelay-=dt;return;}
        Vector2 position=enemy.transform.position;
        if(EnemyRoom!=Layout.CurrentRoom){
            if(enemyDoors.Count==0)return;
            var door=enemyDoors.Peek();if(door.sourceRoom!=EnemyRoom)return;
            Vector2 source=Layout.data.Room(door.sourceRoom).ToWorld(door.sourceLocal);
            MoveEnemyTowards(source.x,enemySpeed*dt);
            if(Mathf.Abs(enemy.transform.position.x-source.x)<.3f){
                enemyDoors.Dequeue();var dest=Layout.data.Room(door.targetRoom).ToWorld(door.destinationLocal);
                EnemyRoom=door.targetRoom;enemy.transform.position=dest;enemyDoorDelay=doorwayGrace;
            }
            return;
        }
        Vector2 actor=Controls.ActivePlayer.transform.position;
        float from=position.x;MoveEnemyTowards(actor.x,enemySpeed*dt);
        float to=enemy.transform.position.x;
        if(grace<=0&&Mathf.Abs(actor.y-enemy.transform.position.y)<1.8f
            && actor.x>=Mathf.Min(from,to)-catchDistance&&actor.x<=Mathf.Max(from,to)+catchDistance)Die();
    }
    void MoveEnemyTowards(float x,float distance)
    {
        Vector2 from=enemy.transform.position,to=new Vector2(Mathf.MoveTowards(from.x,x,distance),from.y);
        if(SchoolChaseGeometry.Segment(Layout,Layout.Variant,EnemyRoom,from,to,.3f))enemy.transform.position=to;
    }
    void ClearEnemy(){if(enemy!=null)enemy.SetActive(false);EnemyRoom=null;enemyDoors.Clear();enemyDoorDelay=0;}
    public void Die()
    {
        if(!IsActive||Step==SchoolChaseStep.Dead||Step==SchoolChaseStep.Restoring)return;
        session.AdvanceGeneration();StopAllCoroutines();OwnSequence();Step=SchoolChaseStep.Dead;DeathCount++;
        CancelOwnedTransition();
        cameraTool.AbortCapture();EndPreview();lamp.LowerLamp();school.CloseModal();inventoryUI?.CloseInventory();
        if(ownedDialogue!=null)school.Dialogue.CancelDialogue(ownedDialogue);ownedDialogue=null;
        ClearEnemy();doorPending=false;pendingPortal=null;deathPanel.SetActive(true);retryArmed=false;inputAfterFrame=Time.frameCount+1;
    }
    public IEnumerator RestoreCheckpoint()
    {
        if(session==null||session.Checkpoint==null)yield break;
        OwnSequence();Step=SchoolChaseStep.Restoring;session.AdvanceGeneration();
        CancelOwnedTransition();
        cameraTool.AbortCapture();EndPreview();school.CloseModal();ClearEnemy();
        var cp=Story.Has(StoryFlag.SchoolChaseCompleted)&&session.CompletedResume!=null&&session.Checkpoint.Reason!=SchoolCheckpointReason.FogReturn?session.CompletedResume:session.Checkpoint;
        school.Inventory.RestoreSession(cp.Inventory);
        PersistentRuntime.Instance.GetComponent<FileArchiveManager>().RestoreSession(cp.Files);
        Story.RestoreSubset(cp.LocalFlags);Story.SetOutfit(cp.Outfit);
        Layout.ApplyVariant(cp.Variant);Layout.SetNarrative(cp.Actor,cp.Phase);
        SetActor(cp.Actor,Layout.data.Room(cp.Room).ToWorld(cp.LocalPosition));Layout.EnterRoom(cp.Room);
        var p=Controls.ActivePlayer;p.transform.localScale=cp.Scale;var body=p.GetComponent<Rigidbody2D>();
        if(body!=null){body.simulated=cp.Simulated;body.linearVelocity=Vector2.zero;}
        var cam=Camera.main;var f=cam.GetComponent<CameraFollow>();cam.orthographicSize=cp.CameraSize;
        f.SetLimits(cp.CameraX.x,cp.CameraX.y);f.SetVerticalLimits(cp.FollowY,cp.CameraOffset,cp.CameraY.x,cp.CameraY.y);f.SetFacing(cp.Facing);f.SnapToTarget();
        lamp.SetStoryUsable(cp.LampUsable);lamp.RestoreUseState(cp.LampRaised);
        Route=cp.Route;roofCrossed=false;doorPending=false;pendingPortal=null;ResetRandom();grace=doorwayGrace;
        SetHorrorPresentation(cp.Reason!=SchoolCheckpointReason.Office);
        Step=cp.Reason==SchoolCheckpointReason.Office?SchoolChaseStep.Dormant:SchoolChaseStep.SecurityArrival;
        cameraTool.SetCaptureContext(cp.Reason==SchoolCheckpointReason.Office?school.captureContext:null);
        cameraTool.ContextAvailable=cp.Reason==SchoolCheckpointReason.Office;
        RestoreSecurity(cp.Reason);
        if(cp.Route==SchoolChaseRoute.Completed&&cp.LocalFlags.TryGetValue(StoryFlag.SchoolChaseCompleted,out bool completed)&&completed){
            Step=SchoolChaseStep.Exploring;Route=SchoolChaseRoute.Completed;Layout.SetNarrative(SchoolActor.Adult,SchoolPhase.SchoolRecovery);
            cameraTool.ContextAvailable=true;SyncSecurityPickups();
        }
        Physics2D.SyncTransforms();yield return new WaitForFixedUpdate();yield return null;
        while(Input.GetKey(KeyCode.E))yield return null;
        inputAfterFrame=Time.frameCount+1;deathPanel.SetActive(false);ReleaseSequence();
    }
    partial void RestoreSecurity(SchoolCheckpointReason reason);
    SchoolPortal FindPortal(string id)=>Layout.GetComponentsInChildren<SchoolPortal>(true).FirstOrDefault(p=>p.portalId==id&&p.gameObject.activeInHierarchy);
    void OwnSequence(){if(!ownsSequence){Controls.AcquireLock(ControlLockReason.SchoolSequence);ownsSequence=true;}}
    void ReleaseSequence(){if(ownsSequence&&Controls!=null)Controls.ReleaseLock(ControlLockReason.SchoolSequence);ownsSequence=false;}
    void Notice(string text,float duration=4)=>PersistentRuntime.Instance.GetComponent<StoryNoticeUI>()?.Show(text,duration);
    void OnDisable()
    {
        CancelOwnedTransition();
        StopAllCoroutines();if(cameraTool!=null){cameraTool.AbortCapture();cameraTool.SetCaptureContext(null);}EndPreview();ClearEnemy();
        if(ownedDialogue!=null&&school!=null&&school.Dialogue!=null)school.Dialogue.CancelDialogue(ownedDialogue);
        ReleaseSequence();if(deathPanel!=null)deathPanel.SetActive(false);CleanupSecurity();
    }
    partial void CleanupSecurity();
    void RestoreOpeningCamera(){if(openingFollow!=null)openingFollow.enabled=openingFollowWasEnabled;openingFollow=null;}
    void CancelOwnedTransition(){RestoreOpeningCamera();if(activeTransition!=null)activeTransition.CancelTransition();activeTransition=null;if(fade!=null)fade.CancelOwnedFade(this);}
    void OnDestroy(){if(enemySprite!=null)Destroy(enemySprite);}
}
