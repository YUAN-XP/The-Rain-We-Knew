using UnityEngine;
using UnityEngine.UI;

// Small vector placeholders shared by the childhood close-up and puppet stage.
[RequireComponent(typeof(CanvasRenderer))]
public class ChildhoodIllustration : MaskableGraphic
{
    public enum Picture { Cat, Princess, Knight, Rebel, Castle, Ring, Coin, Prize }
    public Picture picture;
    static readonly Color Ink=new Color(.20f,.18f,.22f), Gold=new Color(.95f,.72f,.31f), Skin=new Color(.98f,.79f,.58f);
    VertexHelper mesh;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();mesh=vh;
        switch(picture)
        {
            case Picture.Cat:
                Oval(.44f,.26f,.39f,.18f,Ink);Oval(.44f,.29f,.36f,.18f,Gold);
                Oval(.79f,.46f,.17f,.20f,Ink);Oval(.79f,.46f,.15f,.18f,Gold);
                Tri(.64f,.56f,.65f,.85f,.79f,.64f,Ink);Tri(.80f,.64f,.94f,.84f,.94f,.54f,Ink);
                Tri(.68f,.60f,.69f,.76f,.77f,.63f,Gold);Tri(.82f,.62f,.91f,.76f,.91f,.58f,Gold);
                Oval(.74f,.47f,.018f,.03f,Ink);Oval(.85f,.47f,.018f,.03f,Ink);Tri(.77f,.40f,.83f,.40f,.80f,.37f,new Color(.66f,.35f,.34f));
                Oval(.61f,.15f,.14f,.045f,Skin);Oval(.34f,.16f,.14f,.045f,Skin);
                break;
            case Picture.Ring:
                Ring(.5f,.5f,.47f,.35f,.10f,Ink);Ring(.5f,.55f,.45f,.33f,.055f,Gold);break;
            case Picture.Coin:
                Oval(.5f,.5f,.43f,.43f,Ink);Oval(.5f,.52f,.38f,.38f,Gold);Ring(.5f,.52f,.29f,.29f,.025f,new Color(.73f,.46f,.16f));Quad(.46f,.34f,.54f,.70f,Ink);break;
            case Picture.Castle:
                Quad(.10f,.08f,.90f,.65f,new Color(.70f,.73f,.70f));
                foreach(float x in new[]{.06f,.72f}){Quad(x,.07f,x+.22f,.85f,new Color(.82f,.82f,.70f));Tri(x-.025f,.85f,x+.11f,1,x+.245f,.85f,new Color(.44f,.49f,.64f));}
                Oval(.5f,.36f,.14f,.23f,Ink);Quad(.36f,.08f,.64f,.36f,Ink);
                Quad(.15f,.61f,.20f,.74f,Ink);Quad(.80f,.61f,.85f,.74f,Ink);break;
            case Picture.Prize:
                Oval(.5f,.18f,.36f,.10f,Ink);Quad(.32f,.19f,.68f,.62f,Gold);Oval(.5f,.64f,.28f,.22f,Skin);Oval(.42f,.67f,.025f,.03f,Ink);Oval(.58f,.67f,.025f,.03f,Ink);break;
            default:
                Color coat=picture==Picture.Princess?new Color(.80f,.39f,.50f):picture==Picture.Knight?new Color(.40f,.65f,.74f):new Color(.57f,.39f,.59f);
                Quad(.18f,.04f,.40f,.14f,Ink);Quad(.60f,.04f,.82f,.14f,Ink);
                Quad(.31f,.11f,.44f,.32f,Ink);Quad(.56f,.11f,.69f,.32f,Ink);
                Tri(.18f,.29f,.5f,.69f,.82f,.29f,Ink);Tri(.22f,.31f,.5f,.66f,.78f,.31f,coat);
                Oval(.5f,.73f,.25f,.22f,Ink);Oval(.5f,.72f,.21f,.19f,Skin);
                Oval(.43f,.74f,.019f,.025f,Ink);Oval(.57f,.74f,.019f,.025f,Ink);Quad(.46f,.64f,.55f,.655f,Ink);
                if(picture==Picture.Princess){Quad(.30f,.89f,.70f,.96f,Gold);for(int i=0;i<3;i++)Tri(.30f+i*.14f,.92f,.35f+i*.14f,1,.42f+i*.14f,.92f,Gold);}
                if(picture==Picture.Knight){Quad(.26f,.87f,.73f,.94f,new Color(.63f,.74f,.77f));Quad(.77f,.20f,.81f,.76f,Ink);Tri(.77f,.76f,.79f,.84f,.81f,.76f,Ink);Quad(.71f,.36f,.88f,.40f,Gold);}
                if(picture==Picture.Rebel){Tri(.22f,.88f,.52f,1,.80f,.88f,coat);Quad(.12f,.27f,.16f,.83f,Ink);Tri(.14f,.83f,.14f,.61f,.38f,.75f,coat);}
                break;
        }
    }
    Vector2 P(float x,float y){var r=rectTransform.rect;return new Vector2(r.xMin+x*r.width,r.yMin+y*r.height);}
    void Tri(float x,float y,float xx,float yy,float xxx,float yyy,Color c)
    {int n=mesh.currentVertCount;mesh.AddVert(P(x,y),c,Vector2.zero);mesh.AddVert(P(xx,yy),c,Vector2.zero);mesh.AddVert(P(xxx,yyy),c,Vector2.zero);mesh.AddTriangle(n,n+1,n+2);}
    void Quad(float x,float y,float xx,float yy,Color c){Tri(x,y,x,yy,xx,yy,c);Tri(x,y,xx,yy,xx,y,c);}
    void Oval(float x,float y,float rx,float ry,Color c){for(int i=0;i<40;i++){float a=i*Mathf.PI/20,b=(i+1)*Mathf.PI/20;Tri(x,y,x+Mathf.Cos(a)*rx,y+Mathf.Sin(a)*ry,x+Mathf.Cos(b)*rx,y+Mathf.Sin(b)*ry,c);}}
    void Ring(float x,float y,float rx,float ry,float width,Color c)
    {for(int i=0;i<48;i++){float a=i*Mathf.PI/24,b=(i+1)*Mathf.PI/24;Vector2 p=P(x+Mathf.Cos(a)*rx,y+Mathf.Sin(a)*ry),q=P(x+Mathf.Cos(b)*rx,y+Mathf.Sin(b)*ry),r=P(x+Mathf.Cos(a)*(rx-width),y+Mathf.Sin(a)*(ry-width)),s=P(x+Mathf.Cos(b)*(rx-width),y+Mathf.Sin(b)*(ry-width));int n=mesh.currentVertCount;mesh.AddVert(p,c,Vector2.zero);mesh.AddVert(q,c,Vector2.zero);mesh.AddVert(r,c,Vector2.zero);mesh.AddVert(s,c,Vector2.zero);mesh.AddTriangle(n,n+1,n+2);mesh.AddTriangle(n+1,n+3,n+2);}}
}
