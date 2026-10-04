using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class P04Audit {
 public const string Work="D:/游戏设计/Codex工作区/work/2026-10-04-P04/";
 [Serializable] public class Node { public string path,parent;public int id;public Vector3 position,local,scale;public bool active,enabled,hidden;public string sprite,asset;public Rect rect;public Bounds bounds;public int order;public List<string> components=new List<string>();}
 [Serializable] public class Data {public string scene,time;public List<Node> nodes=new List<Node>();}
 static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
 public static Transform[] All()=>SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
 public static void Baseline(){Audit("baseline");}
 public static void Current(){Audit("current");}
 static void Audit(string folder){
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit mode required");
  Directory.CreateDirectory(Work+folder);
  foreach(var scene in new[]{"Scene03_Childhood","Scene02_Courtyard"}){
   EditorSceneManager.OpenScene("Assets/Scenes/"+scene+".unity");var ts=All();var data=new Data{scene=scene,time=DateTime.Now.ToString("O")};
   foreach(var t in ts){var n=new Node{path=PathOf(t),parent=t.parent?PathOf(t.parent):"",position=t.position,local=t.localPosition,scale=t.lossyScale,active=t.gameObject.activeSelf};
    var r=t.GetComponent<SpriteRenderer>();if(r){n.enabled=r.enabled;n.hidden=r.forceRenderingOff;n.order=r.sortingOrder;n.bounds=r.bounds;if(r.sprite){n.sprite=r.sprite.name;n.asset=AssetDatabase.GetAssetPath(r.sprite);n.rect=r.sprite.rect;}}
    foreach(var c in t.GetComponents<Component>())if(c is MonoBehaviour||c is Collider2D||c is Rigidbody2D||c is Camera||c is TextMesh) n.components.Add(c.GetType().Name+" "+EditorJsonUtility.ToJson(c));
    data.nodes.Add(n);
   }File.WriteAllText(Work+folder+"/"+scene+".json",JsonUtility.ToJson(data,true));
   var cam=Camera.main;if(!cam)continue;cam.orthographicSize=2.6f;cam.aspect=16f/9;
   if(scene=="Scene03_Childhood"){
    var chapter=UnityEngine.Object.FindAnyObjectByType<Scene03Chapter>();var maze=chapter.maze;
    foreach(var q in new[]{("HomeLeft",-13f,1.95f),("HomeStairs",7f,1.95f),("Master",72f,1.95f),("Second",134f,1.95f),("Living",201f,1.95f),("Gate",0f,32.05f),("ShopLeft",-14f,32.05f),("Story",30f,32.05f),("WangHome",51f,32.05f),("Breakfast",58f,32.05f),("Pond",71f,32.05f),("Sign",96f,32.05f),("RiverGate",122f,32.05f),("RiverTree",315f,73.9f),("RiverPavilion",301f,73.9f)}){
     chapter.homeRoot.SetActive(q.Item1.StartsWith("Home")||q.Item1=="Master"||q.Item1=="Second");
     chapter.livingRoomRoot.SetActive(q.Item1=="Living");
     chapter.streetRoot.SetActive(q.Item3>30&&q.Item3<40);
     maze.riverRoot.SetActive(q.Item3>70);
     cam.transform.position=new Vector3(q.Item2,q.Item3,-10);if(q.Item3>70)cam.orthographicSize=3.1f;else cam.orthographicSize=2.6f;
     P03SceneEvidence.Render(cam,Work+folder+"/"+q.Item1+".png",1440,810);
    }
   }else{ts.First(t=>t.name=="LivingRoomInterior_Return").gameObject.SetActive(true);cam.transform.position=new Vector3(351,1.95f,-10);P03SceneEvidence.Render(cam,Work+folder+"/ReturnLiving.png");}
  }
  EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");
 }
}

