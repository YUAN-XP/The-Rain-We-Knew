using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
[InitializeOnLoad] public static class RemainingArtEditor
{
 public const string W="C:/Users/31975/Documents/Codex/2026-09-05/unity-x20/work/art_remaining/";
 static double next;
 static RemainingArtEditor(){EditorApplication.update+=Tick;}
 static void Tick(){if(EditorApplication.isCompiling||EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.4;
  if(!File.Exists(W+"command.txt"))return;var cmd=File.ReadAllText(W+"command.txt").Trim();File.Delete(W+"command.txt");
  try{Run(cmd);File.AppendAllText(W+"events.txt",DateTime.Now.ToString("O")+" OK "+cmd+"\n");}catch(Exception e){File.AppendAllText(W+"errors.txt",cmd+" "+e+"\n");}}
 public static Transform[] All()=>SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
 public static string PathOf(Transform t)=>t.parent==null?t.name:PathOf(t.parent)+"/"+t.name;
 static void Run(string cmd){
  if(!Application.dataPath.Replace('\\','/').Equals("D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets",StringComparison.OrdinalIgnoreCase))throw new Exception("Wrong project");
  if(cmd=="status"){var lt=typeof(Editor).Assembly.GetType("UnityEditor.LogEntries");object[] c={0,0,0};lt.GetMethod("GetCountsByType",BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,c);File.WriteAllText(W+"status.txt",$"{SceneManager.GetActiveScene().name} dirty={SceneManager.GetActiveScene().isDirty} play={EditorApplication.isPlaying} counts={string.Join(",",c)}");return;}
  if(cmd.StartsWith("open:")){if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Scene dirty or playing");EditorSceneManager.OpenScene("Assets/Scenes/"+cmd.Substring(5)+".unity");Dump();return;}
  if(cmd=="dump"){Dump();return;}
  if(cmd=="play"){EditorApplication.isPlaying=true;return;}
  if(cmd=="stop"){EditorApplication.isPlaying=false;return;}
  if(cmd=="save"){if(EditorApplication.isPlaying)throw new Exception("Playing");EditorSceneManager.SaveScene(SceneManager.GetActiveScene());return;}
  if(cmd.StartsWith("capture:")){ScreenCapture.CaptureScreenshot(W+cmd.Substring(8)+".png");return;}
  if(cmd.StartsWith("place:")){var a=cmd.Substring(6).Split(',');var actor=PersistentRuntime.Instance.GetComponent<GameControlManager>().ActivePlayer;EndingSession.Place(actor,new Vector3(float.Parse(a[0]),float.Parse(a[1]),0));FindAnyObjectByType<CameraFollow>().SnapToTarget();return;}
  if(cmd.StartsWith("invoke:")){var a=cmd.Substring(7).Split('|');Type.GetType(a[0]+",Assembly-CSharp-Editor").GetMethod(a[1],BindingFlags.Public|BindingFlags.Static).Invoke(null,null);return;}
  throw new Exception("Unknown "+cmd);
 }
 static T FindAnyObjectByType<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindAnyObjectByType<T>();
 [Serializable] public class Row{public string path;public Vector3 pos,scale,size;public float bottom;public int order;public string sprite;public bool active,enabled;public string[] scripts;}
 [Serializable] class Rows{public Row[] rows;}
 public static void Dump(){var rows=All().Select(t=>{var r=t.GetComponent<SpriteRenderer>();return new Row{path=PathOf(t),pos=t.position,scale=t.lossyScale,size=r?r.bounds.size:Vector3.zero,bottom=r?r.bounds.min.y:0,order=r?r.sortingOrder:0,sprite=r&&r.sprite?AssetDatabase.GetAssetPath(r.sprite):"",active=t.gameObject.activeSelf,enabled=r&&r.enabled,scripts=t.GetComponents<MonoBehaviour>().Where(m=>m!=null).Select(m=>m.GetType().Name).ToArray()};}).ToArray();File.WriteAllText(W+SceneManager.GetActiveScene().name+"_live.json",JsonUtility.ToJson(new Rows{rows=rows},true));}
}
