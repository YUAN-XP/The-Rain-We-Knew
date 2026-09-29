using UnityEngine;

/// <summary>Read-only link to a story renderer, including its preview/reveal alpha.</summary>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class SpriteVisibilityAlphaLink : MonoBehaviour
{
    [SerializeField] private SpriteRenderer source;
    [SerializeField] private SpriteRenderer visual;
    private bool previousForce;
    private void OnEnable() { if(source!=null){previousForce=source.forceRenderingOff;source.forceRenderingOff=true;} Sync(); }
    private void LateUpdate() => Sync();
    private void OnDisable() { if (visual != null) visual.enabled = false; if(source!=null)source.forceRenderingOff=previousForce; }
    private void Sync()
    {
        if (visual == null) return;
        visual.enabled = source != null && source.enabled && source.gameObject.activeInHierarchy;
        if (source != null) { var c = visual.color; c.a = source.color.a; visual.color = c; }
    }
}
