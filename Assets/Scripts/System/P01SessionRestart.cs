using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// A separate runner survives disposal of the old persistent session.
public sealed class P01SessionRestart : MonoBehaviour {
    public static void Begin(GameObject oldRuntime){var runner=new GameObject("SessionRestart").AddComponent<P01SessionRestart>();DontDestroyOnLoad(runner.gameObject);runner.StartCoroutine(runner.Run(oldRuntime));}
    IEnumerator Run(GameObject oldRuntime){
        var root=RuntimePanelUI.Canvas("RestartCurtain",transform,32750);RuntimePanelUI.Panel("Black",root,Vector2.zero,Vector2.one,Color.black);
        Destroy(oldRuntime);yield return null;GameplayClock.Restore(0);
        var operation=SceneManager.LoadSceneAsync("Scene01_Rain");while(!operation.isDone)yield return null;
        yield return null;Destroy(gameObject);
    }
}
