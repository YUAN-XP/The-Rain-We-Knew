using UnityEngine;

/// <summary>Opt-in, visual-only outline of the existing interaction target.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(200)]
public sealed class HandDrawnInteractionHighlight : MonoBehaviour
{
    [SerializeField] private MonoBehaviour interactionSource;
    [SerializeField] private SpriteRenderer[] visuals;
    [SerializeField] private Material outlineMaterial;
    [SerializeField] private Color lineColor = new Color(.94f, .93f, .84f, .62f);
    [SerializeField, Range(.5f, 4f)] private float strokePixels = 1.6f;
    [SerializeField, Range(2f, 12f)] private float redrawRate = 6f;
    [SerializeField, Range(0f, .35f)] private float strokeVariation = .16f;

    [Tooltip("Legacy serialized field only. Reach is owned by PlayerInteraction.")]
    [SerializeField, Min(0)] private float maximumHighlightGap;
    [SerializeField, Range(0, .4f)] private float opacityBreath;

    [Header("Optional artist-authored outer contour (normalized sprite rect)")]
    [SerializeField] private Vector2[] outerContour;
    [SerializeField] private Material contourMaterial;
    private LineRenderer[] contours;
    private Vector3[] contourPositions;
    private SpriteRenderer[] outlines;
    private Collider2D[] interactionColliders;
    private MaterialPropertyBlock properties;
    private GameControlManager controls;
    private IInteractable target;
    private static readonly int LineColor = Shader.PropertyToID("_LineColor");
    private static readonly int StrokePixels = Shader.PropertyToID("_StrokePixels");
    private static readonly int RedrawRate = Shader.PropertyToID("_RedrawRate");
    private static readonly int StrokeVariation = Shader.PropertyToID("_StrokeVariation");

    // The current painted footprint is authoritative; old placeholder sensors can be larger.
    public bool TryGetInteractionBounds(out Bounds bounds)
    {
        bounds=default;bool found=false;
        if(visuals==null)return false;
        foreach(var visual in visuals)
        {
            if(!visual||!visual.enabled||!visual.gameObject.activeInHierarchy||!visual.sprite||visual.color.a<=.01f)continue;
            // A custom contour may deliberately use an invisible calibrated surface.
            if(visual.forceRenderingOff&&(outerContour==null||outerContour.Length<3))continue;
            if(!found){bounds=visual.bounds;found=true;}else bounds.Encapsulate(visual.bounds);
        }
        return found;
    }

    private void Awake()
    {
        target = interactionSource as IInteractable;
        if (target == null || outlineMaterial == null || visuals == null) return;
        interactionColliders = interactionSource.GetComponents<Collider2D>();
        properties = new MaterialPropertyBlock();
        outlines = new SpriteRenderer[visuals.Length];
        bool custom = outerContour != null && outerContour.Length >= 3 && contourMaterial != null;
        if (custom) { contours = new LineRenderer[visuals.Length]; contourPositions = new Vector3[outerContour.Length]; }
        for (int i = 0; i < visuals.Length; i++)
        {
            if (visuals[i] == null) continue;
            var go = new GameObject("HandDrawnHighlight_Runtime") { hideFlags = HideFlags.DontSave };
            go.layer = visuals[i].gameObject.layer;
            go.transform.SetParent(visuals[i].transform, false);
            if (custom)
            {
                var line = go.AddComponent<LineRenderer>();
                line.sharedMaterial = contourMaterial; line.useWorldSpace = true;
                line.loop = true; line.positionCount = outerContour.Length;
                line.numCornerVertices = 2; line.numCapVertices = 2; line.enabled = false;
                contours[i] = line; continue;
            }
            outlines[i] = go.AddComponent<SpriteRenderer>();
            outlines[i].sharedMaterial = outlineMaterial;
            outlines[i].enabled = false;
        }
    }

