using System;
using UnityEngine;

/// <summary>One continuous painting, calibrated to existing room anchors. Owns no gameplay state.</summary>
[ExecuteAlways, RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
[DefaultExecutionOrder(960)]
public sealed class ContinuousRoomArtwork : MonoBehaviour
{
    [Serializable] public struct Knot { public float pixel, world; public Knot(float p,float w){pixel=p;world=w;} }
    public Texture2D painting;
    public Material material;
    public Knot[] horizontal, vertical;
    public Rect actorRegion;
    public int sortingOrder=-30;
    public SpriteRenderer recoverySource;
    public Color tint=Color.white;
    public Vector2 fadeLeft;
    Mesh mesh;
    MeshRenderer output;
    MaterialPropertyBlock properties;
    GameControlManager controls;
    bool rebuild;
    void OnEnable(){rebuild=true;}
    void OnValidate(){rebuild=true;}
    public static float Map(Knot[] knots,float pixel)
    {
        if(knots==null||knots.Length<2)return 0;
        for(int i=1;i<knots.Length;i++)if(pixel<=knots[i].pixel)
            return Mathf.LerpUnclamped(knots[i-1].world,knots[i].world,
                (pixel-knots[i-1].pixel)/Mathf.Max(.001f,knots[i].pixel-knots[i-1].pixel));
        return knots[knots.Length-1].world;
    }
    public void Rebuild()
    {
        if(!painting||horizontal==null||vertical==null||horizontal.Length<2||vertical.Length<2)return;
        if(mesh)ReleaseMesh();
        mesh=new Mesh{name="ContinuousRoomPainting_Runtime",hideFlags=HideFlags.HideAndDontSave};
        int nx=horizontal.Length,ny=vertical.Length;
        var vertices=new Vector3[nx*ny];var uv=new Vector2[vertices.Length];var colors=new Color[vertices.Length];
        var triangles=new int[(nx-1)*(ny-1)*6];int index=0;
        for(int y=0;y<ny;y++)for(int x=0;x<nx;x++)
        {
            int i=y*nx+x;
            vertices[i]=transform.InverseTransformPoint(new Vector3(horizontal[x].world,vertical[y].world,0));
            uv[i]=new Vector2(horizontal[x].pixel/painting.width,1-vertical[y].pixel/painting.height);
            float alpha=fadeLeft.y>fadeLeft.x?Mathf.InverseLerp(fadeLeft.x,fadeLeft.y,horizontal[x].world):1;
            colors[i]=new Color(1,1,1,alpha);
            if(x==nx-1||y==ny-1)continue;
            triangles[index++]=i;triangles[index++]=i+nx;triangles[index++]=i+1;
            triangles[index++]=i+1;triangles[index++]=i+nx;triangles[index++]=i+nx+1;
        }
        mesh.vertices=vertices;mesh.uv=uv;mesh.colors=colors;mesh.triangles=triangles;mesh.RecalculateBounds();
        GetComponent<MeshFilter>().sharedMesh=mesh;
        output=GetComponent<MeshRenderer>();output.sharedMaterial=material;
        output.sortingLayerName="Default";output.sortingOrder=sortingOrder;
        rebuild=false;
    }
    void LateUpdate()
    {
        if(rebuild||!mesh)Rebuild();if(!output)return;
        bool show=true;
        if(Application.isPlaying)
        {
            if(!controls&&PersistentRuntime.Instance)controls=PersistentRuntime.Instance.GetComponent<GameControlManager>();
            show=controls&&controls.ActivePlayer&&actorRegion.Contains(controls.ActivePlayer.transform.position);
        }
        output.enabled=show;
        properties??=new MaterialPropertyBlock();
        Color color=tint;
        if(recoverySource&&Application.isPlaying)
        {
            var c=recoverySource.color;
            float recovery=Mathf.InverseLerp(.20f,.69f,Mathf.Max(c.r,c.g,c.b));
            color*=Mathf.Lerp(.68f,1f,recovery);color.a=tint.a;
            if(c.r>c.g*1.3f){color.g*=.8f;color.b*=.8f;}
        }
        properties.SetTexture("_MainTex",painting);properties.SetColor("_Color",color);
        output.SetPropertyBlock(properties);
    }
    void OnDisable(){if(output)output.enabled=false;}
    void OnDestroy(){ReleaseMesh();}
    void ReleaseMesh(){if(!mesh)return;if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);mesh=null;}
}
