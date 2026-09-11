using UnityEngine;
using UnityEngine.UI;

// Replaceable vector placeholder: guitar body, strings and the boy's hands.
[RequireComponent(typeof(CanvasRenderer))]
public class GuitarCloseupGraphic : MaskableGraphic
{
    VertexHelper mesh;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); mesh=vh;
        Oval(.30f,.43f,.27f,.37f,new Color(.28f,.17f,.10f));
        Oval(.31f,.45f,.25f,.34f,new Color(.76f,.48f,.22f));
        Oval(.48f,.51f,.19f,.25f,new Color(.87f,.63f,.33f));
        Quad(.41f,.46f,.96f,.58f,new Color(.30f,.20f,.13f));
        Oval(.34f,.52f,.085f,.12f,new Color(.18f,.13f,.10f));
        Quad(.20f,.39f,.23f,.64f,new Color(.30f,.20f,.13f));
        for(int i=0;i<6;i++)Quad(.215f,.484f+i*.012f,.98f,.486f+i*.012f,new Color(.91f,.85f,.66f));
        for(int i=0;i<9;i++)Quad(.52f+i*.046f,.46f,.522f+i*.046f,.58f,new Color(.66f,.65f,.55f));
        Oval(.68f,.60f,.10f,.055f,new Color(.97f,.75f,.53f));
        Quad(.63f,.61f,.69f,.87f,new Color(.95f,.73f,.51f));
        Oval(.30f,.36f,.075f,.065f,new Color(.97f,.75f,.53f));
        Quad(.18f,.10f,.25f,.37f,new Color(.95f,.73f,.51f));
    }
    Vector2 P(float x,float y){var r=rectTransform.rect;return new Vector2(r.xMin+x*r.width,r.yMin+y*r.height);}
    void Tri(float x,float y,float xx,float yy,float xxx,float yyy,Color c)
    {int n=mesh.currentVertCount;mesh.AddVert(P(x,y),c,Vector2.zero);mesh.AddVert(P(xx,yy),c,Vector2.zero);mesh.AddVert(P(xxx,yyy),c,Vector2.zero);mesh.AddTriangle(n,n+1,n+2);}
    void Quad(float x,float y,float xx,float yy,Color c){Tri(x,y,x,yy,xx,yy,c);Tri(x,y,xx,yy,xx,y,c);}
    void Oval(float x,float y,float rx,float ry,Color c){for(int i=0;i<40;i++){float a=i*Mathf.PI/20,b=(i+1)*Mathf.PI/20;Tri(x,y,x+Mathf.Cos(a)*rx,y+Mathf.Sin(a)*ry,x+Mathf.Cos(b)*rx,y+Mathf.Sin(b)*ry,c);}}
}
