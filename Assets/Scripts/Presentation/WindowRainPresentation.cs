using System.Collections.Generic;
using UnityEngine;
/// <summary>Rain is clipped into individual authored glass panes, behind the furniture. No audio.</summary>
public sealed class WindowRainPresentation:MonoBehaviour
{
 public SpriteRenderer window;
 public Material material;
 public Rect[] panes={new Rect(220f/1043,151f/658,75f/1043,412f/658),new Rect(331f/1043,151f/658,243f/1043,412f/658),new Rect(603f/1043,151f/658,208f/1043,412f/658)};
 [Range(0,1)]public float opacity=.23f;
 Mesh mesh;readonly List<MeshRenderer> rain=new List<MeshRenderer>();
 void Start()
 {
  if(!window||!material)return;
  mesh=new Mesh{name="Clipped window glass",hideFlags=HideFlags.DontSave};
  mesh.vertices=new[]{new Vector3(-.5f,-.5f),new Vector3(.5f,-.5f),new Vector3(.5f,.5f),new Vector3(-.5f,.5f)};
  mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();
  foreach(var pane in panes)
  {
   var g=new GameObject("WindowRain_Pane_Runtime");g.hideFlags=HideFlags.DontSave;g.transform.SetParent(transform,false);
   g.AddComponent<MeshFilter>().sharedMesh=mesh;var sr=g.AddComponent<MeshRenderer>();sr.sharedMaterial=material;sr.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;sr.receiveShadows=false;
   var p=new MaterialPropertyBlock();p.SetFloat("_Mode",3);p.SetColor("_Tint",new Color(.70f,.80f,.91f,opacity));sr.SetPropertyBlock(p);rain.Add(sr);
  }
 }
 void LateUpdate()
 {
  if(!window)return;
  var b=window.bounds;var scale=transform.lossyScale;
  for(int i=0;i<rain.Count;i++)
  {
   var r=rain[i];r.enabled=window.enabled&&!window.forceRenderingOff&&window.gameObject.activeInHierarchy&&window.color.a>.01f;
   var p=panes[i];r.transform.position=new Vector3(b.min.x+(p.x+p.width*.5f)*b.size.x,b.min.y+(p.y+p.height*.5f)*b.size.y,window.transform.position.z);
   r.transform.localScale=new Vector3(b.size.x*p.width/Mathf.Abs(scale.x),b.size.y*p.height/Mathf.Abs(scale.y),1);
   r.sortingLayerID=window.sortingLayerID;r.sortingOrder=window.sortingOrder+1;
  }
 }
 void OnDisable(){foreach(var r in rain)if(r)r.enabled=false;}
 void OnDestroy(){if(mesh)Destroy(mesh);}
}
