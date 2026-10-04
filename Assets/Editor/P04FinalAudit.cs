using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class P04FinalAudit {
 public static void Run(){
  if(EditorApplication.isPlaying||UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit mode required");
  var rows=new List<string>();
  Action<bool,string> check=(ok,msg)=>rows.Add((ok?"PASS ":"FAIL ")+msg);
  var cat=AssetDatabase.LoadAssetAtPath<DemoArtCatalog>("Assets/Resources/DemoArtCatalog.asset");var entries=new SerializedObject(cat).FindProperty("entries");var keys=new HashSet<string>();int empty=0,dup=0;
  for(int i=0;i<entries.arraySize;i++){var a=entries.GetArrayElementAtIndex(i);if(!keys.Add(a.FindPropertyRelative("key").stringValue))dup++;if(!a.FindPropertyRelative("sprite").objectReferenceValue)empty++;}
  check(empty==0,"catalog has no missing sprites; entries="+entries.arraySize);check(dup==18,"catalog retains 18 pre-existing duplicate aliases; exact old mapping separately verified");
  foreach(var path in EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path)){
   EditorSceneManager.OpenScene(path);var ts=P04Audit.All();int missing=ts.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));check(missing==0,path+" missing scripts="+missing);
   var broken=new List<string>();foreach(var c in ts.SelectMany(t=>t.GetComponents<Component>()).Where(c=>c is MonoBehaviour)){
    var so=new SerializedObject(c);var it=so.GetIterator();while(it.NextVisible(true))if(it.propertyType==SerializedPropertyType.ObjectReference&&it.objectReferenceValue==null&&!it.objectReferenceEntityIdValue.Equals(default(UnityEngine.EntityId)))broken.Add(c.name+"/"+c.GetType().Name+"/"+it.propertyPath);
   }check(broken.Count==0,path+" broken nonzero serialized references="+broken.Count);rows.AddRange(broken.Select(s=>"DETAIL "+s));
   if(path.EndsWith("Scene03_Childhood.unity")){
    var ch=UnityEngine.Object.FindAnyObjectByType<Scene03Chapter>();var chair=ts.Single(t=>t.name=="HomeNearChairBack");check(chair.GetComponent<RetiredArtVisibility>()&&chair.GetComponentsInChildren<Renderer>(true).All(r=>r.forceRenderingOff||!r.enabled),"obsolete home foreground chair retired");
    var fisher=ch.streetRoot.transform.Find("P04_Fisher").GetComponent<SpriteRenderer>();check(Mathf.Abs(fisher.bounds.size.y-1.30f)<.015f,"fisher final painted height 1.30");check(fisher.GetComponent<P04FishingRod>().actor==fisher,"one fishing rod bound to final fisherman");
    var n=ch.streetRoot.transform.Find("NewArt_Street/ChattingNeighbours");check(n.GetComponentsInChildren<Renderer>(true).All(r=>r.forceRenderingOff||!r.enabled),"obsolete doubled chess neighbor retired");
   }
   if(path.EndsWith("Scene02_Courtyard.unity")){
    var root=ts.Single(t=>t.name=="LivingRoomInterior_Return").Find("P04_GroundedFurniture");check(root&&root.childCount==4,"return living room has four grounded authored furnishings");
   }
  }
  File.WriteAllLines(P04Regression.Work+"checks/final_edit_audit.txt",rows);EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");if(rows.Any(s=>s.StartsWith("FAIL")))throw new Exception("Final edit audit failed; inspect D-drive evidence");
 }
}
