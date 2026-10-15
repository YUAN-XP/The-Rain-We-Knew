using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public static class P03Regression
{
 public const string Work="D:/游戏设计/Codex工作区/work/2026-10-04-P03/";
 public static void Run(){if(!EditorApplication.isPlaying||!PersistentRuntime.Instance)throw new Exception("Bootstrap Scene01 Play Mode required");if(UnityEngine.Object.FindAnyObjectByType<P03RegressionDriver>())throw new Exception("Previous P03 test is still running");var g=new GameObject("P03 isolated verification");UnityEngine.Object.DontDestroyOnLoad(g);g.AddComponent<P03RegressionDriver>().Begin();}
 public static void Status(){var d=UnityEngine.Object.FindAnyObjectByType<P03RegressionDriver>();Debug.Log(d?d.Status:"P03 test not running");}
 public static void EditAudit()
 {
  if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Requires clean edit mode");
  var rows=new List<string>();var global=new List<string>();
  foreach(var name in new[]{"Scene01_Rain","Scene02_Courtyard","Scene03_Childhood","Scene04_School","EndingSequence"})
  {
   EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");var transforms=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
   foreach(var r in transforms.Select(t=>t.GetComponent<SpriteRenderer>()).Where(r=>r&&r.enabled&&r.gameObject.activeInHierarchy&&!r.forceRenderingOff&&r.sprite&&(r.name.IndexOf("Window",StringComparison.OrdinalIgnoreCase)>=0||r.name.Contains("Table")||r.name.Contains("Desk")||r.name.Contains("Lamp"))))
    global.Add(name+" | "+P03RegressionDriver.PathOf(r.transform)+" | sprite="+r.sprite.name+" bounds="+r.bounds+" rain="+(r.GetComponent<WindowRainPresentation>()!=null));
   if(name=="Scene02_Courtyard")
   {
    var retiredFloors=transforms.Select(t=>t.GetComponent<SpriteRenderer>()).Where(r=>r&&r.name.StartsWith("Demo2026_Floor_")).ToArray();
    rows.Add((!retiredFloors.Any(r=>r.gameObject.activeInHierarchy&&r.enabled&&!r.forceRenderingOff)?"PASS ":"FAIL ")+"no active old segmented courtyard floor remains visible");
    rows.Add((retiredFloors.All(r=>r.GetComponent<RetiredArtVisibility>()&&r.GetComponent<RetiredArtVisibility>().Renderers.Contains(r))?"PASS ":"FAIL ")+"all "+retiredFloors.Length+" old floor strips retain suppression on reactivation");
    var stairs=transforms.First(t=>t.name=="P03_CompactStairs").GetComponent<SpriteRenderer>();rows.Add((Mathf.Abs(stairs.bounds.size.y-P02VisualAssetRules.CourtyardStairHeight)<.01f?"PASS ":"FAIL ")+"stairs retain a human-scale landing and railing, native aspect");
    foreach(var window in transforms.Select(t=>t.GetComponent<WindowRainPresentation>()).Where(r=>r))rows.Add((window.panes.Length==3&&window.panes[0].width<.08f?"PASS ":"FAIL ")+"glass clip "+P03RegressionDriver.PathOf(window.transform));
    var photo=transforms.First(t=>t.name=="P03_PhotoSupport").GetComponent<SpriteRenderer>();rows.Add((Mathf.Abs(photo.bounds.max.y-1.66f)<.02f?"PASS ":"FAIL ")+"photo tabletop support");
    rows.Add((transforms.Any(t=>t.name=="P03_ContinuousCorridor")?"PASS ":"FAIL ")+"continuous second floor geometry");
    foreach(var n in new[]{"P03_LampWallShelf_0","P03_LampWallShelf_1"})rows.Add((transforms.Any(t=>t.name==n)?"PASS ":"FAIL ")+"lamp support "+n);
   }
   if(name=="Scene01_Rain")
   {
    var crowd=transforms.First(t=>t.name=="ReportersCrowd_Visual");rows.Add((crowd.GetComponent<P03ReporterIdle>()?"PASS ":"FAIL ")+"scene-owned reporter idle, P02 rig untouched");
    var blocker=transforms.First(t=>t.name=="CrowdPlaceholder").GetComponent<Collider2D>();rows.Add((blocker&&!blocker.isTrigger?"PASS ":"FAIL ")+"reporter solid blocking collider");
   }
  }
  File.WriteAllLines(Work+"edit_audit.txt",rows);File.WriteAllLines(Work+"demo_window_tabletop_inventory.txt",global);
  EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");Debug.Log(string.Join("\n",rows));
 }
}
public sealed class P03RegressionDriver:MonoBehaviour,GameplayInput.IReplaySource
{
 const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
 public string Status {get;private set;}="initializing";
 readonly List<string> checks=new List<string>();readonly HashSet<KeyCode> held=new HashSet<KeyCode>();int downFrame;int serial;
 GameSaveService saves;FourSlotSaveStore oldStore;GameSaveData baseline;string folder;bool failed;double lastTrace;
 public bool Key(KeyCode k)=>held.Contains(k);public bool KeyDown(KeyCode k)=>held.Contains(k)&&Time.frameCount==downFrame;
 public float Axis(string a)=>a=="Horizontal"?((Key(KeyCode.D)?1:0)-(Key(KeyCode.A)?1:0)):0;
 public bool Mouse(int b)=>false;public bool MouseDown(int b)=>false;public bool MouseUp(int b)=>false;public Vector3 Pointer=>Vector3.zero;
 PersistentRuntime Rt=>PersistentRuntime.Instance;GameControlManager Controls=>Rt.GetComponent<GameControlManager>();StoryStateManager Story=>Rt.GetComponent<StoryStateManager>();PlayerController Player=>Controls.ActivePlayer;
 public static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
 public void Begin()
 {
  if(GameplayInput.Replay!=null)throw new Exception("Finish previous isolated replay first");
  folder=P03Regression.Work+"runtime_final";Directory.CreateDirectory(folder);saves=Rt.GetComponent<GameSaveService>();oldStore=saves.Store;
  typeof(GameSaveService).GetProperty("Store").GetSetMethod(true).Invoke(saves,new object[]{new FourSlotSaveStore(folder+"/isolated-slots")});
  GameplayInput.Replay=this;StartCoroutine(Guard(Tests()));
 }
 IEnumerator Guard(IEnumerator task)
 {
  // Unity coroutines report nested exceptions themselves; explicit runner keeps root diagnostics reviewable.
  while(true){bool more;object step=null;try{more=task.MoveNext();if(more)step=task.Current;}catch(Exception e){Check(false,"EXCEPTION "+e);Finish();yield break;}if(!more)break;yield return step;}
  Finish();
 }
 void Finish(){Input();Status="DONE "+checks.Count+" checks; failures="+checks.Count(x=>x.StartsWith("FAIL"));File.WriteAllText(folder+"/status.txt",Status);if(File.Exists(P03Regression.Work+"runtime_final/position_trace.csv")){}File.WriteAllLines(folder+"/results.txt",checks);Cleanup();Debug.Log(Status);}
 void Cleanup(){P03EvidenceRecorder.Stop();if(saves&&oldStore!=null)typeof(GameSaveService).GetProperty("Store").GetSetMethod(true).Invoke(saves,new object[]{oldStore});if(ReferenceEquals(GameplayInput.Replay,this))GameplayInput.Replay=null;}
 void OnDestroy(){Cleanup();}
 void Check(bool pass,string message){checks.Add((pass?"PASS ":"FAIL ")+message);failed|=!pass;File.WriteAllLines(folder+"/results.txt",checks);}
 void Label(string label){Status=label;File.WriteAllText(folder+"/status.txt",label);}
 void Input(params KeyCode[] keys){held.Clear();foreach(var k in keys)held.Add(k);downFrame=Time.frameCount+1;File.AppendAllText(folder+"/input_trace.txt",DateTime.Now.ToString("O")+" keys="+string.Join(",",keys)+" p="+(Player?Player.transform.position.ToString("F3"):"none")+" scene="+SceneManager.GetActiveScene().name+"\n");}
 IEnumerator Foreground()
 {
  while(!Application.isFocused){Label("WAIT foreground Unity Game; pause rule is preserved");yield return null;}
  if(GamePause.IsPaused)Rt.GetComponent<GamePause>().SetPaused(false);
 }
 IEnumerator Wait(float seconds)
 {
  float t=0;while(t<seconds){if(GamePause.IsPaused){yield return Foreground();yield return null;}t+=GameplayClock.Delta;yield return null;}
 }
 IEnumerator Until(Func<bool> condition,float seconds=20)
 {
  float t=0;while(!condition()&&t<seconds){if(GamePause.IsPaused){yield return Foreground();yield return null;}t+=GameplayClock.Delta;yield return null;}Check(condition(),"settled "+Status);
 }
 IEnumerator Hold(float seconds,params KeyCode[] keys){yield return Foreground();Input(keys);yield return Wait(seconds);Input();yield return Wait(.10f);}
 IEnumerator Pulse(KeyCode key){yield return Hold(.075f,key);yield return Wait(.25f);}
 IEnumerator Dialogue()
 {
  var d=Rt.GetComponentInChildren<DialogueManager>(true);float t=0;while(d&&d.IsDialogueActive()&&t<40){yield return Pulse(KeyCode.E);t+=.425f;}Check(!d||!d.IsDialogueActive(),"dialogue released through E replay");
 }
 IEnumerator Shot(string label)
 {
  Label(label);Canvas.ForceUpdateCanvases();P03EvidenceRecorder.ReserveStillFrame();yield return new WaitForEndOfFrame();
  File.AppendAllText(folder+"/capture_diagnostics.txt",DateTime.Now.ToString("O")+" "+label+" frame="+Time.frameCount+" transition="+BlackSceneTransition.IsActive+" UI="+string.Join(";",FindObjectsByType<CanvasGroup>().Select(g=>g.name+":"+g.alpha.ToString("F3")))+"\n");
  var image=ScreenCapture.CaptureScreenshotAsTexture();
  try {var pixels=image.GetPixels32();long light=0;for(int i=0;i<pixels.Length;i+=31)light+=pixels[i].r+pixels[i].g+pixels[i].b;Check(light>pixels.Length/31*6,"final screenshot has composed picture "+label);File.WriteAllBytes(folder+"/"+label+".png",image.EncodeToPNG());}
  finally {UnityEngine.Object.Destroy(image);}yield return Wait(.1f);
 }
 IEnumerator Fixture(string scene,Vector3 at,string[] flags=null,bool camera=false,int stage=0,bool joined=false,int revision=3)
 {
  Label("fixture "+scene+" "+at);var d=JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(baseline));d.scene=scene;d.position=at;d.actor="adult";d.actorFacing="right";d.companions=new SavedCompanion[0];d.cameraPosition=new Vector3(at.x,at.y<5?1.85f:7.88f,-10);d.cameraSize=2.5f;d.cameraX=new Vector2(-30,160);d.cameraY=new Vector2(-10,45);d.followY=false;d.planar=false;d.story.flags=flags??new string[0];d.school=new SavedSchool();d.items=new SavedItem[0];d.files=new SavedFile[0];d.lampRaised=false;d.cameraAvailable=camera;d.distortedJoined=joined;d.corridorLampStage=stage;d.courtyardLayoutRevision=revision;d.threeDoorProgress=0;
  if(camera){var c=Rt.GetComponent<CameraModeController>();var item=(ItemData)c.GetType().GetField("requiredItem",Hidden).GetValue(c);var reg=SaveAssetRegistry.Load();d.items=new[]{new SavedItem{key=reg.Key(item),id=item.itemId,displayName=item.itemName,description=item.description}};}
  saves.Store.Write(1,d,true);Check(saves.Load(1,out var error),"fixture original save loader: "+error);
  float end=Time.realtimeSinceStartup+35;while(saves.IsRestoring&&Time.realtimeSinceStartup<end)yield return null;Check(!saves.IsRestoring&&saves.LastError==null,"load settled without error "+saves.LastError);
  yield return Foreground();yield return Wait(.5f);Check(GroundedPlacement.IsSupported(Player),"fixture actor supported");Input();
 }
 Transform T(string suffix)=>SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).First(t=>PathOf(t).EndsWith(suffix));
 static object Field(object obj,string name)=>obj.GetType().GetField(name,Hidden).GetValue(obj);
 void Place(float x,float y){EndingSession.Place(Player,new Vector3(x,y,0));GroundedPlacement.Settle(Player);RoomPresentationFraming.PrepareCurrentView();}
 IEnumerator Door(RoomTransitionInteractable door,string label)
 {
  Label(label);var destination=(Transform)Field(door,"destination");Place(door.transform.position.x,Player.transform.position.y);
  bool moved=false,arrived=false,black=false;Action<RoomTransitionInteractable> m=x=>{moved=true;var fade=FindAnyObjectByType<ScreenFadeController>();black=fade.GetComponent<CanvasGroup>().alpha>.99f;};Action<RoomTransitionInteractable> a=x=>arrived=true;door.MovedUnderFade+=m;door.Arrived+=a;
  door.Interact();Check(!saves.CanSave(out _),"door transit forbids save "+label);
  yield return Until(()=>arrived,12);door.MovedUnderFade-=m;door.Arrived-=a;
  Check(moved&&black,"only original door fade "+label);Check(Mathf.Abs(Player.transform.position.x-destination.position.x)<.1f,"original destination "+label);Check(GroundedPlacement.IsSupported(Player),"no landing drop "+label);
  yield return Wait(.15f);yield return Shot(label);
 }
 IEnumerator Tests()
 {
  yield return Foreground();yield return Wait(.4f);Label("initial Scene01");
  Check(SceneManager.GetActiveScene().name=="Scene01_Rain","starts original Scene01");
  Check(!Rt.GetComponent<InventoryManager>().Items.Any(i=>i&&i.itemId=="key_oil_lamp"),"Scene01 start has no unearned lamp");
  var camera=Rt.GetComponent<CameraModeController>();var cameraItem=(ItemData)Field(camera,"requiredItem");Check(!Rt.GetComponent<InventoryManager>().HasItem(cameraItem),"Scene01 start has no unearned camera");
  yield return Dialogue();yield return Until(()=>saves.CanSave(out _),15);baseline=saves.Capture(1);
  // Source geometry tests run in normal gameplay, using isolated starting states only.
  yield return Fixture("Scene01_Rain",new Vector3(-16.2f,-.09f,0),new[]{StoryFlag.Scene01MemoryFlashPlayed.ToString()});
  yield return Hold(4,KeyCode.A);Check(Player.transform.position.x>-18.1f,"reporter crowd blocks passage");yield return Shot("01_Reporters_Blocking");
  Place(-15f,-.09f);var holder=FindAnyObjectByType<IDHolderInteractable>();holder.Interact();yield return Wait(.2f);yield return Dialogue();Check(Story.Has(StoryFlag.Scene01IDHolderTaken),"flat credential retains original pickup");Check(!holder.GetComponentsInChildren<SpriteRenderer>().Any(r=>r.enabled&&!r.forceRenderingOff),"credential cannot leave a duplicate ground image");var idle=FindAnyObjectByType<P03ReporterIdle>();Check(idle&&idle.GetComponentInChildren<MeshRenderer>()&&idle.GetComponent<SpriteRenderer>().forceRenderingOff,"reporter scene-owned idle is running");
  Place(5.2f,-.09f);yield return Wait(.5f);yield return Shot("02_Bicycle_Grounding");
  Place(10.1f,-.09f);yield return Wait(.5f);yield return Shot("03_Lamp_Emitter");
  var emitters=FindObjectsByType<Transform>(FindObjectsInactive.Exclude).Where(t=>t.name=="P03_BulbEmitter").ToArray();var pools=FindAnyObjectByType<StreetAtmospherePresentation>().GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name=="LampPool_Presentation").ToArray();
  File.WriteAllLines(folder+"/lamp_geometry.txt",emitters.Select(e=>"emitter "+e.position).Concat(pools.Select(r=>"pool "+r.bounds+" transform="+r.transform.position+" scale="+r.transform.lossyScale)));
  Check(emitters.Length==3&&pools.Length==3&&emitters.All(e=>pools.Any(r=>Mathf.Abs(r.bounds.max.y-e.position.y)<.02f&&Mathf.Abs(r.bounds.center.x-e.position.x)<.02f)),"three lamp cones terminate exactly at bulb emitters");
  var puddle=FindAnyObjectByType<PuddleReflectionInteractable>();Place(puddle.transform.position.x,-.09f);puddle.Interact();yield return Wait(.1f);yield return Dialogue();
  yield return Until(()=>FindAnyObjectByType<DemoCloseupPresentation>()?.IsOpen==true,8);yield return Wait(1.7f);yield return Shot("04_Puddle_Revealed");yield return Pulse(KeyCode.E);yield return Wait(.8f);yield return Dialogue();yield return Until(()=>!Controls.IsLocked(),8);
  Check(!(bool)Field(puddle,"sequenceRunning")&&!((GameObject)Field(puddle,"reflectionObject")).activeSelf,"puddle first close clears old portrait");
  puddle.Interact();yield return Wait(.2f);yield return Dialogue();Check(!FindObjectsByType<DemoCloseupPresentation>().Any(c=>c.IsOpen),"repeat puddle uses original dialogue only");
  // Authored memory event is reached by real movement across its actual trigger.
  P03EvidenceRecorder.Begin(this,"Rain_memory",7);var memory=FindAnyObjectByType<MemoryFlashTrigger>();Story.Set(StoryFlag.Scene01MemoryFlashPlayed,false);Place(memory.transform.position.x-1.6f,-.09f);yield return Hold(.9f,KeyCode.D);
  yield return Wait(.4f);yield return Shot("05_Comic_Face");yield return Wait(1.2f);yield return Shot("06_Comic_Voice");yield return Wait(1.1f);yield return Shot("07_Comic_Bang");yield return Wait(1.8f);
  Check(Story.Has(StoryFlag.Scene01MemoryFlashPlayed)&&!Controls.IsLocked(),"memory entered via movement and original event releases lock");
  yield return Fixture("Scene02_Courtyard",new Vector3(-9,1.435f,0));
  var photo=T("DirtyPhoto_Placeholder/NewArt_Visual").GetComponent<SpriteRenderer>();Check(photo.sprite.name=="FILE_FAMILY_PHOTO_STAINED","photo remains stained before birthday");yield return Shot("08_Photo_Stained");
  Story.Set(StoryFlag.Scene03BirthdayKnown);yield return Wait(.15f);Check(photo.sprite.name=="FILE_FAMILY_PHOTO_CLEAN","photo switches only at birthday flag");yield return Shot("09_Photo_Clean");
  P03EvidenceRecorder.Begin(this,"Courtyard_windows_and_tabletop",8);yield return Hold(1.7f,KeyCode.D);yield return Wait(2.8f);yield return Hold(1.7f,KeyCode.A);yield return Wait(2.0f);
  var windows=FindObjectsByType<WindowRainPresentation>(FindObjectsInactive.Exclude);Check(windows.Length>=4&&windows.All(w=>w.GetComponentsInChildren<MeshRenderer>().Count()==3),"rain allocated exactly three glass panes on four windows");
  var stairUp=FindObjectsByType<RoomTransitionInteractable>().First(d=>d.name=="StairsUp_InteractZone");Place(4.1f,1.435f);yield return Door(stairUp,"10_StairsUp");
  var stairDown=FindObjectsByType<RoomTransitionInteractable>().First(d=>d.name=="StairExit_Point");yield return Door(stairDown,"11_StairsDown");
  yield return Door(stairUp,"12_StairsUpAgain");
  var bedroom=T("Environment/Floor02/MasterBedroom_Placeholder").GetComponent<RoomTransitionInteractable>();yield return Door(bedroom,"13_MasterEnter");
  var masterExit=T("MasterBedroomInterior/ExitDoor_Point").GetComponent<RoomTransitionInteractable>();yield return Door(masterExit,"14_MasterExit");
  yield return Fixture("Scene02_Courtyard",new Vector3(350,1.435f,0),new[]{StoryFlag.Scene03Completed.ToString(),StoryFlag.Scene03BirthdayKnown.ToString(),StoryFlag.Scene03FamilyPhotoTaken.ToString(),StoryFlag.Scene03ReturnReactionPlayed.ToString(),StoryFlag.CameraGentlemanCardObtained.ToString()},true);
  yield return Dialogue();yield return Until(()=>FindAnyObjectByType<Scene02ReturnController>().Ready&&!Controls.IsLocked(),12);
  var returnWindow=T("LivingRoomInterior_Return/NewArt_RoomShell/LivingWindow").GetComponent<WindowRainPresentation>();
  Check(returnWindow&&returnWindow.GetComponentsInChildren<MeshRenderer>().Length==3,"return living room keeps three animated glass panes");
  yield return Shot("14D_ReturnLivingRoom");
  yield return Door(T("LivingRoomInterior_Return/ReturnLivingRoomDoor").GetComponent<RoomTransitionInteractable>(),"14E_ReturnLivingExit");
  Check(Story.Has(StoryFlag.LivingRoomDoorOpened),"return living door unlocks only through original inside callback");
  yield return Door(T("Environment/Floor01/LockedLivingRoom_Placeholder").GetComponent<RoomTransitionInteractable>(),"14F_ReturnLivingEnter");
  Check(GroundedPlacement.IsSupported(Player),"return living room both directions remain grounded");
    yield return Fixture("Scene02_Courtyard",new Vector3(68.5f,1.435f,0),new[]{StoryFlag.CameraDoorRevealed.ToString()},true);
  yield return Hold(1.0f,KeyCode.A);yield return Wait(1.7f);
  var parent=FindAnyObjectByType<ParentShadowSequence>();var parentShadows=(SpriteRenderer[])Field(parent,"shadows");var lamp=(SpriteRenderer)Field(parent,"lamp");
  Check(parentShadows.All(s=>s.enabled&&s.color.a>.9f),"fragmented parent silhouettes displayed through original event");
  Check(parentShadows.All(s=>Camera.main.WorldToViewportPoint(s.bounds.min).x>0&&Camera.main.WorldToViewportPoint(s.bounds.max).x<1)&&Camera.main.WorldToViewportPoint(lamp.transform.position).x>0&&Camera.main.WorldToViewportPoint(lamp.transform.position).x<1,"both parent shadows and flickering lamp share view");
  yield return Shot("14B_ParentShadows");yield return Dialogue();yield return Hold(1.5f,KeyCode.A);yield return Until(()=>Story.Has(StoryFlag.ParentShadowEventPlayed),10);
  Check(parentShadows.All(s=>!s.enabled),"original approach clears parent event and restores lamp");
  var flags=new[]{StoryFlag.CameraDoorRevealed.ToString(),StoryFlag.ParentShadowEventPlayed.ToString()};
  yield return Fixture("Scene02_Courtyard",new Vector3(-11.7f,7.43f,0),flags,true);yield return Dialogue();yield return Until(()=>!Controls.IsLocked(),8);yield return Door(T("Environment/Floor02/HiddenBedroom_RevealDoor").GetComponent<RoomTransitionInteractable>(),"14C_HiddenEnter");
  yield return Fixture("Scene02_Courtyard",new Vector3(136,1.435f,0),flags,true);
  var desk=Rt.GetComponent<DeskCloseupController>();Check(desk.OpenDesk(),"new authored desk opens through existing controller");yield return Wait(.2f);Check(!saves.CanSave(out _),"desk closeup forbids save during interaction");yield return Shot("15_Desk");
  var drawn=desk.GetComponentsInChildren<DrawnHotspotGraphic>();foreach(var g in drawn){var mesh=g.canvasRenderer?g.canvasRenderer.GetMesh():null;Check(g.canvasRenderer&&g.depth>=0&&!g.canvasRenderer.cull&&mesh&&mesh.vertexCount>0,"authored contour actually renders "+g.name);}
  var buttons=desk.GetComponentsInChildren<Button>(true);var computer=buttons.First(b=>b.name=="ComputerHotspot");ExecuteEvents.Execute(computer.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);yield return Wait(.1f);Check(desk.Page=="Computer","computer contour reaches original login page");desk.SubmitComputer();Check(!Story.Has(StoryFlag.SchoolComputerLoggedIn),"computer cannot skip school login gates");desk.Back();yield return Wait(.15f);Canvas.ForceUpdateCanvases();var hotspot=buttons.First(b=>b.name=="DrawerHotspot");var contour=hotspot.GetComponent<DrawnHotspotGraphic>();var r=contour.rectTransform;var p=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.min+Vector2.Scale(r.rect.size,new Vector2(.74f,.50f))));
  var uiHits=new List<RaycastResult>();EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=p},uiHits);File.WriteAllLines(folder+"/drawer_raycast.txt",uiHits.Select(h=>h.gameObject.name+" depth="+h.depth));Check(uiHits.Count>0&&uiHits[0].gameObject==hotspot.gameObject,"actual graphic raycaster resolves drawn drawer as topmost target");
  Check(contour.Raycast(p,null),"drawn drawer contour accepts inside point");var outp=RectTransformUtility.WorldToScreenPoint(null,r.TransformPoint(r.rect.min+Vector2.Scale(r.rect.size,new Vector2(.5f,.10f))));Check(!contour.Raycast(outp,null),"drawn drawer rejects desk legs");
  ExecuteEvents.Execute(hotspot.gameObject,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);yield return Wait(.2f);Check(desk.Page=="Drawer","drawer UI event reaches original page");desk.ConfirmLock();Check(desk.LockMessage=="密码不对。","wrong password retained");
  var wheels=desk.GetComponentsInChildren<FourDigitLock>(true);
  Canvas.ForceUpdateCanvases();var lockArt=desk.GetComponentsInChildren<Image>(true).First(i=>i.name=="AuthoredLock");
  var lockCorners=new Vector3[4];lockArt.rectTransform.GetWorldCorners(lockCorners);
  Check(lockArt.sprite.name=="P03_LockCloseup"&&Vector3.Distance(lockCorners[0],lockCorners[3])<=lockArt.sprite.rect.width+1,"lock closeup uses native pixel budget without magnifying tiny desk crop");
  foreach(var wheel in wheels)
  {
   var wr=wheel.GetComponent<RectTransform>();var center=RectTransformUtility.WorldToScreenPoint(null,wr.TransformPoint(wr.rect.center));
   var data=new PointerEventData(EventSystem.current){position=center,scrollDelta=Vector2.up};var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(data,hits);
   Check(hits.Count>0&&hits[0].gameObject==wheel.gameObject,"live roller raycast matches painted window "+wheel.name);
   ExecuteEvents.Execute(wheel.gameObject,data,ExecuteEvents.scrollHandler);Check(wheel.Value==1,"original scroll handler increments "+wheel.name);wheel.SetDigit(0);
   ExecuteEvents.Execute(wheel.gameObject,data,ExecuteEvents.beginDragHandler);data.delta=new Vector2(0,24);ExecuteEvents.Execute(wheel.gameObject,data,ExecuteEvents.dragHandler);
   Check(wheel.Value==1,"original drag handler increments "+wheel.name);wheel.SetDigit(0);
  }
  int[] digits={0,7,1,5};for(int i=0;i<4;i++)wheels[i].SetDigit(digits[i]);desk.ConfirmLock();Check(!Story.Has(StoryFlag.OilLampObtained),"correct digits still require birthday knowledge");Story.Set(StoryFlag.Scene03BirthdayKnown);desk.ConfirmLock();Check(Story.Has(StoryFlag.OilLampObtained)&&Story.Has(StoryFlag.DrawerOpened),"known birthday unlocks original lamp");desk.ConfirmLock();Check(Rt.GetComponent<InventoryManager>().Items.Count(i=>i&&i.itemId=="key_oil_lamp")==1,"repeated confirm cannot duplicate lamp");yield return Shot("16_Drawer");
  desk.Back();yield return Pulse(KeyCode.Escape);yield return Until(()=>Story.Has(StoryFlag.ChildShadowSequencePlayed),12);Check(Story.Has(StoryFlag.DeskEventTriggered),"desk close retains child sequence order");yield return Wait(.3f);
  var hiddenExit=T("HiddenBedroomArea/HiddenBedroom_Exit").GetComponent<RoomTransitionInteractable>();yield return Door(hiddenExit,"17_HiddenExit_Continuous");
  var corridor=FindAnyObjectByType<DistortedCorridorController>();Check(corridor.HasJoined&&Player.transform.position.x<0&&Player.transform.position.y<12,"bedroom exit is distorted second floor without remote teleport");
  Check(!T("Environment/Floor02/StairExit_Point").gameObject.activeInHierarchy,"old second floor stairs disappear");
  yield return Until(()=>!Controls.IsLocked(),10);Check(Story.Has(StoryFlag.ChildShadowFollowupPlayed),"wait follow-up retained before walking");
  P03EvidenceRecorder.Begin(this,"Continuous_corridor",30);yield return Hold(6.6f,KeyCode.D);Check(corridor.LampStage==1,"first real crossing raises collapse stage 1");yield return Shot("18_Corridor_Stage1");
  var collapse=FindAnyObjectByType<P03CorridorCollapse>();var fragments=collapse.GetComponentsInChildren<SpriteRenderer>().Select(r=>r.transform).ToArray();var beforePause=fragments.Select(t=>t.position).ToArray();double clockBefore=GameplayClock.Elapsed;bool saveBeforePause=saves.CanSave(out _);
  Rt.GetComponent<GamePause>().SetPaused(true);yield return new WaitForSecondsRealtime(.4f);
  Check(Math.Abs(GameplayClock.Elapsed-clockBefore)<.001&&corridor.LampStage==1&&fragments.Select((t,i)=>(t.position-beforePause[i]).sqrMagnitude).All(v=>v<.000001f),"pause freezes collapse clock, fragment positions, and lamp stage");Check(saves.CanSave(out _)==saveBeforePause,"pause preserves existing free-action save permission");Rt.GetComponent<GamePause>().SetPaused(false);
  Check(saves.Save(2,true,out var error),"save stage1 "+error);yield return Hold(1.5f,KeyCode.A);Check(corridor.LampStage==1,"backtracking cannot reverse first collapse");
  Check(saves.Load(2,out error),"cold restore stage1 "+error);while(saves.IsRestoring)yield return null;yield return Foreground();yield return Wait(.4f);corridor=FindAnyObjectByType<DistortedCorridorController>();Check(corridor.HasJoined&&corridor.LampStage==1&&Player.transform.position.x<7,"stage1 restored in same floor");yield return Shot("19_Corridor_Stage1_Load");
  yield return Hold(3.8f,KeyCode.D);Check(corridor.LampStage==2,"second real crossing raises collapse stage 2");yield return Shot("20_Corridor_Stage2");
  Check(saves.Save(3,true,out error),"save stage2 "+error);Check(saves.Load(3,out error),"cold restore stage2 "+error);while(saves.IsRestoring)yield return null;yield return Foreground();yield return Wait(.4f);corridor=FindAnyObjectByType<DistortedCorridorController>();Check(corridor.LampStage==2,"stage2 restored without replaying first stage");
  // Whole corridor is traversed through production player input; no speed or camera-follow override.
  yield return Hold(3.2f,KeyCode.D);Check(Player.transform.position.x>10&&Player.transform.position.x<19,"walking reaches three-door area with production speed");
  var puzzle=FindAnyObjectByType<ThreeDoorPuzzleController>();Check(puzzle.IsActive,"three-door collider activates without blocking forward entry");yield return Shot("21_ThreeDoors");
  Check(new[]{T("ThreeDoor_Left/NewArt_Visual"),T("ThreeDoor_Middle/NewArt_Visual"),T("ThreeDoor_Right/NewArt_Visual")}.All(t=>{var b=t.GetComponent<SpriteRenderer>().bounds;return Camera.main.WorldToViewportPoint(b.min).x>0&&Camera.main.WorldToViewportPoint(b.max).x<1;}),"all three doors share actual camera view");
  Check(puzzle.Choose(0),"wrong original door accepted");yield return Until(()=>!puzzle.IsBusy,8);Check(puzzle.Progress==0&&Mathf.Abs(Player.transform.position.x-10.5f)<.1f&&GroundedPlacement.IsSupported(Player),"wrong selection resets safely");yield return Shot("22_WrongDoorReset");
  foreach(int index in new[]{2,0}){var door=FindObjectsByType<PuzzleDoorInteractable>(FindObjectsInactive.Exclude).First(d=>(int)Field(d,"doorIndex")==index);Place(door.transform.position.x,7.43f);yield return Wait(.25f);Check(ReferenceEquals(Player.GetComponent<PlayerInteraction>().CurrentVisualTarget,door),"original E target reaches relocated door "+index);int next=puzzle.Progress+1;yield return Pulse(KeyCode.E);yield return Until(()=>puzzle.Progress==next&&!puzzle.IsBusy,8);}
  Check(puzzle.Progress==2,"right-left order retained");Check(saves.Save(4,true,out error),"save between puzzle choices "+error);Check(saves.Load(4,out error),"restore puzzle progress "+error);while(saves.IsRestoring)yield return null;yield return Foreground();yield return Wait(.5f);puzzle=FindAnyObjectByType<ThreeDoorPuzzleController>();yield return Wait(.2f);Check(puzzle.Progress==2&&puzzle.CorrectDoor==1,"puzzle save retains required middle final door");
  var finalDoor=FindObjectsByType<PuzzleDoorInteractable>(FindObjectsInactive.Exclude).First(d=>(int)Field(d,"doorIndex")==1);Place(finalDoor.transform.position.x,7.43f);yield return Wait(.25f);Check(ReferenceEquals(Player.GetComponent<PlayerInteraction>().CurrentVisualTarget,finalDoor),"original E target reaches final middle door");yield return Pulse(KeyCode.E);yield return Until(()=>SceneManager.GetActiveScene().name=="Scene03_Childhood",20);Check(Story.Has(StoryFlag.ThreeDoorPuzzleCompleted),"original chapter transition flag retained");yield return Until(()=>!BlackSceneTransition.IsActive&&FindAnyObjectByType<ScreenFadeController>().GetComponent<CanvasGroup>().alpha<.01f,12);yield return Wait(.2f);yield return Shot("23_OriginalScene03Transition");
  // Legacy migration is an explicit isolated data test, not a player replay.
  var legacy=JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(baseline));legacy.scene="Scene02_Courtyard";legacy.position=new Vector3(260,31.43f);legacy.cameraPosition=new Vector3(266,31.88f,-10);legacy.courtyardLayoutRevision=0;legacy.distortedJoined=true;P03SceneLayoutMigration.Apply(legacy);Check(legacy.courtyardLayoutRevision==3&&legacy.position.y<12&&legacy.position.x>=10&&legacy.corridorLampStage==2,"legacy remote corridor save migrates to joined floor");
  P03EvidenceRecorder.Stop();yield return P03MediaVerifier.Run(Check);
  Label("P03 runtime complete");
 }
 void LateUpdate()
 {
  if(string.IsNullOrEmpty(folder)||Time.realtimeSinceStartupAsDouble<lastTrace+.4||!Player)return;lastTrace=Time.realtimeSinceStartupAsDouble;
  var corridor=FindAnyObjectByType<DistortedCorridorController>();var follow=Camera.main?Camera.main.GetComponent<CameraFollow>():null;
  File.AppendAllText(folder+"/position_trace.csv",DateTime.Now.ToString("O")+","+SceneManager.GetActiveScene().name+","+Player.transform.position.x.ToString("F3")+","+Player.transform.position.y.ToString("F3")+","+GamePause.IsPaused+","+Application.isFocused+","+(Camera.main?Camera.main.transform.position.ToString("F3"):"none")+","+(corridor?corridor.LampStage:-1)+","+(follow?follow.enabled:false)+"\n");
 }
}
