using System.Collections.Generic;
using UnityEngine;

/// <summary>Scene-owned, visual-only rain, haze and local light accents. No gameplay clocks or locks.</summary>
public sealed class StreetAtmospherePresentation : MonoBehaviour
{
    [SerializeField] private Material effectMaterial;
    [SerializeField] private Vector2 coverage = new Vector2(-39,59);
    [SerializeField] private Transform[] lampEmitters;
    [SerializeField] private float groundY=-1.08f;
    private readonly List<MeshRenderer> lampPools=new List<MeshRenderer>();
    [SerializeField] private SpriteRenderer policeVisual;
    [SerializeField] private SpriteRenderer reportersVisual;
    [SerializeField, Range(0,1)] private float rainOpacity = .11f;
    [SerializeField, Range(0,1)] private float fogOpacity = .035f;
    private readonly List<GameObject> owned = new List<GameObject>();
    private Mesh mesh;
    private MeshRenderer red, blue, flash;
    private MaterialPropertyBlock properties;
    private float nextFlash, flashUntil;
    private System.Random flashRandom;
    static readonly int Tint=Shader.PropertyToID("_Tint"),Mode=Shader.PropertyToID("_Mode");

    private void Start()
    {
        if(effectMaterial==null)return;
        properties=new MaterialPropertyBlock();flashRandom=new System.Random();
        mesh=new Mesh { name="Street atmosphere quad",hideFlags=HideFlags.DontSave };
        mesh.vertices=new[]{new Vector3(-.5f,-.5f),new Vector3(.5f,-.5f),new Vector3(.5f,.5f),new Vector3(-.5f,.5f)};
        mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();
        float width=coverage.y-coverage.x,center=(coverage.x+coverage.y)*.5f;
        Effect("DistantHaze",new Vector2(center,2),new Vector2(width,8),-35,1,new Color(.36f,.40f,.56f,.09f));
        Effect("LowDriftingMist",new Vector2(center,-.6f),new Vector2(width,1.8f),3,1,new Color(.43f,.53f,.61f,fogOpacity));
        Effect("DrawnRain",new Vector2(center,2),new Vector2(width,13),42,3,new Color(.65f,.73f,.84f,rainOpacity));
        if(lampEmitters!=null)foreach(var emitter in lampEmitters)
        {
            if(!emitter)continue;
            float height=Mathf.Max(.1f,emitter.position.y-groundY);
            lampPools.Add(Effect("LampPool",new Vector2(emitter.position.x,groundY+height*.5f),new Vector2(height*1.08f,height),-12,0,new Color(1,.66f,.38f,.20f)));
        }
        if(policeVisual!=null)
        {
            red=Effect("PoliceRed",Vector2.zero,new Vector2(.46f,.34f),12,2,Color.clear);
            blue=Effect("PoliceBlue",Vector2.zero,new Vector2(.46f,.34f),12,2,Color.clear);
        }
        if(reportersVisual!=null)flash=Effect("ReporterFlash",Vector2.zero,new Vector2(.45f,.25f),12,2,Color.clear);
        nextFlash=Time.time+4.5f;
    }
    private MeshRenderer Effect(string label,Vector2 position,Vector2 size,int order,int mode,Color color)
    {
        var go=new GameObject(label+"_Presentation") { hideFlags=HideFlags.DontSave };owned.Add(go);go.transform.SetParent(transform,false);
        go.transform.position=new Vector3(position.x,position.y,0);go.transform.localScale=new Vector3(size.x,size.y,1);
        go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=effectMaterial;
        renderer.sortingLayerName="Default";renderer.sortingOrder=order;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
        properties.Clear();properties.SetFloat(Mode,mode);properties.SetColor(Tint,color);renderer.SetPropertyBlock(properties);return renderer;
    }
    private void Update()
    {
        if(properties==null)return;
        if(red!=null)
        {
            bool show=policeVisual!=null&&policeVisual.enabled&&policeVisual.gameObject.activeInHierarchy;
            red.enabled=blue.enabled=show;
            if(show)
            {
                var b=policeVisual.bounds;red.transform.position=new Vector3(b.center.x+.02f,b.max.y-.10f,0);blue.transform.position=new Vector3(b.center.x+.45f,b.max.y-.10f,0);
                float phase=Mathf.Repeat(Time.time,2.8f);
                TintPulse(red,new Color(1,.17f,.20f),phase<.17f||phase>.33f&&phase<.49f?.58f:.025f);
                TintPulse(blue,new Color(.26f,.43f,1),phase>1.4f&&phase<1.57f||phase>1.73f&&phase<1.89f?.58f:.025f);
            }
        }
        if(flash!=null)
        {
            bool show=reportersVisual!=null&&reportersVisual.enabled&&reportersVisual.gameObject.activeInHierarchy;
            flash.enabled=show;
            if(show)
            {
                if(Time.time>=nextFlash){flashUntil=Time.time+.10f;nextFlash=Time.time+4.5f+(float)flashRandom.NextDouble()*4.5f;}
                var b=reportersVisual.bounds;flash.transform.position=new Vector3(b.min.x+b.size.x*.125f,b.min.y+b.size.y*.76f,0);
                TintPulse(flash,new Color(.90f,.94f,1),Time.time<flashUntil?.60f:0);
            }
        }
    }
    private void TintPulse(MeshRenderer renderer,Color color,float opacity)
    {renderer.GetPropertyBlock(properties);color.a=opacity;properties.SetColor(Tint,color);renderer.SetPropertyBlock(properties);}
    private void OnDestroy()
    {foreach(var go in owned)if(go!=null)Destroy(go);if(mesh!=null)Destroy(mesh);}
}
