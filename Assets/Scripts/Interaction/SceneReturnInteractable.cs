using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneReturnInteractable : MonoBehaviour,IInteractable
{
    [SerializeField] private string interactionPrompt="[E] 返回";
    [SerializeField] private string targetSceneName;
    [SerializeField] private Vector3 arrivalPosition;
    [SerializeField] private ScreenFadeController screenFade;
    private bool busy,warned;
    public string GetInteractionPrompt()=>interactionPrompt;
    public void Interact()
    {
        var controls=FindAnyObjectByType<GameControlManager>();var player=FindAnyObjectByType<PlayerController>();
        if(busy || controls!=null && controls.IsLocked())return;
        if(controls==null || player==null || screenFade==null || string.IsNullOrEmpty(targetSceneName) || !Application.CanStreamedLevelBeLoaded(targetSceneName))
        {if(!warned){Debug.LogWarning("SceneReturnInteractable needs a valid destination, player, controls and fade.",this);warned=true;}return;}
        busy=true;controls.AcquireLock(ControlLockReason.Interaction);
        var fade=Instantiate(screenFade);fade.name="SceneReturn_Fade";DontDestroyOnLoad(fade.gameObject);
        // The persistent controls host owns this operation across destruction of the source scene.
        controls.StartCoroutine(Return(controls,player,fade,targetSceneName,arrivalPosition));
    }
    private static IEnumerator Return(GameControlManager controls,PlayerController player,ScreenFadeController fade,string scene,Vector3 position)
    {
        try
        {
            yield return fade.FadeOutAndHold();
            yield return SceneManager.LoadSceneAsync(scene);
            yield return null;
            if(player!=null){player.transform.position=position;var rb=player.GetComponent<Rigidbody2D>();rb.position=position;rb.linearVelocity=Vector2.zero;}
            var camera=FindAnyObjectByType<CameraFollow>();if(camera!=null)camera.SnapToTarget();
            yield return fade.FadeThroughBlack(null);
        }
        finally{if(controls!=null)controls.ReleaseLock(ControlLockReason.Interaction);if(fade!=null)Destroy(fade.gameObject);}
    }
}
