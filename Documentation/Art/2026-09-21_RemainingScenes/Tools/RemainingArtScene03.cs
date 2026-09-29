using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public static partial class RemainingArtBuild
{
 static SpriteRenderer Actor(string path,string key,float foot,float height,int order=8){var t=F(path);var old=t.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>!P(r.transform).Contains("NewArt")&&!r.name.Contains("Guitar")&&!r.name.Contains("FishingRod")).ToArray();var view=V(t,"NewArt_Actor",key,new Vector2(t.position.x,foot),0,height,order);Retire(view.transform,old);var interaction=t.GetComponents<MonoBehaviour>().FirstOrDefault(m=>m is IInteractable);if(interaction)Highlight(interaction,new[]{view},false);return view;}
 public static void Scene03Cast(){
  if(SceneManager.GetActiveScene().name!="Scene03_Childhood"||Application.isPlaying)throw new Exception("Scene03 edit only");
  Actor("YoungXiaoPeng/幼年肖鹏","child_xp",.53f,1.42f,10);
  Actor("Environment/LivingRoomInterior/LivingRoomFurniture/母亲","mother",.5f,1.8f);
  Actor("Environment/LivingRoomInterior/LivingRoomFurniture/父亲","father",.5f,1.92f);
  Actor("Riverbank/Boy_WaitingWithGuitar","child_qt",70.5f,1.58f,10);
  Actor("Riverbank/Zhao_RiverVisit","zhaobingcheng",70.5f,1.65f,10);
  Replace("Riverbank/Boy_WaitingWithGuitar/FathersNewGuitar","guitar",new Vector2(301.45f,70.7f),.45f,1.15f,12);
  var neck=F("Riverbank/Boy_WaitingWithGuitar/GuitarNeck");Retire(Child(neck,"NewArt_Retired"),neck.GetComponents<SpriteRenderer>());
  var parents=F("Environment/LivingRoomInterior/LivingRoomFurniture/S03_Parents").GetComponent<Scene03Interactable>();
  Highlight(parents,new[]{F("Environment/LivingRoomInterior/LivingRoomFurniture/母亲/NewArt_Actor").GetComponent<SpriteRenderer>()},false);
  Finish();
 }
 static void Finish(){EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());RemainingArtEditor.Dump();}
 public static void Scene03Street(){
  if(SceneManager.GetActiveScene().name!="Scene03_Childhood"||Application.isPlaying)throw new Exception("Scene03 edit only");
  string path="Environment/ChildhoodStreet";var root=F(path);
  var originals=root.GetComponentsInChildren<Renderer>(true).Where(r=>!P(r.transform).Contains("NewArt")&&!r.name.Contains("游戏币")).ToArray();Retire(Child(root,"NewArt_RetiredStreet"),originals);
  var art=Child(root,"NewArt_Street");
  for(int i=0;i<23;i++)V(art,"Pavement_"+i,"floor_n",new Vector2(-35+(i+.5f)*8,24.5f),8.025f,6.05f,-20).color=new Color(.86f,.88f,.91f,1);
  for(int i=0;i<4;i++)V(art,"FarTown_"+i,"town_day",new Vector2(-35+(i+.5f)*52,30.4f),52.025f,10.45f,-45);
  foreach(var b in new[]{("LiMei","facade_home",-15f,7.8f),("Shop","facade_shop",-5f,9.5f),("ChessHome","facade_chess",27f,9.5f),("Restaurant","facade_food",47f,11f),("ZhaoHome","facade_zhao",61f,10f)})V(art,b.Item1,b.Item2,new Vector2(b.Item3,30.5f),b.Item4,7.3f,-12);
  V(art,"NormalGate","street_gate",new Vector2(15,30.5f),6.2f,3.9f,-8);
  for(int i=0;i<3;i++)V(art,"AlleyWall_"+i,"facade_alley",new Vector2(118+i*9,30.5f),9.1f,8,-12).color=Color.Lerp(Color.white,new Color(.70f,.72f,.8f),i*.23f);
  foreach(float x in new[]{-21f,2f,10f,20.5f,33f,56f,66f,93f})V(art,"Planter_"+x,"plant",new Vector2(x,30.5f),1,1.4f,-5);
  Actor(path+"/李梅","limei",30.5f,1.82f,6);
  var shop=V(F(path+"/S03_Shop"),"NewArt_Vendor","shop_vendor",new Vector2(-4.6f,30.5f),2.05f,2.1f,3);Highlight(F(path+"/S03_Shop").GetComponent<DialogueInteractable>(),new[]{shop},false);
  var chess=V(F(path+"/S03_Chess"),"NewArt_ChessGroup","chess_group",new Vector2(27,30.5f),2.6f,2.1f,4);Highlight(F(path+"/S03_Chess").GetComponent<Scene03Interactable>(),new[]{chess},false);
  Actor(path+"/王平","wangping",30.5f,1.85f,5);
  Actor(path+"/赵炳程","zhaobingcheng",30.5f,1.6f,7);
  Actor(path+"/套圈老板","rings_vendor",30.5f,1.86f,7);
  Actor(path+"/说书人","storyteller",30.5f,1.85f,7);
  V(F(path+"/S03_Restaurant"),"NewArt_Diners","diners",new Vector2(47,30.6f),3.25f,1.8f,3);
  V(art,"ChattingNeighbours","neighbours",new Vector2(21,30.5f),1.85f,1.85f,4);
  V(art,"StoryAudience","neighbours",new Vector2(-30,30.5f),1.85f,1.8f,4).color=new Color(.88f,.89f,.91f,1);
  Replace(path+"/小狗","dog",new Vector2(7,30.5f),1,0,4);
  Replace(path+"/踩水的孩子","puddle_child",new Vector2(1.3f,30.5f),0,1.25f,4);
  var cat=F(path+"/S03_Cat/小猫");var sleeping=V(cat,"NewArt_CurledCat","cat_sleep",new Vector2(37,30.5f),.9f,0,7);var standing=V(cat,"NewArt_StandingCat","cat_stand",new Vector2(37,30.5f),1,0,7);standing.gameObject.SetActive(false);
  var choice=Ensure<StoryArtworkChoice>(cat.gameObject);Ref(choice,"before",sleeping.gameObject);Ref(choice,"after",standing.gameObject);var cs=new SerializedObject(choice);cs.FindProperty("flag").intValue=(int)StoryFlag.CatFed;cs.ApplyModifiedPropertiesWithoutUndo();Highlight(F(path+"/S03_Cat").GetComponent<Scene03Interactable>(),new[]{sleeping,standing},false);
  V(art,"Pond","pond",new Vector2(75,30.1f),9,3.3f,-9);
  var notice=V(F(path+"/S03_Bulletin"),"NewArt_Noticeboard","noticeboard",new Vector2(87,30.5f),3.8f,3.1f,-5);Highlight(F(path+"/S03_Bulletin").GetComponent<Scene03Interactable>(),new[]{notice},false);
  V(art,"Prizes","ring_prizes",new Vector2(103,30.5f),3.4f,1.25f,3);
  V(art,"RiversideSign","signpost",new Vector2(109,30.5f),1.2f,3.1f,-5);
  var arrow=All().FirstOrDefault(t=>P(t)==path+"/河堤 →");if(arrow){var mr=arrow.GetComponent<Renderer>();if(mr){mr.forceRenderingOff=false;var owner=F(path+"/NewArt_RetiredStreet").GetComponent<RetiredArtVisibility>();Refs(owner,"renderers",originals.Where(r=>r!=mr&& !r.transform.IsChildOf(arrow)));}}
  Atmosphere(root,new Vector2(-35,24),new Vector2(143,46),30.5f,false,new Vector2(-5,33),new Vector2(27,33),new Vector2(47,33),new Vector2(75,33));
  Finish();
 }
}
