using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Linq;
using System;
public static partial class P05ProductionBuild {
 static void ReplaceNative(string key,string original,float bottomFraction=0,float topFraction=0){var source=S(original);var rect=source.rect;rect.y+=rect.height*bottomFraction;rect.height*=1-bottomFraction-topFraction;var sprite=Sprite.Create(source.texture,rect,new Vector2(.5f,0),256,0,SpriteMeshType.FullRect);sprite.name=key;AssetDatabase.AddObjectToAsset(sprite,catalog);var e=catalog.entries.FirstOrDefault(x=>x.key==key);if(e!=null)e.sprite=sprite;else catalog.entries=catalog.entries.Concat(new[]{new DemoArtCatalog.Entry{key=key,sprite=sprite}}).ToArray();typeof(DemoArtCatalog).GetField("lookup",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(catalog,null);}
 public static void Refinements(){if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit mode required");catalog=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>("Assets/Resources/DemoArtCatalog.asset");
 ReplaceNative("P05_WallMemory","MOD_SCHOOL_LOW_SHELL_V02:0",.18f);ReplaceNative("P05_WallOther","MOD_SCHOOL_LOW_SHELL_V02:1",.18f);ReplaceNative("P05_CourtWallMemory","MOD_SCHOOL_COURTYARD_SHELL_MEMORY",.283f,.112f);ReplaceNative("P05_CourtWallOther","MOD_SCHOOL_COURTYARD_SHELL_OTHER",.283f,.112f);ReplaceNative("P05_OilJar","SPR_SCHOOL_PICKUPS_SMALL:lamp oil");ReplaceNative("P05_Anonymous_CLOSEUP","P05_Anonymous");
 EditorSceneManager.OpenScene("Assets/Scenes/Scene04_School.unity");layout=UnityEngine.Object.FindAnyObjectByType<SchoolLayoutController>();var flow=UnityEngine.Object.FindAnyObjectByType<SchoolSceneController>();
 foreach(var v in new[]{layout.otherworldRoot,layout.memoryRoot})foreach(var r in layout.data.rooms){bool m=v==layout.memoryRoot;var root=v.transform.Find(r.id);if(!root)continue;var art=root.Find("P05_Artwork");float x=r.origin.x,f=r.origin.y,w=r.width;
 foreach(var strip in art.GetComponentsInChildren<P03RepeatedRoomStrip>()){if(strip.name=="SchoolWall"){strip.source=S(m?"P05_WallMemory":"P05_WallOther");strip.height=4.1f;strip.transform.position=new Vector3(strip.transform.position.x,f+.25f,0);strip.Rebuild();}if(strip.name=="CourtTrees"){strip.tint=m?Color.white:new Color(.32f,.39f,.46f);strip.height=4.1f;strip.transform.position=new Vector3(strip.transform.position.x,f+.25f,0);strip.Rebuild();}}
 if(r.id=="Floor01"){
  Strip(art,"CourtBoundaryWall",m?"P05_CourtWallMemory":"P05_CourtWallOther",x+34,f+.25f,20.08f,1.05f,-25,3.5f);
  Shadow(Paint(art,"BuildingCornerPlant","MOD_SCHOOL_COMMON_DRESSING:2",x+24,f+.25f,1.55f,-12));
  Paint(art,"CourtHoop","MOD_SCHOOL_SPORTS_SIDE_V02:"+(m?0:1),x+34.4f,f+.23f,2.8f,-10);
  var b=root.Find("Basketball");if(b)Paint(b,"P05_Prop","MOD_SCHOOL_SPORTS_SIDE_V02:"+(m?2:3),b.position.x,b.position.y-.13f,.26f,4);
  var k=root.Find("BasketKey");if(k){foreach(var rr in k.GetComponentsInChildren<SpriteRenderer>(true))if(rr.transform==k||rr.name!="P05_Prop")Hide(rr);var key=Paint(k,"P05_Prop","SPR_PICKUPS_KEYS_FISH_COIN:school_equipment_key",k.position.x,k.position.y-.07f,.18f,4);if(!m){var source=k.GetComponent<SpriteRenderer>(); source.enabled=true;flow.basketKeyVisual=source;var vis=Get<SpriteVisibilityAlphaLink>(key.transform);Field(vis,"source",q=>q.objectReferenceValue=source);Field(vis,"visual",q=>q.objectReferenceValue=key);}}
  if(m){var c=art.Find("StudentCourt_C01");if(c){var ball=Paint(c,"DribbleBall","MOD_SCHOOL_SPORTS_SIDE_V02:2",c.position.x-.28f,f+.45f,.24f,-4);Get<P05DribbleBall>(ball.transform).owner=c.GetComponent<P05PaintedLoop>();}}
 }
 if(r.id=="Class3_2"||r.id=="Class3_1"){
  Paint(art,"Board","MOD_SCHOOL_WINDOWS_BOARDS:"+(m?2:3),x+w*.55f,f+1.75f,1.05f,-15);
  if(m&&r.id=="Class3_2"){var d=art.Find("Desk_0");if(d)d.gameObject.SetActive(false);}
  if(m&&r.id=="Class3_1"){var z=root.Find("B5_ZhangRuoyuan/P05_Zhang");if(z){Life(z.GetComponent<SpriteRenderer>(),"ZhangRuoyuan","breathing",null,.24f,1.65f);}}
 }
 if(r.id=="SecurityRoom"){Paint(art,"OperatingDesk","MOD_SCHOOL_OFFICE_SECURITY:"+(m?0:1),x+4.7f,f+.23f,1.15f,-8);Paint(art,"FileCabinet","MOD_SCHOOL_OFFICE_SECURITY:"+(m?2:3),x+7.4f,f+.23f,1.48f,-9);}
 if(r.id=="EquipmentRoom"){Paint(art,"BallRack","MOD_SCHOOL_SPORTS:"+(m?2:3),x+4.9f,f+.23f,1.5f,-10);Paint(art,"EquipmentCupboard","MOD_SCHOOL_OFFICE_SECURITY:"+(m?2:3),x+8.3f,f+.23f,1.62f,-10);if(!m&&flow.uniformVisual){Retire(flow.uniformVisual.transform);Paint(flow.uniformVisual.transform,"P05_FoldedUniform","LEGACY:uniform_drop",x+3.1f,f+.12f,.34f,-5);}}
 if(r.id=="ReadingPassage"){
  var bad=art.Find("Landscape");if(bad)bad.gameObject.SetActive(false);
  for(int i=0;i<3;i++){Paint(art,"Bookshelf_"+i,"MOD_SCHOOL_READING_FURNITURE:"+(m?0:1),x+2.5f+i*7,f+.23f,1.95f,-10);Paint(art,"Plant_"+i,"MOD_SCHOOL_COMMON_DRESSING:2",x+5+i*7,f+.23f,1.05f,-8);}
  Paint(art,"ReadingBench","MOD_SCHOOL_READING_FURNITURE:"+(m?2:3),x+7,f+.23f,.74f,-8);
  foreach(float wx in new[]{10.5f,15f})Paint(art,"Window_"+wx,"MOD_SCHOOL_WINDOWS_BOARDS:"+(m?0:1),x+wx,f+1.45f,1.4f,-17);
  if(m){var l=Paint(art,"ReadingStudent_R01","P05_Student:3",x+17.2f,f+.23f,1.65f,-6);Life(l,"R01","reading",Frames("P05_Student",3),.91f,1.65f);}
 }
 if(r.id=="StaffOffice")Paint(art,"TeacherDesk","MOD_SCHOOL_OFFICE_SECURITY:"+(m?0:1),x+11.5f,f+.23f,1.03f,-9);
 if(r.id=="Roof"){foreach(var sp in root.GetComponentsInChildren<SchoolPortal>(true))if(sp.portalId.Contains("Roof_Left")||sp.portalId.Contains("Roof_Right")){var dir=root.Find("P05_Artwork/Gateway_"+sp.portalId);if(dir){dir.position=new Vector3(sp.transform.position.x,f+.25f,0);}}}
 }
 // All scene transitions, including legacy equipment/security doors, use the same compact destinations.
 foreach(var i in layout.GetComponentsInChildren<SchoolInteractable>(true))if(i.transition&&!string.IsNullOrEmpty(i.destinationRoomId)){var dst=layout.data.Room(i.destinationRoomId);if(dst!=null){Field(i.transition,"destinationCameraLimits",q=>q.vector2Value=dst.CameraX);Field(i.transition,"destinationVerticalCameraLimits",q=>q.vector2Value=Vector2.one*dst.CameraY);}}
 var basket=flow.basketball;Field(basket,"worldCamera",q=>q.objectReferenceValue=Camera.main);var bso=new SerializedObject(basket);
 var board=bso.FindProperty("backboard").objectReferenceValue as BoxCollider2D;if(board){board.transform.position=new Vector3(74.24f,3.25f,0);board.transform.localScale=Vector3.one;board.size=new Vector2(.1f,.8f);board.offset=Vector2.zero;}
 var hoop=bso.FindProperty("hoopCenter").objectReferenceValue as Transform;if(hoop)hoop.position=new Vector3(73.96f,3.04f,0);
 flow.basketKeyOnHoop.position=new Vector3(73.96f,3.10f,0);flow.basketKeyOnGround.position=new Vector3(72.3f,.64f,0);
 var rest=bso.FindProperty("ballRestPoint").objectReferenceValue as Transform;if(rest)rest.position=new Vector3(74.4f,.64f,0);
 var frame=bso.FindProperty("cameraFrame").objectReferenceValue as Transform;if(frame)frame.position=new Vector3(71.3f,2.10f,-10);
 var hold=Get<P05BasketHand>(basket.transform);hold.basket=basket;hold.ball=bso.FindProperty("ball").objectReferenceValue as Rigidbody2D;hold.launch=bso.FindProperty("launchPoint").objectReferenceValue as Transform;
 var chair=flow.youthFlow.readingChair;chair.wideSize=3.65f;chair.restingMusic=AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Stage2Placeholder/GuitarPlaceholder.wav");chair.seat.position=new Vector3(33,33.05f,0);chair.standPosition.position=new Vector3(31.9f,33.2f,0);
 Get<P05ReadingWalk>(layout.transform).school=flow;
 EditorUtility.SetDirty(catalog);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");BasketballSpacing();CampusPopulation();Debug.Log("P05 Refinements saved");
 }
 public static void CampusPopulation(){
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit mode required");
  catalog=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>("Assets/Resources/DemoArtCatalog.asset");
  EditorSceneManager.OpenScene("Assets/Scenes/Scene04_School.unity");layout=UnityEngine.Object.FindAnyObjectByType<SchoolLayoutController>();
  var art=layout.memoryRoot.transform.Find("Floor02/P05_Artwork");
  var sources=new[]{"GR_SCHOOL_STUDENTS_A:0","GR_SCHOOL_STUDENTS_A:2","GR_SCHOOL_STUDENTS_B:0","GR_SCHOOL_STUDENTS_B:2"};
  var xs=new[]{8.8f,11.1f,24.8f,27.5f};var hs=new[]{1.70f,1.74f,1.68f,1.72f};
  for(int i=0;i<4;i++){string id="S"+(i+4).ToString("00");var r=Paint(art,"Corridor_"+id,sources[i],xs[i],16.23f,hs[i],-5);Life(r,id,"conversation",null,.17f+i*.213f,hs[i]);r.GetComponent<P05PaintedLoop>().period=4.65f+i*.37f;}
  var court=layout.memoryRoot.transform.Find("Floor01/P05_Artwork/StudentCourt_C01");if(court)court.GetComponent<P05PaintedLoop>().period=.96f;
  var reading=layout.memoryRoot.transform.Find("ReadingPassage/P05_Artwork");
  var student=Paint(reading,"ReadingStudent_R01","GR_SCHOOL_STUDENTS_B:1",43.2f,32.23f,1.65f,-6);Life(student,"R01","reading",null,.91f,1.65f);
  EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");
  Debug.Log("P05 unique corridor population and distinct reading identity saved");
 }

 public static void CourtWallCrop(){
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit mode required");
  catalog=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>("Assets/Resources/DemoArtCatalog.asset");
  ReplaceNative("P05_CourtWallMemory","MOD_SCHOOL_COURTYARD_SHELL_MEMORY",.283f,.112f);ReplaceNative("P05_CourtWallOther","MOD_SCHOOL_COURTYARD_SHELL_OTHER",.283f,.112f);
  EditorSceneManager.OpenScene("Assets/Scenes/Scene04_School.unity");layout=UnityEngine.Object.FindAnyObjectByType<SchoolLayoutController>();
  foreach(var v in new[]{layout.otherworldRoot,layout.memoryRoot}){var strip=v.transform.Find("Floor01/P05_Artwork/CourtBoundaryWall").GetComponent<P03RepeatedRoomStrip>();strip.source=S(v==layout.memoryRoot?"P05_CourtWallMemory":"P05_CourtWallOther");strip.Rebuild();}
  EditorUtility.SetDirty(catalog);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());AssetDatabase.SaveAssets();EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");
 }

}
