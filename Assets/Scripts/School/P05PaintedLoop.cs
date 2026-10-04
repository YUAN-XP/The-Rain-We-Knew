using UnityEngine;
// Scene-owned continuous deformation. A single reference painting keeps furniture,
// shoes and silhouette anchors stable; alternate complete pictures are never flipped.
[DisallowMultipleComponent, DefaultExecutionOrder(1100)]
public sealed class P05PaintedLoop:MonoBehaviour {
 public string identity,activity;public SpriteRenderer target;public Sprite[] frames;
 public float height=1.72f,period=4,phaseOffset,breath=.0025f;
 const int Columns=48,Rows=64;
 SchoolChaseController chase;Mesh mesh;MeshRenderer output;Vector3[] rest,animated;Sprite reference;bool wasHidden;
 public bool UsesStableSource=>reference&&target&&target.sprite==reference;
 public bool VisualActive=>output&&output.enabled;
 public float FurnitureDrift{get;private set;}public float SoleDrift{get;private set;}
 public float MotionRange{get;private set;}
 bool Seated=>activity=="writing"||activity=="typing";
 void Start(){
  if(!target)target=GetComponent<SpriteRenderer>();if(!target)return;
  chase=FindAnyObjectByType<SchoolChaseController>(FindObjectsInactive.Include);
  reference=frames!=null&&frames.Length>0&&frames[0]?frames[0]:target.sprite;
  if(!reference)return;target.sprite=reference;
  float k=height/reference.bounds.size.y;var p=transform.parent?transform.parent.lossyScale:Vector3.one;
  transform.localScale=new Vector3(k/Mathf.Abs(p.x),k/Mathf.Abs(p.y),1);
  rest=new Vector3[(Columns+1)*(Rows+1)];animated=new Vector3[rest.Length];
  var uv=new Vector2[rest.Length];var triangles=new int[Columns*Rows*6];var b=reference.bounds;
  for(int y=0;y<=Rows;y++)for(int x=0;x<=Columns;x++){int i=y*(Columns+1)+x;
   rest[i]=new Vector3(b.min.x+b.size.x*x/Columns,b.min.y+b.size.y*y/Rows,0);
   uv[i]=new Vector2((reference.rect.x+reference.rect.width*x/Columns)/reference.texture.width,(reference.rect.y+reference.rect.height*y/Rows)/reference.texture.height);}
  int n=0;for(int y=0;y<Rows;y++)for(int x=0;x<Columns;x++){int i=y*(Columns+1)+x;
   triangles[n++]=i;triangles[n++]=i+Columns+1;triangles[n++]=i+1;
   triangles[n++]=i+1;triangles[n++]=i+Columns+1;triangles[n++]=i+Columns+2;}
  mesh=new Mesh{name="P05 pinned life "+identity,hideFlags=HideFlags.DontSave};mesh.MarkDynamic();mesh.vertices=rest;mesh.uv=uv;mesh.triangles=triangles;mesh.RecalculateBounds();
  var g=new GameObject("P05_ContinuousLife_Runtime"){hideFlags=HideFlags.DontSave};g.transform.SetParent(transform,false);
  g.AddComponent<MeshFilter>().sharedMesh=mesh;output=g.AddComponent<MeshRenderer>();output.sharedMaterial=target.sharedMaterial;
  output.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;output.receiveShadows=false;
  wasHidden=target.forceRenderingOff;target.forceRenderingOff=true;SyncOutput();
 }
 static float Patch(Vector2 q,float x,float y,float rx,float ry){float d=Mathf.Pow((q.x-x)/rx,2)+Mathf.Pow((q.y-y)/ry,2);float a=Mathf.Clamp01(1-d);return a*a;}
 bool Pinned(Vector2 q)=>Seated&&(q.y<=.525f||activity=="typing"&&q.x>.59f&&q.y<.815f);
 void LateUpdate(){
  if(!mesh||!target)return;SyncOutput();if(GamePause.IsPaused)return;
  float t=Mathf.Repeat(GameplayClock.Now/Mathf.Max(.2f,period)+phaseOffset,1),a=t*Mathf.PI*2;
  var b=reference.bounds;FurnitureDrift=SoleDrift=MotionRange=0;
  for(int y=0;y<=Rows;y++)for(int x=0;x<=Columns;x++){
   int i=y*(Columns+1)+x;var q=new Vector2((float)x/Columns,(float)y/Rows);Vector2 d=Vector2.zero;
   if(!Pinned(q)){
    if(Seated){
     // Desk, chair, monitor and legs are outside the deforming hand/head regions.
     float head=Patch(q,.515f,.855f,.12f,.12f);
     d.x=.0009f*Mathf.Sin(a+.6f)*head;d.y=.00065f*Mathf.Sin(a)*head;
     float edge=Mathf.SmoothStep(0,1,Mathf.Clamp01((q.y-.525f)/.025f));
     if(activity=="typing"){
      float left=Patch(q,.466f,.584f,.095f,.043f),right=Patch(q,.574f,.584f,.065f,.043f);
      d.y+=.0030f*(Mathf.Sin(a*7)*left+Mathf.Sin(a*7+1.8f)*right)*edge;
      d.x+=.0011f*Mathf.Sin(a*3)*left*edge;
     }else{
      float hand=Patch(q,.448f,.560f,.13f,.072f);
      d.x+=.006f*(.7f*Mathf.Sin(a*4)+.3f*Mathf.Sin(a*8))*hand*edge;
      d.y+=.0018f*Mathf.Sin(a*8+.4f)*hand*edge;
     }
    }else{
     float upper=Mathf.SmoothStep(0,1,Mathf.Clamp01((q.y-.32f)/.55f));
     d.y=Mathf.Sin(a)*breath*(q.y-.32f)*upper;d.x=Mathf.Sin(a+.35f)*.0007f*upper;
     float hand=Patch(q,.68f,.42f,.23f,.18f),head=Patch(q,.62f,.88f,.2f,.11f);
     if(activity=="dribbling"){
      float near=1-Mathf.Pow(2*t-1,2);d.y-=.05f*near*Patch(q,.85f,.48f,.18f,.22f);d.x-=.012f*near*hand;
      d.y-=.003f*near*head;
     }else{d.x+=.003f*Mathf.Sin(a+.9f)*hand;d.y+=.0015f*Mathf.Sin(a*2)*hand;d.x+=.0012f*Mathf.Sin(a+.4f)*head;}
    }
   }
   if(q.y<.12f)d=Vector2.zero;
   var delta=new Vector3(d.x*b.size.x,d.y*b.size.y,0);animated[i]=rest[i]+delta;
   float world=Vector3.Scale(delta,transform.lossyScale).magnitude;MotionRange=Mathf.Max(MotionRange,world);
   if(Pinned(q))FurnitureDrift=Mathf.Max(FurnitureDrift,world);if(q.y<.12f)SoleDrift=Mathf.Max(SoleDrift,world);
  }
  mesh.vertices=animated;mesh.RecalculateBounds();
 }
 void SyncOutput(){if(!output)return;if(!chase)chase=FindAnyObjectByType<SchoolChaseController>(FindObjectsInactive.Include);output.enabled=!(chase&&chase.IsPreviewing)&&target.enabled&&target.gameObject.activeInHierarchy&&target.color.a>.01f;
  output.sortingOrder=target.sortingOrder;output.sortingLayerID=target.sortingLayerID;
  var p=new MaterialPropertyBlock();p.SetTexture("_MainTex",reference.texture);p.SetColor("_Color",target.color);output.SetPropertyBlock(p);}
 void OnDisable(){if(output)output.enabled=false;if(target)target.forceRenderingOff=wasHidden;}
 void OnEnable(){if(mesh&&target)target.forceRenderingOff=true;}
 void OnDestroy(){if(target)target.forceRenderingOff=wasHidden;if(mesh)Destroy(mesh);}
}
