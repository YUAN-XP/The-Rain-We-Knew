using UnityEngine;
// Frozen P05 coordinate contract, also used to migrate old save positions.
public static class P05SchoolCoordinates {
 public const int Revision=1;
 public static Vector2 Origin(string id,Vector2 old)=>id=="Floor01"?new Vector2(38,.5f):id=="ReadingPassage"?new Vector2(26,32):old;
 public static float Width(string id,float old)=>id=="Floor01"?44:id=="Floor02"?36:id=="ReadingPassage"?22:id=="SecurityRoom"?9:id=="EquipmentRoom"?11:id=="Class3_2"?14:id=="Class3_1"?16:id=="MaleToilet"?9:id=="StaffOffice"?14:old;
 public static float Local(string id,float x){switch(id){
 case "Floor01":return Map(x,new[]{0f,18,38,60,72,83,93,112},new[]{0f,6,12,20,24,30,36,44});
 case "Floor02":return Map(x,new[]{0f,4,16,30,40,52,60,68,80},new[]{0f,2,7,13,17,23,27,31,36});
 case "SecurityRoom":return Map(x,new[]{0f,2,12,20,24},new[]{0f,1,4.6f,7.4f,9});
 case "EquipmentRoom":return x*11/28;
 case "Class3_2":return Map(x,new[]{0f,1,19,20},new[]{0f,1,13,14});
 case "Class3_1":return Map(x,new[]{0f,2,38,40},new[]{0f,1,15,16});
 case "MaleToilet":return x*9/22;
 case "StaffOffice":return x*14/32;
 case "ReadingPassage":return x*22/28;
 default:return x;}}
 static float Map(float x,float[] a,float[] b){int i=0;while(i<a.Length-2&&x>a[i+1])i++;return Mathf.LerpUnclamped(b[i],b[i+1],(x-a[i])/(a[i+1]-a[i]));}
 public static Vector2 OldOrigin(string id)=>id=="ReadingPassage"?new Vector2(200,64):id=="SecurityRoom"?new Vector2(200,0):id=="EquipmentRoom"?new Vector2(240,0):id=="Class3_2"?new Vector2(200,16):id=="Class3_1"?new Vector2(260,16):id=="MaleToilet"?new Vector2(320,16):id=="StaffOffice"?new Vector2(260,32):id=="Floor02"?new Vector2(0,16):id=="Floor03Left"?new Vector2(0,32):id=="Floor03Right"?new Vector2(48,32):id=="Roof"?new Vector2(0,48):Vector2.zero;
 public static void Migrate(GameSaveData save){if(save.schoolLayoutRevision>=Revision)return;
  if(save.school!=null&&save.school.present){var o=OldOrigin(save.school.room);var n=Origin(save.school.room,o);var v=(Vector2)save.position-o;save.position=new Vector3(n.x+Local(save.school.room,v.x),n.y+v.y,save.position.z);var c=(Vector2)save.cameraPosition-o;save.cameraPosition=new Vector3(n.x+Local(save.school.room,c.x),n.y+c.y,save.cameraPosition.z);save.cameraX=new Vector2(n.x+Local(save.school.room,save.cameraX.x-o.x),n.x+Local(save.school.room,save.cameraX.y-o.x));save.cameraY+=Vector2.one*(n.y-o.y);}
  if(save.story!=null&&save.story.hasSchoolPosition)save.story.schoolLocal=new Vector2(Local(save.story.schoolRoom,save.story.schoolLocal.x),save.story.schoolLocal.y);
  save.schoolLayoutRevision=Revision;
 }
}
