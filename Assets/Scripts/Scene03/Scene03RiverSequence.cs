using System.Collections;
using TMPro;
using UnityEngine;

public class Scene03RiverSequence : MonoBehaviour, IInteractable
{
    public Scene03Chapter chapter;
    public GuitarRhythmGame rhythm;
    public DialogueData introduction, praise, zhaoCall, zhaoAside;
    public AudioSource music;
    public AudioClip fullBirthdaySong;
    public Transform boy, childSeat, boySeat, zhao, departure;
    public GameObject guitar;
    public Transform childVisual, fishingRod;
    public Transform[] seatedLegs;
    [Header("River view composition")]
    public Vector2 wideViewCenter = new Vector2(303,75.4f);
    public float wideViewSize = 7.1f;
    public bool usePaintedPoses;
    SpriteRenderer boyPainting;P02ActorPresentation boyView;
    public bool Busy {get;private set;}
    public bool Appreciating {get;private set;}
    public bool CanLeaveSong {get;private set;}
    bool ownsLock, ready;
    TMP_Text leaveText;
    Camera view;
    Vector3[] legPositions;
    Quaternion[] legRotations;
    Vector3 childVisualPosition, boyStandingPosition;
    bool seated, fishing;
    public string GetInteractionPrompt()=>ready&&!Busy&&!chapter.Story.Has(StoryFlag.Scene03RiverSequenceCompleted)?"[E] 坐一会儿":"";
    public void Interact()
    {
        if(!ready||Busy||chapter.Controls.IsLocked()||chapter.Story.Has(StoryFlag.Scene03RiverSequenceCompleted))return;
        StartCoroutine(Sequence());
    }
    IEnumerator Start()
    {
        if(chapter==null||rhythm==null||music==null||fullBirthdaySong==null||boy==null||childSeat==null||boySeat==null||zhao==null||departure==null||introduction==null||praise==null||zhaoCall==null||zhaoAside==null)
        {Debug.LogWarning("River sequence needs its dialogues, actors, rhythm and replaceable full song.",this);yield break;}
        while(!chapter.Ready)yield return null;
        view=Camera.main;ready=true;zhao.gameObject.SetActive(false);
        var canvas=RuntimePanelUI.Canvas("RiverSongNotice",transform,120);
        leaveText=RuntimePanelUI.Text("[E] 离开",canvas,chapter.font,new Vector2(.81f,.08f),new Vector2(.98f,.16f),27);leaveText.alpha=0;
        if(chapter.Story.Has(StoryFlag.Scene03RiverSequenceCompleted)&&!chapter.Story.Has(StoryFlag.Scene03ReturnedHome))
        {
            while(!chapter.maze.IsAtRiver)yield return null;
            chapter.Controls.AcquireLock(ControlLockReason.Cutscene,this);ownsLock=true;
            zhao.gameObject.SetActive(true);
            zhao.position=new Vector3(297.3f,70.86f,0);var standing=zhao.GetComponentInChildren<P04PaintedLife>();if(standing){standing.walking=false;standing.frames=new[]{DemoArtCatalog.Find("CHR_STORY_ZHAO_ZHANG_BASE:1")};}Release();
        }
    }
    IEnumerator Sequence()
    {
        PersistentRuntime.Instance.GetComponent<QuestProgress>()?.Publish(QuestEvent.BoyFound);
        Busy=true;chapter.Controls.AcquireLock(ControlLockReason.Cutscene,this);ownsLock=true;
        try
        {
            if(!chapter.Story.Has(StoryFlag.Scene03RhythmCompleted))
            {
                yield return chapter.Say(introduction);
                yield return Move(chapter.child.transform,childSeat.position,.8f);
                boy.position=boySeat.position;
                SetSeated(true);
                yield return Zoom(2.6f,new Vector3((childSeat.position.x+boySeat.position.x)/2,72.1f,0),1.1f);
                rhythm.Begin();
                if(!rhythm.IsRunning)yield break;
                while(rhythm.IsRunning)yield return null;
                if(!rhythm.Completed)yield break;
                chapter.Story.Set(StoryFlag.Scene03RhythmCompleted);
            }
            else {PlaceChild(childSeat.position);boy.position=boySeat.position;SetSeated(true);}
            yield return AppreciateSong();
            yield return chapter.Say(praise);
            zhao.gameObject.SetActive(true);
            zhao.position=new Vector3(294.8f,70.86f,0);var step=zhao.GetComponentInChildren<P04PaintedLife>();if(step)step.walking=true;
            yield return Move(zhao,new Vector3(297.3f,70.86f,0),2.2f);
            if(step){step.walking=false;step.frames=new[]{DemoArtCatalog.Find("CHR_STORY_ZHAO_ZHANG_BASE:1")};}
            yield return chapter.Say(zhaoCall);
            SetSeated(false);
            Vector3 childFrom=chapter.child.transform.position,boyFrom=boy.position;
            for(float t=0;t<1.4f;t+=GameplayClock.Delta)
            {float k=t/1.4f;PlaceChild(Vector3.Lerp(childFrom,departure.position,k));boy.position=Vector3.Lerp(boyFrom,departure.position+Vector3.right,k);yield return null;}
            PlaceChild(departure.position);boy.position=departure.position+Vector3.right;
            fishing=true;yield return chapter.Say(zhaoAside);fishing=false;
            chapter.Story.SetTogether(StoryFlag.Scene03RiverSequenceCompleted,StoryFlag.Scene03FreeRiver);
            PersistentRuntime.Instance.GetComponent<QuestProgress>()?.Publish(QuestEvent.ReturnControlAfterZhao);
            chapter.follow.SnapToTarget();RoomPresentationFraming.PrepareCurrentView();
        }
        finally {SetSeated(false);Busy=false;Appreciating=false;CanLeaveSong=false;if(leaveText!=null)leaveText.alpha=0;Release();}
    }
    IEnumerator AppreciateSong()
    {
        Appreciating=true;CanLeaveSong=false;leaveText.alpha=0;
        var musicManager=MusicManager.Instance;
        if(musicManager&&musicManager.Ready)yield return musicManager.PreparePerformance(music);
        music.clip=fullBirthdaySong;music.loop=false;music.volume=musicManager&&musicManager.Ready?0:.55f;music.Play();
        float began=GameplayClock.Now;Vector3 center=new Vector3(wideViewCenter.x,wideViewCenter.y,0);
        yield return Zoom(wideViewSize,center,3);
        while(music.isPlaying)
        {
            float elapsed=GameplayClock.Now-began;
            CanLeaveSong=elapsed>=8;leaveText.alpha=CanLeaveSong?Mathf.Clamp01((elapsed-8)/.8f):0;
            if(CanLeaveSong&&GameplayInput.GetKeyDown(KeyCode.E))break;
            yield return null;
        }
        if(musicManager&&musicManager.Ready)yield return musicManager.FinishPerformance(music);
        music.Stop();leaveText.alpha=0;CanLeaveSong=false;Appreciating=false;
        yield return Zoom(2.6f,new Vector3((childSeat.position.x+boySeat.position.x)/2,72.1f,0),1.5f);
    }
    IEnumerator Zoom(float size,Vector3 position,float seconds)
    {
        var from=view.transform.position;position.z=from.z;float old=view.orthographicSize;
        for(float t=0;t<seconds;t+=GameplayClock.Delta){float k=Mathf.SmoothStep(0,1,t/seconds);view.orthographicSize=Mathf.Lerp(old,size,k);view.transform.position=Vector3.Lerp(from,position,k);yield return null;}
        view.orthographicSize=size;view.transform.position=position;
    }
    IEnumerator Move(Transform actor,Vector3 target,float seconds)
    {
        Vector3 from=actor.position;
        for(float t=0;t<seconds;t+=GameplayClock.Delta){Vector3 p=Vector3.Lerp(from,target,t/seconds);if(actor==chapter.child.transform)PlaceChild(p);else actor.position=p;yield return null;}
        if(actor==chapter.child.transform)PlaceChild(target);else actor.position=target;
    }
    void PlaceChild(Vector3 position){var body=chapter.child.GetComponent<Rigidbody2D>();body.linearVelocity=Vector2.zero;body.position=position;chapter.child.transform.position=position;}
    void SetSeated(bool value)
    {
        if(value==seated||childVisual==null||seatedLegs==null)return;
        if(value)
        {
            childVisualPosition=childVisual.localPosition;boyStandingPosition=boy.position;
            legPositions=new Vector3[seatedLegs.Length];legRotations=new Quaternion[seatedLegs.Length];
            for(int i=0;i<seatedLegs.Length;i++)
            {legPositions[i]=seatedLegs[i].localPosition;legRotations[i]=seatedLegs[i].localRotation;seatedLegs[i].localPosition+=new Vector3(.15f,.2f,0);seatedLegs[i].localRotation=Quaternion.Euler(0,0,90);}
            if(!usePaintedPoses){childVisual.localPosition-=Vector3.up*.12f;boy.position-=Vector3.up*.15f;}
        }
        else
        {
            childVisual.localPosition=childVisualPosition;boy.position=boyStandingPosition;
            for(int i=0;i<seatedLegs.Length;i++){seatedLegs[i].localPosition=legPositions[i];seatedLegs[i].localRotation=legRotations[i];}
        }
        seated=value;if(boyView)boyView.SetStaticPose(value);
        if(usePaintedPoses&&chapter&&chapter.child){var art=chapter.child.GetComponent<DemoActorArtwork>();if(art)art.SetPose(value?"CHR_XP_CHILD_ACTION4_B:2":null,1.08f,.48f);}

    }
    void LateUpdate(){
        if(!usePaintedPoses||!boy)return;var family=GetComponent<Scene03FamilySequence>();if(family&&family.Busy)return;
        if(!boyPainting){var p=boy.Find("NewArt_Actor");if(p)boyPainting=p.GetComponent<SpriteRenderer>();}
        if(!boyPainting)return;
        if(!boyView){boyView=P02ActorPresentation.AttachCompanion(boy,boyPainting,"boy_child",1.46f);boyView.SetStaticPose(seated);}
        boyPainting.forceRenderingOff=boyView&&boyView.OwnsBody;
        if(guitar)guitar.SetActive(!usePaintedPoses); // the approved seated pose already paints this guitar
        if(boyView&&boyView.OwnsBody)return;
        var sprite=DemoArtCatalog.Find(seated?"CHR_BOY_CHILD_ACTION4_V02:1":"CHR_BOY_CHILD_ACTION4_V02:0");if(!sprite)return;
        boyPainting.sprite=sprite;float h=seated?1.18f:1.46f;float u=h/sprite.bounds.size.y;var scale=boyPainting.transform.parent.lossyScale;
        boyPainting.transform.localScale=new Vector3(u/Mathf.Abs(scale.x),u/Mathf.Abs(scale.y),1);
        boyPainting.transform.position=boy.position+Vector3.up*(-.7f+(seated?.48f:0));
    }
    void Update(){if(fishing&&fishingRod!=null)fishingRod.localRotation=Quaternion.Euler(0,0,Mathf.Sin(GameplayClock.Now*2)*5);}
    void Release(){if(ownsLock&&chapter!=null&&chapter.Controls!=null)chapter.Controls.ReleaseLock(ControlLockReason.Cutscene,this);ownsLock=false;}
    void OnDisable(){StopAllCoroutines();SetSeated(false);if(music!=null)music.Stop();Busy=false;Release();}
}
