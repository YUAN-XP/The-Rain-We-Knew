using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using System.IO;
using System.Linq;
public static class Scene02ArtReview
{
 static (string name,float x,float y)[] points={ ("F1_Photo",-14,1.4f),("F1_Stairs",4,1.4f),("F1_RightFog",19,1.4f),("F2_MasterDoor",-4,7.43f),("Master_Bed",70,1.4f),("Master_Camera",77,1.4f),("Second_Desk",138,1.4f),("Living_Sofa",345,1.4f),("Living_Table",350,1.4f)};
 static int shot; static bool waiting;static double next;
 public static void Views(){if(!Application.isPlaying)throw new System.Exception("Runtime fixture only");shot=0;waiting=false;next=0;EditorApplication.update-=Step;EditorApplication.update+=Step;}
 static void Step(){if(!Application.isPlaying||shot>=points.Length){EditorApplication.update-=Step;return;}if(EditorApplication.timeSinceStartup<next)return;var p=points[shot];if(!waiting){EndingSession.Place(PersistentRuntime.Instance.GetComponent<GameControlManager>().ActivePlayer,new Vector3(p.x,p.y,0));Object.FindAnyObjectByType<CameraFollow>().SnapToTarget();waiting=true;next=EditorApplication.timeSinceStartup+2;}else{ScreenCapture.CaptureScreenshot(ArtRebuildEditor.W+"Review_"+p.name+".png");shot++;waiting=false;next=EditorApplication.timeSinceStartup+1;}}
 public static void Load02(){if(!Application.isPlaying||!PersistentRuntime.Instance)throw new System.Exception("Requires Scene01 Play runtime");SceneManager.LoadScene("Scene02_Courtyard");}
 public static void ReturnFixture(){var s=PersistentRuntime.Instance.GetComponent<StoryStateManager>();s.SetTogether(StoryFlag.Scene03Completed,StoryFlag.Scene03ReturnReactionPlayed,StoryFlag.CameraGentlemanCardObtained);SceneManager.LoadScene("Scene02_Courtyard");}
 public static void CorridorFixture(){var s=PersistentRuntime.Instance.GetComponent<StoryStateManager>();s.SetTogether(StoryFlag.ChildShadowSequencePlayed,StoryFlag.ChildShadowFollowupPlayed);EndingSession.Place(PersistentRuntime.Instance.GetComponent<GameControlManager>().ActivePlayer,new Vector3(8.6f,7.43f,0));Object.FindAnyObjectByType<CameraFollow>().SnapToTarget();}
 public static void Report(){var c=Camera.main;var g=Object.FindAnyObjectByType<GameControlManager>();var p=g?g.ActivePlayer:null;File.WriteAllText(ArtRebuildEditor.W+"runtime.txt",$"scene={SceneManager.GetActiveScene().name} player={(p?p.transform.position:Vector3.zero)} camera={(c?c.transform.position:Vector3.zero)} size={(c?c.orthographicSize:0)} lock={(g&&g.IsLocked())}\n"+string.Join("\n",Object.FindObjectsByType<ContinuousRoomArtwork>(FindObjectsInactive.Include,FindObjectsSortMode.None).Select(x=>$"{x.name} active={x.gameObject.activeInHierarchy} renderer={x.GetComponent<Renderer>().enabled} forceOff={x.GetComponent<Renderer>().forceRenderingOff}")));}
}
