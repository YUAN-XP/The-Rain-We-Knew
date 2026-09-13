using UnityEngine;
using UnityEngine.UI;

// Layered vector placeholder: graphite tip, pencil, writing fingers/wrist, then a hand entering from the right.
// Coordinates are virtual tabletop units (1000 x 500), never independent hit-test coordinates.
[RequireComponent(typeof(CanvasRenderer))]
public class PencilDeskHandsGraphic : MaskableGraphic
{
    Vector2 tip = new Vector2(520, 235);
    float grab;
    VertexHelper mesh;
    public Vector2 TipPosition { get => tip; set { tip = value; SetVerticesDirty(); } }
    public float GrabProgress { get => grab; set { grab = Mathf.Clamp01(value); SetVerticesDirty(); } }
    public Vector2 WristScreenPosition
    {
        get
        {
            var world = rectTransform.TransformPoint(P(tip + new Vector2(100, -70)));
            return RectTransformUtility.WorldToScreenPoint(null, world);
        }
    }
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); mesh = vh;
        var skin = new Color(.80f, .60f, .42f);
        var shade = new Color(.48f, .29f, .20f);
        Vector2 wrist = tip + new Vector2(100, -70);
        // The writing forearm comes from the bottom right; the pencil tip is exactly TipPosition.
        Line(tip + new Vector2(190, -300), wrist, 74, new Color(.15f, .18f, .19f));
        Line(wrist + new Vector2(25, -58), wrist, 58, skin);
        Oval(wrist + new Vector2(-18, 17), 40, 31, shade);
        Line(wrist, tip + new Vector2(42, 6), 45, skin);
        Line(tip + new Vector2(3, 4), tip + new Vector2(68, 112), 10, new Color(.78f, .52f, .15f));
        Line(tip + new Vector2(64, 107), tip + new Vector2(73, 121), 11, new Color(.63f, .29f, .26f));
        Triangle(tip, tip + new Vector2(0, 14), tip + new Vector2(11, 8), new Color(.18f, .16f, .14f));
        Oval(tip + new Vector2(35, 22), 16, 11, new Color(.92f, .71f, .50f));
        Oval(tip + new Vector2(49, 11), 15, 10, skin);
        Oval(tip + new Vector2(59, -3), 13, 9, skin);
        // Small point shadow emphasizes the tip and gives the player an exact rubbing cursor.
        Oval(tip + new Vector2(1, -1), 2.5f, 1.7f, new Color(.09f, .075f, .06f, .8f));
        if (grab <= 0) return;
        Vector2 palm = Vector2.Lerp(new Vector2(1150, wrist.y + 18), wrist + new Vector2(7, 5), grab);
        Line(new Vector2(1120, wrist.y - 3), palm + new Vector2(20, 0), 66, new Color(.09f, .105f, .11f));
        Line(palm + new Vector2(60, 0), palm, 44, new Color(.56f, .51f, .43f));
        Oval(palm, 28, 33, new Color(.64f, .58f, .47f));
        for (int i = 0; i < 4; i++)
        {
            Vector2 start = palm + new Vector2(-14 + i * 9, 15);
            Vector2 end = palm + new Vector2(-24 + i * 9, -25 - (i % 2) * 5);
            Line(start, end, 9, new Color(.68f, .61f, .49f));
            Oval(end, 4.5f, 6, new Color(.56f, .48f, .38f));
        }
        Line(palm + new Vector2(20, 14), palm + new Vector2(2, 28), 12, new Color(.73f, .65f, .52f));
    }
    Vector2 P(Vector2 p)
    {
        var r = rectTransform.rect;
        return new Vector2(r.xMin + p.x / 1000 * r.width, r.yMin + p.y / 500 * r.height);
    }
    void Triangle(Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        int n = mesh.currentVertCount;
        mesh.AddVert(P(a), color, Vector2.zero); mesh.AddVert(P(b), color, Vector2.zero); mesh.AddVert(P(c), color, Vector2.zero);
        mesh.AddTriangle(n, n + 1, n + 2);
    }
    void Line(Vector2 a, Vector2 b, float width, Color color)
    {
        var direction = (b - a).normalized;
        var normal = new Vector2(-direction.y, direction.x) * width * .5f;
        Triangle(a - normal, a + normal, b + normal, color);
        Triangle(a - normal, b + normal, b - normal, color);
    }
    void Oval(Vector2 center, float rx, float ry, Color color)
    {
        for (int i = 0; i < 24; i++)
        {
            float a = i * Mathf.PI / 12, b = (i + 1) * Mathf.PI / 12;
            Triangle(center, center + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry),
                center + new Vector2(Mathf.Cos(b) * rx, Mathf.Sin(b) * ry), color);
        }
    }
}
