using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class P05FinalVerify
{
 public static void Run()
 {
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit mode required");
  var checks=new List<string>();
  Action<bool,string> check=(ok,name)=>checks.Add((ok?"PASS ":"FAIL ")+name);
  foreach(var entry in EditorBuildSettings.scenes.Where(s=>s.enabled))
  {
   var scene=EditorSceneManager.OpenScene(entry.path);
   var objects=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
   check(objects.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject))==0,"no missing scripts "+scene.name);
   check(!objects.Any(t=>t.GetComponent<P05RegressionDriver>()),"no serialized test driver "+scene.name);
   if(scene.name!="Scene04_School"&&scene.name!="Scene02_Courtyard")continue;
   int broken=0;
   foreach(var b in objects.SelectMany(t=>t.GetComponents<MonoBehaviour>()).Where(b=>b))
   {
    var so=new SerializedObject(b);var prop=so.GetIterator();
    while(prop.NextVisible(true))if(prop.propertyType==SerializedPropertyType.ObjectReference&&prop.objectReferenceEntityIdValue.GetHashCode()!=0&&prop.objectReferenceValue==null)broken++;
   }
   check(broken==0,"no broken serialized behaviour references "+scene.name+" count="+broken);
   if(scene.name!="Scene04_School")continue;
   var layout=objects.Select(t=>t.GetComponent<SchoolLayoutController>()).First(x=>x);
   check(layout.data.rooms.Length==12&&layout.data.rooms.Select(r=>r.id).Distinct().Count()==12,"12 unique rooms");
   check(layout.data.portals.Select(r=>r.id).Distinct().Count()==layout.data.portals.Length,"unique original portal IDs");
   check(layout.data.portals.All(d=>layout.data.Room(d.sourceRoom)!=null&&layout.data.Room(d.targetRoom)!=null),"all portal room references valid");
   var loops=layout.memoryRoot.GetComponentsInChildren<P05PaintedLoop>(true);
   check(loops.All(l=>!string.IsNullOrEmpty(l.identity))&&loops.Select(l=>l.identity).Distinct().Count()==loops.Length,"distinct campus identities "+loops.Length);
   check(loops.Where(l=>l.activity=="writing"||l.activity=="typing").All(l=>l.breath==0&&l.frames.Length==3),"seated NPC base sources available for pinned continuous motion");
   check(loops.All(l=>l.target&&l.target.sprite&&l.period>0),"all life-loop sprite references valid");
   check(loops.Where(l=>l.identity.StartsWith("S")&&l.activity=="conversation").Count()==6,"six separate corridor students");
   check(loops.All(l=>Mathf.Abs(l.target.bounds.min.y-(l.transform.position.y))<.025f),"life-loop visible feet aligned to authored floor anchors");
   foreach(var variant in new[]{layout.otherworldRoot,layout.memoryRoot})
   {
    var roof=variant.transform.Find("Roof/P05_Artwork");
    foreach(var side in new[]{"Left","Right"})
    {
     var door=roof.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r=>r.name=="Gateway_Stair_F03_Roof_"+side+"_BtoA");
     check(door&&door.enabled&&!door.forceRenderingOff&&Mathf.Abs(door.bounds.min.y-48.25f)<.02f,variant.name+" real roof exit grounded "+side);
    }
    check(!roof.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="CentralStair"),variant.name+" no fake central roof entrance");
   }
  }
  foreach(var path in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Art/正式/P05"}).Select(AssetDatabase.GUIDToAssetPath))
  {
   var importer=(TextureImporter)AssetImporter.GetAtPath(path);var tex=AssetDatabase.LoadAssetAtPath<Texture2D>(path);importer.GetSourceTextureWidthAndHeight(out int w,out int h);
   check(tex.width==w&&tex.height==h&&!importer.mipmapEnabled&&importer.textureCompression==TextureImporterCompression.Uncompressed,"native texture pixels retained "+Path.GetFileName(path)+" "+w+"x"+h);
  }
  EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");
  Directory.CreateDirectory(P05Audit.Work+"checks");File.WriteAllLines(P05Audit.Work+"checks/final-edit-checks.txt",checks);
  File.WriteAllText(P05Audit.Work+"checks/final-edit-summary.txt","checks="+checks.Count+" failures="+checks.Count(x=>x.StartsWith("FAIL")));
  if(checks.Any(x=>x.StartsWith("FAIL")))throw new Exception("P05 final edit checks failed; inspect final-edit-checks.txt");
 }
}
