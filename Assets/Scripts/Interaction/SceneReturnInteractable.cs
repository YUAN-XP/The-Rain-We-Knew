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
        PersistentRuntime.Instance?.GetComponent<EndingSession>()?.PrepareStreetReturn(targetSceneName,arrivalPosition);
        busy=true;
        if(!BlackSceneTransition.Begin(targetSceneName,controls,()=>{
            bool endingArrival=controls.GetComponent<EndingSession>() is EndingSession ending && ending.IsEndingVisit && ending.Data!=null && targetSceneName==ending.Data.streetScene;
            if(player!=null&&!endingArrival)EndingSession.Place(player,arrivalPosition);
        }))busy=false;
        return;
    }
}
