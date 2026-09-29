using UnityEngine;
using UnityEngine.UI;

/// <summary>Small deterministic brush accents for UI borders, independent of scene geometry.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class DemoPaintedFrame : MaskableGraphic
{
    public float thickness=2.2f;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();var r=GetPixelAdjustedRect();
        var corners=new[]{new Vector2(r.xMin+2,r.yMin+1),new Vector2(r.xMax-3,r.yMin+4),new Vector2(r.xMax-1,r.yMax-2),new Vector2(r.xMin+4,r.yMax-1)};
        for(int edge=0;edge<4;edge++)for(int i=0;i<12;i++){
            var a=Vector2.Lerp(corners[edge],corners[(edge+1)%4],i/12f);var b=Vector2.Lerp(corners[edge],corners[(edge+1)%4],(i+1)/12f);
            var n=new Vector2(-(b-a).y,(b-a).x).normalized;float w=thickness*(.8f+.2f*Mathf.Sin(i*3.3f+edge));
            int v=vh.currentVertCount;vh.AddVert(a-n*w,color,Vector2.zero);vh.AddVert(a+n*w,color,Vector2.zero);vh.AddVert(b+n*w,color,Vector2.zero);vh.AddVert(b-n*w,color,Vector2.zero);vh.AddTriangle(v,v+1,v+2);vh.AddTriangle(v,v+2,v+3);
        }
    }
    public static void Add(Transform parent,Color color,float width=2.2f)
    {
        var r=RuntimePanelUI.Rect("PaintedContour",parent,Vector2.zero,Vector2.one);var frame=r.gameObject.AddComponent<DemoPaintedFrame>();frame.color=color;frame.thickness=width;frame.raycastTarget=false;
    }
}
