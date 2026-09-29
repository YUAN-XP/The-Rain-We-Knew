using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

public static partial class RemainingArtBuild
{
 public static void SchoolEnvironment(){
  if(Application.isPlaying||SceneManager.GetActiveScene().name!="Scene04_School")throw new Exception("School edit only");
  var layout=UnityEngine.Object.FindAnyObjectByType<SchoolLayoutController>(FindObjectsInactive.Include);
  foreach(bool dark in new[]{true,false}){
   var variant=(dark?layout.otherworldRoot:layout.memoryRoot).transform;string suffix=dark?"d":"n";
   foreach(var room in layout.data.rooms){
    var root=variant.Find(room.id);if(!root)throw new Exception("Missing room "+room.id);
    var old=root.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>!P(r.transform).Contains("NewArt")&&!r.name.Contains("Fog")&&!r.name.Contains("BasketKey")&&r.name!="Basketball"&&r.name!="UniformOnFloor").ToArray();
    Retire(Child(root,"NewArt_Retired"),old);
    var art=Child(root,"NewArt_Environment");float floor=room.origin.y;
    int tiles=Mathf.CeilToInt(room.width/8);float width=room.width/tiles;
    for(int i=0;i<tiles;i++){
     float x=room.origin.x+(i+.5f)*width;
     var wall=V(art,"Wall_"+i,"school_wall_"+suffix,new Vector2(x,floor),width+.02f,8.6f,-40);wall.color=dark?new Color(.52f,.55f,.67f,1):new Color(.90f,.89f,.85f,1);
     V(art,"Floor_"+i,"school_floor_"+suffix,new Vector2(x,floor-2),width+.02f,2.06f,-20).color=dark?new Color(.55f,.6f,.72f,1):new Color(.85f,.86f,.84f,1);
    }
    var doorXs=layout.data.portals.Where(p=>p.sourceRoom==room.id).Select(p=>room.origin.x+p.sourceLocal.x).ToArray();
    if(room.id!="Roof"){
     for(float x=room.origin.x+7;x<room.origin.x+room.width-3;x+=8){
      if(doorXs.Any(d=>Mathf.Abs(d-x)<3.5f))continue;
      V(art,"Window_"+x,"school_window_"+suffix,new Vector2(x,floor+2.6f),4.6f,3.25f,-25);
     }
    }
    // Reuse the exact door positions and states of the shared school data.
    foreach(var definition in layout.data.portals.Where(p=>p.sourceRoom==room.id)){
     var anchor=root.Find(definition.id);if(!anchor)continue;
     bool stair=definition.id.StartsWith("Stair_");bool closed=(dark?definition.otherworld:definition.memory)==SchoolPortalGate.Closed;
     var door=V(anchor,"NewArt_Door",stair&&!closed?"school_stairs_"+suffix:"school_door_"+suffix,new Vector2(anchor.position.x,floor),stair?3.7f:1.8f,stair?4.1f:2.9f,-6);
     var interaction=anchor.GetComponents<MonoBehaviour>().FirstOrDefault(m=>m is IInteractable);if(interaction)Highlight(interaction,new[]{door},dark);
    }
    foreach(Transform t in root.Cast<Transform>().ToArray()){
     if(t.name.StartsWith("Desk_")||t.name=="ReadingDesk")V(t,"NewArt_Desk","school_desks_"+suffix,new Vector2(t.position.x,floor),3,1.65f,-3);
     if(t.name=="TeacherDesk")V(t,"NewArt_OfficeDesk","school_office_"+suffix,new Vector2(t.position.x,floor),4.7f,3.6f,-5);
     if(t.name=="SecurityDesk")V(t,"NewArt_CCTVDesk","school_cctv_"+suffix,new Vector2(t.position.x,floor),4.8f,3.6f,-5);
     if(t.name=="Blackboard")V(t,"NewArt_Board","school_board_"+suffix,new Vector2(t.position.x,floor+3),7,3,-10);
    }
    if(room.id.StartsWith("Class")){
     // A quieter distant desk row adds depth without covering the playable lane.
     for(int i=0;i<4;i++)V(art,"BackDesk_"+i,"school_desks_"+suffix,new Vector2(room.origin.x+9+i*6.6f,floor+.9f),2.45f,1.35f,-12).color=dark?new Color(.48f,.5f,.61f,1):new Color(.73f,.77f,.79f,1);
     if(!dark){
      V(art,"StudentReading","student_read",new Vector2(room.origin.x+9,floor+.95f),0,1.7f,-11);
      V(art,"StudentSleeping","student_sleep",new Vector2(room.origin.x+29,floor+.95f),0,1.6f,-11);
      V(art,"QuietConversation","students_chat",new Vector2(room.origin.x+6,floor),0,1.85f,-2);
     }
    }
    if(room.id=="EquipmentRoom")for(int n=0;n<3;n++)V(art,"SportsRack_"+n,"school_sports_"+suffix,new Vector2(room.origin.x+8+n*7,floor),4.5f,3.5f,-6);
    if(room.id=="MaleToilet")for(int n=0;n<3;n++)V(art,"Toilets_"+n,"school_toilet_"+suffix,new Vector2(room.origin.x+8+n*4,floor),3.8f,2.7f,-6);
    if(!dark&&room.id=="Floor02"){
     var crowd=root.Find("B5_Crowd");if(crowd){var v=V(crowd,"NewArt_Crowd","students_chat",new Vector2(crowd.position.x,floor),4.3f,2,-1);foreach(var m in crowd.GetComponentsInChildren<MonoBehaviour>(true).Where(m=>m is IInteractable))Highlight(m,new[]{v},false);}
     V(art,"CorridorStudents","students_back",new Vector2(room.origin.x+24,floor),0,1.85f,-2);
    }
    if(room.id!="Roof")Atmosphere(root,room.origin+new Vector2(-1,-2),room.origin+new Vector2(room.width+1,10),floor,dark,room.origin+new Vector2(7,3),room.origin+new Vector2(room.width-9,3));
    // Preserve the actual readable room names and floor signs, hide only debug captions.
    foreach(var text in root.GetComponentsInChildren<TMPro.TextMeshPro>(true))if(text.transform.parent==root&&(text.text=="黑雾"||text.text=="篮球场"))Retire(Child(text.transform,"NewArt_RetiredLabel"),text.GetComponents<Renderer>());
   }
   foreach(var action in variant.GetComponentsInChildren<SchoolInteractable>(true)){
    if(action.action==SchoolAction.OwnDesk||action.action==SchoolAction.ScratchedDesk){var nearest=action.transform.parent.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name=="NewArt_Desk").OrderBy(r=>Mathf.Abs(r.transform.position.x-action.transform.position.x)).FirstOrDefault();if(nearest)Highlight(action,new[]{nearest},dark);}
   }
  }
  Actor("School/SchoolFlow/YouthXiaoPeng","youth_xp",16,1.85f,12);
  Actor("School/Memory/Class3_1/B5_ZhangRuoyuan","zhangruoyuan",16,1.78f,8);
  Actor("School/Memory/StaffOffice/B5_Director","director",32,1.92f,8);
  Finish();
 }
}
