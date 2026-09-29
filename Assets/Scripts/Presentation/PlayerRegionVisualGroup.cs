using UnityEngine;

/// <summary>Opt-in art masks for vertically adjacent spaces; never changes gameplay objects.</summary>
[ExecuteAlways]
[DisallowMultipleComponent]
[DefaultExecutionOrder(300)]
public sealed class PlayerRegionVisualGroup : MonoBehaviour
{
    [SerializeField] private bool showInEditMode;
    [SerializeField] private Vector2 regionMin;
    [SerializeField] private Vector2 regionMax;
    [SerializeField] private SpriteRenderer[] visuals;
    private GameControlManager controls;
    private void LateUpdate()
    {
        if (!Application.isPlaying) { SetVisible(showInEditMode); return; }
        if (controls == null && PersistentRuntime.Instance != null)
            controls = PersistentRuntime.Instance.GetComponent<GameControlManager>();
        var player = controls != null ? controls.ActivePlayer : null;
        bool visible = false;
        if (player != null && player.gameObject.activeInHierarchy)
        {
            var p = player.transform.position;
            visible = p.x >= regionMin.x && p.x <= regionMax.x && p.y >= regionMin.y && p.y <= regionMax.y;
        }
        SetVisible(visible);
    }
    private void OnEnable() => SetVisible(!Application.isPlaying && showInEditMode);
    private void OnDisable() => SetVisible(false);
    private void SetVisible(bool visible)
    {
        if (visuals == null) return;
        foreach (var renderer in visuals) if (renderer != null && renderer.enabled != visible) renderer.enabled = visible;
    }
}
