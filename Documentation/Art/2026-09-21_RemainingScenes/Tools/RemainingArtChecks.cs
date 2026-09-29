using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;

public static class RemainingArtChecks
{
 static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
 static void Log(string message)=>File.AppendAllText(RemainingArtEditor.W+"checks.txt",DateTime.Now.ToString("O")+" "+message+"\n");
 public static void Export(){
  string Q(object value)=>"\""+(value?.ToString()??"").Replace("\"","\"\"")+"\"";
  var rows=new System.Collections.Generic.List<string>{"path,source,crop,pivot,PPU,localPosition,localScale,localEuler,worldPosition,worldScale,sortingLayer,sortingOrder"};
  foreach(var t in RemainingArtEditor.All()){var crop=t.GetComponent<SpriteArtRegion>();if(!crop)continue;var s=new SerializedObject(crop);var renderer=t.GetComponent<SpriteRenderer>();var source=(Sprite)s.FindProperty("source").objectReferenceValue;
   rows.Add(string.Join(",",new object[]{RemainingArtEditor.PathOf(t),source?AssetDatabase.GetAssetPath(source):"",s.FindProperty("pixelRegion").rectValue,s.FindProperty("pivot").vector2Value,source?source.pixelsPerUnit:0,t.localPosition,t.localScale,t.localEulerAngles,t.position,t.lossyScale,renderer.sortingLayerName,renderer.sortingOrder}.Select(Q)));
  }File.WriteAllLines(RemainingArtEditor.W+SceneManager.GetActiveScene().name+"_Visuals.csv",rows,new System.Text.UTF8Encoding(true));
 }
 public static void Bedside(){var d=Find<EndingDirector>();float x=(float)typeof(EndingDirector).GetField("bedAnchorX",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(d);typeof(EndingDirector).GetMethod("PlaceForeground",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(d,new object[]{x});typeof(EndingDirector).GetMethod("UpdateWalkStage",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(d,null);Log("FIXTURE placed at actual bed anchor for closeup art check; not continuous route");}
 public static void Static(){
  int missing=0,invalid=0,physics=0;var all=RemainingArtEditor.All();
  foreach(var t in all){missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);if(RemainingArtEditor.PathOf(t).Contains("NewArt"))physics+=t.GetComponents<Collider2D>().Length;var crop=t.GetComponent<SpriteArtRegion>();if(crop){crop.Rebuild();var r=t.GetComponent<SpriteRenderer>();if(!r.sprite||r.sprite.rect.width<=0||r.sprite.GetPhysicsShapeCount()!=0)invalid++;}}
  Log($"STATIC {SceneManager.GetActiveScene().name} missingScripts={missing} invalidCropOrPhysicsShape={invalid} newArtColliders={physics}");
  if(missing+invalid+physics>0)throw new Exception("Art static contract failed");
 }
 public static void MemoryClassroom(){
  if(!Application.isPlaying)throw new Exception("Runtime fixture only");
  var school=Find<SchoolSceneController>();var controls=PersistentRuntime.Instance.GetComponent<GameControlManager>();
  controls.ActivePlayer.gameObject.SetActive(false);school.youth.gameObject.SetActive(true);controls.BindPlayer(school.youth,false);
  school.youth.GetComponent<PlayerInteraction>().BindRuntime(PersistentRuntime.Instance.GetComponentInChildren<InteractionUI>(true),PersistentRuntime.Instance.GetComponentInChildren<DialogueManager>(true));
  school.layout.ApplyVariant(SchoolWorldVariant.Memory);school.layout.SetNarrative(SchoolActor.Youth,SchoolPhase.YouthExplore);
  EndingSession.Place(school.youth,new Vector3(220,17.2f,0));school.layout.EnterRoom("Class3_2");
  Log("FIXTURE Memory classroom, not normal route");
 }
 public static void Street(){
  if(!Application.isPlaying)throw new Exception("Runtime fixture only");var chapter=Find<Scene03Chapter>();
  typeof(Scene03Chapter).GetMethod("SetArea",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(chapter,new object[]{true});
  EndingSession.Place(chapter.child,new Vector3(-5,31.25f,0));chapter.follow.SnapToTarget();Log("FIXTURE Street shop, not normal route");
 }
 public static void Maze(){if(!Application.isPlaying)throw new Exception("Runtime fixture only");Find<Scene03MazeController>().Enter();Log("API maze Enter fixture, not route");}
 public static void MazePreview(){if(!Application.isPlaying)throw new Exception("Runtime fixture only");var m=Find<Scene03MazeController>();m.Preview(Camera.main);Log("API maze preview fixture");}
 public static void MazeCancel(){Find<Scene03MazeController>().CancelPreview(Camera.main);}
 public static void CameraItemFixture(){var mode=PersistentRuntime.Instance.GetComponent<CameraModeController>();var item=(ItemData)new SerializedObject(mode).FindProperty("requiredItem").objectReferenceValue;PersistentRuntime.Instance.GetComponent<InventoryManager>().AddItem(item);Log("FIXTURE supplied camera item for real Q/Space visual test; not acquisition route");}
 public static void MazeState(){var m=Find<Scene03MazeController>();var p=m.chapter.child;Log($"MAZE layout={m.CurrentLayout} position={p.transform.position} raised={PersistentRuntime.Instance.GetComponent<CameraModeController>().IsRaised} collisionOverlap={m.WouldOverlap(m.CurrentLayout,p.transform.position,p.GetComponent<Collider2D>().bounds.extents)}");}
 public static void VisibilityLifecycle(){
  var target=new GameObject("ArtLifecycleCheckTarget");var owner=new GameObject("ArtLifecycleCheckOwner");
  try{var r=target.AddComponent<SpriteRenderer>();owner.SetActive(false);var h=owner.AddComponent<RetiredArtVisibility>();h.SetRenderers(new Renderer[]{r});if(r.forceRenderingOff)throw new Exception("Inactive owner hid target");owner.SetActive(true);if(!r.forceRenderingOff)throw new Exception("Active owner failed to hide");owner.SetActive(false);if(r.forceRenderingOff)throw new Exception("Owner failed to restore");owner.SetActive(true);owner.SetActive(false);if(r.forceRenderingOff)throw new Exception("Second cycle failed");Log("PASS visibility lifecycle: inactive setup, show/hide/restore, second cycle");}
  finally{UnityEngine.Object.DestroyImmediate(owner);UnityEngine.Object.DestroyImmediate(target);}
 }
 public static void EndingViews(){
  foreach(var r in PersistentRuntime.Instance.GetComponentsInChildren<SpriteRenderer>(true))Log($"VIEW {RemainingArtEditor.PathOf(r.transform)} enabled={r.enabled} forceOff={r.forceRenderingOff} active={r.gameObject.activeInHierarchy}");
  foreach(var h in UnityEngine.Object.FindObjectsByType<RetiredArtVisibility>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(h.name=="NewArt_CarStandingCover"){
   var prev=(bool[])typeof(RetiredArtVisibility).GetField("previous",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(h);Log("CARCOVER active="+h.gameObject.activeInHierarchy+" previous="+(prev==null?"null":string.Join(",",prev)));}
 }
 public static void EndingFixture(){
  if(!Application.isPlaying)throw new Exception("Runtime fixture only");
  var runtime=PersistentRuntime.Instance;var session=runtime.GetComponent<EndingSession>()??runtime.gameObject.AddComponent<EndingSession>();
  var data=AssetDatabase.LoadAssetAtPath<EndingData>(AssetDatabase.GUIDToAssetPath(AssetDatabase.FindAssets("t:EndingData")[0]));session.Configure(data);session.SequenceStarted=true;
  SceneManager.LoadScene("EndingSequence");Log("FIXTURE ending with explicit boarding session seed; not boarding route test");
 }
 static void EndingStage(EndingStep step){var d=Find<EndingDirector>();d.StopAllCoroutines();var dialogue=PersistentRuntime.Instance.GetComponentInChildren<DialogueManager>(true);var end=typeof(DialogueManager).GetMethod("EndDialogue",BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);end?.Invoke(dialogue,new object[]{false});if(typeof(EndingDirector).GetField("laneTiles",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(d)==null)typeof(EndingDirector).GetMethod("ResumeWalking",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(d,null);typeof(EndingDirector).GetMethod("EnterWalkStage",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(d,new object[]{step});Log("FIXTURE visual ending stage "+step);}
 public static void EndingChild(){EndingStage(EndingStep.WalkChild);}
 public static void EndingYouth(){EndingStage(EndingStep.WalkYouth);}
 public static void EndingHospital(){EndingStage(EndingStep.WalkHospital);}
 public static void EndingWard(){EndingStage(EndingStep.WalkBed);}
 public static void Cat(){Find<Scene03Chapter>().activities.OpenCat();Log("API cat closeup fixture; not fish acquisition route");}
 public static void Puppet(){Find<Scene03Chapter>().activities.Close();Find<Scene03Chapter>().activities.OpenStory();Log("API puppet activity fixture; not NPC route");}
 public static void Rings(){var chapter=Find<Scene03Chapter>();chapter.activities.Close();chapter.Inventory.AddItem(chapter.content.coinItem);chapter.activities.OpenRingOffer();Log("API rings fixture with supplied coin; not acquisition route");}
 public static void PreviewContract(){
  if(!Application.isPlaying)throw new Exception("Runtime fixture only");var chase=Find<SchoolChaseController>();
  var property=typeof(SchoolChaseController).GetProperty("Step");var old=property.GetValue(chase);property.SetValue(chase,SchoolChaseStep.Escaping);
  bool enemy=chase.EnemyActive;var enemyPoint=chase.EnemyPosition;var position=PersistentRuntime.Instance.GetComponent<GameControlManager>().ActivePlayer.transform.position;var variant=chase.school.layout.Variant;
  chase.BeginPreview();var root=chase.transform.Find("SchoolCameraVisualPreview");var copies=root?root.GetComponentsInChildren<SpriteRenderer>():Array.Empty<SpriteRenderer>();
  int fullAtlases=copies.Count(r=>r.sprite&&r.sprite.rect.width>=1500&&r.sprite.rect.height>=1000);
  bool unchanged=chase.EnemyActive==enemy&&chase.EnemyPosition==enemyPoint&&position==PersistentRuntime.Instance.GetComponent<GameControlManager>().ActivePlayer.transform.position&&chase.school.layout.Variant==variant;
  Log($"API preview copies={copies.Length} fullAtlases={fullAtlases} immediateStateUnchanged={unchanged}; not continuous Q input or pursuit test");
  chase.EndPreview();property.SetValue(chase,old);
  if(copies.Length==0||fullAtlases!=0||!unchanged)throw new Exception("Preview art contract failed");
 }
 public static void MirrorTiles(){
  if(Application.isPlaying)throw new Exception("Edit only");
  foreach(var t in RemainingArtEditor.All().Where(t=>RemainingArtEditor.PathOf(t).Contains("NewArt"))){var r=t.GetComponent<SpriteRenderer>();if(!r)continue;
   if(t.name.StartsWith("Floor_")||t.name.StartsWith("Wall_")||t.name.StartsWith("Pavement_")||t.name.StartsWith("NewArt_Bank_")){var suffix=t.name.Substring(t.name.LastIndexOf('_')+1);if(int.TryParse(suffix,out int n))r.flipX=(n%2)==1;}
  }
  UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());UnityEditor.SceneManagement.EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
 }
}

