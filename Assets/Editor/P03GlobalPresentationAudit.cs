using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class P03GlobalPresentationAudit
{
 [Serializable] public class View {public string path,sprite,asset;public bool active,enabled,hidden;public int order;public Bounds bounds;}
 [Serializable] public class Part {public string scene,time;public List<View> surfaces=new List<View>();public List<string> geometry=new List<string>();}
 public static void Run()
 {
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Requires clean edit mode");
  string root=P03Regression.Work+"global_audit/";Directory.CreateDirectory(root);
  foreach(string scene in new[]{"Scene01_Rain","Scene02_Courtyard","Scene03_Childhood","Scene04_School","EndingSequence"})
  {
   EditorSceneManager.OpenScene("Assets/Scenes/"+scene+".unity");var ts=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();var audit=new Part{scene=scene,time=DateTime.Now.ToString("O")};
   foreach(var t in ts)
   {
    var r=t.GetComponent<SpriteRenderer>();if(r&&r.sprite)audit.surfaces.Add(new View{path=P03RegressionDriver.PathOf(t),sprite=r.sprite.name,asset=AssetDatabase.GetAssetPath(r.sprite),active=t.gameObject.activeInHierarchy,enabled=r.enabled,hidden=r.forceRenderingOff,order=r.sortingOrder,bounds=r.bounds});
    var c=t.GetComponent<Collider2D>();if(c&&(t.name.Contains("Ground")||t.name.Contains("Boundary")||t.name.Contains("Blocker")))audit.geometry.Add(P03RegressionDriver.PathOf(t)+" | "+c.GetType().Name+" active="+t.gameObject.activeInHierarchy+" enabled="+c.enabled+" trigger="+c.isTrigger+" bounds="+c.bounds);
    var limits=t.GetComponent<RoomCameraBounds>();if(limits)audit.geometry.Add(P03RegressionDriver.PathOf(t)+" | RoomCameraBounds "+EditorJsonUtility.ToJson(limits));
    var framing=t.GetComponent<RoomPresentationFraming>();if(framing)audit.geometry.Add(P03RegressionDriver.PathOf(t)+" | RoomPresentationFraming "+EditorJsonUtility.ToJson(framing));
    var audio=t.GetComponent<AudioSource>();if(audio)audit.geometry.Add(P03RegressionDriver.PathOf(t)+" | AudioSource active="+t.gameObject.activeInHierarchy+" enabled="+audio.enabled+" loop="+audio.loop+" clip="+(audio.clip?AssetDatabase.GetAssetPath(audio.clip):"none"));
   }
   File.WriteAllText(root+scene+".json",JsonUtility.ToJson(audit,true));
   var cam=Camera.main;if(!cam)continue;cam.orthographicSize=2.5f;cam.aspect=16f/9;
   var points=scene=="Scene01_Rain"?new[]{("Rain_BackgroundWindow",-8f,.35f)}:scene=="Scene02_Courtyard"?new[]{("ReturnLiving",351f,1.85f)}:scene=="Scene03_Childhood"?new[]{("ChildHome_Table",-10f,1.85f),("ChildMaster_Table",73.5f,1.85f),("ChildSecond_Window",134.5f,1.85f),("ChildLiving",201f,1.85f)}:scene=="Scene04_School"?new[]{("SchoolSecurity",212f,1.35f),("SchoolClass32",208f,17.35f),("SchoolClass31",277f,17.35f),("SchoolOffice",277f,33.35f),("SchoolEquipment",253f,1.35f),("SchoolReading",213f,65.35f)}:new[]{("Ending",0f,1.85f)};
   if(scene=="Scene02_Courtyard")ts.First(t=>t.name=="LivingRoomInterior_Return").gameObject.SetActive(true);
   foreach(var q in points){cam.transform.position=new Vector3(q.Item2,q.Item3,-10);P03SceneEvidence.Render(cam,root+q.Item1+".png",1280,720);}
  }
  EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");
 }
}