using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Shallow painted extrusion of the SAME rectangles used by the maze colliders.
// A union grid removes internal edges at corners and T junctions.
[ExecuteAlways]
public sealed class MazeWallPresentation : MonoBehaviour
{
 public MazeLayoutData layout;
 public float cellSize=3.2f,wallThickness=.2f;
 [Range(.15f,.65f)] public float projectedHeight=.42f;
 public Material paintMaterial,shadowMaterial;
 public Renderer[] retiredWalls;
 Transform generated;
 readonly List<Mesh> meshes=new();
 public int WallCount {get;private set;}
 public int FilledCellCount {get;private set;}
 void OnEnable(){Rebuild();}
 void LateUpdate(){if(retiredWalls!=null)foreach(var r in retiredWalls)if(r)r.forceRenderingOff=true;}
 void OnDisable(){Clear();}
 void OnDestroy(){Clear();}
 public void Rebuild()
 {
  Clear();if(!layout||!layout.IsValid()||!paintMaterial||!shadowMaterial)return;
  var walls=layout.WallRects(cellSize,wallThickness).ToArray();WallCount=walls.Length;
  var xs=walls.SelectMany(r=>new[]{r.xMin,r.xMax}).Select(v=>Mathf.Round(v*10000)/10000).Distinct().OrderBy(v=>v).ToArray();
  var ys=walls.SelectMany(r=>new[]{r.yMin,r.yMax}).Select(v=>Mathf.Round(v*10000)/10000).Distinct().OrderBy(v=>v).ToArray();
  int nx=xs.Length-1,ny=ys.Length-1;var occupied=new bool[nx,ny];FilledCellCount=0;
  for(int x=0;x<nx;x++)for(int y=0;y<ny;y++){
   var p=new Vector2((xs[x]+xs[x+1])*.5f,(ys[y]+ys[y+1])*.5f);
   occupied[x,y]=walls.Any(r=>r.Contains(p));if(occupied[x,y])FilledCellCount++;
  }
  generated=new GameObject("NewArt_ContinuousMazeWalls").transform;
  generated.gameObject.hideFlags=HideFlags.HideAndDontSave;generated.SetParent(transform,false);
  var cap=new Surface();var face=new Surface();var edges=new Surface();var shadow=new Surface();
  var lift=new Vector2(-.075f,projectedHeight);var shadeOffset=new Vector2(.11f,-.11f);
  for(int x=0;x<nx;x++)for(int y=0;y<ny;y++)if(occupied[x,y]){
   var a=new Vector2(xs[x],ys[y]);var b=new Vector2(xs[x+1],ys[y]);
   var c=new Vector2(xs[x+1],ys[y+1]);var d=new Vector2(xs[x],ys[y+1]);
   cap.Quad(a+lift,b+lift,c+lift,d+lift,new Color(1.08f,1.04f,.92f));
   shadow.Quad(a+shadeOffset,b+shadeOffset,c+shadeOffset,d+shadeOffset,Color.white);
   if(y==0||!occupied[x,y-1]){
    face.Quad(a,b,b+lift,a+lift,new Color(.66f,.63f,.54f));
    edges.Quad(a+lift,b+lift,b+lift+Vector2.up*.025f,a+lift+Vector2.up*.025f,new Color(.87f,.82f,.67f));
   }
   if(x==nx-1||!occupied[x+1,y]){
    face.Quad(b,c,c+lift,b+lift,new Color(.76f,.73f,.63f));
    edges.Quad(b+lift,c+lift,c+lift-Vector2.right*.018f,b+lift-Vector2.right*.018f,new Color(.92f,.87f,.72f));
   }
  }
  Render("FootShadow",shadow,shadowMaterial,-4);
  Render("WallFaces",face,paintMaterial,0);
  Render("ContinuousCap",cap,paintMaterial,1);
  Render("CapRim",edges,paintMaterial,2);
  LateUpdate();
 }
 void Render(string name,Surface s,Material material,int order){
  var node=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));node.hideFlags=HideFlags.HideAndDontSave;node.transform.SetParent(generated,false);
  var mesh=new Mesh{name="Maze_"+name,hideFlags=HideFlags.HideAndDontSave};
  mesh.SetVertices(s.points);mesh.SetColors(s.colors);mesh.SetTriangles(s.triangles,0);mesh.RecalculateBounds();meshes.Add(mesh);
  node.GetComponent<MeshFilter>().sharedMesh=mesh;var r=node.GetComponent<MeshRenderer>();r.sharedMaterial=material;r.sortingOrder=order;
  r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;
 }
 void Clear(){
  if(generated){generated.gameObject.SetActive(false);Release(generated.gameObject);generated=null;}
  foreach(var mesh in meshes)if(mesh)Release(mesh);meshes.Clear();
 }
 static void Release(Object o){if(Application.isPlaying)Destroy(o);else DestroyImmediate(o);}
 sealed class Surface{
  public readonly List<Vector3> points=new();public readonly List<Color> colors=new();public readonly List<int> triangles=new();
  public void Quad(Vector2 a,Vector2 b,Vector2 c,Vector2 d,Color color){
   int i=points.Count;points.Add(a);points.Add(b);points.Add(c);points.Add(d);for(int j=0;j<4;j++)colors.Add(color);
   triangles.AddRange(new[]{i,i+1,i+2,i,i+2,i+3});
  }
 }
}
