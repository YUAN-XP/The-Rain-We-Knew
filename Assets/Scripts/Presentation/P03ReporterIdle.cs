using UnityEngine;
/// <summary>Scene01 reporters breathe independently with shoes pinned; leaves actors and interaction unchanged.</summary>
[DisallowMultipleComponent]
public sealed class P03ReporterIdle:MonoBehaviour
{
 public SpriteRenderer source;
 Mesh mesh;MeshRenderer output;Vector3[] rest,animated;bool hiddenBefore;float clock;
 const int Columns=50,Rows=20;
 void Start()
 {
  if(!source||!source.sprite)return;
  var sp=source.sprite;var b=sp.bounds;rest=new Vector3[(Columns+1)*(Rows+1)];animated=new Vector3[rest.Length];var uv=new Vector2[rest.Length];var triangles=new int[Columns*Rows*6];
  for(int y=0;y<=Rows;y++)for(int x=0;x<=Columns;x++){int i=y*(Columns+1)+x;rest[i]=new Vector3(b.min.x+b.size.x*x/Columns,b.min.y+b.size.y*y/Rows,0);uv[i]=new Vector2((sp.rect.x+sp.rect.width*x/Columns)/sp.texture.width,(sp.rect.y+sp.rect.height*y/Rows)/sp.texture.height);}
  int k=0;for(int y=0;y<Rows;y++)for(int x=0;x<Columns;x++){int i=y*(Columns+1)+x;triangles[k++]=i;triangles[k++]=i+Columns+1;triangles[k++]=i+1;triangles[k++]=i+1;triangles[k++]=i+Columns+1;triangles[k++]=i+Columns+2;}
  mesh=new Mesh{name="Reporters pinned-foot idle",hideFlags=HideFlags.DontSave};mesh.MarkDynamic();mesh.vertices=rest;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateBounds();
  var g=new GameObject("ReportersIdle_Runtime");g.hideFlags=HideFlags.DontSave;g.transform.SetParent(transform,false);g.AddComponent<MeshFilter>().sharedMesh=mesh;output=g.AddComponent<MeshRenderer>();output.sharedMaterial=source.sharedMaterial;output.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;output.receiveShadows=false;
  var p=new MaterialPropertyBlock();p.SetTexture("_MainTex",sp.texture);p.SetColor("_Color",source.color);output.SetPropertyBlock(p);
  hiddenBefore=source.forceRenderingOff;source.forceRenderingOff=true;
 }
 void LateUpdate()
 {
  if(!mesh||!source)return;
  clock+=Time.deltaTime;var b=source.sprite.bounds;
  for(int y=0;y<=Rows;y++)for(int x=0;x<=Columns;x++)
  {
   int i=y*(Columns+1)+x;float h=(float)y/Rows,person=Mathf.Floor((float)x/Columns*5);float breathing=Mathf.Sin(clock*(1.8f+person*.12f)+person*1.31f);
   float upper=Mathf.SmoothStep(0,1,Mathf.Clamp01((h-.38f)/.62f));animated[i]=rest[i]+new Vector3(b.size.x*.00055f*breathing*upper,b.size.y*.0016f*breathing*upper,0);
  }
  mesh.vertices=animated;mesh.RecalculateBounds();output.enabled=source.enabled&&source.gameObject.activeInHierarchy&&source.color.a>.01f;output.sortingOrder=source.sortingOrder;output.sortingLayerID=source.sortingLayerID;
 }
 void OnDisable(){if(output)output.enabled=false;if(source)source.forceRenderingOff=hiddenBefore;}
 void OnEnable(){if(mesh&&source)source.forceRenderingOff=true;}
 void OnDestroy(){if(source)source.forceRenderingOff=hiddenBefore;if(mesh)Destroy(mesh);}
}