using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static partial class P03ProductionBuild
{
 static Transform Optional(string path)=>All().FirstOrDefault(t=>PathOf(t)==path);
 static void RectField(UnityEngine.Object o,string name,Rect value){var s=new SerializedObject(o);s.FindProperty(name).rectValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
 static void VecField(UnityEngine.Object o,string name,Vector2 value){var s=new SerializedObject(o);s.FindProperty(name).vector2Value=value;s.ApplyModifiedPropertiesWithoutUndo();}
 static void IntField(UnityEngine.Object o,string name,int value){var s=new SerializedObject(o);s.FindProperty(name).intValue=value;s.ApplyModifiedPropertiesWithoutUndo();}
 static void ObjectArray(UnityEngine.Object o,string name,UnityEngine.Object[] values){var s=new SerializedObject(o);var a=s.FindProperty(name);a.arraySize=values.Length;for(int i=0;i<values.Length;i++)a.GetArrayElementAtIndex(i).objectReferenceValue=values[i];s.ApplyModifiedPropertiesWithoutUndo();}
 static TComponent Get<TComponent>(Transform t) where TComponent:Component{var c=t.GetComponent<TComponent>();return c?c:t.gameObject.AddComponent<TComponent>();}
 static Sprite S(string key)=>catalog.Get(key)??throw new Exception("Missing sprite "+key);
 static Transform Node(Transform parent,string name)
 {var t=parent.Find(name);if(t)return t;t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
 static SpriteRenderer Visual(Transform parent,string name,string key,float x,float bottom,float height,int order=-6)
 {var t=Node(parent,name);var r=Get<SpriteRenderer>(t);Paint(r,S(key),x,bottom,height);r.sortingOrder=order;return r;}
 static void RetireBackground(Transform room)
 {
  foreach(var t in room.GetComponentsInChildren<Transform>(true))
  {
   string n=t.name;
   bool old=(t.GetComponent<ContinuousRoomArtwork>()!=null)||(t.GetComponent<SpriteRenderer>()&&(n.StartsWith("Wall_")||n.StartsWith("Floor_")||n.StartsWith("Ground_")||n.StartsWith("Revision_CloseWall_")));
   if(old){if(t.GetComponent<Collider2D>()){var r=t.GetComponent<SpriteRenderer>();if(r)Get<RetiredArtVisibility>(t).SetRenderers(new Renderer[]{r});}else t.gameObject.SetActive(false);}
  }
 }
 static Material StripMaterial()
 {
  const string path=Art+"P03RepeatedPainting.mat";
  var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
  if(!mat){mat=new Material(Shader.Find("ProjectMystery/P03RepeatedPainting"));AssetDatabase.CreateAsset(mat,path);}
  return mat;
 }
 static Transform Strip(Transform parent,string name,float x,float y,float width,float height,string key,int order,SpriteRenderer tint=null)
 {
  var t=Node(parent,name);t.position=new Vector3(x,y,0);t.localScale=Vector3.one;
  var c=Get<P03RepeatedRoomStrip>(t);
  c.source=S(key);c.material=StripMaterial();c.width=width;c.height=height;c.tileWidth=height*c.source.rect.width/c.source.rect.height;c.order=order;c.tintSource=tint;c.Rebuild();return t;
 }
 static Transform Shell(Transform room,string name,float x,float floor,float width,SpriteRenderer tint=null)
 {
  var t=Node(room,name);
  Strip(t,"ContinuousWall",x,floor+.52f,width,3,"MOD_HOME_WALL_CLOSE_OTHER_V03",-35,tint);
  Strip(t,"ShallowQuietGround",x,floor-.48f,width,1,"TEX_FLOOR_QUIET_V04:1",-19);
  return t;
 }
 static void Box(Transform t,float x,float y,float width,float height,bool trigger=false)
 {
  t.position=new Vector3(x,y,0);t.localScale=Vector3.one;
  var b=Get<BoxCollider2D>(t);b.offset=Vector2.zero;b.size=new Vector2(width,height);b.isTrigger=trigger;b.enabled=true;
  var sr=t.GetComponent<SpriteRenderer>();if(sr){sr.forceRenderingOff=true;var retire=Get<RetiredArtVisibility>(t);retire.SetRenderers(retire.Renderers.Concat(new Renderer[]{sr}).Distinct().ToArray());}
 }
 static void Bounds(Transform parent,string name,Rect region,Vector2 edges,int priority=0)
 {
  var c=Node(parent,name).GetComponent<RoomCameraBounds>();if(!c)c=parent.Find(name).gameObject.AddComponent<RoomCameraBounds>();
  RectField(c,"actorRegion",region);VecField(c,"horizontalEdges",edges);Set(c,"leftWall",null);Set(c,"rightWall",null);IntField(c,"priority",priority);
 }
 static void Door(Transform t,float bottom)
 {
  var r=t.Find("NewArt_Visual")?.GetComponent<SpriteRenderer>();if(!r)return;
  Paint(r,S("SPR_HOME_DOOR_TVCAB_OTHER:0"),t.position.x,bottom,2.22f);
 }
 static void RainWindow(SpriteRenderer sr)
 {
  var rain=Get<WindowRainPresentation>(sr.transform);
  rain.window=sr;rain.material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/正式/Effects/Scene01/DrawnStreetAtmosphere.mat");
 }
 public static void Courtyard()
 {
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Requires clean edit mode");
  EditorSceneManager.OpenScene("Assets/Scenes/Scene02_Courtyard.unity");
  catalog=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>("Assets/Resources/DemoArtCatalog.asset");
  var f1=T("Environment/Floor01");var f2=T("Environment/Floor02");
  if(Optional("Environment/Floor01/P03_LayoutRevision3"))throw new Exception("P03 courtyard already patched. Use targeted refinements, never recompress.");
  // Move whole object anchors, never individual rig bones or stretched artwork.
  foreach(Transform t in f1)
   if(t.name!="NewArt_RoomShell"&&!t.name.Contains("Concept")&&t.name!="Camera_ViewBounds"){var p=t.position;p.x*=.6f;t.position=p;}
  var shell=T("Environment/Floor01/NewArt_RoomShell");
  foreach(Transform t in shell)
   if(t.name=="LampTable"||t.name=="ShoeRack"||t.name=="UpperWindow"){var p=t.position;p.x*=.6f;t.position=p;}
  foreach(var t in All().Where(t=>!t.GetComponent<RectTransform>()&&t.childCount==0&&t.position.x>=-30&&t.position.x<=30&&t.position.y<5.5f))
  {
   string path=PathOf(t);
   if(path=="PlayerSpawnPoint"||(path.StartsWith("Gameplay/")&&(t.name.Contains("Destination")||t.name.Contains("Spawn")||t.name.Contains("GuideAfter")))||path.StartsWith("EndingHome/"))
   {var p=t.position;p.x*=.6f;t.position=p;}
  }
  var guide=T("Characters/QiuTian_Placeholder");var gp=guide.position;gp.x*=.6f;guide.position=gp;
  Box(T("Environment/Floor01/Ground"),0,0,32,1);
  Box(T("Environment/Floor01/LeftBoundary"),-15.4f,2.5f,.8f,6);
  Box(T("Environment/Floor01/RightBoundary"),12.7f,2.5f,.8f,6);
  Box(T("Environment/Floor01/DarkFog_Blocker"),10.7f,2.5f,.45f,6);
  var fog=T("Environment/Floor01/DarkFog_Placeholder");Box(fog,10.4f,2.1f,1.25f,3.2f,true);
  foreach(var r in fog.GetComponentsInChildren<SpriteRenderer>(true))if(r.transform!=fog)r.gameObject.SetActive(false);
  var paintedFog=Visual(fog,"P03_PaintedFog","FX_FOG_SHAPES:0",12.2f,.5f,3.3f,40);
  paintedFog.color=new Color(.045f,.05f,.065f,.99f);
  // A second painted mass covers the solid blocker; silhouettes do not end in empty road.
  var fog2=Visual(fog,"P03_FogCore","FX_FOG_SHAPES:0",13f,.5f,3.3f,41);fog2.color=new Color(.025f,.03f,.04f,.94f);
  RetireBackground(f1);Shell(f1,"P03_ContinuousHall",-1.2f,.5f,30);
  Bounds(f1,"Camera_ViewBounds",new Rect(-18,-4,36,9.5f),new Vector2(-15,12.25f));
  Door(T("Environment/Floor01/BackGate_Placeholder"),1.02f);
  Door(T("Environment/Floor01/LockedLivingRoom_Placeholder"),1.02f);
  var table=T("Environment/Floor01/NewArt_RoomShell/LampTable").GetComponent<SpriteRenderer>();
  Paint(table,S("SPR_HOME_EMPTY_SIDEBOARD_V01:1"),-6,.86f,.88f);
  var lampRoot=T("Environment/Floor01/BrokenLamp_Placeholder");lampRoot.position=new Vector3(-6,1.91f,0);
  var lamp=lampRoot.Find("NewArt_Visual").GetComponent<SpriteRenderer>();Paint(lamp,S("SPR_HOME_DRESSING_OTHER:2"),-6,1.71f,.42f);
  var shoes=T("Environment/Floor01/NewArt_RoomShell/ShoeRack").GetComponent<SpriteRenderer>();Paint(shoes,S("SPR_HOME_DRESSING_OTHER:0"),-11.28f,.86f,1.05f);
  var photo=T("Environment/Floor01/DirtyPhoto_Placeholder/NewArt_Visual").GetComponent<SpriteRenderer>();Paint(photo,S("FILE_FAMILY_PHOTO_STAINED"),-9,1.65f,.45f);
  var photoMode=Get<P03FamilyPhotoPresentation>(photo.transform);photoMode.target=photo;
  var stairs=T("Environment/Floor01/Stairs_Placeholder");
  foreach(var r in stairs.GetComponentsInChildren<SpriteRenderer>(true))r.gameObject.SetActive(false);
  stairs.gameObject.SetActive(true);
  var steps=Visual(stairs,"P03_CompactStairs","P03_CompactStairs",4.8f,.86f,P02VisualAssetRules.CourtyardStairHeight,-8);
  var sh=Get<PaintedContactShadow>(steps.transform);sh.target=steps;
  Box(T("Environment/Floor01/StairsUp_InteractZone"),4.10f,1.6f,1.6f,2.2f,true);
  foreach(var t in new[]{T("Environment/Floor02/MasterBedroom_Placeholder"),T("Environment/Floor02/HiddenBedroom_RevealDoor")})Door(t,7.05f);
  RetireBackground(f2);var normal2=Shell(f2,"P03_ContinuousHall",-6.5f,6.53f,33);
  // Keep window glass visible and rain behind all indoor props.
  foreach(var sr in new[]{T("Environment/Floor01/NewArt_RoomShell/UpperWindow").GetComponent<SpriteRenderer>(),T("Environment/Floor02/NewArt_RoomShell/UpperWindow").GetComponent<SpriteRenderer>(),T("Environment/MasterBedroomInterior/NewArt_RoomShell/BedroomWindow").GetComponent<SpriteRenderer>()})RainWindow(sr);
  Master();HiddenBedroom();Corridor(normal2);
  var framing=Camera.main.GetComponent<RoomPresentationFraming>();var rooms=framing.rooms;
  for(int i=0;i<rooms.Length;i++)rooms[i].correctRoomLimits=true;
  rooms[0].area=new Rect(-20,-4,40,9.8f);rooms[1].area=new Rect(-30,5.8f,55,15);
  // The old remote corridor is no longer a room; the same second-floor region frames it.
  framing.rooms=rooms.Where(r=>r.floorY<22).ToArray();
  Node(f1,"P03_LayoutRevision3");
  EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
  AssetDatabase.SaveAssets();P03SceneEvidence.References();Debug.Log("P03 courtyard: compact anchors, grounded props, continuous corridor, unchanged puzzle sequence.");
 }
 static void Master()
 {
  var room=T("Environment/MasterBedroomInterior");RetireBackground(room);Shell(room,"P03_ContinuousBedroom",69.8f,.5f,16);
  Door(T("Environment/MasterBedroomInterior/ExitDoor_Point"),1.02f);
  Visual(room,"P03_ShadowLampSupport","SPR_HOME_EMPTY_SIDEBOARD_V01:1",65.45f,.86f,.60f,-7);
  var root=T("Environment/MasterBedroomInterior/Lamp_Placeholder");root.position=new Vector3(65.45f,1.64f,0);
  Paint(root.Find("NewArt_Visual").GetComponent<SpriteRenderer>(),S("SPR_HOME_DRESSING_OTHER:2"),65.45f,1.43f,.42f);
  var sequence=T("Environment/MasterBedroomInterior/MasterBedroom_ShadowEvent").GetComponent<ParentShadowSequence>();
  var a=T("Environment/MasterBedroomInterior/MasterBedroom_ShadowEvent/SilentShadow_Tall/NewArt_Visual").GetComponent<SpriteRenderer>();
  var b=T("Environment/MasterBedroomInterior/MasterBedroom_ShadowEvent/SilentShadow_Short/NewArt_Visual").GetComponent<SpriteRenderer>();
  foreach(var sr in new[]{a,b})
  {
   foreach(var c in sr.GetComponents<MonoBehaviour>())if(c is SpriteVisibilityAlphaLink||c is RendererVisibilityLink)UnityEngine.Object.DestroyImmediate(c);
   sr.transform.parent.GetComponent<SpriteRenderer>().forceRenderingOff=true;
  }
  Paint(a,S("P03_ParentShadows:father"),64.85f,1.02f,2.05f);Paint(b,S("P03_ParentShadows:mother"),66.05f,1.02f,1.86f);
  a.sortingOrder=b.sortingOrder=-4;a.enabled=b.enabled=false;
  ObjectArray(sequence,"shadows",new UnityEngine.Object[]{a,b});
 }
 static void HiddenBedroom()
 {
  var room=T("Environment/HiddenBedroomArea");RetireBackground(room);Shell(room,"P03_ContinuousBedroom",132,.5f,16);
  Door(T("Environment/HiddenBedroomArea/HiddenBedroom_Exit"),1.02f);
  var desk=T("Environment/HiddenBedroomArea/HiddenBedroom_Desk");var p=desk.position;p.x=136.85f;desk.position=p;
  var visual=desk.Find("NewArt_Visual").GetComponent<SpriteRenderer>();Paint(visual,S("P03_PasswordDesk"),136.85f,.86f,1.65f);
  // Resize only the interaction collider, not the actor or the authored desk.
  var collider=desk.GetComponent<BoxCollider2D>();collider.size=new Vector2(2.1f/desk.lossyScale.x,1.9f/desk.lossyScale.y);
  T("Environment/HiddenBedroomArea/Computer_Background").gameObject.SetActive(false);
  var win=T("Environment/HiddenBedroomArea/Window_Background");p=win.position;p.x=136.85f;win.position=p;
  var wr=win.Find("NewArt_Visual").GetComponent<SpriteRenderer>();Paint(wr,S("SPR_HOME_WINDOW_BOOKCASE_OTHER:0"),136.85f,1.6f,1.65f);RainWindow(wr);
 }
 static void Corridor(Transform normal2)
 {
  var controller=T("Gameplay/Scene02_Midgame").GetComponent<DistortedCorridorController>();
  var area=T("Gameplay/Scene02_Midgame/DistortedCorridorArea");
  string[] keep={"MasterBedroom_Reference","ThreeDoor_Left","ThreeDoor_Middle","ThreeDoor_Right","ThreeDoorPuzzleArea","ThreeDoor_ResetPoint","ThreeDoor_LeftBoundary","ThreeDoor_NormalWall","JoinedCorridor_RearBoundary","CorridorGuitar"};
  foreach(Transform t in area)if(!keep.Contains(t.name))t.gameObject.SetActive(false);
  var source=T("Gameplay/Scene02_Midgame/SourceCorridor_ContinuityGround");source.gameObject.SetActive(false);Set(controller,"sourceExtension",null);
  var rear=T("Gameplay/Scene02_Midgame/PostShadowRearBoundary");Box(rear,-14.4f,8.5f,.4f,6);
  var joinedRear=T("Gameplay/Scene02_Midgame/DistortedCorridorArea/JoinedCorridor_RearBoundary");Box(joinedRear,-14.8f,8.5f,.3f,6);
  var ground=Node(area,"P03_CorridorGround");Box(ground,3.5f,6.03f,37.5f,1);
  Box(Node(area,"P03_RightBoundary"),21.9f,8.5f,.4f,6);
  var walls=area.Find("ThreeDoor_NormalWall").GetComponent<SpriteRenderer>();walls.forceRenderingOff=true;
  var hall=Shell(area,"P03_ContinuousCorridor",3.5f,6.53f,37.5f);
  Shell(area,"P03_ThreeDoorWall",15f,6.53f,12,walls);
  // Puzzle wall overlays the same native painting at the same physical scale.
  area.Find("P03_ThreeDoorWall/ContinuousWall").GetComponent<P03RepeatedRoomStrip>().order=-34;
  var echo=area.Find("MasterBedroom_Reference");echo.position=new Vector3(-3.62f,8.16f,0);echo.GetComponent<SpriteRenderer>().forceRenderingOff=true;
  Door(echo,7.05f);
  string[] names={"ThreeDoor_Left","ThreeDoor_Middle","ThreeDoor_Right"};
  for(int i=0;i<3;i++)
  {
   var door=area.Find(names[i]);Box(door,12.5f+i*2.5f,8.16f,1.05f,2.6f,true);Door(door,7.05f);
   foreach(var child in door.GetComponentsInChildren<Transform>(true))if(child.name.Contains("Concept")||child.name=="Door_Visual")child.gameObject.SetActive(false);
  }
  var puzzle=area.Find("ThreeDoorPuzzleArea").GetComponent<ThreeDoorPuzzleController>();Box(puzzle.transform,15.2f,8.25f,12.4f,4,true);
  area.Find("ThreeDoor_ResetPoint").position=new Vector3(10.5f,7.43f,0);
  Box(area.Find("ThreeDoor_LeftBoundary"),9.2f,8.5f,.3f,6);
  Set(controller,"bedroomExit",T("Environment/HiddenBedroomArea/HiddenBedroom_Exit").GetComponent<RoomTransitionInteractable>());
  var lampList=new SpriteRenderer[2];
  for(int i=0;i<2;i++)lampList[i]=Visual(area,"P03_MilestoneLamp_"+i,"SPR_HOME_DRESSING_OTHER:2",i*7,9.45f,.34f,-5);
  ObjectArray(controller,"lamps",lampList.Cast<UnityEngine.Object>().ToArray());
  var anomalies=new System.Collections.Generic.List<SpriteRenderer>();
  string[] keys={"FX_HOME_CORRIDOR_WARP:0","FX_HOME_CORRIDOR_WARP:1","FX_HOME_CORRIDOR_WARP:2"};
  // Existing authored ink is reused; stage changes remain local to presentation.
  var available=catalog.entries.Where(e=>e.key.StartsWith("FX_")&&e.key.Contains("CORRIDOR")).Select(e=>e.key).ToArray();
  if(available.Length==0)available=new[]{"FX_FOG_SHAPES:0"};
  var fragments=new System.Collections.Generic.List<Transform>();
  for(int i=0;i<6;i++)
  {
   var sr=Visual(area,"P03_CollapseInk_"+i,available[i%available.Length],-.5f+i*3.4f,7.05f,2.9f,-25);
   sr.color=new Color(.24f,.28f,.31f,.7f);anomalies.Add(sr);fragments.Add(sr.transform);
  }
  ObjectArray(controller,"anomalyLayers",anomalies.Cast<UnityEngine.Object>().ToArray());
  var collapse=Get<P03CorridorCollapse>(area);collapse.corridor=controller;collapse.fragments=fragments.ToArray();
  ObjectArray(puzzle,"abnormalLayers",anomalies.Cast<UnityEngine.Object>().ToArray());
  ObjectArray(controller,"disabledDuringDistortion",new UnityEngine.Object[]{
   T("Environment/Floor02/StairExit_Point").gameObject,normal2.gameObject,
   T("Environment/Floor02/MasterBedroom_Placeholder").gameObject,T("Environment/Floor02/HiddenBedroom_RevealDoor").gameObject,
   T("Environment/Floor02/NewArt_RoomShell").gameObject,T("Environment/Floor02/Demo2026_ConceptR2_WholeSecondFloor").gameObject,
   T("Environment/Floor02/Ground_Floor02").gameObject});
  Bounds(area,"P03_CorridorCamera",new Rect(-16,5.8f,40,10),new Vector2(-15,22),1);
  Bounds(area,"P03_ThreeDoorCamera",new Rect(9,5.8f,15,10),new Vector2(10.4f,19.6f),2);
  area.gameObject.SetActive(false);
 }
 public static void RefineCurrent()
 {
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Requires clean edit mode");
  EditorSceneManager.OpenScene("Assets/Scenes/Scene02_Courtyard.unity");
  catalog=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>("Assets/Resources/DemoArtCatalog.asset");
  Paint(T("Environment/Floor01/Stairs_Placeholder/P03_CompactStairs").GetComponent<SpriteRenderer>(),S("P03_CompactStairs"),4.8f,.86f,P02VisualAssetRules.CourtyardStairHeight);
  var area=T("Gameplay/Scene02_Midgame/DistortedCorridorArea");
  var wall=T("Gameplay/Scene02_Midgame/DistortedCorridorArea/ThreeDoor_NormalWall").GetComponent<SpriteRenderer>();
  area.Find("P03_ThreeDoorWall").gameObject.SetActive(false);
  area.Find("P03_ContinuousCorridor/ContinuousWall").GetComponent<P03RepeatedRoomStrip>().tintSource=wall;
  for(int i=0;i<6;i++)
  {
   var r=area.Find("P03_CollapseInk_"+i).GetComponent<SpriteRenderer>();Paint(r,S("P03_CollapseWall"),-.5f+i*3.4f,7.05f,2.9f);
   r.sortingOrder=-25;r.color=new Color(.65f,.65f,.65f,.9f);
  }
  EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
 }

 public static void FinalLayoutFix()
 {
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Requires clean edit mode");
  EditorSceneManager.OpenScene("Assets/Scenes/Scene02_Courtyard.unity");
  var area=T("Gameplay/Scene02_Midgame/DistortedCorridorArea");
  Box(area.Find("ThreeDoor_LeftBoundary"),8.2f,8.5f,.3f,6);
  catalog=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>("Assets/Resources/DemoArtCatalog.asset");
  foreach(var rain in All().Select(t=>t.GetComponent<WindowRainPresentation>()).Where(r=>r))
   rain.panes=new[]{new Rect(220f/1043,151f/658,75f/1043,412f/658),new Rect(331f/1043,151f/658,243f/1043,412f/658),new Rect(603f/1043,151f/658,208f/1043,412f/658)};
  Visual(T("Environment/Floor01"),"P03_PhotoSupport","SPR_HOME_EMPTY_SIDEBOARD_V01:1",-9,.86f,.80f,-7);
  var cabinet=S("SPR_HOME_EMPTY_SIDEBOARD_V01:1");string shelfKey="P03_WallShelf";
  if(!catalog.Get(shelfKey)){var r=cabinet.rect;var part=Sprite.Create(cabinet.texture,new Rect(r.center.x-110,r.yMax-28,220,26),new Vector2(.5f,0),256,0,SpriteMeshType.FullRect);part.name=shelfKey;AssetDatabase.AddObjectToAsset(part,catalog);catalog.entries=catalog.entries.Concat(new[]{new DemoArtCatalog.Entry{key=shelfKey,sprite=part}}).ToArray();EditorUtility.SetDirty(catalog);typeof(DemoArtCatalog).GetField("lookup",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).SetValue(catalog,null);}
  for(int i=0;i<2;i++)Visual(area,"P03_LampWallShelf_"+i,shelfKey,i*7,9.40f,.06f,-6);
  AssetDatabase.SaveAssets();
  Bounds(area,"P03_ThreeDoorCamera",new Rect(10,5.8f,14,10),new Vector2(9.5f,19f),2);
  EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
  EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");
  var crowd=T("Environment/CrowdPlaceholder/ReportersCrowd_Visual").GetComponent<SpriteRenderer>();Get<P03ReporterIdle>(crowd.transform).source=crowd;
  EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
 }

 public static void RepairGrounds()
 {
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Requires clean edit mode");
  EditorSceneManager.OpenScene("Assets/Scenes/Scene02_Courtyard.unity");
  foreach(string path in new[]{"Environment/MasterBedroomInterior/Ground_MasterBedroom","Environment/HiddenBedroomArea/Ground_HiddenBedroom","Environment/Floor02/Ground_Floor02"})
  {
   var t=T(path);t.gameObject.SetActive(true);var r=t.GetComponent<SpriteRenderer>();if(r)Get<RetiredArtVisibility>(t).SetRenderers(new Renderer[]{r});
  }
  EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
  EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");
 }
 public static void FinalPolish()
 {
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Requires clean edit mode");
  EditorSceneManager.OpenScene("Assets/Scenes/Scene02_Courtyard.unity");
  foreach(var t in All())
  {
   var r=t.GetComponent<SpriteRenderer>();if(!r||!r.sprite||r.sprite.name!="Square")continue;
   if(t.name.Contains("Boundary")||t.name.StartsWith("Ground")||t.name.StartsWith("ThreeDoor_")||t.name=="MasterBedroom_Reference")
   {var retire=Get<RetiredArtVisibility>(t);retire.SetRenderers(retire.Renderers.Concat(new Renderer[]{r}).Distinct().ToArray());}
  }
  RainWindow(T("LivingRoomInterior_Return/NewArt_RoomShell/LivingWindow").GetComponent<SpriteRenderer>());
  var panes=new[]{new Rect(220f/1043,151f/658,75f/1043,412f/658),new Rect(331f/1043,151f/658,243f/1043,412f/658),new Rect(603f/1043,151f/658,208f/1043,412f/658)};
  foreach(var rain in All().Select(t=>t.GetComponent<WindowRainPresentation>()).Where(r=>r))rain.panes=panes;
  foreach(var parent in new[]{T("Environment/Floor01"),T("Environment/MasterBedroomInterior"),T("Environment/HiddenBedroomArea")})
  {var cam=parent.Find("Camera_ViewBounds");if(cam){var b=cam.GetComponent<RoomCameraBounds>();if(b)IntField(b,"priority",0);}}
  EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
  EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");
 }
}
