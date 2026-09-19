using UnityEngine;

/// <summary>Follows a logic-owned renderer's visibility without changing its state.</summary>
[DisallowMultipleComponent]
public sealed class RendererVisibilityLink : MonoBehaviour
{
    [SerializeField] private Renderer source;
    [SerializeField] private Renderer visual;

    private void OnEnable() => SyncVisibility();
    private void LateUpdate() => SyncVisibility();
    private void OnDisable() { if (visual != null) visual.enabled = false; }

    private void SyncVisibility()
    {
        if (visual != null)
            visual.enabled = source != null && source.enabled && source.gameObject.activeInHierarchy;
    }
}
