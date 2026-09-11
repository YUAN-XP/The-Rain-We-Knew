using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// A single white mask survives scene loading; the destination acquires its own story lock.
public class WhiteSceneTransition : MonoBehaviour
{
    public static bool IsActive {get;private set;}
    GameControlManager controls;CanvasGroup mask;
    public static bool Begin(string sceneName,GameControlManager owner)
    {
        if(IsActive)return false;
        if(!Application.CanStreamedLevelBeLoaded(sceneName)){Debug.LogWarning("White transition target is not available.");return false;}
        var g=new GameObject("WhiteSceneTransition");DontDestroyOnLoad(g);
        var bridge=g.AddComponent<WhiteSceneTransition>();bridge.controls=owner;
        IsActive=true;owner.AcquireLock(ControlLockReason.SceneTransition);
        var root=RuntimePanelUI.Canvas("WhiteSceneMask",g.transform,300);
        RuntimePanelUI.Panel("White",root,Vector2.zero,Vector2.one,Color.white);
        bridge.mask=root.gameObject.AddComponent<CanvasGroup>();bridge.mask.alpha=1;
        bridge.StartCoroutine(bridge.Run(sceneName));return true;
    }
    IEnumerator Run(string scene)
    {
        yield return SceneManager.LoadSceneAsync(scene);yield return null;yield return new WaitForFixedUpdate();
        for(float t=0;t<.85f;t+=Time.unscaledDeltaTime){mask.alpha=1-t/.85f;yield return null;}
        mask.alpha=0;Destroy(gameObject);
    }
    void OnDestroy(){IsActive=false;if(controls!=null)controls.ReleaseLock(ControlLockReason.SceneTransition);}
}
