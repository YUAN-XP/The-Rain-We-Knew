using UnityEngine;
// Retain the serialized/API identity used by story controllers. Scene changes now
// share a black mask; authored white story flashes remain in their own sequences.
public class WhiteSceneTransition : MonoBehaviour
{
 public static bool IsActive => BlackSceneTransition.IsActive;
 public static bool Begin(string sceneName,GameControlManager owner) => BlackSceneTransition.Begin(sceneName,owner);
}
