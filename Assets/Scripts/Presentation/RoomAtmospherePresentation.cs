using System.Collections.Generic;
using UnityEngine;

/// <summary>Local, optional art lighting and mist. Never touches cameras, actors or gameplay clocks.</summary>
public sealed class RoomAtmospherePresentation : MonoBehaviour
{
    [SerializeField] private Material material;
    [SerializeField] private Vector2 regionMin, regionMax;
    [SerializeField] private Vector2[] lightAnchors;
    [SerializeField] private Color lightColor = new Color(1,.72f,.43f,.12f);
    [SerializeField] private Color mistColor = new Color(.35f,.31f,.52f,.045f);
    [SerializeField] private float floorY;
    private readonly List<MeshRenderer> views = new List<MeshRenderer>();
    private Mesh mesh;
    private GameControlManager controls;
    private void Start()
    {
        if(!material)return;
        mesh=new Mesh{name="Room atmosphere",hideFlags=HideFlags.DontSave};
        mesh.vertices=new[]{new Vector3(-.5f,-.5f),new Vector3(.5f,-.5f),new Vector3(.5f,.5f),new Vector3(-.5f,.5f)};
        mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();
        Add("LowMist",new Vector2((regionMin.x+regionMax.x)*.5f,floorY+.25f),new Vector2(regionMax.x-regionMin.x,1.35f),1,mistColor,4);
        if(lightAnchors!=null)foreach(var p in lightAnchors)Add("SoftWindowLight",p,new Vector2(5,4),0,lightColor,-8);
    }
    private void Add(string label,Vector2 p,Vector2 size,float mode,Color tint,int order)
    {
        var go=new GameObject(label){hideFlags=HideFlags.DontSave};go.transform.SetParent(transform,false);go.transform.position=p;go.transform.localScale=new Vector3(size.x,size.y,1);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;var r=go.AddComponent<MeshRenderer>();r.sharedMaterial=material;r.sortingOrder=order;r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
        var properties=new MaterialPropertyBlock();properties.SetFloat("_Mode",mode);properties.SetColor("_Tint",tint);r.SetPropertyBlock(properties);views.Add(r);
    }
    private void LateUpdate()
    {
        if(!controls&&PersistentRuntime.Instance)controls=PersistentRuntime.Instance.GetComponent<GameControlManager>();
        var actor=controls?controls.ActivePlayer:null;bool show=false;
        if(actor){var p=actor.transform.position;show=p.x>=regionMin.x&&p.x<=regionMax.x&&p.y>=regionMin.y&&p.y<=regionMax.y;}
        foreach(var r in views)if(r)r.enabled=show;
    }
    private void OnDestroy(){foreach(var r in views)if(r)Destroy(r.gameObject);if(mesh)Destroy(mesh);}
}
