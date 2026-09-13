using UnityEngine;
using UnityEngine.UI;

// Replaceable desktop placeholder. The faint scratches are deliberately not the unrevealed name.
[RequireComponent(typeof(CanvasRenderer))]
public class PencilDeskSurfaceGraphic : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper mesh)
    {
        mesh.Clear();
        var r = rectTransform.rect;
        var grain = new Color(.23f, .14f, .075f, .2f);
        for (int i = 0; i < 15; i++)
        {
            float y = .035f + i * .067f;
            Quad(mesh, r, .012f, y, .988f, y + .0028f, grain);
        }
        Quad(mesh, r, .018f, .02f, .982f, .028f, new Color(.75f, .56f, .34f, .35f));
        Quad(mesh, r, .022f, .963f, .978f, .975f, new Color(.25f, .15f, .075f, .55f));
        for (int i = 0; i < 24; i++)
        {
            float x = .335f + ((i * 17) % 29) * .010f;
            float y = .42f + ((i * 13) % 17) * .011f;
            Quad(mesh, r, x, y, x + .006f + (i % 4) * .007f, y + .0019f,
                new Color(.22f, .14f, .08f, .13f));
        }
        foreach (var p in new[] { new Vector2(.04f,.06f), new Vector2(.96f,.06f), new Vector2(.04f,.94f), new Vector2(.96f,.94f) })
            Quad(mesh, r, p.x - .005f, p.y - .006f, p.x + .005f, p.y + .006f, new Color(.21f, .19f, .16f));
    }
    static void Quad(VertexHelper mesh, Rect r, float x, float y, float xx, float yy, Color c)
    {
        int n = mesh.currentVertCount;
        mesh.AddVert(new Vector2(r.xMin + x * r.width, r.yMin + y * r.height), c, Vector2.zero);
        mesh.AddVert(new Vector2(r.xMin + x * r.width, r.yMin + yy * r.height), c, Vector2.zero);
        mesh.AddVert(new Vector2(r.xMin + xx * r.width, r.yMin + yy * r.height), c, Vector2.zero);
        mesh.AddVert(new Vector2(r.xMin + xx * r.width, r.yMin + y * r.height), c, Vector2.zero);
        mesh.AddTriangle(n, n + 1, n + 2);
        mesh.AddTriangle(n, n + 2, n + 3);
    }
}
