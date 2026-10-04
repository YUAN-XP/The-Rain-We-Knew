using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using TMPro;
public static partial class P05ProductionBuild {
 const string Art="Assets/Art/正式/P05/",Work=P05Audit.Work;
 static DemoArtCatalog catalog;static SchoolLayoutController layout;
 static T Get<T>(Transform t)where T:Component{var c=t.GetComponent<T>();return c?c:t.gameObject.AddComponent<T>();}
 static Transform Node(Transform p,string n){var t=p.Find(n);if(!t){t=new GameObject(n).transform;t.SetParent(p,false);}return t;}
 static Sprite S(string key)=>catalog.Get(key)??throw new Exception("Missing P05 source "+key);
 static void Field(UnityEngine.Object o,string key,Action<SerializedProperty> set){var so=new SerializedObject(o);var f=so.FindProperty(key);if(f==null)throw new Exception(o.name+":"+key);set(f);so.ApplyModifiedPropertiesWithoutUndo();}
 static void Hide(Renderer r){r.enabled=false;if(r is SpriteRenderer s)s.forceRenderingOff=true;Get<RetiredArtVisibility>(r.transform).SetRenderers(new[]{r});}
 static void Retire(Transform root){foreach(var c in root.GetComponentsInChildren<ContinuousRoomArtwork>(true))c.enabled=false;foreach(var c in root.GetComponentsInChildren<P03RepeatedRoomStrip>(true))c.enabled=false;foreach(var r in root.GetComponentsInChildren<Renderer>(true))Hide(r);foreach(var t in root.GetComponentsInChildren<TMP_Text>(true))t.gameObject.SetActive(false);}
 static SpriteRenderer Paint(Transform parent,string name,string key,float x,float foot,float height,int order=-7){var t=Node(parent,name);var r=Get<SpriteRenderer>(t);var old=t.GetComponent<RetiredArtVisibility>();if(old)UnityEngine.Object.DestroyImmediate(old);r.enabled=true;r.forceRenderingOff=false;r.sortingOrder=order;P03ProductionBuild.Paint(r,S(key),x,foot,height);return r;}
 static void Shadow(SpriteRenderer r){var sh=Get<PaintedContactShadow>(r.transform);sh.target=r;sh.depth=.045f;sh.widthFactor=.7f;}
 static P03RepeatedRoomStrip Strip(Transform parent,string name,string key,float center,float floor,float width,float height,int order,float tile=0){var t=Node(parent,name);t.position=new Vector3(center,floor,0);t.localScale=Vector3.one;var r=Get<P03RepeatedRoomStrip>(t);r.source=S(key);r.material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/正式/P03/P03RepeatedPainting.mat");r.width=width;r.height=height;r.tileWidth=tile>0?tile:height*r.source.rect.width/r.source.rect.height;r.order=order;r.Rebuild();return r;}
 static void Add(string key,string file,Rect rect){if(catalog.entries.Any(e=>e.key==key))return;var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+file+".png");var sp=Sprite.Create(tex,rect,new Vector2(.5f,0),256,0,SpriteMeshType.FullRect);sp.name=key;AssetDatabase.AddObjectToAsset(sp,catalog);catalog.entries=catalog.entries.Concat(new[]{new DemoArtCatalog.Entry{key=key,sprite=sp}}).ToArray();}
 static void Crop(string key,string file,int x0,int y0,int x1,int y1){var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+file+".png");Add(key,file,new Rect(x0,tex.height-y1,x1-x0,y1-y0));}
 static void Native(string key,string original,int yInset=0){if(catalog.entries.Any(e=>e.key==key))return;var s=S(original);var r=s.rect;r.y+=yInset;r.height-=yInset*2;var sp=Sprite.Create(s.texture,r,new Vector2(.5f,0),256,0,SpriteMeshType.FullRect);sp.name=key;AssetDatabase.AddObjectToAsset(sp,catalog);catalog.entries=catalog.entries.Concat(new[]{new DemoArtCatalog.Entry{key=key,sprite=sp}}).ToArray();}
 public static void DiscardUnfinished(){EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");AssetDatabase.ImportAsset("Assets/GameData/School/SchoolLayout.asset",ImportAssetOptions.ForceUpdate);}
 public static void Import(){if(EditorApplication.isPlaying)throw new Exception("Edit mode required");Directory.CreateDirectory(Art);catalog=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>("Assets/Resources/DemoArtCatalog.asset");
  foreach(var f in Directory.GetFiles(Work+"art","*.png")){var path=Art+Path.GetFileName(f);File.Copy(f,path,true);AssetDatabase.ImportAsset(path);var imp=(TextureImporter)AssetImporter.GetAtPath(path);imp.textureType=TextureImporterType.Sprite;imp.spriteImportMode=SpriteImportMode.Single;imp.spritePixelsPerUnit=256;imp.alphaIsTransparency=true;imp.mipmapEnabled=false;imp.textureCompression=TextureImporterCompression.Uncompressed;imp.maxTextureSize=4096;imp.filterMode=FilterMode.Bilinear;imp.wrapMode=TextureWrapMode.Clamp;imp.SaveAndReimport();}
  Crop("P05_StairUp","P05_AuxAtlas",132,70,438,492);Crop("P05_IronGate","P05_AuxAtlas",603,68,904,494);Crop("P05_ShoeLocker","P05_AuxAtlas",33,689,507,927);Crop("P05_Monitors","P05_AuxAtlas",550,598,985,929);Crop("P05_Anonymous","P05_AuxAtlas",45,1020,480,1485);Crop("P05_RoofAccess","P05_AuxAtlas",523,1020,987,1460);
  Crop("P05_Ghost","P05_Working",850,50,1080,700);
  Crop("P05_PencilHand","P05_Hands",52,163,454,907);Crop("P05_GrabbingHand","P05_Hands",479,272,1039,660);Crop("P05_NotePaper","P05_Hands",1063,175,1525,863);
  foreach(var f in new[]{"P05_Roof","P05_Security","P05_Downstairs","P05_Desk"}){var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+f+".png");Add(f,f,new Rect(0,0,tex.width,tex.height));}
  Crop("P05_YouthSeated","P05_YouthSeated",49,0,1012,1360);
  // Explicit alpha regions retain figure feet and remove empty cell gutters.
  var students=new[]{new[]{44,21,299,509},new[]{385,21,640,509},new[]{726,21,981,509},new[]{53,536,297,1022},new[]{394,536,638,1022},new[]{735,536,979,1022},new[]{28,1041,316,1508},new[]{369,1041,657,1508},new[]{710,1041,998,1508}};
  for(int i=0;i<students.Length;i++){var a=students[i];Crop("P05_Student:"+i,"P05_Students",a[0],a[1],a[2],a[3]);}
  var texLoop=AssetDatabase.LoadAssetAtPath<Texture2D>(Art+"P05_Loops.png");if(texLoop){int cw=texLoop.width/3,ch=texLoop.height/3;for(int i=0;i<9;i++)Crop("P05_Loop:"+i,"P05_Loops",(i%3)*cw+8,(i/3)*ch+5,(i%3+1)*cw-8,(i/3+1)*ch-7);}
  Native("P05_WallMemory","MOD_SCHOOL_LOW_SHELL_V02:0");Native("P05_WallOther","MOD_SCHOOL_LOW_SHELL_V02:1");
  EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssets();typeof(DemoArtCatalog).GetField("lookup",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(catalog,null);
 }
 static void MoveRoom(Transform root,SchoolRoomDefinition room){var old=room.origin;var now=P05SchoolCoordinates.Origin(room.id,old);float width=P05SchoolCoordinates.Width(room.id,room.width);
  var nodes=root.GetComponentsInChildren<Transform>(true);var pos=nodes.Select(t=>t.position).ToArray();for(int i=0;i<nodes.Length;i++)nodes[i].position=new Vector3(now.x+P05SchoolCoordinates.Local(room.id,pos[i].x-old.x),now.y+pos[i].y-old.y,pos[i].z);
  foreach(var c in root.GetComponentsInChildren<BoxCollider2D>(true)){if(c.name=="Ground"){c.size=new Vector2(width+.04f,c.size.y);c.transform.position=new Vector3(now.x+width*.5f,now.y-.35f,0);}if(c.name=="Boundary"){float lx=c.transform.position.x-now.x;if(room.id=="ReadingPassage"||room.id=="Floor03Left"&&lx>width/2||room.id=="Floor03Right"&&lx<width/2)c.enabled=false;}}
 }
 static void Life(SpriteRenderer r,string id,string activity,Sprite[] frames,float phase,float height,float breath=.0025f){var l=Get<P05PaintedLoop>(r.transform);l.identity=id;l.activity=activity;l.target=r;l.frames=frames;l.phaseOffset=phase;l.height=height;l.breath=breath;l.period=activity=="typing"?3.3f:activity=="writing"?4.2f:5.3f;Shadow(r);}
 static Sprite[] Frames(string key,int first)=>Enumerable.Range(first,3).Select(i=>S(key+":"+i)).ToArray();
 public static void School(){if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit mode required");EditorSceneManager.OpenScene("Assets/Scenes/Scene04_School.unity");catalog=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>("Assets/Resources/DemoArtCatalog.asset");layout=UnityEngine.Object.FindAnyObjectByType<SchoolLayoutController>();if(layout.transform.Find("P05_LayoutRevision1"))throw new Exception("P05 already mapped; use Refinements");
  foreach(var variant in new[]{layout.otherworldRoot,layout.memoryRoot})foreach(var room in layout.data.rooms){var root=variant.transform.Find(room.id);if(root)MoveRoom(root,room);}
  foreach(var d in layout.data.portals){d.sourceLocal.x=P05SchoolCoordinates.Local(d.sourceRoom,d.sourceLocal.x);d.destinationLocal.x=P05SchoolCoordinates.Local(d.targetRoom,d.destinationLocal.x);}
  foreach(var room in layout.data.rooms){room.origin=P05SchoolCoordinates.Origin(room.id,room.origin);room.width=P05SchoolCoordinates.Width(room.id,room.width);}
  foreach(var tr in layout.GetComponentsInChildren<RoomTransitionInteractable>(true)){var src=tr.GetComponentInParent<SchoolPortal>();if(src){var d=src.Definition;var dest=layout.data.Room(d.targetRoom);Field(tr,"destinationCameraLimits",p=>p.vector2Value=dest.CameraX);Field(tr,"destinationVerticalCameraLimits",p=>p.vector2Value=Vector2.one*dest.CameraY);}}
  foreach(var v in new[]{layout.otherworldRoot,layout.memoryRoot})foreach(var room in layout.data.rooms){var root=v.transform.Find(room.id);if(root)Dress(root,room,v==layout.memoryRoot);}
  var flow=UnityEngine.Object.FindAnyObjectByType<SchoolSceneController>(FindObjectsInactive.Include);var cam=Camera.main;var framing=cam.GetComponent<RoomPresentationFraming>();framing.rooms=layout.data.rooms.Select(r=>new RoomPresentationFraming.Room{area=new Rect(r.origin.x-1,r.origin.y-2,r.width+2,8),floorY=r.origin.y,size=2.5f,correctRoomLimits=true}).ToArray();
  // Logical actors and physical markers remain the original instances.
  if(flow.zhengTaoVisual){Retire(flow.zhengTaoVisual.transform);var cls=layout.data.Room("Class3_2");flow.zhengTaoVisual.transform.position=new Vector3(cls.origin.x+8.6f,cls.origin.y+.22f,0);var z=Paint(flow.zhengTaoVisual.transform,"P05_ZhengTao","CHR_ZHENGTAO_YOUTH_BASE:1",cls.origin.x+8.6f,cls.origin.y+.22f,1.72f,-5);Shadow(z);}
  var youthOld=flow.youth.GetComponentsInChildren<SpriteRenderer>(true);foreach(var r in youthOld)if(r.name.Contains("Uniform")||r.name=="BlueBib"||r.name=="Strap")Hide(r);
  var arrival=UnityEngine.Object.FindObjectsByType<PlayerSpawnPoint>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(s=>s.gameObject.scene==SceneManager.GetActiveScene());if(arrival)arrival.transform.position=new Vector3(41.5f,1.7f,0);
  var basket=flow.basketball;Field(basket,"framingOrthographicSize",p=>p.floatValue=2.6f);
  var framingPoint=new SerializedObject(basket).FindProperty("cameraFrame").objectReferenceValue as Transform;if(framingPoint)framingPoint.position=new Vector3(71f,2.05f,-10);
  Field(flow.chase,"readingThreatX",p=>p.floatValue=32.91f);
  Node(layout.transform,"P05_LayoutRevision1");EditorUtility.SetDirty(layout.data);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");Debug.Log("P05 geometry and school art saved, original gates and stable interactable instances retained.");
 }
 static void Dress(Transform root,SchoolRoomDefinition r,bool memory){Retire(root);var art=Node(root,"P05_Artwork");float x=r.origin.x,f=r.origin.y,w=r.width;var ground=Strip(art,"QuietGround",memory?"P04_QuietMemory":"P04_QuietOther",x+w/2,f-1.3f,w+.05f,1.55f,-19,8);ground.Rebuild();
  if(r.id=="Roof"){Strip(art,"DistantCity","P05_Roof",x+w/2,f+.25f,w+18,5,-45,7.5f);}
  else if(r.id=="Floor01"){Strip(art,"SchoolWall",memory?"P05_WallMemory":"P05_WallOther",x+12,f+.25f,24,3.6f,-35);Strip(art,"CourtTrees","BG_COURT_DISTANT_TREES_V04",x+34,f+.35f,20.05f,3.6f,-35,8);}
  else {Strip(art,"SchoolWall",memory?"P05_WallMemory":"P05_WallOther",x+w/2,f+.25f,w+.04f,3.6f,-35);}
  var bounds=Get<RoomCameraBounds>(Node(art,"ViewBounds"));Field(bounds,"actorRegion",p=>p.rectValue=new Rect(x,f-2,w,8));Field(bounds,"horizontalEdges",p=>p.vector2Value=r.id.StartsWith("Floor03")||r.id=="ReadingPassage"?new Vector2(0,80):new Vector2(x,x+w));Field(bounds,"priority",p=>p.intValue=20);
  // Paint only actual traversable gateways. Reading boundaries have no fake doors.
  foreach(var p in root.GetComponentsInChildren<SchoolPortal>(true)){if(p.portalId.StartsWith("Reading_"))continue;var d=p.Definition;string key=d.id.Contains("Roof")?r.id=="Roof"?"P05_RoofAccess":"P05_IronGate":d.id.StartsWith("Stair_")?"P05_StairUp":"MOD_SCHOOL_DOORS:"+(memory?0:1);if(r.id=="Floor02"&&d.targetRoom=="Floor01")key="P05_Downstairs";float h=key=="P05_RoofAccess"?2.65f:key=="P05_Downstairs"?2.8f:2.35f;float px=p.transform.position.x,foot=f+.25f;if(key=="P05_Downstairs"){px+=1.7f;foot-=1.4f;}Paint(art,"Gateway_"+p.portalId,key,px,foot,h,-11);}
  foreach(var p in root.GetComponentsInChildren<SchoolInteractable>(true)){if(p.name.Contains("Door"))Paint(art,"Gateway_"+p.name,"MOD_SCHOOL_DOORS:"+(memory?0:1),p.transform.position.x,f+.25f,2.35f,-11);}
  if(r.id=="Floor01"){
   Paint(art,"InsideSecurityWindow","P05_Security",x+4.4f,f+1.05f,1.15f,-12);
   Shadow(Paint(art,"LowLocker","P05_ShoeLocker",x+16,f+.23f,.8f));
   var hoop=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Backboard");if(hoop){Paint(art,"CourtHoop","MOD_SCHOOL_SPORTS_SIDE_V02:0",x+36,f+.23f,2.8f,-10);}
   foreach(var n in new[]{"Basketball","BasketKey"}){var t=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==n);if(!t)continue;Paint(t,"P05_Prop",n=="Basketball"?"MOD_SCHOOL_SPORTS:1":"MOD_SCHOOL_SPORTS:2",t.position.x,t.position.y-(n=="Basketball"?.13f:.07f),n=="Basketball"?.25f:.22f,4);}
   if(memory){var a=Paint(art,"StudentCourt_C01","P05_Student:6",x+31,f+.23f,1.72f,-6);Life(a,"C01","dribbling",Frames("P05_Student",6),.34f,1.72f);}
  }
  if(r.id=="Floor02"||r.id.StartsWith("Floor03")){
   for(float t=6;t<w-2;t+=9)Shadow(Paint(art,"Locker_"+t,"P05_ShoeLocker",x+t,f+.23f,.74f));
   if(memory&&r.id=="Floor02"){Life(Paint(art,"Corridor_S01","P05_Student:0",x+18,f+.23f,1.72f,-5),"S01","conversation",Frames("P05_Student",0),.1f,1.72f);Life(Paint(art,"Corridor_S02","P05_Student:3",x+20,f+.23f,1.65f,-5),"S02","conversation",Frames("P05_Student",3),.67f,1.65f);}
  }
  if(r.id=="Class3_2"||r.id=="Class3_1"){
   Paint(art,"Board","MOD_SCHOOL_WINDOWS_BOARDS:0",x+w*.55f,f+1.75f,1.05f,-15);
   for(int i=0;i<3;i++)Shadow(Paint(art,"Desk_"+i,"MOD_SCHOOL_DESK_CHAIR_PAIR_V02:"+(memory?0:1),x+3.3f+i*3.2f,f+.23f,1.05f,-10));
   if(memory&&r.id=="Class3_2")Life(Paint(art,"Seated_S03","P05_Loop:0",x+4.2f,f+.23f,1.31f,-9),"S03","writing",Frames("P05_Loop",0),.42f,1.31f,0);
   if(memory&&r.id=="Class3_1"){var z=root.Find("B5_ZhangRuoyuan");if(z){Paint(z,"P05_Zhang","CHR_SCHOOL_SUPPORT_BASE:0",z.position.x,f+.23f,1.65f,-5);}}
  }
  if(r.id=="SecurityRoom"){
   Paint(art,"Monitoring","P05_Monitors",x+5.2f,f+1.48f,1.4f,-13);Shadow(Paint(art,"OperatingDesk","MOD_SCHOOL_OFFICE_SECURITY:0",x+5.1f,f+.23f,1.15f,-8));Shadow(Paint(art,"FileCabinet","MOD_SCHOOL_OFFICE_SECURITY:1",x+7.6f,f+.23f,1.63f,-9));
  }
  if(r.id=="EquipmentRoom"){
   Shadow(Paint(art,"BallRack","MOD_SCHOOL_SPORTS_SIDE_V02:1",x+4.8f,f+.23f,1.65f));Shadow(Paint(art,"EquipmentCupboard","MOD_SCHOOL_SPORTS:3",x+8.1f,f+.23f,1.72f));var u=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name.Contains("UniformPickup"));if(u)Paint(u,"P05_FoldedUniform","MOD_SCHOOL_FURNITURE_OTHER:3",u.position.x,f+.06f,.27f,1);
  }
  if(r.id=="ReadingPassage"){
   // One open public gallery; quiet floor and corridor cap remain level at both ends.
   for(int i=0;i<3;i++){Shadow(Paint(art,"Bookshelf_"+i,"MOD_SCHOOL_READING_FURNITURE:0",x+2.5f+i*7,f+.23f,2.0f,-10));Shadow(Paint(art,"Plant_"+i,"MOD_SCHOOL_COMMON_DRESSING:0",x+5f+i*7,f+.23f,1.05f,-8));}
   Shadow(Paint(art,"ReadingBench","MOD_SCHOOL_READING_FURNITURE:1",x+7,f+.23f,.84f,-8));Paint(art,"Landscape","BG_SCHOOL_CLASS3_2_MEMORY",x+10.5f,f+1.2f,1.8f,-22);
   if(!memory)Shadow(Paint(art,"FurnitureBlockade","MOD_SCHOOL_READING_BLOCKADE:0",x+11,f+.23f,2.35f,-4));
  }
  if(r.id=="StaffOffice"){
   if(memory){Life(Paint(art,"WritingTeacher_T01","P05_Loop:3",x+4.1f,f+.23f,1.37f,-8),"T01","writing",Frames("P05_Loop",3),.25f,1.37f,0);Life(Paint(art,"TypingTeacher_T02","P05_Loop:6",x+7.8f,f+.23f,1.37f,-8),"T02","typing",Frames("P05_Loop",6),.65f,1.37f,0);var d=root.Find("B5_Director");if(d){Life(Paint(d,"P05_Director","CHR_SCHOOL_SUPPORT_BASE:1",d.position.x,f+.23f,1.8f,-5),"Director","breathing",null,.51f,1.8f);}}
   Shadow(Paint(art,"TeacherDesk","MOD_SCHOOL_OFFICE_SECURITY:0",x+11.2f,f+.23f,1.03f,-9));
  }
  if(r.id=="MaleToilet"){Paint(art,"RestroomFixtures","MOD_SCHOOL_RESTROOM:0",x+4.9f,f+.23f,1.65f,-10);}
 }
}
