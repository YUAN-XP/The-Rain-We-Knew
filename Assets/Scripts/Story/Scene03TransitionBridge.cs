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
        yield return screenFade.FadeOutAndHold();EntryReached=true;
        if(!string.IsNullOrWhiteSpace(targetSceneName) && Application.CanStreamedLevelBeLoaded(targetSceneName)){
            SceneManager.LoadScene(targetSceneName);yield break;
        }
        Debug.Log("Scene03 entry reached / waiting for scene implementation");
    }
    private void OnDisable(){if(started && controls!=null)controls.ReleaseLock(ControlLockReason.Cutscene);}
}
