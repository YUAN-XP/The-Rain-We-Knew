using System.Collections;
using UnityEngine;

public class Scene02ReturnController : MonoBehaviour
{
    public GameObject interior, fragmentObject, cardObject;
    public Transform arrival, guide, guideAfterReturn;
    public DialogueInteractable guideInteraction;
    public DialogueData returnReaction, cardReaction, guideDialogue, fogBlocked;
    public FileData cleanPhoto;
    public ItemData fragment, oilLamp;
    public Scene04TransitionBridge bridge;
    public AudioClip rain;
    public SchoolHomeReturnController schoolHome;
    public Vector2 roomCameraLimits=new Vector2(345,355);
    public bool Ready {get;private set;}
    StoryStateManager story;GameControlManager controls;InventoryManager inventory;FileArchiveManager archive;DialogueManager dialogue;
    bool busy,ownsLock;
    void Awake()
    {
        var runtime=PersistentRuntime.Instance;
        if(runtime!=null&&runtime.GetComponent<StoryStateManager>().Has(StoryFlag.Scene03Completed))
        {controls=runtime.GetComponent<GameControlManager>();controls.AcquireLock(ControlLockReason.Cutscene);ownsLock=true;}
    }
    IEnumerator Start()
    {
        var runtime=PersistentRuntime.Instance;
        if(runtime==null||interior==null||arrival==null||cleanPhoto==null||fragment==null||oilLamp==null||guide==null||guideAfterReturn==null)
        {Debug.LogWarning("Scene02 return needs its scene references and item/file assets.",this);Unlock();yield break;}
        story=runtime.GetComponent<StoryStateManager>();controls=runtime.GetComponent<GameControlManager>();inventory=runtime.GetComponent<InventoryManager>();archive=runtime.GetComponent<FileArchiveManager>();dialogue=runtime.GetComponentInChildren<DialogueManager>(true);
        if(schoolHome!=null&&schoolHome.HasPriority){yield return schoolHome.Resume();busy=false;Unlock();Ready=true;yield break;}
        if(!story.Has(StoryFlag.Scene03Completed)){interior.SetActive(false);Ready=true;yield break;}
        busy=true;if(!ownsLock){controls.AcquireLock(ControlLockReason.Cutscene);ownsLock=true;}
        interior.SetActive(true);
        guide.position=guideAfterReturn.position;guideInteraction.ConfigureDialogue(guideDialogue);
        fragmentObject.SetActive(!story.Has(StoryFlag.Scene02Fragment2Collected));
        cardObject.SetActive(story.Has(StoryFlag.CameraGentlemanCardObtained)&&!story.Has(StoryFlag.Scene03CameraGentlemanEchoCollected));
        archive.ReplaceFileById(cleanPhoto);
        var player=controls.ActivePlayer;var body=player.GetComponent<Rigidbody2D>();body.linearVelocity=Vector2.zero;
        if(!story.Has(StoryFlag.LivingRoomDoorOpened)||WhiteSceneTransition.IsActive)
        {
            body.position=arrival.position;player.transform.position=arrival.position;
            var follow=Camera.main.GetComponent<CameraFollow>();follow.SetTarget(player.transform);follow.SetLimits(roomCameraLimits.x,roomCameraLimits.y);follow.SetVerticalLimits(true,.5f,-1,10);Camera.main.orthographicSize=5.2f;follow.SnapToTarget();
        }
        PersistentRuntime.Instance.GetComponent<CameraModeController>().ContextAvailable=true;
        while(WhiteSceneTransition.IsActive)yield return null;
        if(!story.Has(StoryFlag.Scene03ReturnReactionPlayed))
        {yield return Say(returnReaction);story.Set(StoryFlag.Scene03ReturnReactionPlayed);}
        busy=false;Unlock();Ready=true;
        if(!story.Has(StoryFlag.Scene03FileNoticeShown))
        {story.Set(StoryFlag.Scene03FileNoticeShown);runtime.GetComponent<StoryNoticeUI>().Show("文件已更新",4);}
    }
    public string Prompt(Scene02ReturnAction action)
    {
        if(action==Scene02ReturnAction.Fog)return inventory!=null&&inventory.HasItem(oilLamp)?"[E] 进入":"[E] 调查";
        return action==Scene02ReturnAction.Fragment?"[E] 拾取":"[E] 调查";
    }
    public void Interact(Scene02ReturnAction action)
    {
        if(!Ready||busy||controls==null||controls.IsLocked())return;
        if(action==Scene02ReturnAction.Fog)
        {if(story.Has(StoryFlag.SchoolReturnedHome)){PersistentRuntime.Instance.GetComponent<StoryNoticeUI>().Show("先回房间休息吧。",3);return;}if(inventory.HasItem(oilLamp))bridge.BeginTransition();else StartCoroutine(Act(action));return;}
        if(!story.Has(StoryFlag.Scene03Completed))return;
        StartCoroutine(Act(action));
    }
    IEnumerator Act(Scene02ReturnAction action)
    {
        busy=true;controls.AcquireLock(ControlLockReason.Interaction);
        try
        {
            if(action==Scene02ReturnAction.Fog){yield return Say(fogBlocked);yield break;}
            if(action==Scene02ReturnAction.Fragment)
            {
                if(!story.Has(StoryFlag.Scene02Fragment2Collected))
                {inventory.AddItem(fragment);story.Set(StoryFlag.Scene02Fragment2Collected);fragmentObject.SetActive(false);PersistentRuntime.Instance.GetComponent<StoryNoticeUI>().Show("获得了文件碎片2。",3);}
            }
            else if(story.Has(StoryFlag.CameraGentlemanCardObtained)&&!story.Has(StoryFlag.Scene03CameraGentlemanEchoCollected))
            {yield return Say(cardReaction);story.Set(StoryFlag.Scene03CameraGentlemanEchoCollected);cardObject.SetActive(false);}
        }
        finally {busy=false;controls.ReleaseLock(ControlLockReason.Interaction);}
    }
    public void OpenedFromInside()
    {
        if(story==null||!story.Has(StoryFlag.Scene03Completed))return;
        story.Set(StoryFlag.LivingRoomDoorOpened);
        if(!story.Has(StoryFlag.Scene03DrawerNoticeShown))StartCoroutine(DrawerNotice());
    }
    IEnumerator DrawerNotice()
    {
        float free=0;
        while(free<.75f){free=controls.IsLocked()?0:free+Time.unscaledDeltaTime;yield return null;}
        if(story.Set(StoryFlag.Scene03DrawerNoticeShown))PersistentRuntime.Instance.GetComponent<StoryNoticeUI>().Show("二楼抽屉应该可以打开了。",4);
    }
    IEnumerator Say(DialogueData data){if(data==null)yield break;while(dialogue.IsDialogueActive())yield return null;dialogue.StartDialogue(data);yield return null;while(dialogue.IsDialogueActive())yield return null;}
    void Unlock(){if(ownsLock&&controls!=null)controls.ReleaseLock(ControlLockReason.Cutscene);ownsLock=false;}
    void OnDisable(){StopAllCoroutines();Unlock();if(busy&&controls!=null)controls.ReleaseLock(ControlLockReason.Interaction);}
}
