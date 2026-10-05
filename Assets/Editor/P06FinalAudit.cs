using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class P06FinalAudit {
 static readonly List<string> checks=new List<string>();
 static void Check(bool ok,string label){checks.Add((ok?"PASS ":"FAIL ")+label);}
 static void Save(string file){Directory.CreateDirectory(P06Regression.Work+"audit");File.WriteAllLines(P06Regression.Work+"audit/"+file,checks);if(checks.Any(c=>c.StartsWith("FAIL")))throw new Exception("P06 final audit failed: "+file);}
 public static void RuntimeCleanup(){
  checks.Clear();if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");
  var rt=PersistentRuntime.Instance;var svc=rt.GetComponent<GameSaveService>();
  Check(GameplayInput.Replay==null,"test replay removed after final fixture");
  Check(svc.Store.DirectoryPath==Path.GetFullPath(Path.Combine(Application.persistentDataPath,"Saves")),"original player save store restored; isolated fixture remains on D drive");
  Check(UnityEngine.Object.FindObjectsByType<P06FlowDriver>(FindObjectsInactive.Include,FindObjectsSortMode.None).All(t=>t.hideFlags==HideFlags.None&&t.gameObject.hideFlags==HideFlags.DontSave),"test driver object is editor-only DontSave");
  Save("final-runtime-cleanup.txt");Debug.Log("P06 final runtime cleanup audit passed");
 }
 public static void Edit(){
  checks.Clear();if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean Edit Mode required");
  Check(GameplayInput.Replay==null,"replay cleared after exiting Play Mode");
  var cat=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>("Assets/Resources/DemoArtCatalog.asset");
  Check(cat&&cat.entries.All(e=>e.sprite),"catalogue references resolve");
  var additions=cat.entries.Where(e=>e.key.StartsWith("P06_")).ToArray();Check(additions.Select(e=>e.key).Distinct().Count()==additions.Length,"P06 catalogue additions have no duplicate keys; baseline catalogue is retained");
  var keys=new[]{"P06_BedCloseup","P06_WardWide","P06_TransformationComic:0","P06_TransformationComic:1","P06_TransformationComic:2","P06_TransformationComic:3","P06_CarSeated:0","P06_CarSeated:1","P06_CarSeated:2","P06_CarSeated:3","P06_XiangHeads:0","P06_XiangHeads:1"};
  foreach(var key in keys)Check(DemoArtCatalog.Find(key),"approved sprite resolves "+key);
  foreach(var path in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Art/Demo2026/P06"}).Select(AssetDatabase.GUIDToAssetPath)){
   var im=(TextureImporter)AssetImporter.GetAtPath(path);Check(im.spriteImportMode==SpriteImportMode.Multiple&&!im.mipmapEnabled&&im.maxTextureSize==4096&&im.textureCompression==TextureImporterCompression.Uncompressed,"native approved import settings "+Path.GetFileName(path));
  }
  EditorSceneManager.OpenScene("Assets/Scenes/Scene03_Childhood.unity");
  var all=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
  string[] names={"S03_Camera","S03_Parents","S03_Table"};var pos=new[]{new Vector3(200,1.43f,0),new Vector3(199.9f,1.3f,0),new Vector3(200,.5f,0)};var anchors=new[]{new Vector3(200,1.5f,0),new Vector3(199.15f,.9f,0),new Vector3(200.9f,.85f,0)};
  var sizes=new[]{new Vector2(.45f,.35f),new Vector2(.7f,1.3f),new Vector2(.6f,.65f)};
  for(int n=0;n<names.Length;n++){var t=all.Single(a=>a.name==names[n]);var reach=t.GetComponent<InteractionReach>();Check(Vector3.Distance(t.position,pos[n])<.001f,"original visual position retained "+names[n]);Check(reach&&reach.anchor&&Vector3.Distance(reach.anchor.position,anchors[n])<.001f,"authored interaction point "+names[n]);Check(Vector2.Distance(t.GetComponent<BoxCollider2D>().size,sizes[n])<.001f,"distinct sensor size "+names[n]);}
  foreach(var scene in EditorBuildSettings.scenes.Where(s=>s.enabled)){
   Check(!File.ReadAllText(scene.path).Contains("P06_IsolatedFlow")&&!File.ReadAllText(scene.path).Contains("P06_IsolatedCameraRegression"),"no test fixture serialized "+Path.GetFileName(scene.path));
  }
  EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");Check(!SceneManager.GetActiveScene().isDirty,"returned to clean original Scene01 Edit Mode");Save("final-edit-audit.txt");Debug.Log("P06 final edit audit "+checks.Count+" checks passed");
 }
}
