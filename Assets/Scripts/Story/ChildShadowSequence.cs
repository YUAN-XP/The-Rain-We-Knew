using System.Collections;
using UnityEngine;

public class ChildShadowSequence : MonoBehaviour
{
    [SerializeField] private SpriteRenderer childVisual;
    [SerializeField,Min(1f)] private float runSpeed=7f;
    [SerializeField] private Transform bedroomSpawn;
    [SerializeField] private Transform bedroomExit;
    [SerializeField] private Collider2D bedroomBounds;
    private StoryStateManager story;
    private GameControlManager controls;
    private StoryNoticeUI notice;
    private PlayerController player;
    private bool ownsLock;
    public bool IsPlaying {get;private set;}
    private void Awake(){if(childVisual!=null)childVisual.gameObject.SetActive(false);}
    private void Start()
    {
        story=FindAnyObjectByType<StoryStateManager>();controls=FindAnyObjectByType<GameControlManager>();
        notice=FindAnyObjectByType<StoryNoticeUI>();player=FindAnyObjectByType<PlayerController>();
        if(story!=null)story.Changed+=TryPlay;
    }
    private void TryPlay()
    {
        if(IsPlaying || story==null || controls==null || player==null || childVisual==null || bedroomSpawn==null || bedroomExit==null || bedroomBounds==null)return;
        if(!story.Has(StoryFlag.DeskEventTriggered) || story.Has(StoryFlag.ChildShadowSequencePlayed) || !bedroomBounds.bounds.Contains(player.transform.position))return;
        IsPlaying=true;StartCoroutine(Play());
    }
    private IEnumerator Play()
    {
        // CloseUp releases its own lock after publishing DeskEventTriggered.
        yield return null;
        while(controls.IsLocked())yield return null;
        controls.AcquireLock(ControlLockReason.Cutscene);ownsLock=true;
        childVisual.transform.position=bedroomSpawn.position;childVisual.gameObject.SetActive(true);
        yield return GameplayClock.Wait(.55f);
        var camera=Camera.main;var original=camera!=null?camera.transform.position:Vector3.zero;
        while(Vector3.Distance(childVisual.transform.position,bedroomExit.position)>.03f)
        {
            childVisual.transform.position=Vector3.MoveTowards(childVisual.transform.position,bedroomExit.position,runSpeed*GameplayClock.Delta);
            if(camera!=null){var pos=camera.transform.position;pos.x=Mathf.Lerp(pos.x,childVisual.transform.position.x+2f,4f*GameplayClock.Delta);camera.transform.position=pos;}
            yield return null;
        }
        childVisual.gameObject.SetActive(false);
        if(camera!=null){var from=camera.transform.position;for(float t=0;t<.4f;t+=GameplayClock.Delta){camera.transform.position=Vector3.Lerp(from,original,t/.4f);yield return null;}camera.transform.position=original;}
        story.Set(StoryFlag.ChildShadowSequencePlayed);
        controls.ReleaseLock(ControlLockReason.Cutscene);ownsLock=false;IsPlaying=false;
    }
    // A brief second glimpse on the corridor side preserves the original "wait" beat.
    public void PlayOnBedroomExit()
    {
        if(IsPlaying || story==null || controls==null || player==null || childVisual==null || controls.IsLocked())return;
        if(!story.Has(StoryFlag.ChildShadowSequencePlayed) || story.Has(StoryFlag.ChildShadowFollowupPlayed))return;
        IsPlaying=true;StartCoroutine(CorridorFollowup());
    }
    private IEnumerator CorridorFollowup()
    {
        controls.AcquireLock(ControlLockReason.Cutscene);ownsLock=true;
        childVisual.transform.position=player.transform.position+new Vector3(3f,-.3f,-.1f);
        childVisual.gameObject.SetActive(true);
        if(notice!=null)notice.Show("等等。",3f);
        var camera=Camera.main;
        for(float elapsed=0;elapsed<6f;elapsed+=GameplayClock.Delta)
        {
            childVisual.transform.position+=Vector3.right*(runSpeed*GameplayClock.Delta);
            if(elapsed>.75f && (camera==null || camera.WorldToViewportPoint(childVisual.bounds.min).x>1.1f))break;
            yield return null;
        }
        childVisual.gameObject.SetActive(false);
        story.Set(StoryFlag.ChildShadowFollowupPlayed);
        controls.ReleaseLock(ControlLockReason.Cutscene);ownsLock=false;IsPlaying=false;
    }
    private void OnDisable()
    {
        StopAllCoroutines();
        if(ownsLock && controls!=null)controls.ReleaseLock(ControlLockReason.Cutscene);
        ownsLock=false;IsPlaying=false;if(childVisual!=null)childVisual.gameObject.SetActive(false);
    }
    private void OnDestroy(){if(story!=null)story.Changed-=TryPlay;}
}
