using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class P04SettledEvidence {
 static int index;static double next;static Camera cam;static Scene03Chapter ch;static readonly (string,float,float)[] views={
 ("HomeLeft",-10.3f,2.05f),("HomeMiddle",-4f,2.05f),("HomeStairs",2.29f,2.05f),("Master",71f,2.05f),("Second",133f,2.05f),("Living",200.3f,2.05f),
 ("Gate",15f,32.05f),("LiMei",-14.5f,32.05f),("Dog",0f,32.05f),("Story",-26f,32.05f),("Chess",26.7f,32.05f),("Breakfast",47f,32.05f),("Pond",72.2f,32.05f),("Sign",96.4f,32.05f),("RiverGate",105f,32.05f),("RiverTree",309f,72.05f),("RiverPavilion",302.5f,72.05f)};
 public static void Begin(){if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit mode");Directory.CreateDirectory(P04Audit.Work+"audit_final");EditorSceneManager.OpenScene("Assets/Scenes/Scene03_Childhood.unity");cam=Camera.main;ch=UnityEngine.Object.FindAnyObjectByType<Scene03Chapter>();index=0;Configure();EditorApplication.update+=Tick;}
 static void Configure(){if(index<views.Length){var q=views[index];ch.homeRoot.SetActive(q.Item3<10&&q.Item1!="Living");ch.livingRoomRoot.SetActive(q.Item1=="Living");ch.streetRoot.SetActive(q.Item3>30&&q.Item3<40);ch.maze.riverRoot.SetActive(q.Item3>70);cam.orthographicSize=q.Item3>70?2.6f:2.2f;cam.aspect=16f/9;cam.transform.position=new Vector3(q.Item2,q.Item3,-10);foreach(var t in UnityEngine.Object.FindObjectsByType<TMPro.TextMeshPro>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))t.ForceMeshUpdate(true);}
 else{EditorSceneManager.OpenScene("Assets/Scenes/Scene02_Courtyard.unity");var r=Array.Find(P04Audit.All(),t=>t.name=="LivingRoomInterior_Return");r.gameObject.SetActive(true);cam=Camera.main;cam.orthographicSize=2.2f;cam.aspect=16f/9;cam.transform.position=new Vector3(351,2.05f,-10);}next=EditorApplication.timeSinceStartup+.4;EditorApplication.QueuePlayerLoopUpdate();}
 static void Tick(){if(EditorApplication.timeSinceStartup<next)return;try{foreach(var s in UnityEngine.Object.FindObjectsByType<RetiredArtVisibility>(FindObjectsInactive.Exclude,FindObjectsSortMode.None))s.SendMessage("LateUpdate");P03SceneEvidence.Render(cam,P04Audit.Work+"audit_final/"+(index<views.Length?views[index].Item1:"ReturnLiving")+".png");index++;if(index>views.Length){EditorApplication.update-=Tick;EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");File.WriteAllText(P04Audit.Work+"audit_final/complete.txt",DateTime.Now.ToString("O"));}else Configure();}catch(Exception e){EditorApplication.update-=Tick;File.WriteAllText(P04Audit.Work+"audit_final/error.txt",e.ToString());Debug.LogException(e);}}
}
