using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class P03SceneEvidence
{
 public const string Work="D:/游戏设计/Codex工作区/work/2026-10-04-P03/";
 static Transform[] All()=>SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
 static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
 public static void Baseline()
 {
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Requires clean edit mode");
  Directory.CreateDirectory(Work+"baseline");
  foreach(var scene in new[]{"Scene01_Rain","Scene02_Courtyard"})
  {
   EditorSceneManager.OpenScene("Assets/Scenes/"+scene+".unity");References();
   var cam=Camera.main;var pos=cam.transform.position;var size=cam.orthographicSize;var aspect=cam.aspect;
   var points=scene=="Scene01_Rain"?new[]{("Crowd",-18f,.35f),("Puddle",0f,.35f),("Bike",5.2f,.35f),("Lamp",10.1f,.35f)}:
    new[]{("FirstLeft",-21f,1.85f),("FirstPhoto",-14f,1.85f),("FirstStairs",7f,1.85f),("FirstFog",19f,1.85f),("SecondLeft",-12f,7.88f),("SecondRight",5f,7.88f),("Master",70.3f,1.85f),("SecondBed",132f,1.85f),("SecondDesk",138f,1.85f),("ThreeDoors",269f,31.88f)};
   try{cam.orthographicSize=2.5f;cam.aspect=16f/9;
    foreach(var p in points){cam.transform.position=new Vector3(p.Item2,p.Item3,-10);Render(cam,Work+"baseline/"+scene+"_"+p.Item1+".png");}}
   finally{cam.transform.position=pos;cam.orthographicSize=size;cam.aspect=aspect;}
  }
 }
 public static void References()
 {
  var lines=new List<string>();
  foreach(var t in All())foreach(var c in t.GetComponents<Component>())
  {
   if(!c)continue;
   if(c is MonoBehaviour||c is Collider2D||c is Camera)
   {
    var so=new SerializedObject(c);var p=so.GetIterator();
    while(p.Next(true))if(p.propertyType==SerializedPropertyType.ObjectReference && p.objectReferenceValue)
    {
     var o=p.objectReferenceValue;var other=o is GameObject g?g.transform:o is Component cc?cc.transform:null;
     lines.Add(PathOf(t)+" | "+c.GetType().Name+"."+p.propertyPath+" = "+(other?PathOf(other):AssetDatabase.GetAssetPath(o)));
    }
   }
   if(c is SpriteRenderer sr && sr.sprite)
   {
    var sp=sr.sprite;lines.Add("SPRITE | "+PathOf(t)+" | "+(sr.enabled&&t.gameObject.activeInHierarchy&&!sr.forceRenderingOff)+" | "+sp.name+" | "+AssetDatabase.GetAssetPath(sp.texture)+" | rect="+sp.rect+" | bounds="+sr.bounds+" | order="+sr.sortingOrder);
   }
  }
  File.WriteAllLines(Work+SceneManager.GetActiveScene().name+"_references.txt",lines);
 }
 public static void Render(Camera cam,string path,int width=1440,int height=810)
 {
  var target=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);var tex=new Texture2D(width,height,TextureFormat.RGBA32,false);
  var old=cam.targetTexture;var active=RenderTexture.active;
  try{cam.targetTexture=target;cam.Render();RenderTexture.active=target;tex.ReadPixels(new Rect(0,0,width,height),0,0);tex.Apply(false);File.WriteAllBytes(path,tex.EncodeToPNG());}
  finally{cam.targetTexture=old;RenderTexture.active=active;UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(tex);}
 }
 public static void Current()
 {
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Requires clean edit mode");
  Directory.CreateDirectory(Work+"current");
  foreach(var scene in new[]{"Scene01_Rain","Scene02_Courtyard"})
  {
   EditorSceneManager.OpenScene("Assets/Scenes/"+scene+".unity");References();
   var cam=Camera.main;var pos=cam.transform.position;var size=cam.orthographicSize;var aspect=cam.aspect;
   var points=scene=="Scene01_Rain"?new[]{("Crowd",-18f,.35f),("Credential",-15f,.35f),("Bike",5.2f,.35f),("Lamp",10.1f,.35f)}:
    new[]{("FirstLeft",-10.8f,1.85f),("FirstPhoto",-8f,1.85f),("FirstMiddle",-2f,1.85f),("FirstStairs",4.8f,1.85f),("FirstFog",7.8f,1.85f),("SecondLeft",-13f,7.88f),("SecondRight",5f,7.88f),("MasterLeft",67.9f,1.85f),("MasterRight",71.5f,1.85f),("SecondBed",130f,1.85f),("SecondDesk",133.9f,1.85f)};
   try{cam.orthographicSize=2.5f;cam.aspect=16f/9;
    foreach(var q in points){cam.transform.position=new Vector3(q.Item2,q.Item3,-10);Render(cam,Work+"current/"+scene+"_"+q.Item1+".png");}
    if(scene=="Scene02_Courtyard")
    {
     var root=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).First(t=>t.name=="DistortedCorridorArea");root.gameObject.SetActive(true);
     var controller=UnityEngine.Object.FindAnyObjectByType<DistortedCorridorController>();var serialized=new SerializedObject(controller);var disabled=serialized.FindProperty("disabledDuringDistortion");for(int j=0;j<disabled.arraySize;j++)if(disabled.GetArrayElementAtIndex(j).objectReferenceValue is GameObject normal)normal.SetActive(false);
     for(int i=0;i<3;i++){cam.transform.position=new Vector3(i==0?-9:i==1?3.5f:15,7.88f,-10);Render(cam,Work+"current/Corridor_"+i+".png");}root.gameObject.SetActive(false);
    }
   }finally{cam.transform.position=pos;cam.orthographicSize=size;cam.aspect=aspect;}
  }
  // Observation-only temporary visibility changes are discarded, not saved.
  EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");
 }

}
