using UnityEngine;

// Owned by the scene, never parented to the persistent actor. Only the default
// outfit is covered; existing outfit renderers and their selection stay intact.
[DisallowMultipleComponent]
public sealed class SceneActorVisualOverride : MonoBehaviour
{
    [SerializeField] private SpriteRenderer visual;
    private PlayerController actor;
    private SpriteRenderer original;
    private StoryStateManager story;
    private bool originalEnabled;
    private bool ownsVisibility;

    private void LateUpdate()
    {
        var runtime = PersistentRuntime.Instance;
        var active = runtime != null ? runtime.GetComponent<GameControlManager>()?.ActivePlayer : null;
        if (active != actor)
        {
            Restore();
            actor = active;
            original = actor != null ? actor.GetComponent<SpriteRenderer>() : null;
            story = runtime != null ? runtime.GetComponent<StoryStateManager>() : null;
        }

        bool show = visual != null && original != null && original.sprite != null &&
            actor.gameObject.activeInHierarchy && actor.GetComponent<AdultOutfitController>() != null &&
            story != null && story.Outfit == AdultOutfit.Default;
        if (!show)
        {
            Restore();
            if (visual != null) visual.enabled = false;
            return;
        }

        if (!ownsVisibility)
        {
            originalEnabled = original.enabled;
            ownsVisibility = true;
            original.enabled = false;
        }
        // Read the existing body foot point; don't change the body, physics,
        // camera target, tool anchors, or the renderer bounds used by the ending.
        var foot = original.transform.TransformPoint(new Vector3(0, original.sprite.bounds.min.y, 0));
        visual.transform.position = foot;
        visual.enabled = originalEnabled;
    }

    private void Restore()
    {
        if (ownsVisibility && original != null) original.enabled = originalEnabled;
        ownsVisibility = false;
    }

    private void OnDisable()
    {
        Restore();
        if (visual != null) visual.enabled = false;
    }

    private void OnDestroy() { Restore(); }
}
