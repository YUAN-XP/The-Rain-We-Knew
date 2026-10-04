using UnityEngine;
/// <summary>Repeats the native painting at its calibrated density; mirrored edges join continuously.</summary>
[ExecuteAlways]
public sealed class P03RepeatedRoomStrip:MonoBehaviour
{
 public Sprite source;
 public Material material;
 public float width=16,height=3,tileWidth=4;
 public float nativeTileHeight; // Zero keeps the single native vertical region; floor extensions repeat at original density.
 public int order=-35;
 public Color tint=Color.white;
 public SpriteRenderer tintSource;
 Mesh mesh;MeshRenderer output;MaterialPropertyBlock props;
 void OnEnable()=>Rebuild();
 public void Rebuild()
 {
  if(!source||!material)return;
  if(mesh){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}
  mesh=new Mesh{name="Native-density painting strip",hideFlags=HideFlags.HideAndDontSave};
  mesh.vertices=new[]{new Vector3(-width*.5f,0),new Vector3(width*.5f,0),new Vector3(width*.5f,height),new Vector3(-width*.5f,height)};
  mesh.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();
  var filter=GetComponent<MeshFilter>();if(!filter)filter=gameObject.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
  output=GetComponent<MeshRenderer>();if(!output)output=gameObject.AddComponent<MeshRenderer>();output.sharedMaterial=material;output.sortingOrder=order;output.enabled=true;
  output.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;output.receiveShadows=false;props=new MaterialPropertyBlock();Apply();
 }
 void LateUpdate()=>Apply();
 void Apply()
 {
  if(!output||!source)return;
  var rect=source.rect;var tex=source.texture;
  props.SetTexture("_MainTex",tex);props.SetVector("_Region",new Vector4(rect.x/tex.width,rect.y/tex.height,rect.width/tex.width,rect.height/tex.height));
  props.SetFloat("_Repeat",width/Mathf.Max(.01f,tileWidth));props.SetFloat("_RepeatY",nativeTileHeight>0?height/nativeTileHeight:1);props.SetColor("_Tint",tintSource?tintSource.color:tint);
  output.SetPropertyBlock(props);
 }
 void OnDisable(){if(output)output.enabled=false;}
 void OnDestroy(){if(mesh){if(Application.isPlaying)Destroy(mesh);else DestroyImmediate(mesh);}}
}
