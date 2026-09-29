using UnityEngine;

/// <summary>Mirrors brightness changes from a story-owned renderer onto an independent art layer.</summary>
[DisallowMultipleComponent]
public sealed class SpriteBrightnessLink : MonoBehaviour
{
    [SerializeField] private SpriteRenderer source;
    [SerializeField] private SpriteRenderer visual;
    [SerializeField, Min(0.001f)] private float referenceBrightness = 1f;
    private Color baseColor;

    private void Awake()
    {
        if (visual != null) baseColor = visual.color;
    }

    private void LateUpdate()
    {
        if (visual == null) return;
        visual.enabled = source != null && source.enabled && source.gameObject.activeInHierarchy;
        if (!visual.enabled) return;
        Color c = source.color;
        float brightness = Mathf.Max(c.r, Mathf.Max(c.g, c.b));
        float strength = Mathf.Clamp(brightness / Mathf.Max(.001f, referenceBrightness), 0f, 2f);
        visual.color = new Color(baseColor.r * strength, baseColor.g * strength, baseColor.b * strength, baseColor.a);
    }

    private void OnDisable()
    {
        if (visual != null) visual.enabled = false;
    }
}
