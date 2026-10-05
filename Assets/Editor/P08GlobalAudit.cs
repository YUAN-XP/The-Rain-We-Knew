using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class P08GlobalAudit {
 const string W="D:/游戏设计/Codex工作区/work/2026-10-05-P08/checks/global/";
 static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
 static Transform[] All()=>SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
 static readonly List<string> checks=new List<string>();
 static void Check(bool ok,string name){checks.Add((ok?"PASS ":"FAIL ")+name);}
 static void View(Camera cam,string label,float x,float y,float size=2.5f){cam.transform.position=new Vector3(x,y,-10);cam.orthographicSize=size;cam.aspect=16f/9;P03SceneEvidence.Render(cam,W+label+".png",1280,720);}
 public static void Run(){
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit mode required");Directory.CreateDirectory(W);checks.Clear();
  foreach(var entry in EditorBuildSettings.scenes.Where(s=>s.enabled)){
   var scene=EditorSceneManager.OpenScene(entry.path);var ts=All();var lines=new List<string>{DateTime.Now.ToString("O"),"EDITOR observation only; temporary visibility/camera settings discarded without scene save"};
   Check(ts.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))==0,scene.name+" missing scripts zero");
   Check(!ts.Any(t=>t.name.StartsWith("P08 opt-in")||t.GetComponent<P01ReplayDriver>()),scene.name+" no serialized replay driver");
   foreach(var t in ts){
    var sr=t.GetComponent<SpriteRenderer>();if(sr&&sr.sprite)lines.Add("SPRITE "+PathOf(t)+" | active="+t.gameObject.activeInHierarchy+" enabled="+sr.enabled+" retired="+sr.forceRenderingOff+" order="+sr.sortingOrder+" bounds="+sr.bounds.ToString("F4")+" asset="+AssetDatabase.GetAssetPath(sr.sprite));
    var c=t.GetComponent<Collider2D>();if(c)lines.Add("COLLIDER "+PathOf(t)+" | enabled="+c.enabled+" active="+t.gameObject.activeInHierarchy+" trigger="+c.isTrigger+" bounds="+c.bounds.ToString("F4"));
    var rb=t.GetComponent<RoomCameraBounds>();if(rb)lines.Add("BOUNDS "+PathOf(t)+" | "+EditorJsonUtility.ToJson(rb));
    var rain=t.GetComponent<WindowRainPresentation>();if(rain)lines.Add("WINDOW_RAIN "+PathOf(t)+" | "+EditorJsonUtility.ToJson(rain));
    var frame=t.GetComponent<RoomPresentationFraming>();if(frame){lines.Add("FRAMING "+PathOf(t)+" | "+EditorJsonUtility.ToJson(frame));Check(frame.rooms!=null&&frame.rooms.All(r=>r.area.width>0&&r.area.height>0&&r.size>0),scene.name+" valid authored room rectangles");}
    var audio=t.GetComponent<AudioSource>();if(audio)lines.Add("AUDIO "+PathOf(t)+" | loop="+audio.loop+" clip="+(audio.clip?AssetDatabase.GetAssetPath(audio.clip):"none"));
   }
   File.WriteAllLines(W+scene.name+"_surfaces_and_limits.txt",lines);var cam=Camera.main;if(!cam)continue;
   if(scene.name=="Scene01_Rain"){foreach(float x in new[]{-29f,-18f,-8f,0f,8f,17f,28f,37f})View(cam,"Rain_"+x,x,.35f);}
   if(scene.name=="Scene02_Courtyard"){
    foreach(float x in new[]{-10.5f,-8f,0f,7.8f})View(cam,"Courtyard_First_"+x,x,1.85f);
    foreach(float x in new[]{-13f,-4f,5f})View(cam,"Courtyard_Second_"+x,x,7.88f);
    View(cam,"Courtyard_Master",70.3f,1.85f);View(cam,"Courtyard_SecondBed",132f,1.85f);
    var living=ts.Single(t=>t.name=="LivingRoomInterior_Return");living.gameObject.SetActive(true);View(cam,"Courtyard_ReturnLiving",351f,1.85f);
   }
   if(scene.name=="Scene03_Childhood"){
    foreach(float x in new[]{-10f,1f})View(cam,"ChildHome_"+x,x,1.85f);
    View(cam,"ChildMaster",73.5f,1.85f);View(cam,"ChildSecondBed",134.5f,1.85f);View(cam,"ChildLiving",201f,1.85f);
    ts.Single(t=>t.name=="ChildhoodStreet").gameObject.SetActive(true);foreach(float x in new[]{-28f,-16f,25.9f,47f,63f,67f,72f,78f,96.4f,105f})View(cam,"ChildStreet_"+x,x,31.56f);
    View(cam,"ChildRiverNear",303f,72.06f,2.5f);View(cam,"ChildRiverWide",303f,75.62f,7.1f);
    var gate=ts.Single(t=>t.name=="S03_Gate");Check(gate.gameObject.layer==6,scene.name+" original home-return gate included in interaction mask");
   }
   if(scene.name=="Scene04_School"){
    var layout=ts.Select(t=>t.GetComponent<SchoolLayoutController>()).First(c=>c);
    foreach(var variant in new[]{layout.otherworldRoot,layout.memoryRoot}){
     layout.otherworldRoot.SetActive(variant==layout.otherworldRoot);layout.memoryRoot.SetActive(variant==layout.memoryRoot);
     foreach(var room in layout.data.rooms){
      float half=2.5f*16f/9;float l=room.origin.x+Mathf.Min(room.width*.5f,half),r=room.origin.x+room.width-Mathf.Min(room.width*.5f,half);
      float y=room.origin.y+1.35f;
      foreach(var point in new[]{("left",l),("right",r)})View(cam,"School_"+variant.name+"_"+room.id+"_"+point.Item1,point.Item2,y);
      var root=variant.transform.Find(room.id);Check(root,variant.name+" actual room root "+room.id);
      Check(root&&root.GetComponentsInChildren<Collider2D>(true).Any(c=>c.enabled&&!c.isTrigger&&c.bounds.size.x>room.width*.5f),variant.name+" supporting floor collider "+room.id);
     }
     View(cam,"School_"+variant.name+"_OutdoorCourt",69.9f,2.1f,2.6f);
    }
   }
  }
  EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");File.WriteAllLines(W+"checks.txt",checks);File.WriteAllText(W+"summary.txt",DateTime.Now.ToString("O")+" checks="+checks.Count+" failures="+checks.Count(s=>s.StartsWith("FAIL")));if(checks.Any(s=>s.StartsWith("FAIL")))throw new Exception("P08 global checks failed; inspect evidence");
 }
}