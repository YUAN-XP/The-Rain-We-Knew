using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Scene03TransitionBridge : MonoBehaviour
{
    [SerializeField] private string targetSceneName="";
    [SerializeField] private ScreenFadeController screenFade;
    private GameControlManager controls;
    public bool EntryReached {get;private set;}
    private bool started;
    public void BeginTransition(){if(started)return;started=true;StartCoroutine(Transition());}
    private IEnumerator Transition()
    {
        controls=FindAnyObjectByType<GameControlManager>();if(controls!=null)controls.AcquireLock(ControlLockReason.Cutscene);
        if(screenFade==null){Debug.LogWarning("Scene transition needs a ScreenFadeController.",this);if(controls!=null)controls.ReleaseLock(ControlLockReason.Cutscene);started=false;yield break;}
        var puzzle=FindAnyObjectByType<ThreeDoorPuzzleController>();if(puzzle!=null)puzzle.enabled=false;
        var sounds=FindObjectsByType<AudioSource>();var volumes=new float[sounds.Length];
        for(int i=0;i<sounds.Length;i++)volumes[i]=sounds[i].volume;
        for(float t=0;t<.65f;t+=GameplayClock.Delta){for(int i=0;i<sounds.Length;i++)if(sounds[i]!=null)sounds[i].volume=volumes[i]*(1-t/.65f);yield return null;}
        yield return screenFade.FadeOutAndHold();EntryReached=true;
        if(!string.IsNullOrWhiteSpace(targetSceneName) && Application.CanStreamedLevelBeLoaded(targetSceneName)){
            BlackSceneTransition.Begin(targetSceneName,controls,null,true);yield break;
        }
        Debug.LogWarning("Scene transition target is unavailable; returning control.",this);
        yield return screenFade.FadeThroughBlack(()=>{});
        if(controls!=null)controls.ReleaseLock(ControlLockReason.Cutscene);
        started=false;
    }
    private void OnDisable(){if(started && controls!=null)controls.ReleaseLock(ControlLockReason.Cutscene);}
}
