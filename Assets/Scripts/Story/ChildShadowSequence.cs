using System.Collections;
using UnityEngine;

public class ChildShadowSequence : MonoBehaviour
{
    [SerializeField] private SpriteRenderer childVisual;
    [SerializeField,Min(1f)] private float runSpeed=7f;
    private StoryStateManager story;private GameControlManager controls;private StoryNoticeUI notice;
    public bool IsPlaying {get;private set;}
    private void Awake(){childVisual.gameObject.SetActive(false);}
    private void Start(){story=FindAnyObjectByType<StoryStateManager>();controls=FindAnyObjectByType<GameControlManager>();notice=FindAnyObjectByType<StoryNoticeUI>();}
    public void PlayOnBedroomExit()
    {
        var player=FindAnyObjectByType<PlayerController>();
        if(player==null || story==null || controls==null || controls.IsLocked() || IsPlaying || !story.Has(StoryFlag.DeskEventTriggered) || story.Has(StoryFlag.ChildShadowSequencePlayed))return;
        StartCoroutine(Play(player.transform));
    }
    private IEnumerator Play(Transform player)
    {
        IsPlaying=true;controls.AcquireLock(ControlLockReason.Cutscene);
        childVisual.transform.position=player.position+new Vector3(3f,-.3f,-.1f);childVisual.gameObject.SetActive(true);
        notice.Show("等等。",3f);var camera=Camera.main;
        float elapsed=0;
        while(elapsed<6f) {
            childVisual.transform.position+=Vector3.right*(runSpeed*Time.unscaledDeltaTime);elapsed+=Time.unscaledDeltaTime;
            if(elapsed>.75f && (camera==null || camera.WorldToViewportPoint(childVisual.bounds.min).x>1.1f))break;
            yield return null;
        }
        childVisual.gameObject.SetActive(false);story.Set(StoryFlag.ChildShadowSequencePlayed);
        controls.ReleaseLock(ControlLockReason.Cutscene);IsPlaying=false;
    }
    private void OnDisable(){if(IsPlaying && controls!=null)controls.ReleaseLock(ControlLockReason.Cutscene);if(childVisual!=null)childVisual.gameObject.SetActive(false);IsPlaying=false;}
}
