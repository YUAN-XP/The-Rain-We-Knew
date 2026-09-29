using System.Collections;
using UnityEngine;
using TMPro;

public class Scene03Chapter : MonoBehaviour
{
    public Scene03Content content;
    public Scene03MazeController maze;
    public bool Ready => ready;
    public TMP_FontAsset font;
    public PlayerController child;
    public CameraFollow follow;
    public Transform homeSpawn, streetSpawn;
    public GameObject livingRoomRoot;
    public GameObject homeRoot, streetRoot, carriedCamera, cameraOnTable, catVisual, coinVisual;
    public CanvasGroup curtain;
    public ChildhoodActivityUI activities;
    public AudioSource ambience, neighbours, phone, water, effects;
    public StoryStateManager Story {get; private set;}
    public GameControlManager Controls {get;private set;}
    public DialogueManager Dialogue {get;private set;}
    public InventoryManager Inventory {get;private set;}
    public FileArchiveManager Archive {get;private set;}
    CameraModeController cameraMode;
    bool busy, ownsLock, ready; float reminderAt;
    public bool Busy => busy;
    void Awake(){if(curtain!=null){curtain.alpha=1;curtain.blocksRaycasts=true;}}
    IEnumerator Start()
    {
        yield return null;
        var runtime=PersistentRuntime.Instance;
        if(runtime==null || content==null || child==null || curtain==null){Debug.LogWarning("Scene03Chapter needs its configured runtime, actor and content.",this);yield break;}
        Story=runtime.GetComponent<StoryStateManager>();Controls=runtime.GetComponent<GameControlManager>();
        Dialogue=runtime.GetComponentInChildren<DialogueManager>(true);Inventory=runtime.GetComponent<InventoryManager>();cameraMode=runtime.GetComponent<CameraModeController>();
        Archive=runtime.GetComponent<FileArchiveManager>();
        if(Story.Has(StoryFlag.CameraGentlemanCardObtained))ArchiveCameraGentleman();
        Controls.BindPlayer(child,false);child.GetComponent<PlayerInteraction>().BindRuntime(runtime.GetComponentInChildren<InteractionUI>(true),Dialogue);
        follow.SetTarget(child.transform);
        Story.Set(StoryFlag.Scene03Started);cameraMode.ContextAvailable=Story.Has(StoryFlag.Scene03CameraTaken);
        bool street=Story.Has(StoryFlag.Scene03StreetReached)&&!Story.Has(StoryFlag.Scene03ReturnedHome);SetArea(street);
        SyncProps();SetupSound();activities.Initialize(this);busy=true;Lock();curtain.alpha=1;
        yield return Fade(0,1.1f);Unlock();busy=false;ready=true;reminderAt=Time.time+25;
    }
    void SetupSound()
    {
        Loop(phone,content.phoneRing,Story.Has(StoryFlag.Scene03PhoneAnswered)?0:.2f);
        if(Story.Has(StoryFlag.Scene03PhoneAnswered))phone.Stop();
    }
    static void Loop(AudioSource s,AudioClip clip,float volume){s.clip=clip;s.loop=true;s.volume=volume;s.Play();}
    void Update()
    {
        if(!ready)return;
        if(Story.Has(StoryFlag.FishObtained) && !Story.Has(StoryFlag.Scene03FishHintShown) && !Controls.IsLocked() && !busy)
        {
            Story.Set(StoryFlag.Scene03FishHintShown);
            PersistentRuntime.Instance.GetComponent<StoryNoticeUI>().Show("不知道小猫喜不喜欢吃……",4);
        }
        if (maze != null && maze.OwnsView) return;
        bool street=streetRoot.activeSelf;
        float x=child.transform.position.x;
        var framing=Camera.main.GetComponent<RoomPresentationFraming>();
        if(street && x>108 && (!framing||!framing.FramesPosition(child.transform.position)))Camera.main.orthographicSize=Mathf.Lerp(Camera.main.orthographicSize,6.8f,Time.deltaTime);
        if(street && x>112 && !Story.Has(StoryFlag.Scene03MazePreludeReached) && !Controls.IsLocked() && !busy)StartCoroutine(Prelude());
        if(!street&&!Story.Has(StoryFlag.Scene03PhoneAnswered)&&Time.time>reminderAt&&!Controls.IsLocked())
        {PersistentRuntime.Instance.GetComponent<StoryNoticeUI>().Show("小鹏，电话响半天了。",4);reminderAt=Time.time+25;}
    }
    void SetArea(bool street)
    {
        homeRoot.SetActive(!street);streetRoot.SetActive(street);
        if(livingRoomRoot!=null)livingRoomRoot.SetActive(!street);
        var p=(street?streetSpawn:homeSpawn).position;child.transform.position=p;child.GetComponent<Rigidbody2D>().position=p;
        child.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;
        follow.SetLimits(street?-22:-19,street?135:15);Camera.main.orthographicSize=street?5.7f:5.2f;follow.SnapToTarget();
    }
    public void ReturnHomeFromRiver()
    {
        Story.Set(StoryFlag.Scene03ReturnedHome);
        if(maze!=null)maze.ReleaseToHome();
        SetArea(false);
    }
    void SyncProps()
    {
        bool taken=Story.Has(StoryFlag.Scene03CameraTaken);carriedCamera.SetActive(taken);cameraOnTable.SetActive(!taken);
        if(Story.Has(StoryFlag.CatFed))catVisual.transform.localPosition=new Vector3(2,0,0);
        coinVisual.SetActive(false);
    }
    public void Interact(ChildhoodAction action)
    {
        if(!ready||busy||Controls.IsLocked())return;
        if (action == ChildhoodAction.Endpoint && maze != null) { maze.Enter(); return; }
        if(Story.Has(StoryFlag.Scene03ReturnedHome))
        {
            if(action==ChildhoodAction.Stairs){PersistentRuntime.Instance.GetComponent<StoryNoticeUI>().Show("现在去一楼客厅吃晚餐吧。");return;}
            if(action==ChildhoodAction.LeaveHome){PersistentRuntime.Instance.GetComponent<StoryNoticeUI>().Show("先去客厅吧，晚餐准备好了。");return;}
            if(action==ChildhoodAction.Phone){PersistentRuntime.Instance.GetComponent<StoryNoticeUI>().Show("电话已经挂断了。");return;}
            if(action==ChildhoodAction.Parents){GetComponent<Scene03FamilySequence>()?.EnterLivingRoom();return;}
        }
        switch(action)
        {
            case ChildhoodAction.Cat:activities.OpenCat();return;
            case ChildhoodAction.Storyteller:activities.OpenStory();return;
            case ChildhoodAction.Bulletin:activities.OpenBoard();return;
            case ChildhoodAction.Rings:activities.OpenRingOffer();return;
            default:StartCoroutine(Act(action));return;
        }
    }
    IEnumerator Act(ChildhoodAction action)
    {
        busy=true;Lock();
        try
        {
            switch(action)
            {
                case ChildhoodAction.Phone:
                    if(Story.Has(StoryFlag.Scene03PhoneAnswered)){yield return Say(content.parentsRepeat);break;}
                    phone.Stop();yield return Say(content.phone);Story.Set(StoryFlag.Scene03PhoneAnswered);
                    yield return Say(content.parents);Story.Set(StoryFlag.Scene03IntroCompleted);break;
                case ChildhoodAction.Parents:yield return Say(Story.Has(StoryFlag.Scene03IntroCompleted)?content.parentsRepeat:content.stairs);break;
                case ChildhoodAction.Camera:
                    if(!Story.Has(StoryFlag.Scene03IntroCompleted)){yield return Say(content.stairs);break;}
                    if(!Story.Has(StoryFlag.Scene03CameraTaken))
                    {yield return Say(content.takeCamera);Story.Set(StoryFlag.Scene03CameraTaken);if(!Inventory.HasItem(content.oldCamera))Inventory.AddItem(content.oldCamera);cameraMode.ContextAvailable=true;SyncProps();}break;
                case ChildhoodAction.LeaveHome:
                    if(!Story.Has(StoryFlag.Scene03CameraTaken)){yield return Say(content.exitLocked);break;}
                    yield return Fade(1,.35f);Story.Set(StoryFlag.Scene03StreetReached);SetArea(true);yield return null;yield return Fade(0,.45f);break;
                case ChildhoodAction.Stairs:if(Story.Has(StoryFlag.Scene03IntroCompleted))PersistentRuntime.Instance.GetComponent<StoryNoticeUI>().Show("准备出门吧。");else yield return Say(content.stairs);break;
                case ChildhoodAction.Fish:
                    if(Story.Has(StoryFlag.FishObtained)){yield return Say(content.fishRepeat);break;}
                    yield return Say(content.fish);Inventory.AddItem(content.fishItem);Story.Set(StoryFlag.FishObtained);break;
                case ChildhoodAction.LiMei:yield return Say(content.liMei);Story.Set(StoryFlag.LiMeiMessageAccepted);break;
                case ChildhoodAction.Chess:
                    yield return Say(Story.Has(StoryFlag.LiMeiMessageAccepted)&&!Story.Has(StoryFlag.LiMeiMessageDelivered)?content.messageDelivered:content.chess);
                    if(Story.Has(StoryFlag.LiMeiMessageAccepted))Story.Set(StoryFlag.LiMeiMessageDelivered);break;
                case ChildhoodAction.Endpoint:yield return Say(content.endpoint);break;
            }
        }
        finally{busy=false;Unlock();}
    }
    IEnumerator Prelude(){busy=true;Lock();try{yield return Say(content.prelude);Story.Set(StoryFlag.Scene03MazePreludeReached);}finally{busy=false;Unlock();}}
    public bool ArchiveCameraGentleman()
    {
        if(Archive==null || content.cameraGentlemanFile==null)
        {
            Debug.LogWarning("Scene03 card reward needs its FileArchiveManager and FileData.",this);
            return false;
        }
        if(!Archive.HasFileById(content.cameraGentlemanFile.fileId))Archive.AddFile(content.cameraGentlemanFile);
        return true;
    }
    // Called by the chapter return bridge; only childhood consumables are removed.
    public void ClearChildhoodItems()
    {
        if(Inventory==null)return;
        Inventory.RemoveItem(content.fishItem);
        Inventory.RemoveItem(content.coinItem);
    }
    public IEnumerator Say(DialogueData data)
    {
        if(data==null)yield break;
        while(Dialogue.IsDialogueActive())yield return null;
        Dialogue.StartDialogue(data);yield return null;
        while(Dialogue.IsDialogueActive())yield return null;
        yield return null;
    }
    public IEnumerator Fade(float alpha,float duration)
    {
        if(alpha<=0){RoomPresentationFraming.PrepareCurrentView();yield return null;}
        float start=curtain.alpha;curtain.blocksRaycasts=true;
        for(float t=0;t<duration;t+=Time.unscaledDeltaTime){curtain.alpha=Mathf.Lerp(start,alpha,t/duration);yield return null;}
        curtain.alpha=alpha;curtain.blocksRaycasts=alpha>.01f;
    }
    void Lock(){Controls.AcquireLock(ControlLockReason.Interaction);ownsLock=true;}
    void Unlock(){if(ownsLock&&Controls!=null)Controls.ReleaseLock(ControlLockReason.Interaction);ownsLock=false;}
    void OnDisable(){StopAllCoroutines();Unlock();if(cameraMode!=null)cameraMode.ContextAvailable=true;}
}
