using System.Collections;
using UnityEngine;

// Scene02 return reason has priority over the earlier Scene03 living-room arrival.
public sealed class SchoolHomeReturnController : MonoBehaviour
{
    public Scene02ReturnController previousReturn;
    public FogIntroController fog;
    public SchoolHomeData data;
    public Transform homeArrival,guideArrival;
    public Vector2 cameraX,cameraY;
    public bool followY;
    public float cameraOffset,cameraSize;
    SchoolChaseSession session;StoryStateManager story;GameControlManager controls;DialogueManager dialogue;
    bool initialized;int guideCompletion;
    public bool HasPriority {
        get {var runtime=PersistentRuntime.Instance;if(runtime==null)return false;
            var s=runtime.GetComponent<SchoolChaseSession>();return s!=null&&(s.PendingReturnFog||s.PendingHomeArrival)||runtime.GetComponent<StoryStateManager>().Has(StoryFlag.SchoolReturnedHome);}
    }
    public IEnumerator Resume()
    {
        var runtime=PersistentRuntime.Instance;session=runtime.GetComponent<SchoolChaseSession>();story=runtime.GetComponent<StoryStateManager>();controls=runtime.GetComponent<GameControlManager>();dialogue=runtime.GetComponentInChildren<DialogueManager>(true);
        previousReturn.interior.SetActive(true);previousReturn.fragmentObject.SetActive(!story.Has(StoryFlag.Scene02Fragment2Collected));
        previousReturn.cardObject.SetActive(story.Has(StoryFlag.CameraGentlemanCardObtained)&&!story.Has(StoryFlag.Scene03CameraGentlemanEchoCollected));
        // Do not reissue or replace archived history; the actual session owns it.
        runtime.GetComponent<DeskCloseupController>().ConfigureSchoolComputer(data);
        var sound=gameObject.AddComponent<AudioSource>();sound.clip=previousReturn.rain;sound.loop=true;sound.volume=.18f;sound.Play();
        initialized=true;guideCompletion=dialogue.CompletedCount;
        if(story.Has(StoryFlag.SchoolReturnedHome)){ConfigureGuide();yield break;}
        while(WhiteSceneTransition.IsActive)yield return null;
        if(session!=null&&session.PendingReturnFog){
            if(!fog.TryEnterSchoolReturn(this)){Debug.LogWarning("School return fog could not start; returning to its safe school checkpoint.",this);session.PendingReturnFog=false;WhiteSceneTransition.Begin(data.schoolScene,controls);}
            yield break;
        }
        if(session!=null&&session.PendingHomeArrival){PlaceHome();FinishArrival();}
    }
    public void PlaceHome()
    {
        var p=controls.ActivePlayer;p.transform.position=homeArrival.position;var b=p.GetComponent<Rigidbody2D>();b.position=homeArrival.position;b.linearVelocity=Vector2.zero;
        var cam=Camera.main;var follow=cam.GetComponent<CameraFollow>();cam.orthographicSize=cameraSize;follow.SetTarget(p.transform);
        follow.SetLimits(cameraX.x,cameraX.y);follow.SetVerticalLimits(followY,cameraOffset,cameraY.x,cameraY.y);follow.SetFacing(-1);follow.SnapToTarget();Physics2D.SyncTransforms();
        PersistentRuntime.Instance.GetComponent<OilLampController>().RestoreUseState(true);
    }
    public void FinishArrival()
    {
        session.PendingReturnFog=false;session.PendingHomeArrival=false;story.Set(StoryFlag.SchoolReturnedHome);ConfigureGuide();StartCoroutine(FreeNotice());
    }
    IEnumerator FreeNotice(){float free=0;while(free<.3f){free=controls.IsLocked()?0:free+Time.unscaledDeltaTime;yield return null;}PersistentRuntime.Instance.GetComponent<StoryNoticeUI>().Show("二楼电脑应该可以打开了",5);}
    void ConfigureGuide(){previousReturn.guide.position=guideArrival.position;previousReturn.guideInteraction.ConfigureDialogue(story.Has(StoryFlag.SchoolGuideReturnSpoken)?data.guideRepeat:data.guideFirst);}
    void Update()
    {
        if(!initialized||!story.Has(StoryFlag.SchoolReturnedHome)||story.Has(StoryFlag.SchoolGuideReturnSpoken))return;
        if(dialogue.CompletedCount>guideCompletion&&dialogue.LastCompletedDialogue==data.guideFirst){story.Set(StoryFlag.SchoolGuideReturnSpoken);ConfigureGuide();}
    }
}
