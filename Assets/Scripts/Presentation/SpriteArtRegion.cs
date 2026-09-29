using UnityEngine;

/// <summary>Non-destructive presentation crop; leaves the imported PNG and physics untouched.</summary>
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
public sealed class SpriteArtRegion : MonoBehaviour
{
    [SerializeField] private Sprite source;
    [SerializeField] private Rect pixelRegion;
    [SerializeField] private Vector2 pivot = new Vector2(.5f, 0);
    private Sprite generated;
    private SpriteRenderer output;
    private void OnEnable() => Rebuild();
    public void Rebuild()
    {
        output = GetComponent<SpriteRenderer>();
        Release();
        if (source == null || pixelRegion.width <= 0 || pixelRegion.height <= 0) return;
        var texture = source.texture;
        if (pixelRegion.xMin < 0 || pixelRegion.yMin < 0 || pixelRegion.xMax > texture.width || pixelRegion.yMax > texture.height) return;
        generated = Sprite.Create(texture, pixelRegion, pivot, source.pixelsPerUnit, 0, SpriteMeshType.FullRect, Vector4.zero, false);
        generated.name = source.name + "_VisualRegion";
        generated.hideFlags = HideFlags.HideAndDontSave;
        output.sprite = generated;
    }
    // Inactive layout renderers are read by camera preview; keep their cropped sprite alive.
    private void OnDestroy() => Release();
    private void Release()
    {
        if (generated == null) return;
        if (Application.isPlaying) Destroy(generated); else DestroyImmediate(generated);
        generated = null;
    }
}
