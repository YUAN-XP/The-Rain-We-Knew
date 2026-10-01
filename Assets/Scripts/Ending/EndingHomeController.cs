using System.Collections;
using UnityEngine;

public sealed class EndingHomeController : EndingStageController
{
    public RoomTransitionInteractable bedroomExit;
    public RoomTransitionInteractable[] rooms;
    public SchoolHomeReturnController home;
    public Transform guide;
    public DialogueInteractable oldGuideDialogue;
    public Transform approachStart,approachStop;
    bool following,noticeRunning;
    float lastPlayerX,followDirection=1;
    public float SaveFollowDirection=>followDirection;
    public void RestoreFollowDirection(float direction){followDirection=direction<0?-1:1;if(controls&&controls.ActivePlayer)lastPlayerX=controls.ActivePlayer.transform.position.x;}
    GameObject offeredCard;
    protected override bool PauseAvailable=>busy||session!=null&&session.HandoverStarted;
    protected override IEnumerator Begin(){
        if(bedroomExit==null||guide==null||approachStart==null||approachStop==null){Debug.LogError("Ending home scene bindings missing.",this);yield break;}
        while(home!=null&&home.previousReturn!=null&&!home.previousReturn.Ready)yield return null;
        bedroomExit.Arrived+=ExitedBedroom;
        foreach(var room in rooms)if(room!=null)room.MovedUnderFade+=FollowThroughRoom;
        if(story.Has(StoryFlag.EndingHandoverCompleted)){EnableFollow();FollowThroughRoom(null);}
        else if(session.HandoverStarted){EndingSession.Place(controls.ActivePlayer,session.HandoverAnchor);Camera.main.GetComponent<CameraFollow>().SnapToTarget();StartCoroutine(Handover());}
    }
    void ExitedBedroom(RoomTransitionInteractable door){
        if(!ready||busy||story.Has(StoryFlag.EndingHandoverCompleted)||!story.Has(StoryFlag.SchoolEmailRead)||!story.Has(StoryFlag.EndingEmailReactionCompleted))return;
        session.HandoverStarted=true;session.HandoverAnchor=controls.ActivePlayer.transform.position;StartCoroutine(Handover());
    }
    IEnumerator Handover(){
        Lock();MuteGuide();guide.gameObject.SetActive(true);guide.position=EndingSession.CompanionAnchor(guide,controls.ActivePlayer,approachStart.position.x-controls.ActivePlayer.transform.position.x,data.guideVisualGroundOffset);
        var stop=EndingSession.CompanionAnchor(guide,controls.ActivePlayer,approachStop.position.x-controls.ActivePlayer.transform.position.x,data.guideVisualGroundOffset);
        yield return Move(guide,stop,Vector3.Distance(guide.position,stop)/data.approachSpeed);
        string[] ids={"H01","H02","H03","H04","H05","H06","H07","H08","H09"};
        for(int n=session.HandoverNext;n<ids.Length;n++){
            if(ids[n]=="H04")ShowCard();
            if(ids[n]=="H07"&&!session.SirenPlayed)StartCoroutine(SirenCue());
            yield return Say(ids[n]);if(!speechCompleted){Unlock();yield break;}
            if(ids[n]=="H05"&&!ReceiveCard()){Debug.LogError("Identity award rejected; resume H05 without advancing.",this);Unlock();yield break;}
            session.HandoverNext=n+1;
        }
        if(offeredCard!=null)Destroy(offeredCard);
        story.Set(StoryFlag.EndingHandoverCompleted);EnableFollow();Unlock();StartCoroutine(Notice());
    }
    IEnumerator SirenCue(){while(ownedDialogue!=data.Find("H07"))yield return null;yield return Wait(data.sirenDelay);if(!session.SirenPlayed){Sound(data.siren);session.SirenPlayed=true;}}
    bool ReceiveCard(){
        if(files.HasFile(data.completeCredential))return story.Has(StoryFlag.CompleteCredentialArchived);
        if(!inventory.HasItem(data.identityCard))inventory.AddItem(data.identityCard);
        if(!inventory.HasItem(data.identityCard))return false;
        if(inventory.HasItem(data.originalHolder))inventory.SetRuntimePresentation(data.originalHolder,"破旧证件套",data.originalHolder.description);
        story.Set(StoryFlag.EndingIdentityReceived);return true;
    }
    void ShowCard(){
        if(offeredCard!=null)return;offeredCard=new GameObject("交还的证件");offeredCard.transform.position=Vector3.Lerp(guide.position,controls.ActivePlayer.transform.position,.5f)+Vector3.up*.35f;
        var r=offeredCard.AddComponent<SpriteRenderer>();r.sprite=data.bodySprite;r.color=new Color(.86f,.83f,.65f);r.sortingOrder=30;offeredCard.transform.localScale=new Vector3(.38f,.24f,1);
    }
    void MuteGuide(){if(oldGuideDialogue!=null)oldGuideDialogue.enabled=false;foreach(var c in guide.GetComponents<Collider2D>())c.enabled=false;}
    void EnableFollow(){MuteGuide();following=true;lastPlayerX=controls.ActivePlayer.transform.position.x;}
    void FollowThroughRoom(RoomTransitionInteractable room){if(!following||guide==null)return;var p=controls.ActivePlayer.transform;guide.position=EndingSession.CompanionAnchor(guide,controls.ActivePlayer,-data.followDistance,data.guideVisualGroundOffset);lastPlayerX=p.position.x;}
    protected override void Update(){
        base.Update();if(!ready)return;
        if(following&&!paused&&!busy&&!controls.IsLocked()){
            var p=controls.ActivePlayer.transform;float dx=p.position.x-lastPlayerX;
            var grounded=EndingSession.CompanionAnchor(guide,controls.ActivePlayer,0,data.guideVisualGroundOffset);guide.position=new Vector3(guide.position.x,grounded.y,guide.position.z);
            if(Mathf.Abs(dx)>.0001f){if(Mathf.Abs(p.position.x-guide.position.x)>data.followDistance+.6f)followDirection=Mathf.Sign(dx);
                Vector3 goal=EndingSession.CompanionAnchor(guide,controls.ActivePlayer,-followDirection*data.followDistance,data.guideVisualGroundOffset);
                if(Mathf.Abs(p.position.x-guide.position.x)>data.followDistance)guide.position=Vector3.MoveTowards(guide.position,goal,data.followSpeed*Delta);}
            lastPlayerX=p.position.x;
        }
        if(story.Has(StoryFlag.EndingHandoverCompleted)&&!story.Has(StoryFlag.EndingIdentityHintIssued)&&inventory.HasItem(data.identityCard)&&inventory.HasItem(data.originalHolder)&&!noticeRunning)StartCoroutine(Notice());
    }
    IEnumerator Notice(){
        noticeRunning=true;while(controls.IsLocked()||paused)yield return null;
        var ui=runtime.GetComponent<StoryNoticeUI>();ui.Show("获得证件。",3.5f);yield return Wait(3.6f);
        while(controls.IsLocked()||paused)yield return null;
        if(inventory.HasItem(data.identityCard)&&inventory.HasItem(data.originalHolder)&&!story.Has(StoryFlag.EndingIdentityHintIssued)){ui.Show("证件与破旧证件套可以在背包中组合。",5);story.Set(StoryFlag.EndingIdentityHintIssued);}
        noticeRunning=false;
    }
    protected override void OnDisable(){if(bedroomExit!=null)bedroomExit.Arrived-=ExitedBedroom;if(rooms!=null)foreach(var r in rooms)if(r!=null)r.MovedUnderFade-=FollowThroughRoom;if(offeredCard!=null)Destroy(offeredCard);following=false;noticeRunning=false;base.OnDisable();}
}
