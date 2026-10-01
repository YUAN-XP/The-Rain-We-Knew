using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared curved lower bust edge. UI geometry only; source paintings stay unchanged.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class DialoguePortraitMask : MaskableGraphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();Rect r=rectTransform.rect;
        var points=new System.Collections.Generic.List<Vector2>();
        points.Add(new Vector2(r.xMin,r.yMax));points.Add(new Vector2(r.xMax,r.yMax));
        const int steps=32;
        for(int i=0;i<=steps;i++)
        {
            float a=Mathf.PI*i/steps;
            points.Add(new Vector2(r.center.x+Mathf.Cos(a)*r.width*.5f,r.yMin+r.height*.10f*(1-Mathf.Sin(a))));
        }
        vh.AddVert(r.center,color,Vector2.zero);
        foreach(var p in points)vh.AddVert(p,color,Vector2.zero);
        for(int i=0;i<points.Count;i++)vh.AddTriangle(0,i+1,(i+1)%points.Count+1);
    }
}
