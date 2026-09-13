using UnityEngine;

public class Scene04TransitionBridge : MonoBehaviour
{
    // Existing serialized fields remain available so the prior scene reference
    // is not destroyed by the upgrade. FogIntroController now owns the fade.
    [SerializeField] private ScreenFadeController screenFade;
    [SerializeField] private string targetSceneName = "";
    [SerializeField] private FogIntroController fogIntro;
    public bool EntryReached { get; private set; }
    private bool warned;
    public void BeginTransition()
    {
        if (fogIntro == null)
        {
            if (!warned) { Debug.LogWarning("Scene04 bridge needs its same-scene FogIntroController.", this); warned = true; }
            return;
        }
        // Having the lamp alone is deliberately insufficient. TryEnter checks
        // actual light and captures CP_FOG_OUT before moving the player.
        if (fogIntro.TryEnter(targetSceneName)) EntryReached = true;
    }
}
