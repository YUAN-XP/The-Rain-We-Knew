using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class P05Audit {
 public const string Work="D:/游戏设计/Codex工作区/work/2026-10-04-P05/";
 static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
 public static void Baseline(){Audit("baseline");}
 public static void Current(){Audit("current");}
 public static void Audit(string folder){
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit mode required");
  Directory.CreateDirectory(Work+folder);
  foreach(var scene in new[]{"Scene04_School","Scene02_Courtyard"}){
   EditorSceneManager.OpenScene("Assets/Scenes/"+scene+".unity");
   var data=new P04Audit.Data{scene=scene,time=DateTime.Now.ToString("O")};
   var ts=P04Audit.All();
   foreach(var t in ts){var n=new P04Audit.Node{path=PathOf(t),parent=t.parent?PathOf(t.parent):"",position=t.position,local=t.localPosition,scale=t.lossyScale,active=t.gameObject.activeSelf};
    var r=t.GetComponent<SpriteRenderer>();if(r){n.enabled=r.enabled;n.hidden=r.forceRenderingOff;n.order=r.sortingOrder;n.bounds=r.bounds;if(r.sprite){n.sprite=r.sprite.name;n.asset=AssetDatabase.GetAssetPath(r.sprite);n.rect=r.sprite.rect;}}
    foreach(var c in t.GetComponents<Component>())if(c is MonoBehaviour||c is Collider2D||c is Rigidbody2D||c is Camera||c is TextMesh) n.components.Add(c.GetType().Name+" "+EditorJsonUtility.ToJson(c));
    data.nodes.Add(n);
   }
   File.WriteAllText(Work+folder+"/"+scene+".json",JsonUtility.ToJson(data,true));
   if(scene!="Scene04_School")continue;
   var layout=UnityEngine.Object.FindAnyObjectByType<SchoolLayoutController>();
   var cam=Camera.main;if(!layout||!cam)continue;
   cam.orthographicSize=2.6f;cam.aspect=16f/9;
   foreach(var variant in new[]{SchoolWorldVariant.Otherworld,SchoolWorldVariant.Memory}){
    layout.otherworldRoot.SetActive(variant==SchoolWorldVariant.Otherworld);layout.memoryRoot.SetActive(variant==SchoolWorldVariant.Memory);
    foreach(var room in layout.data.rooms){
     cam.transform.position=new Vector3(room.origin.x+room.width*.5f,room.origin.y+2,-10);
     Render(cam,Work+folder+"/"+variant+"_"+room.id+".jpg");
    }
   }
  }
  EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");
 }
 public static void Render(Camera cam,string path,int w=1280,int h=720){
  var prior=cam.targetTexture;var active=RenderTexture.active;var rt=RenderTexture.GetTemporary(w,h,24);var tex=new Texture2D(w,h,TextureFormat.RGB24,false);
  try{cam.targetTexture=rt;RenderTexture.active=rt;cam.Render();tex.ReadPixels(new Rect(0,0,w,h),0,0);tex.Apply();File.WriteAllBytes(path,tex.EncodeToJPG(93));}
  finally{cam.targetTexture=prior;RenderTexture.active=active;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(tex);}
 }
}
