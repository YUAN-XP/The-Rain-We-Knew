using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;

// One-time P04 authoring. Never runs in a shipped player; refinements are separate.
public static partial class P04ProductionBuild {
 const string Art="Assets/Art/正式/P04/", Work="D:/游戏设计/Codex工作区/work/2026-10-04-P04/";
 static DemoArtCatalog catalog;
 static Transform[] All()=>P04Audit.All();
 static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
 static Transform T(string path)=>All().FirstOrDefault(t=>PathOf(t)==path)??throw new Exception("Missing "+path);
 static Transform Node(Transform parent,string name){var t=parent.Find(name);if(t)return t;t=new GameObject(name).transform;t.SetParent(parent,false);return t;}
 static C Get<C>(Transform t)where C:Component{var c=t.GetComponent<C>();return c?c:t.gameObject.AddComponent<C>();}
 static Sprite Source(SpriteArtRegion region)=>(Sprite)new SerializedObject(region).FindProperty("source").objectReferenceValue;
 static Sprite S(string key)=>catalog.Get(key)??throw new Exception("Missing sprite "+key);
 static void Field(UnityEngine.Object o,string name,Action<SerializedProperty> set){var so=new SerializedObject(o);var p=so.FindProperty(name);if(p==null)throw new Exception(name);set(p);so.ApplyModifiedPropertiesWithoutUndo();}
 static SpriteRenderer V(Transform parent,string name,string key,float x,float foot,float height,int order=-6){var t=Node(parent,name);var sr=Get<SpriteRenderer>(t);var retire=t.GetComponent<RetiredArtVisibility>();if(retire)UnityEngine.Object.DestroyImmediate(retire);P03ProductionBuild.Paint(sr,S(key),x,foot,height);t.gameObject.SetActive(true);sr.sortingOrder=order;return sr;}
 static void Shadow(SpriteRenderer r,float depth=.07f){var sh=Get<PaintedContactShadow>(r.transform);sh.target=r;sh.depth=depth;sh.widthFactor=.6f;}
 static void Hide(Renderer r){if(!r)return;r.enabled=false;if(r is SpriteRenderer sr)sr.forceRenderingOff=true;var retired=Get<RetiredArtVisibility>(r.transform);retired.SetRenderers(new Renderer[]{r});}
 static void HideArt(Transform root){foreach(var r in root.GetComponentsInChildren<Renderer>(true))Hide(r);}
 static void Box(Transform t,float x,float y,float width,float height,bool trigger=false){t.position=new Vector3(x,y,0);t.localScale=Vector3.one;var b=Get<BoxCollider2D>(t);b.offset=Vector2.zero;b.size=new Vector2(width,height);b.isTrigger=trigger;b.enabled=true;var sr=t.GetComponent<SpriteRenderer>();if(sr)Hide(sr);}
 static void Bounds(Transform root,Rect region,Vector2 edges){var t=root.Find("Camera_ViewBounds")??Node(root,"P04_ViewBounds");var b=Get<RoomCameraBounds>(t);Field(b,"actorRegion",p=>p.rectValue=region);Field(b,"horizontalEdges",p=>p.vector2Value=edges);Field(b,"leftWall",p=>p.objectReferenceValue=null);Field(b,"rightWall",p=>p.objectReferenceValue=null);Field(b,"priority",p=>p.intValue=10);}
 static P03RepeatedRoomStrip Strip(Transform parent,string name,string key,float x,float bottom,float width,float height,int order,float tileWidth=0){var t=Node(parent,name);t.position=new Vector3(x,bottom,0);t.localScale=Vector3.one;var s=Get<P03RepeatedRoomStrip>(t);s.source=S(key);s.material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/正式/P03/P03RepeatedPainting.mat");s.width=width;s.height=height;s.tileWidth=tileWidth>0?tileWidth:height*s.source.rect.width/s.source.rect.height;s.order=order;s.Rebuild();return s;}
 static bool Authored(Transform t,Transform root){for(var n=t;n&&n!=root;n=n.parent)if(n.name.StartsWith("P04_"))return true;return false;}
 static void RetireShell(Transform room){
  foreach(var t in room.GetComponentsInChildren<Transform>(true)){
   if(Authored(t,room))continue;
   var continuous=t.GetComponent<ContinuousRoomArtwork>();if(continuous)continuous.enabled=false;
   var repeat=t.GetComponent<P03RepeatedRoomStrip>();if(repeat)repeat.enabled=false;
   var r=t.GetComponent<SpriteRenderer>();var crop=t.GetComponent<SpriteArtRegion>();string key=crop&&Source(crop)?Source(crop).name:r&&r.sprite?r.sprite.name:"";
   if(continuous||repeat||key.StartsWith("TEX_FLOOR_")||key.StartsWith("MOD_HOME_WALL_")||key.StartsWith("MOD_HOME_LOW_SHELL")||t.name.Contains("Projection")||t.name.Contains("Wall")&&r&&r.sprite&&r.sprite.name=="Square")foreach(var output in t.GetComponents<Renderer>())Hide(output);
  }
 }
 static void Shell(Transform room,float x,float floor,float width,bool memory){RetireShell(room);
  foreach(var t in room.GetComponentsInChildren<Transform>(true)){if(t.name.StartsWith("P04_"))continue;if(t.GetComponent<ContinuousRoomArtwork>()||t.GetComponent<P03RepeatedRoomStrip>()||t.GetComponent<SpriteRenderer>()&&(t.name.StartsWith("Wall_")||t.name.StartsWith("Floor_")||t.name.StartsWith("Ground_")||t.name.StartsWith("Revision_CloseWall_"))){var mesh=t.GetComponent<P03RepeatedRoomStrip>();if(mesh)mesh.enabled=false;foreach(var r in t.GetComponents<Renderer>())Hide(r);}}
  var root=Node(room,"P04_ContinuousShell");Strip(root,"Wall",memory?"MOD_HOME_WALL_CLOSE_MEMORY_V03":"MOD_HOME_WALL_CLOSE_OTHER_V03",x,floor+.52f,width,3,-35);
  var ground=Strip(root,"QuietGround",memory?"P04_QuietMemory":"P04_QuietOther",x,floor-1.08f,width,1.6f,-19,8);ground.nativeTileHeight=0;ground.Rebuild();
 }
 static void Life(SpriteRenderer r,Sprite[] frames=null,float period=3.8f,float height=0){var c=Get<P04PaintedLife>(r.transform);c.target=r;c.frames=frames;c.period=period;c.height=height>0?height:r.bounds.size.y;c.breath=.0025f;c.sway=.10f;Shadow(r);}
 static void Point(Transform t,float x,float y)=>t.position=new Vector3(x,y,0);
 static void Door(Transform t,float x,float bottom){var r=t.Find("NewArt_Visual")?.GetComponent<SpriteRenderer>();if(r){var ret=r.GetComponent<RetiredArtVisibility>();if(ret)UnityEngine.Object.DestroyImmediate(ret);P03ProductionBuild.Paint(r,S("SPR_HOME_DOOR_TVCAB_MEMORY:0"),x,bottom,2.22f);}}

 public static void DiscardUnfinished(){if(EditorApplication.isPlaying)throw new Exception("Edit mode required");EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");}
 public static void ImportTown(){
  if(EditorApplication.isPlaying)throw new Exception("Edit mode required");string path=Art+"P04_TownContinuous.png";File.Copy(Work+"art/P04_TownContinuous.png",path,true);AssetDatabase.ImportAsset(path);var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.spritePixelsPerUnit=256;imp.alphaIsTransparency=false;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.maxTextureSize=2048;imp.isReadable=false;imp.wrapMode=TextureWrapMode.Clamp;imp.SaveAndReimport();catalog=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>("Assets/Resources/DemoArtCatalog.asset");Add("P04_TownContinuous",new Rect(60,0,1654,887));EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();typeof(DemoArtCatalog).GetField("lookup",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(catalog,null);
 }
 public static void ImportArt(){
  if(EditorApplication.isPlaying)throw new Exception("Edit mode required");Directory.CreateDirectory(Art);
  foreach(var file in Directory.GetFiles(Work+"art","*.png")){string p=Art+Path.GetFileName(file);File.Copy(file,p,true);AssetDatabase.ImportAsset(p);var imp=(TextureImporter)AssetImporter.GetAtPath(p);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.spritePixelsPerUnit=256;var st=new TextureImporterSettings();imp.ReadTextureSettings(st);st.spriteAlignment=(int)SpriteAlignment.BottomCenter;st.spritePivot=new Vector2(.5f,0);imp.SetTextureSettings(st);imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.maxTextureSize=4096;imp.isReadable=false;imp.wrapMode=TextureWrapMode.Clamp;imp.filterMode=FilterMode.Bilinear;imp.SaveAndReimport();}
  if(!AssetDatabase.LoadAssetAtPath<Material>(Art+"P04Ink.mat"))AssetDatabase.CreateAsset(new Material(Shader.Find("Sprites/Default")),Art+"P04Ink.mat");
  catalog=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>("Assets/Resources/DemoArtCatalog.asset");
  AddNative("P04_QuietMemory","TEX_FLOOR_QUIET_V04:0",10);AddNative("P04_QuietOther","TEX_FLOOR_QUIET_V04:1",10);
  Crop("P04_StoryLoop:0",45,42,575,860,1774,887);Crop("P04_StoryLoop:1",608,42,1138,860,1774,887);Crop("P04_StoryLoop:2",1211,42,1741,860,1774,887);
  Crop("P04_ChessLoop:0",55,48,540,789,1024,1536);Crop("P04_ChessLoop:1",542,48,1015,789,1024,1536);Crop("P04_ChessLoop:2",42,789,528,1513,1024,1536);Crop("P04_ChessLoop:3",529,789,1015,1513,1024,1536);
  Crop("P04_CatSleepLoop:0",20,177,698,550,2172,724);Crop("P04_CatSleepLoop:1",725,177,1430,550,2172,724);Crop("P04_CatSleepLoop:2",1460,177,2148,550,2172,724);Crop("P04_WangPing",210,28,796,1505,1024,1536);
  Crop("P04_DogPuddle",60,370,1720,641,1774,887);Add("P04_Fisher:0",new Rect(40,118,728,750));Add("P04_Fisher:1",new Rect(776,118,728,750));
  Add("P04_DrinkingDog:0",new Rect(29,73,1022,557));Add("P04_DrinkingDog:1",new Rect(1115,73,1022,557));
  Add("P04_LiMeiSeated",new Rect(198,57,664,1383));Add("P04_StoryAudience",new Rect(81,52,1389,929));
  Add("P04_TripodCamera",new Rect(106,38,808,1463));Add("P04_TripodCamera:empty",new Rect(106,38,808,1260));
  Crop("P04_ZhaoWalk:0",183,8,548,637,1211,1299);Crop("P04_ZhaoWalk:1",729,9,1070,641,1211,1299);
  Crop("P04_ZhaoWalk:2",184,644,562,1268,1211,1299);Crop("P04_ZhaoWalk:3",714,644,1077,1272,1211,1299);
  Crop("P04_GuitarHands:0",0,0,887,444,1774,887);Crop("P04_GuitarHands:1",887,0,1774,444,1774,887);Crop("P04_GuitarHands:2",0,444,887,887,1774,887);Crop("P04_GuitarHands:3",887,444,1774,887,1774,887);
  EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();typeof(DemoArtCatalog).GetField("lookup",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(catalog,null);Debug.Log("P04 explicit art regions imported; accepted catalog entries preserved.");
 }
 static void AddNative(string key,string original,int inset){if(catalog.entries.Any(e=>e.key==key))return;var src=S(original);var r=src.rect;r.y+=inset;r.height-=inset*2;var sp=Sprite.Create(src.texture,r,new Vector2(.5f,0),256,0,SpriteMeshType.FullRect);sp.name=key;AssetDatabase.AddObjectToAsset(sp,catalog);catalog.entries=catalog.entries.Concat(new[]{new DemoArtCatalog.Entry{key=key,sprite=sp}}).ToArray();}
 static void Crop(string key,int x0,int y0,int x1,int y1,int w,int h)=>Add(key,new Rect(x0,h-y1,x1-x0,y1-y0));
 static void Add(string key,Rect r){var existing=catalog.entries.FirstOrDefault(e=>e.key==key);if(existing!=null&&existing.sprite&&existing.sprite.rect==r)return;var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+key.Split(':')[0]+".png");var sp=Sprite.Create(tex,r,new Vector2(.5f,0),256,0,SpriteMeshType.FullRect);sp.name=key;AssetDatabase.AddObjectToAsset(sp,catalog);if(existing!=null)existing.sprite=sp;else catalog.entries=catalog.entries.Concat(new[]{new DemoArtCatalog.Entry{key=key,sprite=sp}}).ToArray();}
 public static void Childhood(){
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit mode required");EditorSceneManager.OpenScene("Assets/Scenes/Scene03_Childhood.unity");catalog=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>("Assets/Resources/DemoArtCatalog.asset");
  var chapter=UnityEngine.Object.FindAnyObjectByType<Scene03Chapter>();var home=chapter.homeRoot.transform;
  if(home.Find("P04_LayoutRevision1"))throw new Exception("Already compressed; use refinements");
  // Only the first hallway: bedrooms and upstairs keep their accepted anchors/rigs.
  foreach(Transform t in home)if(t.name!="NewArt_RoomShell"&&!t.name.Contains("Concept")&&t.name!="Camera_ViewBounds"&&t.name!="Floor02_Normal"&&!t.name.Contains("BedroomInterior")){var p=t.position;p.x*=.6f;t.position=p;}
  var roomShell=home.Find("NewArt_RoomShell");foreach(Transform t in roomShell)if(new[]{"LampTable","ShoeRack","EntryWindow"}.Contains(t.name)){var p=t.position;p.x*=.6f;t.position=p;}
  Point(chapter.homeSpawn,-12,1.23f);Point(T("Scene03Chapter/FamilyPhotoMarkers/BoyWaitingByLivingRoomDoor"),-2.5f,1.23f);
  Box(T("Environment/Home_Floor01_Normal/Ground_Home"),-4.7f,0,22,1);Box(T("Environment/Home_Floor01_Normal/LeftBoundary"),-15.4f,3,.6f,7);Box(T("Environment/Home_Floor01_Normal/RightBoundary"),6.5f,3,.6f,7);
  var right=home.Find("NormalRightWall");if(right)Box(right,6.5f,3,.6f,7);
  Shell(home,-4.6f,.5f,24,true);Bounds(home,new Rect(-17,-4,26,9.5f),new Vector2(-15.1f,6.2f));
  Door(T("Environment/Home_Floor01_Normal/家门"),-13.8f,1.02f);Door(T("Environment/Home_Floor01_Normal/客厅门框"),-1.2f,1.02f);
  foreach(var n in new[]{"Stairs_NormalFullHeight","Stairs","Art_LampTable","Art_ShoeRack","Art_PegRail"})foreach(var t in home.GetComponentsInChildren<Transform>(true).Where(t=>t.name==n))HideArt(t);
  var stairs=V(home,"P04_CompactStairs","P03_CompactStairs",4.9f,.86f,1.60f,-8);Shadow(stairs);
  Box(home.Find("S03_Stairs"),4.1f,1.45f,1.6f,1.9f,true);
  V(roomShell,"LampTable","SPR_HOME_EMPTY_SIDEBOARD_V01:1",-6,.86f,.88f);var lamp=home.Find("台灯");Point(lamp,-6,1.90f);V(lamp,"NewArt_Visual","SPR_HOME_DRESSING_MEMORY:2",-6,1.71f,.42f);
  V(roomShell,"ShoeRack","SPR_HOME_DRESSING_MEMORY:0",-11.28f,.86f,1.05f);V(roomShell,"EntryWindow","SPR_HOME_WINDOW_BOOKCASE_MEMORY:0",-10.8f,1.6f,1.65f);
  var phone=home.Find("S03_Phone");Point(phone,-12.6f,1.35f);V(home,"P04_PhoneSupport","SPR_HOME_EMPTY_SIDEBOARD_V01:1",-12.6f,.86f,.60f);var phoneArt=phone.GetComponentInChildren<SpriteRenderer>(true);if(phoneArt&&phoneArt.sprite&&phoneArt.sprite.name!="Square"){var region=phoneArt.GetComponent<SpriteArtRegion>();var sp=region?Source(region):phoneArt.sprite;P03ProductionBuild.Paint(phoneArt,sp,-12.6f,1.43f,.3f);} 
  foreach(var t in home.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="电话小柜"))HideArt(t);
  var photo=home.Find("照片");HideArt(photo);var ph=V(photo,"P04_FamilyPhoto","FILE_FAMILY_PHOTO_CLEAN",-9,1.65f,.45f);ph.gameObject.SetActive(false);
  foreach(var t in new[]{home.Find("Floor02_Normal"),home.Find("MasterBedroomInterior_Normal"),home.Find("SecondBedroomInterior_Normal")})if(t){float f=t.name=="Floor02_Normal"?6.53f:.5f;Shell(t,t.name=="Floor02_Normal"?-5.5f:t.position.x+1,f,t.name=="Floor02_Normal"?48:16,true);}
  foreach(var txt in chapter.homeRoot.GetComponentsInChildren<TMP_Text>(true))if(!(txt is TextMeshProUGUI))txt.gameObject.SetActive(false);
  Living(chapter);Street(chapter);River(chapter);
  var presentation=Get<P04ChapterPresentation>(chapter.transform);presentation.chapter=chapter;presentation.river=chapter.GetComponent<Scene03RiverSequence>();presentation.familyPhoto=ph.gameObject;presentation.pondFisher=T("Environment/ChildhoodStreet/P04_Fisher").gameObject;presentation.leftoverRod=T("Environment/ChildhoodStreet/P04_LeftRod").gameObject;presentation.streetReturn=new Vector3(105.5f,31.23f,0);
  var framing=Camera.main.GetComponent<RoomPresentationFraming>();for(int i=0;i<framing.rooms.Length;i++){var r=framing.rooms[i];r.correctRoomLimits=true;if(r.floorY<2&&r.area.center.x<40)r.area=new Rect(-17,-4,26,9.5f);if(r.area.center.x>180&&r.area.center.x<220)r.area=new Rect(192,-4,17,10);if(r.floorY>29&&r.floorY<40)r.area=new Rect(-36,29,148,20);framing.rooms[i]=r;}
  Node(home,"P04_LayoutRevision1");EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");Debug.Log("P04 childhood scene authored, triggers preserved and tail shortened.");
 }
 static void Living(Scene03Chapter chapter){
  var root=chapter.livingRoomRoot.transform;var f=root.Find("LivingRoomFurniture");Shell(root,200.2f,.5f,12,true);Bounds(root,new Rect(192,-4,17,10),new Vector2(195.5f,205.1f));
  foreach(var t in f.GetComponentsInChildren<Transform>(true))if(t.GetComponent<SpriteRenderer>()||t.GetComponent<TMP_Text>())HideArt(t);
  // Reset roots first; the full-body painting remains continuous and grounded.
  var mother=f.Find("母亲");var father=f.Find("父亲");Point(mother,199.35f,.86f);Point(father,200.40f,.86f);var m=V(mother,"P04_Body","CHR_FAMILY_NEIGHBOR_BASE:0",199.35f,.86f,1.74f);var d=V(father,"P04_Body","CHR_FAMILY_NEIGHBOR_ACTION:1",200.4f,.86f,1.82f);Life(m);Life(d);
  Shadow(V(f,"P04_Sofa","SPR_HOME_SOFA_TABLE_MEMORY:0",198f,.86f,.92f,-8));Shadow(V(f,"P04_Table","SPR_HOME_SOFA_TABLE_MEMORY:1",200.25f,.86f,.72f,-7));Shadow(V(f,"P04_Bookcase","SPR_HOME_WINDOW_BOOKCASE_MEMORY:1",202.05f,.86f,1.55f,-8));Shadow(V(f,"P04_TVSupport","SPR_HOME_DOOR_TVCAB_MEMORY:1",203.6f,.86f,1.55f,-8));
  Point(f.Find("S03_Parents"),199.9f,1.3f);Point(f.Find("S03_Camera"),200.25f,1.60f);V(f.Find("S03_Camera"),"P04_TableCamera","SPR_CAMERA_TABLETOP_V02:0",200.25f,1.54f,.20f,-4);
  var family=chapter.GetComponent<Scene03FamilySequence>();Point(family.cameraPlacement.transform,201.1f,1.23f);HideArt(family.placedCamera.transform);V(family.placedCamera.transform,"P04_TripodCamera","P04_TripodCamera",201.1f,.86f,1.4f,-4);family.placedCamera.SetActive(false);
  var tripod=V(root,"P04_EmptyTripod","P04_TripodCamera:empty",201.1f,.86f,1.206f,-6);var opt=Get<P04FamilyTripod>(tripod.transform);opt.family=family;
  Point(family.roomBoyPoint,196.5f,1.23f);Point(family.motherPose,199.68f,.86f);Point(family.fatherPose,200.40f,.86f);Point(family.childPose,199.7f,1.23f);Point(family.boyEdge,200.65f,1.23f);Point(family.boyOutside,196.5f,1.23f);Point(family.viewPoint,200.15f,1.83f);
  foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name.Contains("Boundary")){var b=t.GetComponent<Collider2D>();if(b)Point(t,t.position.x<200?195.25f:205.35f,t.position.y);}
  foreach(var txt in root.GetComponentsInChildren<TMP_Text>(true))if(!(txt is TextMeshProUGUI))txt.gameObject.SetActive(false);
 }
 static void Street(Scene03Chapter chapter){
  var street=chapter.streetRoot.transform;var art=street.Find("NewArt_Street");
  // Tail compression preserves the order: pond, board, rings, sign, gateway.
  foreach(Transform t in street)if(t!=art&&t.name!="Camera_ViewBounds"&&t.position.x>=88){var p=t.position;p.x=88+(p.x-88)*.4f;t.position=p;}
  foreach(Transform t in art)if(t.position.x>=88){var p=t.position;p.x=88+(p.x-88)*.4f;t.position=p;}
  Box(street.Find("Ground_Street"),38,30,145,1);Box(street.Find("Stage4B_StopBoundary"),109.2f,34,.6f,9);Bounds(street,new Rect(-36,29,148,20),new Vector2(-32.5f,108.9f));
  foreach(var sr in street.GetComponentsInChildren<SpriteRenderer>(true)){var crop=sr.GetComponent<SpriteArtRegion>();string key=crop&&Source(crop)?Source(crop).name:sr.sprite?sr.sprite.name:"";if(sr.name.Contains("LinkWall")||sr.name.Contains("FarTown")||sr.name.Contains("QuietStreetGround")||key.StartsWith("TEX_FLOOR_QUIET"))Hide(sr);}
  Strip(street,"P04_ContinuousTown","BG_STREET_FAR_DAY",38,30.95f,150,6.75f,-45,12);
  var floor=Strip(street,"P04_QuietStreetGround","P04_QuietMemory",38,29.42f,150,1.6f,-19,8);floor.nativeTileHeight=0;floor.Rebuild();
  // Shared pale plaster connects right into the accepted pond piers, with exact overlaps.
  Strip(street,"P04_WallBeforePond","PondDeparture_InfillWithoutEndPier_V3",14.95f,30.70f,100.5f,1.6f,-32,11.04f);
  Strip(street,"P04_WallAfterPond","PondDeparture_InfillWithoutEndPier_V3",97.38f,30.70f,18.76f,1.6f,-32,11.04f);
  V(art,"LiMeiBuilding","MOD_STREET_RESIDENTIAL_CHILD:0",-15.9f,30.70f,3.3f,-28);V(art,"Shop","MOD_STREET_SHOPS_CHILD:0",-5,30.70f,3.25f,-28);V(art,"ChessHome","MOD_STREET_RESIDENTIAL_CHILD:0",27.1f,30.70f,3.3f,-28);V(art,"Restaurant","MOD_STREET_SHOPS_CHILD:1",47,30.70f,3.25f,-28);
  V(art,"NormalGate","SPR_STREET_GATE_MAILBOX:0",15,30.70f,1.9f,-26);
  var gate=street.Find("S03_Gate");Box(gate,15,31.23f,1.5f,1.5f,true);Get<Scene03HomeReturn>(gate).chapter=chapter;
  var lim=street.Find("李梅");HideArt(lim);Point(lim,-13.15f,30.86f);Life(V(lim,"P04_SeatedLiMei","P04_LiMeiSeated",-13.15f,30.86f,1.30f,-5));Point(street.Find("S03_LiMei"),-13.15f,31.23f);
  foreach(var t in street.GetComponentsInChildren<Transform>(true).Where(t=>t.name.Contains("踩水的孩子")))HideArt(t);
  foreach(var t in new[]{street.Find("小狗"),street.Find("S03_Puddle/NewArt_Puddle")})if(t)HideArt(t);
  var dog=V(street,"P04_DrinkingDog","P04_DrinkingDog:0",.2f,30.53f,.53f,-5);Life(dog,new[]{S("P04_DrinkingDog:0"),S("P04_DrinkingDog:1")},1.8f,.53f);dog.GetComponent<P04PaintedLife>().sway=0;
  foreach(var t in new[]{street.Find("孙付"),street.Find("王坤"),street.Find("王平"),street.Find("S03_Chess/NewArt_ChessGroup")})if(t)HideArt(t);
  Life(V(street,"P04_SunFu","CHR_CHESS_NEIGHBORS_ACTION:0",25.1f,30.86f,1.28f,-5));var kun=V(street,"P04_WangKun","CHR_CHESS_NEIGHBORS_ACTION:1",26.7f,30.86f,1.3f,-5);kun.flipX=true;Life(kun);
  Shadow(V(street,"P04_ChessTable","SPR_STREET_LIFE_DRESSING:3",25.90f,30.86f,.54f,-4));Life(V(street,"P04_WangPing","CHR_CHESS_NEIGHBORS_BASE:2",28.3f,30.86f,1.75f,-5));Point(street.Find("S03_Chess"),26.1f,31.23f);
  var story=street.Find("说书人");HideArt(story);foreach(var t in new[]{art.Find("StoryAudience"),art.Find("ChattingNeighbours")})if(t&&t.name=="StoryAudience")HideArt(t);
  // A low roadside platform, behind the free walking lane.
  var stage=Strip(street,"P04_StoryPlatform","TEX_FLOOR_QUIET_V04:0",-28.25f,30.74f,6.5f,.12f,-7,8);stage.tint=new Color(.78f,.68f,.52f);
  Life(V(street,"P04_Storyteller","CHR_STORY_ZHAO_ZHANG_ACTION:0",-25.85f,30.86f,1.35f,-4));Life(V(street,"P04_SeatedAudience","P04_StoryAudience",-29f,30.86f,1.23f,-5));Point(street.Find("S03_Storyteller"),-25.85f,31.23f);
  HideArt(street.Find("赵炳程"));var fisher=V(street,"P04_Fisher","P04_Fisher:0",72.2f,30.86f,1.65f,-4);Life(fisher,new[]{S("P04_Fisher:0"),S("P04_Fisher:1")},5.6f,1.65f);
  var rod=Node(street,"P04_LeftRod");Point(rod,72.75f,30.86f);var line=Get<LineRenderer>(rod);line.useWorldSpace=false;line.positionCount=3;line.SetPositions(new[]{Vector3.zero,new Vector3(.25f,.70f,0),new Vector3(1.10f,1.22f,0)});line.startWidth=.018f;line.endWidth=.008f;line.startColor=new Color(.30f,.26f,.16f);line.endColor=new Color(.20f,.18f,.11f);line.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Art+"P04Ink.mat");line.sortingOrder=-4;rod.gameObject.SetActive(false);
  V(art,"RiversideSign","SPR_STREET_POND_SIGN:1",96.4f,30.86f,1.10f,-6);
  var sign=Node(street,"P04_RiverSignText");var text=Get<TextMeshPro>(sign);text.font=chapter.font;text.text="河堤 →";text.fontSize=1.5f;text.alignment=TextAlignmentOptions.Center;text.color=new Color(.24f,.22f,.15f);text.rectTransform.sizeDelta=new Vector2(1.05f,.30f);Point(text.transform,96.4f,31.68f);text.GetComponent<Renderer>().sortingOrder=-5;
  var opening=V(street,"P04_RiverGateway","SPR_STREET_GATE_MAILBOX:1",108,30.70f,1.9f,-26);Box(street.Find("Stage4B_Entry"),108,31.23f,1.9f,1.8f,true);
  foreach(var txt in street.GetComponentsInChildren<TMP_Text>(true))if(!(txt is TextMeshProUGUI)&&txt!=text)txt.gameObject.SetActive(false);
  // Keep cat/coin components intact; tiny breathing stays on their painted silhouette.
  foreach(var sr in chapter.catVisual.GetComponentsInChildren<SpriteRenderer>(true))if(sr.sprite&&sr.sprite.name!="Square"&&sr.enabled&&!sr.forceRenderingOff)Life(sr,null,4.6f);
 }
 static void River(Scene03Chapter chapter){
  var river=chapter.GetComponent<Scene03RiverSequence>();var root=chapter.maze.riverRoot.transform;
  var tree=root.Find("河堤老树/NewArt_Visual").GetComponent<SpriteRenderer>();if(tree){var sp=tree.GetComponent<SpriteArtRegion>()?Source(tree.GetComponent<SpriteArtRegion>()):tree.sprite;P03ProductionBuild.Paint(tree,sp,309.2f,70.86f,4.8f);}
  Point(chapter.maze.riverSpawn,296,71.23f);Point(river.departure,296.1f,71.23f);
  Box(root.Find("LeftBoundary"),293.9f,74,.5f,10);Box(root.Find("RightBoundary"),313.1f,74,.5f,10);Bounds(root,new Rect(293,65,22,23),new Vector2(294.15f,312.85f));
  foreach(var sr in root.GetComponentsInChildren<SpriteRenderer>(true))if(sr.name.Contains("QuietRiverGround"))Hide(sr);
  var floor=Strip(root,"P04_QuietRiverGround","P04_QuietMemory",303,69.42f,35,1.6f,-19,8);floor.nativeTileHeight=0;floor.Rebuild();
  HideArt(river.zhao);var z=V(river.zhao,"P04_ZhaoBody","CHR_STORY_ZHAO_ZHANG_BASE:1",294.8f,70.86f,1.8f,-3);Point(river.zhao,294.8f,70.86f);Life(z,new[]{S("P04_ZhaoWalk:0"),S("P04_ZhaoWalk:1"),S("P04_ZhaoWalk:2"),S("P04_ZhaoWalk:3")},4.8f,1.8f);river.zhao.gameObject.SetActive(false);
  var riverLife=Get<P04RiverLife>(root);riverLife.sequence=river;riverLife.tree=tree;
 }
 public static void Refine(){
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit mode required");
  EditorSceneManager.OpenScene("Assets/Scenes/Scene03_Childhood.unity");catalog=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>("Assets/Resources/DemoArtCatalog.asset");var ch=UnityEngine.Object.FindAnyObjectByType<Scene03Chapter>();var home=ch.homeRoot.transform;var living=ch.livingRoomRoot.transform;var street=ch.streetRoot.transform;var river=ch.GetComponent<Scene03RiverSequence>();
  RetireShell(home);RetireShell(living);
  foreach(var strip in ch.GetComponentsInChildren<P03RepeatedRoomStrip>(true))if(strip.name.Contains("QuietGround")){strip.source=S("P04_QuietMemory");strip.nativeTileHeight=0;strip.Rebuild();}
  foreach(var strip in UnityEngine.Object.FindObjectsByType<P03RepeatedRoomStrip>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(strip.name.StartsWith("P04_Quiet")){strip.source=S("P04_QuietMemory");strip.nativeTileHeight=0;strip.Rebuild();}
  // The old oversized room concept meshes are deliberately disabled, not just covered.
  foreach(var c in living.GetComponentsInChildren<ContinuousRoomArtwork>(true)){c.enabled=false;foreach(var r in c.GetComponents<Renderer>())Hide(r);}
  var oldPlant=living.Find("NewArt_RoomShell/PottedPlant");if(oldPlant)HideArt(oldPlant);
  V(home.Find("S03_Phone"),"P04_Phone","SPR_HOME_WALL_PHONE_V02:0",-12.6f,1.43f,.30f,-4);
  var support=living.Find("LivingRoomFurniture");V(support,"P04_Table","SPR_HOME_SOFA_TABLE_MEMORY:1",200.15f,.86f,.72f,-7);
  Point(support.Find("S03_Camera"),199.50f,1.6f);V(support.Find("S03_Camera"),"P04_TableCamera","SPR_CAMERA_TABLETOP_V02:0",199.50f,1.54f,.20f,-4);
  var parent=support.Find("父亲");Point(parent,200.45f,.86f);V(parent,"P04_Body","CHR_FAMILY_NEIGHBOR_ACTION:1",200.45f,.86f,1.82f,-6);
  var exit=living.Find("LivingRoomExitVisual/NewArt_Visual");if(exit)P03ProductionBuild.Paint(exit.GetComponent<SpriteRenderer>(),S("SPR_HOME_DOOR_TVCAB_MEMORY:0"),195.9f,1.02f,2.22f);
  // World TextMeshPro uses its RectTransform after the component conversion.
  var text=street.Find("P04_RiverSignText").GetComponent<TextMeshPro>();text.ForceMeshUpdate();text.rectTransform.localPosition=new Vector3(96.4f,1.67f,0);text.rectTransform.anchoredPosition3D=new Vector3(96.4f,1.67f,0);text.rectTransform.localScale=Vector3.one;text.fontSize=1.5f;EditorUtility.SetDirty(text.rectTransform);
  var prize=street.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r=>r.sprite&&r.sprite.name.Contains("GAME_RING_TARGETS")&&r.sprite.name.EndsWith(":2"));if(prize){var src=prize.GetComponent<SpriteArtRegion>()?Source(prize.GetComponent<SpriteArtRegion>()):prize.sprite;P03ProductionBuild.Paint(prize,src,prize.transform.position.x,30.86f,.45f);}
  var puddle=street.Find("S03_Puddle/普通积水");if(puddle)HideArt(puddle);V(street,"P04_DogPuddle","P04_DogPuddle",-.25f,30.82f,.13f,-8);
  var story=V(street,"P04_Storyteller","P04_StoryLoop:0",-25.85f,30.86f,1.35f,-4);Life(story,new[]{S("P04_StoryLoop:0"),S("P04_StoryLoop:1"),S("P04_StoryLoop:2")},5.5f,1.35f);
  var sun=V(street,"P04_SunFu","P04_ChessLoop:0",25.1f,30.86f,1.28f,-5);sun.flipX=true;Life(sun,new[]{S("P04_ChessLoop:0"),S("P04_ChessLoop:1")},8.6f,1.28f);
  var kun=V(street,"P04_WangKun","P04_ChessLoop:2",26.7f,30.86f,1.30f,-5);kun.flipX=false;Life(kun,new[]{S("P04_ChessLoop:2"),S("P04_ChessLoop:3")},8.6f,1.3f);kun.GetComponent<P04PaintedLife>().phaseOffset=.5f;
  Life(V(street,"P04_WangPing","P04_WangPing",28.3f,30.86f,1.72f,-5));
  var cat=ch.catVisual.transform;foreach(var r in cat.GetComponentsInChildren<Renderer>(true))Hide(r);var sleep=V(cat,"P04_SleepingCat","P04_CatSleepLoop:0",37,30.86f,.30f,-4);Life(sleep,new[]{S("P04_CatSleepLoop:0"),S("P04_CatSleepLoop:1"),S("P04_CatSleepLoop:2")},6.3f,.30f);sleep.GetComponent<P04PaintedLife>().sway=0;
  var awake=V(cat,"P04_EatingCat","AN_CAT_NORMAL_V02:1",37,30.86f,.50f,-4);var choice=Get<StoryArtworkChoice>(cat);Field(choice,"flag",p=>p.enumValueIndex=(int)StoryFlag.CatFed);Field(choice,"before",p=>p.objectReferenceValue=sleep.gameObject);Field(choice,"after",p=>p.objectReferenceValue=awake.gameObject);awake.gameObject.SetActive(false);
  V(street,"P04_DrinkingDog","P04_DrinkingDog:0",.2f,30.86f,.53f,-5);var glint=Get<P04CoinGlint>(cat);glint.chapter=ch;
  Point(river.zhao,294.8f,70.86f);var z=river.zhao.Find("P04_ZhaoBody");z.localPosition=Vector3.zero;z.GetComponent<P04PaintedLife>().sway=0;
  EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");Debug.Log("P04 refinement saved: original compression unchanged, old mesh suppression, seamless floor, actual life frames, Wang Ping aunt and Zhao root aligned.");
 }
 public static void ReturnLiving(){
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit mode required");EditorSceneManager.OpenScene("Assets/Scenes/Scene02_Courtyard.unity");catalog=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>("Assets/Resources/DemoArtCatalog.asset");var room=T("LivingRoomInterior_Return");
  if(room.Find("P04_LayoutRevision1"))throw new Exception("Return living already authored");Shell(room,350.9f,.5f,15,false);Bounds(room,new Rect(342,-4,18,10),new Vector2(343.95f,358.1f));
  foreach(var sr in room.GetComponentsInChildren<SpriteRenderer>(true)){string n=sr.name;if(n.Contains("Rug")||n.Contains("Carpet")||n.Contains("Floor_")||n.Contains("Ground_")||n.Contains("Wall_"))Hide(sr);}
  var furniture=Node(room,"P04_GroundedFurniture");Shadow(V(furniture,"Sofa","SPR_HOME_SOFA_TABLE_OTHER:0",347f,.86f,.92f,-8));Shadow(V(furniture,"Table","SPR_HOME_SOFA_TABLE_OTHER:1",350.35f,.86f,.72f,-7));Shadow(V(furniture,"TV","SPR_HOME_DOOR_TVCAB_OTHER:1",354.3f,.86f,1.5f,-8));Shadow(V(furniture,"Bookcase","SPR_HOME_WINDOW_BOOKCASE_OTHER:1",356.3f,.86f,1.55f,-8));
  foreach(var sr in room.GetComponentsInChildren<SpriteRenderer>(true)){if(sr.transform.IsChildOf(furniture)||sr.transform.name.StartsWith("P04_"))continue;var crop=sr.GetComponent<SpriteArtRegion>();string key=crop&&Source(crop)?Source(crop).name:sr.sprite?sr.sprite.name:"";if(key.StartsWith("SPR_HOME_SOFA_TABLE")||key.StartsWith("SPR_HOME_DOOR_TVCAB")&&key.EndsWith(":1")||key.StartsWith("SPR_HOME_WINDOW_BOOKCASE")&&key.EndsWith(":1"))Hide(sr);}
  var doorway=room.Find("ReturnLivingRoomDoor");if(doorway)Door(doorway,doorway.position.x,1.02f);
  Node(room,"P04_LayoutRevision1");EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");Debug.Log("P04 returned living room clean ground and grounded furniture; clue bindings unchanged.");
 }
}





