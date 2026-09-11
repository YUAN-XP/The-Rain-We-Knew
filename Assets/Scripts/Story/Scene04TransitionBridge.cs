using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Scene04TransitionBridge : MonoBehaviour
{
    [SerializeField] private ScreenFadeController screenFade;
    [SerializeField] private string targetSceneName="";
    public bool EntryReached {get;private set;}
    bool started;GameControlManager controls;
    public void BeginTransition()
    {
        if(started)return;
        controls=PersistentRuntime.Instance!=null?PersistentRuntime.Instance.GetComponent<GameControlManager>():null;
        if(controls==null||screenFade==null){Debug.LogWarning("Scene04 bridge needs its existing controls and fade.",this);return;}
        started=true;StartCoroutine(Transition());
    }
    IEnumerator Transition()
    {
        controls.AcquireLock(ControlLockReason.SceneTransition);
        controls.ActivePlayer.GetComponent<Rigidbody2D>().linearVelocity=Vector2.zero;
        yield return screenFade.FadeOutAndHold();EntryReached=true;
        if(!string.IsNullOrWhiteSpace(targetSceneName)&&Application.CanStreamedLevelBeLoaded(targetSceneName))SceneManager.LoadScene(targetSceneName);
        // Intentionally hold at the approved black bridge until Scene04 content exists.
    }
    void OnDisable(){if(started&&controls!=null)controls.ReleaseLock(ControlLockReason.SceneTransition);}
}
