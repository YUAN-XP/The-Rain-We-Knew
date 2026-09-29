using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Opaque across scene destruction, destination Start and the first composed frame.</summary>
public sealed class BlackSceneTransition : MonoBehaviour
{
 public static bool IsActive { get; private set; }
 GameControlManager controls; CanvasGroup curtain; bool ownsLock;
 public static bool Begin(string scene, GameControlManager owner, Action arrived = null, bool alreadyBlack = false)
 {
  if(IsActive || owner == null || !Application.CanStreamedLevelBeLoaded(scene)) return false;
  var root = new GameObject("BlackSceneTransition"); DontDestroyOnLoad(root);
  var bridge = root.AddComponent<BlackSceneTransition>(); bridge.controls = owner;
  IsActive = true;
  bridge.ownsLock = !owner.IsLockedBy(ControlLockReason.SceneTransition);
  if(bridge.ownsLock) owner.AcquireLock(ControlLockReason.SceneTransition);
  var canvas = RuntimePanelUI.Canvas("TransitionMask", root.transform, 30000);
  RuntimePanelUI.Panel("Black", canvas, Vector2.zero, Vector2.one, Color.black);
  bridge.curtain = canvas.gameObject.AddComponent<CanvasGroup>();
  bridge.curtain.alpha = alreadyBlack ? 1 : 0; bridge.curtain.blocksRaycasts = true;
  bridge.StartCoroutine(bridge.Run(scene, arrived)); return true;
 }
 IEnumerator Run(string scene, Action arrived)
 {
  try {
   yield return Fade(1, .28f);
   yield return SceneManager.LoadSceneAsync(scene);
   // Start coroutines bind the correct actor, variant and destination on their first frames.
   yield return null; yield return null;
   arrived?.Invoke();
   RoomPresentationFraming.PrepareCurrentView();
   yield return null; yield return null;
   RoomPresentationFraming.PrepareCurrentView();
   yield return Fade(0, .4f);
  } finally { Destroy(gameObject); }
 }
 IEnumerator Fade(float target, float seconds)
 {
  float from=curtain.alpha;
  for(float t=0;t<seconds;t+=Time.unscaledDeltaTime){curtain.alpha=Mathf.Lerp(from,target,Mathf.SmoothStep(0,1,t/seconds));yield return null;}
  curtain.alpha=target;
 }
 void OnDestroy(){IsActive=false;if(ownsLock&&controls)controls.ReleaseLock(ControlLockReason.SceneTransition);}
}
