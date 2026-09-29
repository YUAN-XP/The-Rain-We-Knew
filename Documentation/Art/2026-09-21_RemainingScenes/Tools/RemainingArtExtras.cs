using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public static partial class RemainingArtBuild
{
 const string LibraryPath="Assets/Art/正式/Effects/RemainingSceneArt.asset";
 public static void ActivityArtwork(){if(Application.isPlaying||SceneManager.GetActiveScene().name!="Scene03_Childhood")throw new Exception("Scene03 edit only");var chapter=UnityEngine.Object.FindAnyObjectByType<Scene03Chapter>(FindObjectsInactive.Include);Ref(Ensure<ChildhoodActivityArtwork>(chapter.gameObject),"art",AssetDatabase.LoadAssetAtPath<PresentationArtLibrary>(LibraryPath));Finish();}
 public static void BuildLibrary(){
  entries=JsonUtility.FromJson<Entries>(File.ReadAllText(RemainingArtEditor.W+"manifest.json")).assets;
  var library=AssetDatabase.LoadAssetAtPath<PresentationArtLibrary>(LibraryPath);
  if(!library){library=ScriptableObject.CreateInstance<PresentationArtLibrary>();AssetDatabase.CreateAsset(library,LibraryPath);}
  var s=new SerializedObject(library);var a=s.FindProperty("regions");a.arraySize=entries.Length;
  for(int i=0;i<entries.Length;i++){var e=entries[i];var r=a.GetArrayElementAtIndex(i);r.FindPropertyRelative("key").stringValue=e.key;r.FindPropertyRelative("source").objectReferenceValue=AssetDatabase.LoadAssetAtPath<Sprite>(e.asset);r.FindPropertyRelative("pixels").rectValue=new Rect(e.rect[0],e.rect[1],e.rect[2],e.rect[3]);}
  s.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
 }
 public static void SchoolDetails(){
  if(Application.isPlaying||SceneManager.GetActiveScene().name!="Scene04_School")throw new Exception("School edit only");
  entries=null;var layout=UnityEngine.Object.FindAnyObjectByType<SchoolLayoutController>(FindObjectsInactive.Include);
  foreach(bool dark in new[]{true,false}){
   string prefix=dark?"School/Otherworld/":"School/Memory/",suffix=dark?"d":"n";
   foreach(var room in layout.data.rooms){
    var art=F(prefix+room.id+"/NewArt_Environment");
    for(int side=0;side<2;side++)V(art,"EndPier_"+side,"school_wall_"+suffix,room.origin+new Vector2(side*room.width,0),.45f,8.6f,-4).color=dark?new Color(.38f,.4f,.48f,1):new Color(.62f,.65f,.63f,1);
    if(room.id=="Roof"){
     foreach(Transform wall in art)if(wall.name.StartsWith("Wall_")){var scale=wall.localScale;scale.y=1.6f/(wall.GetComponent<SpriteRenderer>().sprite.bounds.size.y*wall.parent.lossyScale.y);wall.localScale=scale;}
     for(int n=0;n<3;n++)V(art,"RoofDistance_"+n,"town_day",room.origin+new Vector2(14+n*28,0),28.1f,10,-45).color=dark?new Color(.28f,.29f,.41f,1):new Color(.65f,.7f,.74f,1);
     V(art,"RoofTank","water_tank",room.origin+new Vector2(39,0),5.8f,4.3f,-8).color=dark?new Color(.45f,.49f,.61f,1):Color.white;
    }
    if(room.id=="ReadingPassage"||room.id=="StaffOffice")V(art,"ReadingShelf","school_books",room.origin+new Vector2(room.width-7,0),4.2f,3.9f,-9).color=dark?new Color(.48f,.48f,.61f,1):Color.white;
   }
   var ball=F(prefix+"Floor01/Basketball").GetComponent<SpriteRenderer>();var view=Replace(P(ball.transform),"basketball",new Vector2(ball.bounds.center.x,ball.bounds.min.y),ball.bounds.size.x,ball.bounds.size.y,4);Link(ball,view);
   var window=V(F(prefix+"Floor01"),"NewArt_SecurityWindow","school_window_"+suffix,new Vector2(13,1.2f),3,2.8f,-6);var source=F(prefix+"Floor01/"+(dark?"SecurityWindowInvestigation":"B5_SecurityWindow")).GetComponent<SchoolInteractable>();Highlight(source,new[]{window},dark);
   var computer=F(prefix+"SecurityRoom").Find(dark?"SecurityDesk":"B5_SecurityComputer");if(computer){var visuals=F(prefix+"SecurityRoom").GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name=="NewArt_CCTVDesk").ToArray();foreach(var m in computer.GetComponents<MonoBehaviour>().Where(m=>m is IInteractable))Highlight(m,visuals,dark);}
  }
  Actor("School/Memory/Class3_2/ZhengTao_OpeningOnly","youth_qt",16,1.88f,8);
  var friend=F("School/Memory/Class3_2/ZhengTao_OpeningOnly");Highlight(friend.GetComponentInChildren<SchoolYouthInteractable>(true),new[]{friend.Find("NewArt_Actor").GetComponent<SpriteRenderer>()},false);
  var note=F("School/Memory/Floor01/B5_SecurityNote");var noteView=V(note,"NewArt_Note","postits",new Vector2(note.position.x,1.5f),.35f,.30f,7);Highlight(note.GetComponent<SchoolYouthInteractable>(),new[]{noteView},false);
  Ref(Ensure<SchoolEnemyArtwork>(F("School/SchoolFlow").gameObject),"art",AssetDatabase.LoadAssetAtPath<PresentationArtLibrary>(LibraryPath));
  Finish();
 }
 public static void Ending(){if(Application.isPlaying||SceneManager.GetActiveScene().name!="EndingSequence")throw new Exception("Ending edit only");var d=UnityEngine.Object.FindAnyObjectByType<EndingDirector>(FindObjectsInactive.Include);Ref(Ensure<EndingArtwork>(d.gameObject),"art",AssetDatabase.LoadAssetAtPath<PresentationArtLibrary>(LibraryPath));Finish();}
 public static void SchoolFinalProps(){
  if(Application.isPlaying||SceneManager.GetActiveScene().name!="Scene04_School")throw new Exception("School edit only");entries=null;
  foreach(string prefix in new[]{"School/Otherworld/","School/Memory/"}){
   var root=F(prefix+"Floor01");var art=F(prefix+"Floor01/NewArt_Environment");
   V(art,"BasketSupport","hoop_support",new Vector2(94.785f,0),2.71f,5.06f,-4);
   var board=F(prefix+"Floor01/Backboard").GetComponent<SpriteRenderer>();V(board.transform,"NewArt_Backboard","hoop_board",new Vector2(board.bounds.center.x,board.bounds.min.y),board.bounds.size.x,board.bounds.size.y,-2);
   V(art,"BasketRimNet","hoop_rim",new Vector2(92.646f,3.469f),1.793f,.936f,1);
   var uniform=F(prefix+"EquipmentRoom/UniformOnFloor");var u=Replace(P(uniform),"uniform_drop",new Vector2(uniform.position.x,.04f),1.55f,.65f,4);Link(uniform.GetComponent<SpriteRenderer>(),u);
  }
  Ref(Ensure<RuntimeToolArtwork>(F("School/SchoolFlow").gameObject),"art",AssetDatabase.LoadAssetAtPath<PresentationArtLibrary>(LibraryPath));Finish();
 }
 public static void HomeTool(){if(Application.isPlaying)throw new Exception("Edit only");Ref(Ensure<RuntimeToolArtwork>(F("Gameplay").gameObject),"art",AssetDatabase.LoadAssetAtPath<PresentationArtLibrary>(LibraryPath));var child=F("Gameplay/Scene02_Midgame/ChildShadow_Placeholder");var view=child.Find("NewArt_Visual").GetComponent<SpriteRenderer>();var facing=Ensure<SpriteMotionFacing>(view.gameObject);Ref(facing,"motionSource",child);Ref(facing,"visual",view);Finish();}
 public static void HomeVisibility(){
  if(Application.isPlaying||SceneManager.GetActiveScene().name!="Scene02_Courtyard")throw new Exception("Home edit only");
  foreach(string name in new[]{"Lamp_Placeholder","CameraItem_Placeholder"}){
   var source=F("Environment/MasterBedroomInterior/"+name).GetComponent<SpriteRenderer>();var visual=source.transform.Find("NewArt_Visual").GetComponent<SpriteRenderer>();
   var alpha=visual.GetComponent<SpriteVisibilityAlphaLink>();if(alpha)UnityEngine.Object.DestroyImmediate(alpha);
   var c=visual.color;c.a=1;visual.color=c;visual.enabled=true;visual.forceRenderingOff=false;
   var link=Ensure<RendererVisibilityLink>(visual.gameObject);Ref(link,"source",source);Ref(link,"visual",visual);
  }
  Finish();
 }
 public static void SchoolDepth(){
  if(Application.isPlaying||SceneManager.GetActiveScene().name!="Scene04_School")throw new Exception("School edit only");entries=null;
  var layout=UnityEngine.Object.FindAnyObjectByType<SchoolLayoutController>(FindObjectsInactive.Include);
  foreach(bool dark in new[]{true,false})foreach(var room in layout.data.rooms){
   string prefix=dark?"School/Otherworld/":"School/Memory/",suffix=dark?"d":"n";var art=F(prefix+room.id+"/NewArt_Environment");
   foreach(Transform window in art.Cast<Transform>().Where(t=>t.name.StartsWith("Window_")).ToArray())V(art,window.name,"school_window_"+suffix,new Vector2(window.position.x,room.origin.y+3.1f),4.3f,2.3f,-25);
   if(!room.id.StartsWith("Class"))continue;
   foreach(Transform tile in art.Cast<Transform>().Where(t=>t.name.StartsWith("Floor_")||t.name.StartsWith("Wall_")).ToArray()){
    var sr=tile.GetComponent<SpriteRenderer>();bool wall=tile.name.StartsWith("Wall_");var color=sr.color;bool flip=sr.flipX;
    var r=V(art,tile.name,wall?"school_wall_"+suffix:"school_floor_"+suffix,new Vector2(tile.position.x,room.origin.y+(wall?1.05f:-2)),room.width/Mathf.CeilToInt(room.width/8)+.02f,wall?7.55f:3.08f,wall?-40:-20);r.color=color;r.flipX=flip;
   }
  }
  Finish();
 }
 public static void HomePolish(){
  bool normal=SceneManager.GetActiveScene().name=="Scene03_Childhood";if(Application.isPlaying||(!normal&&SceneManager.GetActiveScene().name!="Scene02_Courtyard"))throw new Exception("Home edit only");
  string prefix=normal?"Environment/Home_Floor01_Normal":"Environment/Floor01";
  foreach(var crop in All().Select(t=>t.GetComponent<SpriteArtRegion>()).Where(c=>c)){
   var sr=crop.GetComponent<SpriteRenderer>();if(!sr.sprite||!sr.sprite.name.Contains("Architecture_N02"))continue;
   var so=new SerializedObject(crop);var rect=so.FindProperty("pixelRegion").rectValue;
   if(rect.width<250){var scale=crop.transform.localScale;scale.y*=2.8f/sr.bounds.size.y;crop.transform.localScale=scale;}
  }
  if(normal){
   V(F(prefix+"/NewArt_RoomShell"),"EntryWindow","window_n",new Vector2(-18,3.7f),3.4f,2.2f,-15);
   var phone=F(prefix+"/电话小柜/NewArt_Visual");var phoneView=phone.GetComponent<SpriteRenderer>();phone.localScale=new Vector3(1.1475f/(phoneView.sprite.bounds.size.x*phone.parent.lossyScale.x),1.189f/(phoneView.sprite.bounds.size.y*phone.parent.lossyScale.y),1);
   foreach(var mesh in All().Select(t=>t.GetComponent<TMPro.TextMeshPro>()).Where(t=>t&&(P(t.transform).StartsWith(prefix)||P(t.transform).StartsWith("Environment/LivingRoomInterior"))))Retire(Child(mesh.transform,"NewArt_RetiredLabel"),mesh.GetComponents<Renderer>());
   Refs(F(prefix+"/NewArt_RoomShell").GetComponent<PlayerRegionVisualGroup>(),"visuals",F(prefix+"/NewArt_RoomShell").GetComponentsInChildren<SpriteRenderer>(true));
   Mask(F(prefix),new Vector2(-40,-4),new Vector2(45,5.8f));
  }
  Finish();
 }
}

