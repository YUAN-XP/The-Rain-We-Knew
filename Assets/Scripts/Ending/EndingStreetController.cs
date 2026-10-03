using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class EndingStreetController:EndingStageController
{
    public Transform guide,zhang,policeCar,driverDoor,rearDoor,playerWait;
    public DialogueInteractable originalGuide,originalCar;
    public Collider2D[] firstVisitOnlyColliders;
    public GameObject[] firstVisitOnlyObjects;
    public GameObject endingViews;
    public EndingStreetInteractable zhangInteraction;
    CameraFollow follow; Camera cam;
    bool activeVisit,followGuide,noticeRunning,panOwned;
    Vector3 panPosition;Vector2 panX,panY;bool panFollowY,panEnabled;float panOffset,panFacing,panSize;
    float lastPlayerX;
    P02CompanionFollow nativeFollow;
    public bool HasFollowingGuide=>activeVisit&&followGuide;
    public float SaveFollowDirection=>nativeFollow?nativeFollow.FacingSide:1;
    public void RestoreFollowDirection(float direction){if(nativeFollow)nativeFollow.RestoreSide(direction);if(controls&&controls.ActivePlayer)lastPlayerX=controls.ActivePlayer.transform.position.x;guide?.GetComponent<P02ActorPresentation>()?.RestoreFacing(direction<0);}
    Rigidbody2D boardingBody;bool boardingSimulated;
    Vector3 zhangWaitingPosition,boardingPlayerPosition;bool hasWaitingPosition,boardingInProgress;
    public string Prompt=>!activeVisit||busy?"":story.Has(StoryFlag.EndingZhangCompleted)?"[E] 上车":"[E] 对话";
    protected override bool PauseAvailable=>activeVisit;
    protected override IEnumerator Begin(){
        activeVisit=session.IsEndingVisit;
        if(endingViews!=null)endingViews.SetActive(activeVisit);
        if(!activeVisit)yield break;
        if(guide==null||zhang==null||policeCar==null){Debug.LogError("Ending street bindings missing.",this);yield break;}
        if(!hasWaitingPosition){zhangWaitingPosition=zhang.position;hasWaitingPosition=true;}
        if(!session.SequenceStarted){zhang.position=zhangWaitingPosition;zhang.gameObject.SetActive(true);}
        foreach(var x in firstVisitOnlyColliders)if(x!=null)x.enabled=false;
        foreach(var x in firstVisitOnlyObjects)if(x!=null)x.SetActive(false);
        if(originalGuide!=null)originalGuide.enabled=false;if(originalCar!=null)originalCar.enabled=false;
        foreach(var c in guide.GetComponents<Collider2D>())c.enabled=false;
        guide.gameObject.SetActive(true);guide.position=EndingSession.CompanionAnchor(guide,controls.ActivePlayer,-data.followDistance,data.guideVisualGroundOffset);
        var painted=guide.Find("NewArt_Actor");if(!painted)painted=guide.Find("QiuTian_Visual");
        P02ActorPresentation.AttachCompanion(guide,painted?painted.GetComponent<SpriteRenderer>():guide.GetComponent<SpriteRenderer>(),"qt_adult",1.8f);
        nativeFollow=guide.GetComponent<P02CompanionFollow>()??guide.gameObject.AddComponent<P02CompanionFollow>();
        nativeFollow.Begin(controls.ActivePlayer.transform,data.followDistance);nativeFollow.Relocated();followGuide=true;lastPlayerX=controls.ActivePlayer.transform.position.x;
        runtime.GetComponent<CameraModeController>()?.SetCaptureContext(null);
        cam=Camera.main;follow=cam.GetComponent<CameraFollow>();
        if(inventory.HasItem(data.originalHolder))inventory.SetRuntimePresentation(data.originalHolder,"破旧证件套",data.originalHolder.description);
        if(session.SequenceStarted){yield return LoadSequence();yield break;}
        while(controls.IsLocked())yield return null;
        if(!story.Has(StoryFlag.EndingStreetPanCompleted))yield return Pan();
    }
    IEnumerator Pan(){
        // Capture every camera field this shot can affect before acquiring our lock.
        panPosition=cam.transform.position;panX=follow.HorizontalLimits;panY=follow.VerticalLimits;panFollowY=follow.FollowsY;panOffset=follow.VerticalOffset;panFacing=follow.Facing;panSize=cam.orthographicSize;panEnabled=follow.enabled;panOwned=true;
        Lock();follow.enabled=false;
        var target=new Vector3(policeCar.position.x-1,panPosition.y,panPosition.z);
        yield return Move(cam.transform,target,data.panSeconds);yield return Wait(data.panHold);yield return Move(cam.transform,panPosition,data.panSeconds);
        Unlock();RestorePan();story.Set(StoryFlag.EndingStreetPanCompleted);
    }
    void RestorePan(){if(!panOwned||follow==null)return;follow.SetLimits(panX.x,panX.y);follow.SetVerticalLimits(panFollowY,panOffset,panY.x,panY.y);follow.SetFacing(panFacing);cam.orthographicSize=panSize;cam.transform.position=panPosition;follow.enabled=panEnabled;panOwned=false;}
    public void Interact(){
        if(!ready||!activeVisit||busy||paused||controls.IsLocked()||dialogue.DidDialogueEndThisFrame())return;
        if(!story.Has(StoryFlag.EndingZhangCompleted)){StartCoroutine(Talk());return;}
        if(!CanBoard){runtime.GetComponent<StoryNoticeUI>().Show(inventory.HasItem(data.identityCard)&&inventory.HasItem(data.originalHolder)?"先把证件整理好。可以在背包中组合。":"先把证件整理好。",4);return;}
        StartCoroutine(Board());
    }
    public bool CanBoard=>ready&&story.Has(StoryFlag.EndingZhangCompleted)&&story.Has(StoryFlag.CompleteCredentialArchived)&&files.HasFile(data.completeCredential);
    IEnumerator Talk(){Lock();for(int n=1;n<=8;n++){yield return Say("Z"+n.ToString("00"));if(!speechCompleted){Unlock();yield break;}}
        story.Set(StoryFlag.EndingZhangCompleted);yield return FreshKeys();Unlock();}
    protected override void Update(){
        base.Update();if(!ready||!activeVisit)return;
        if(followGuide&&!busy&&!paused&&!controls.IsLocked()){
            var player=controls.ActivePlayer.transform;float dx=player.position.x-lastPlayerX;
            var grounded=EndingSession.CompanionAnchor(guide,controls.ActivePlayer,0,data.guideVisualGroundOffset);guide.position=new Vector3(guide.position.x,grounded.y,guide.position.z);
            if(Mathf.Abs(dx)>.0001f&&Mathf.Abs(player.position.x-guide.position.x)>data.followDistance+.15f)
                guide.position=Vector3.MoveTowards(guide.position,EndingSession.CompanionAnchor(guide,controls.ActivePlayer,-Mathf.Sign(dx)*data.followDistance,data.guideVisualGroundOffset),data.followSpeed*Delta);
            lastPlayerX=player.position.x;
        }
        if(nativeFollow)nativeFollow.Observe(Delta,!followGuide||busy||paused||controls.IsLocked());
        if(!noticeRunning&&!story.Has(StoryFlag.EndingIdentityHintIssued)&&inventory.HasItem(data.identityCard)&&inventory.HasItem(data.originalHolder))StartCoroutine(Hint());
    }
    IEnumerator Hint(){noticeRunning=true;while(controls.IsLocked()||paused)yield return null;
        if(inventory.HasItem(data.identityCard)&&inventory.HasItem(data.originalHolder)&&!story.Has(StoryFlag.EndingIdentityHintIssued)){
            inventory.SetRuntimePresentation(data.originalHolder,"破旧证件套",data.originalHolder.description);
            runtime.GetComponent<StoryNoticeUI>().Show("证件与破旧证件套可以在背包中组合。",5);story.Set(StoryFlag.EndingIdentityHintIssued);}
        noticeRunning=false;
    }
    IEnumerator Board(){
        if(!CanBoard)yield break;Lock();followGuide=false;if(nativeFollow)nativeFollow.End();boardingInProgress=true;boardingPlayerPosition=controls.ActivePlayer.transform.position;
        var player=controls.ActivePlayer;var body=player.GetComponent<Rigidbody2D>();bool simulated=body.simulated;boardingBody=body;boardingSimulated=simulated;body.simulated=false;
        try {
            yield return Move(player.transform,playerWait.position,data.boardingSeconds*.6f);
            var comic=DemoComicSequence.For(gameObject);comic.BeginBoarding();
            yield return Move(zhang,driverDoor.position,data.boardingSeconds);zhang.gameObject.SetActive(false);yield return comic.BoardingStep(0);
            yield return Move(guide,rearDoor.position+Vector3.up*.4f,data.boardingSeconds);guide.gameObject.SetActive(false);yield return comic.BoardingStep(1);
            yield return Move(player.transform,rearDoor.position,data.boardingSeconds);yield return comic.BoardingStep(2);
            yield return comic.FinishBoarding();
            // Only the scene transition hides the original actor; boarding completion follows all three motions.
            session.SequenceStarted=true;boardingInProgress=false;yield return LoadSequence();
        } finally {if(body!=null)body.simulated=simulated;boardingBody=null;}
    }
    IEnumerator LoadSequence(){
        if(!Application.CanStreamedLevelBeLoaded(data.sequenceScene)){Debug.LogError("Ending sequence scene is not registered.",this);session.SequenceStarted=false;Unlock();yield break;}
        if(BlackSceneTransition.Begin(data.sequenceScene,controls))
            while(BlackSceneTransition.IsActive)yield return null;
    }
    protected override void OnDisable(){
        if(boardingInProgress&&controls!=null&&controls.ActivePlayer!=null&&!session.SequenceStarted){EndingSession.Place(controls.ActivePlayer,boardingPlayerPosition);if(zhang!=null){zhang.position=zhangWaitingPosition;zhang.gameObject.SetActive(true);}if(guide!=null)guide.gameObject.SetActive(true);}
        boardingInProgress=false;if(nativeFollow)nativeFollow.End();
        base.OnDisable();RestorePan();if(boardingBody!=null)boardingBody.simulated=boardingSimulated;boardingBody=null;}
}