    private void LateUpdate()
    {
        if (outlines == null) return;
        if (controls == null && PersistentRuntime.Instance != null)
            controls = PersistentRuntime.Instance.GetComponent<GameControlManager>();
        var player = controls != null ? controls.ActivePlayer : null;
        var interaction = player != null ? player.GetComponent<PlayerInteraction>() : null;
        bool selected = interactionSource != null && interactionSource.isActiveAndEnabled &&
                        controls != null && !controls.IsLocked() && interaction != null &&
                        ReferenceEquals(interaction.CurrentVisualTarget, target) && HasActiveCollider();
        // Empty prompts are used by several existing story-gated interactions.
        if (selected) selected = !string.IsNullOrWhiteSpace(target.GetInteractionPrompt());
        for (int i = 0; i < outlines.Length; i++)
        {
            var output = outlines[i]; var source = visuals[i];
            bool visible = selected && source != null && source.enabled &&
                source.gameObject.activeInHierarchy && source.sprite != null && source.color.a > .01f &&
                (!source.forceRenderingOff || (contours != null && contours[i] != null));
            if (contours != null && contours[i] != null)
            {
                contours[i].enabled = visible;
                if (visible) UpdateContour(contours[i], source);
                continue;
            }
            if (output == null) continue;
            output.enabled = visible;
            if (!output.enabled) continue;
            output.sprite = source.sprite;
            output.flipX = source.flipX; output.flipY = source.flipY;
            output.drawMode = source.drawMode; output.size = source.size;
            output.sortingLayerID = source.sortingLayerID;
            output.sortingOrder = source.sortingOrder + 1;
            output.maskInteraction = source.maskInteraction;
            output.color = Color.white;
            output.GetPropertyBlock(properties);
            Color color = lineColor; color.a *= source.color.a * (1f - opacityBreath * (.5f + .5f * Mathf.Sin(Time.time * 2.6f)));
            properties.SetColor(LineColor, color);
            properties.SetFloat(StrokePixels, strokePixels);
            properties.SetFloat(RedrawRate, redrawRate);
            properties.SetFloat(StrokeVariation, strokeVariation);
            output.SetPropertyBlock(properties);
        }
    }

    private void UpdateContour(LineRenderer line, SpriteRenderer source)
    {
        var camera = Camera.main;
        float width = camera != null && camera.orthographic
            ? 2f * camera.orthographicSize / Mathf.Max(1, camera.pixelHeight) * strokePixels : .012f;
        float phase = Mathf.Floor(Time.time * redrawRate);
        var bounds = source.sprite.bounds;
        for (int j = 0; j < outerContour.Length; j++)
        {
            Vector3 local = new Vector3(Mathf.Lerp(bounds.min.x, bounds.max.x, outerContour[j].x),
                Mathf.Lerp(bounds.min.y, bounds.max.y, outerContour[j].y), 0);
            if (source.flipX) local.x = -local.x;
            if (source.flipY) local.y = -local.y;
            Vector3 world = source.transform.TransformPoint(local);
            float shift = Mathf.Sin(j * 7.13f + phase * 1.17f) * width * strokeVariation;
            contourPositions[j] = world + new Vector3(shift, shift * .7f, 0);
        }
        line.SetPositions(contourPositions); line.widthMultiplier = width;
        Color color = lineColor; color.a *= source.color.a * (1f - opacityBreath * (.5f + .5f * Mathf.Sin(Time.time * 2.6f)));
        line.startColor = color; line.endColor = color;
        line.sortingLayerID = source.sortingLayerID; line.sortingOrder = source.sortingOrder + 1;
    }

    private bool HasActiveCollider()
    {
        foreach (var collider in interactionColliders)
            if (collider != null && collider.enabled && collider.gameObject.activeInHierarchy) return true;
        return false;
    }
    private void OnDisable()
    {
        if (outlines == null) return;
        foreach (var outline in outlines) if (outline != null) outline.enabled = false;
        if (contours != null) foreach (var line in contours) if (line != null) line.enabled = false;
    }
    private void OnDestroy()
    {
        if (outlines == null) return;
        foreach (var outline in outlines) if (outline != null) Destroy(outline.gameObject);
        if (contours != null) foreach (var line in contours) if (line != null) Destroy(line.gameObject);
    }
}
