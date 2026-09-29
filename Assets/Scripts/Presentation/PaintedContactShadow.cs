using UnityEngine;

/// <summary>Small presentation-only contact mark; follows the visible silhouette, never the collision box.</summary>
[DefaultExecutionOrder(1400)]
public sealed class PaintedContactShadow : MonoBehaviour
{
 public SpriteRenderer target;
 public float widthFactor=.8f;
 public float minimumWidth=.16f;
 public float depth=.12f;
 MeshRenderer ink;Mesh mesh;
 void Start(){
  if(!target)target=GetComponent<SpriteRenderer>();if(!target)return;
  var material=Resources.Load<Material>("DemoContactShadow");if(!material)return;
  var g=new GameObject("PaintedContact_Runtime");g.hideFlags=HideFlags.DontSave;g.transform.SetParent(transform,false);
  mesh=new Mesh{name="Contact plane",hideFlags=HideFlags.DontSave};mesh.vertices=new[]{new Vector3(-.5f,-.5f),new Vector3(.5f,-.5f),new Vector3(.5f,.5f),new Vector3(-.5f,.5f)};mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();
  g.AddComponent<MeshFilter>().sharedMesh=mesh;ink=g.AddComponent<MeshRenderer>();ink.sharedMaterial=material;ink.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;ink.receiveShadows=false;
 }
 void LateUpdate(){
  if(!ink||!target)return;ink.enabled=target.enabled&&!target.forceRenderingOff&&target.gameObject.activeInHierarchy&&target.color.a>.01f;
  var b=target.bounds;ink.transform.position=new Vector3(b.center.x,b.min.y-.008f,target.transform.position.z);
  var scale=ink.transform.parent.lossyScale;ink.transform.localScale=new Vector3(Mathf.Max(minimumWidth,b.size.x*widthFactor)/Mathf.Abs(scale.x),depth/Mathf.Abs(scale.y),1);
  ink.sortingLayerID=target.sortingLayerID;ink.sortingOrder=target.sortingOrder-1;
 }
 void OnDisable(){if(ink)ink.enabled=false;}
 void OnDestroy(){if(mesh)Destroy(mesh);}
}
