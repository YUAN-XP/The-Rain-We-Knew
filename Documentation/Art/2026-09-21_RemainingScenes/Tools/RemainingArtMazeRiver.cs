using System;
using System.Linq;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static partial class RemainingArtBuild
{
 public static void MazePolish(){
  if(Application.isPlaying||SceneManager.GetActiveScene().name!="Scene03_Childhood")throw new Exception("Maze edit only");entries=null;
  var maze=UnityEngine.Object.FindAnyObjectByType<Scene03MazeController>(FindObjectsInactive.Include);Vector2 origin=maze.mazeSpace.position;
  for(int layer=0;layer<2;layer++){
   string suffix=layer==0?"n":"d";var art=Child(maze.visuals[layer].transform,"NewArt_Maze");int i=0;
   for(int y=0;y<8;y++)for(int x=0;x<14;x++)V(art,"PerimeterGround_"+x+"_"+y,"maze_ground_"+suffix,origin+new Vector2(-20+x*8,-8+y*8),8.03f,8.03f,-10).color=layer==0?new Color(.46f,.48f,.46f,1):new Color(.29f,.29f,.4f,1);
   foreach(Rect rect in maze.layouts[layer].WallRects(maze.cellSize,maze.wallThickness)){
    bool horizontal=rect.width>=rect.height;float length=Mathf.Max(rect.width,rect.height);Vector2 center=origin+rect.center;
    var old=art.Find("Masonry_"+i);if(old)old.GetComponent<SpriteRenderer>().enabled=false;
    var view=V(art,"WallFace_"+i,horizontal?"maze_face_"+suffix:"maze_wall_"+suffix,center-new Vector2(0,horizontal?.55f:.22f),length+.05f,horizontal?.72f:.44f,1);
    if(!horizontal){view.transform.position=center+new Vector2(.22f,0);view.transform.rotation=Quaternion.Euler(0,0,90);if(layer==1)view.color=new Color(1.35f,1.35f,1.5f,1);}
    if(horizontal&&length>2.8f&&i%13==0)V(art,"WallPlanter_"+i,"maze_planter_"+suffix,center-new Vector2(.3f,.25f),1.7f,.72f,2);
    if(horizontal&&length>2.8f&&i%23==0)V(art,"WallMeter_"+i,"maze_meter_"+suffix,center-new Vector2(-.55f,.45f),.4f,.7f,3);
    i++;
   }
   foreach(var cell in new[]{new Vector2(1.5f,1.5f),new Vector2(4.5f,6.5f),new Vector2(10.5f,3.5f),new Vector2(16.5f,10.5f),new Vector2(12.5f,13.5f)}){
    var point=origin+cell*maze.cellSize;V(art,"CourtyardChalk_"+cell.x+"_"+cell.y,"maze_chalk_"+suffix,point-new Vector2(0,1.1f),1.35f,2.2f,-7).color=new Color(1,1,1,.65f);
   }
   foreach(var t in art.Cast<Transform>().Where(t=>t.name.StartsWith("Paving_"))){var r=t.GetComponent<SpriteRenderer>();var v=t.name.Split('_');int x=int.Parse(v[1]),y=int.Parse(v[2]);r.flipX=(x+y)%2==0;r.flipY=false;}
  }
  Finish();
 }
 public static void Scene03MazeRiver(){
  if(SceneManager.GetActiveScene().name!="Scene03_Childhood"||Application.isPlaying)throw new Exception("Scene03 edit only");
  var maze=UnityEngine.Object.FindAnyObjectByType<Scene03MazeController>(FindObjectsInactive.Include);
  var paving=F("MazeRoot/MazeSpace/CourtyardPaving");Retire(Child(paving,"NewArt_Retired"),paving.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>!P(r.transform).Contains("NewArt")));
  for(int layer=0;layer<2;layer++){
   var root=maze.visuals[layer].transform;string suffix=layer==0?"n":"d";
   var old=root.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>!P(r.transform).Contains("NewArt")).ToArray();Retire(Child(root,"NewArt_Retired"),old);
   var art=Child(root,"NewArt_Maze");Vector2 origin=maze.mazeSpace.position;
   for(int y=0;y<15;y++)for(int x=0;x<20;x++){
    var tile=V(art,"Paving_"+x+"_"+y,"maze_ground_"+suffix,origin+new Vector2((x+.5f)*maze.cellSize,y*maze.cellSize),maze.cellSize+.02f,maze.cellSize+.02f,-8);
    float value=.80f+((x*7+y*3)%5)*.025f;tile.color=layer==0?new Color(value,value,.94f*value,1):new Color(.80f*value,.81f*value,value,1);
   }
   int i=0;
   foreach(Rect rect in maze.layouts[layer].WallRects(maze.cellSize,maze.wallThickness)){
    bool horizontal=rect.width>=rect.height;float length=Mathf.Max(rect.width,rect.height);Vector2 center=origin+rect.center;
    var wall=V(art,"Masonry_"+(i++),"maze_wall_"+suffix,center-new Vector2(0,.15f),length+.06f,.30f,1);
    if(!horizontal){wall.transform.position=center+new Vector2(.15f,0);wall.transform.rotation=Quaternion.Euler(0,0,90);}
   }
   // Background buildings stay beyond the walkable rectangle. The wall faces
   // coincide with the existing collision edges; scenery never invents paths.
   for(int n=0;n<6;n++)V(art,"BoundaryShed_"+n,"maze_shed_"+suffix,origin+new Vector2(5+n*10.5f,48.35f),9,7,-4);
   for(int n=0;n<4;n++)V(art,"BoundaryCourtyard_"+n,"maze_corner_"+suffix,origin+new Vector2(-4.7f,3+n*13),7,7,-4);
  }
  var landmarks=F("MazeRoot/MazeSpace/SharedLandmarks");
  foreach(Transform t in landmarks){if(t.name.StartsWith("NewArt"))continue;string key=t.name.Contains("树")?"river_tree":t.name.Contains("车")?"maze_corner_n":"signpost";var old=t.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>!P(r.transform).Contains("NewArt")).ToArray();var view=V(t,"NewArt_Landmark",key,(Vector2)t.position-new Vector2(0,.45f),key=="river_tree"?2.1f:1.3f,0,2);view.color=new Color(.8f,.82f,.82f,1);Retire(view.transform,old);}
  var river=F("Riverbank");var oldRiver=river.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>!P(r.transform).Contains("NewArt")&&!P(r.transform).Contains("Boy_WaitingWithGuitar")&&!P(r.transform).Contains("Zhao_RiverVisit")).ToArray();Retire(Child(river,"NewArt_RetiredScenery"),oldRiver);
  var backdrop=V(river,"NewArt_RiverPanorama","river_day",new Vector2(303,69.9f),46,15.34f,-30);
  var rain=V(river,"NewArt_RiverRain","river_rain",new Vector2(303,69.9f),46,15.34f,-29);rain.gameObject.SetActive(false);
  for(int i=0;i<6;i++)V(river,"NewArt_Bank_"+i,"floor_n",new Vector2(279+(i+.5f)*8,66),8.02f,4.55f,-5).color=new Color(.69f,.74f,.72f,1);
  Replace("Riverbank/河堤老树","river_tree",new Vector2(308,70.5f),7.7f,7.4f,-3);
  Replace("Riverbank/小凉亭","pavilion",new Vector2(299,70.5f),7,4.65f,-4);
  Atmosphere(river,new Vector2(280,65),new Vector2(326,87),70.5f,false,new Vector2(297,73),new Vector2(310,73));
  Finish();
 }
}

