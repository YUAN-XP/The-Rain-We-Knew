using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
// Additive residency changes visibility and chapter ownership, never actor coordinates or inventory.
[DefaultExecutionOrder(-10000)]
public sealed class P05WorldLink:MonoBehaviour {
 public static P05WorldLink Owner{get;private set;}
 public static string PreloadingScene{get;private set;}
 public bool ResidentOnly{get;private set;}
 public static bool IsPreloading(Scene scene){
  if(PreloadingScene==scene.name)return true;
  if(!scene.IsValid()||!scene.isLoaded)return false;
  return scene.GetRootGameObjects().Any(r=>r.GetComponentsInChildren<P05WorldLink>(true).Any(l=>l.ResidentOnly));
 }
 public static bool Available=>Owner&&Owner.enabled;
 public FogIntroController fog;public Collider2D homeBarrier,schoolBarrier;public SchoolSceneController school;
 RoomPresentationFraming.Room[] authoredRooms;
 readonly Dictionary<GameObject,bool> homeRooms=new Dictionary<GameObject,bool>();
 readonly List<Behaviour> dormant=new List<Behaviour>();Camera residentCamera;bool loading,schoolActivated,returning;float retryAt;
 public static Scene BoundsScene(Vector3 position){var active=SceneManager.GetActiveScene();if(Available&&position.y<4&&position.y>.0f&&position.x<38.4f){var home=SceneManager.GetSceneByName("Scene02_Courtyard");if(home.isLoaded)return home;}return active;}
 public bool BothResident=>SceneManager.GetSceneByName("Scene02_Courtyard").isLoaded&&SceneManager.GetSceneByName("Scene04_School").isLoaded;
 void Awake(){authoredRooms=gameObject.scene.GetRootGameObjects().SelectMany(root=>root.GetComponentsInChildren<RoomPresentationFraming>(true)).SelectMany(c=>c.rooms).ToArray();if(IsPreloading(gameObject.scene)){ResidentOnly=true;Dormant();return;}Owner=this;residentCamera=Camera.main;}
 void Dormant(){foreach(var root in gameObject.scene.GetRootGameObjects())foreach(var b in root.GetComponentsInChildren<Behaviour>(true)){
  bool logic=b is SchoolSceneController||b is SchoolLayoutController||b is Scene02ReturnController||b is SchoolHomeReturnController||b is SchoolRuntimeInstaller||b is EndingHomeController||b is ParentShadowSequence||b is ChildShadowSequence||b is DistortedCorridorController||b is ThreeDoorPuzzleController||b is Scene03TransitionBridge;
  if(logic&&b.enabled){dormant.Add(b);b.enabled=false;}
  if(b is Camera c){c.enabled=false;c.tag="Untagged";}if(b is AudioListener)b.enabled=false;
 }}
 IEnumerator Start(){if(Owner!=this)yield break;yield return null;if(gameObject.scene.name=="Scene04_School")yield return Prepare("Scene02_Courtyard");}
 public IEnumerator Prepare(string name){if(SceneManager.GetSceneByName(name).isLoaded)yield break;if(loading)yield break;loading=true;PreloadingScene=name;var op=SceneManager.LoadSceneAsync(name,LoadSceneMode.Additive);while(!op.isDone)yield return null;yield return null;PreloadingScene=null;loading=false;BindResidents();}
 void HouseRooms(bool visible){
  if(gameObject.scene.name!="Scene02_Courtyard")return;
  if(homeRooms.Count==0)foreach(var root in gameObject.scene.GetRootGameObjects()){
   if(root.name=="Environment")foreach(Transform child in root.transform)if(child.name!="Floor01")homeRooms[child.gameObject]=child.gameObject.activeSelf;
   if(root.name=="LivingRoomInterior_Return")homeRooms[root]=root.activeSelf;
   if(root.name=="Gameplay"){var doors=root.transform.Find("RoomTransitions");if(doors)homeRooms[doors.gameObject]=doors.gameObject.activeSelf;}
  }
  foreach(var pair in homeRooms)if(pair.Key)pair.Key.SetActive(visible&&pair.Value);
 }
 void BindResidents(){var links=FindObjectsByType<P05WorldLink>(FindObjectsInactive.Include);var h=links.FirstOrDefault(a=>a.gameObject.scene.name=="Scene02_Courtyard");var s=links.FirstOrDefault(a=>a.gameObject.scene.name=="Scene04_School");if(h){fog=h.fog;homeBarrier=h.homeBarrier;h.HouseRooms(SceneManager.GetActiveScene().name=="Scene02_Courtyard");}if(s){school=s.school;schoolBarrier=s.schoolBarrier;}
  if(fog)fog.PrepareContinuousBackdrop();
  if(!residentCamera)residentCamera=Camera.main;if(!residentCamera)return;residentCamera.enabled=true;residentCamera.tag="MainCamera";var listener=residentCamera.GetComponent<AudioListener>();if(listener)listener.enabled=true;
  ApplyResidentFrames();
 }
 void ApplyResidentFrames(){if(!residentCamera)residentCamera=Camera.main;if(!residentCamera)return;var framing=residentCamera.GetComponent<RoomPresentationFraming>();if(!framing)return;var active=SceneManager.GetActiveScene();var links=FindObjectsByType<P05WorldLink>(FindObjectsInactive.Include);var rooms=links.OrderBy(a=>a.gameObject.scene==active?0:1).SelectMany(a=>a.authoredRooms??System.Array.Empty<RoomPresentationFraming.Room>()).GroupBy(a=>a.area).Select(g=>g.First()).ToList();rooms.Add(new RoomPresentationFraming.Room{area=new Rect(10,-1.5f,28.5f,7),floorY=.5f,size=2.5f,correctRoomLimits=true});framing.rooms=rooms.ToArray();}
 void ActivateSchool(){var s=FindObjectsByType<P05WorldLink>(FindObjectsInactive.Include).FirstOrDefault(a=>a.gameObject.scene.name=="Scene04_School");if(!s||!BothResident)return;s.ResidentOnly=false;foreach(var b in s.dormant)if(b)b.enabled=true;s.dormant.Clear();schoolActivated=true;var h=FindObjectsByType<P05WorldLink>(FindObjectsInactive.Include).FirstOrDefault(a=>a.gameObject.scene.name=="Scene02_Courtyard");if(h)h.HouseRooms(false);SceneManager.SetActiveScene(s.gameObject.scene);ApplyResidentFrames();school.layout.EnterRoom("Floor01");PersistentRuntime.Instance.GetComponent<GameControlManager>().BindPlayer(PersistentRuntime.Instance.GetComponent<GameControlManager>().ActivePlayer,true);RoomPresentationFraming.PrepareCurrentView();}
 void Update(){if(Owner!=this||GamePause.IsPaused||!PersistentRuntime.Instance)return;var rt=PersistentRuntime.Instance;var state=rt.GetComponent<StoryStateManager>();var controls=rt.GetComponent<GameControlManager>();var p=controls.ActivePlayer;if(!p)return;float x=p.transform.position.x,y=p.transform.position.y;
  if(!school&&SceneManager.GetActiveScene().name=="Scene02_Courtyard"&&state.Has(StoryFlag.Scene03Completed)&&!state.Has(StoryFlag.SchoolReturnedHome)&&!loading)StartCoroutine(Prepare("Scene04_School"));
  if(homeBarrier)homeBarrier.enabled=!(fog&&(fog.IsActive||state.Has(StoryFlag.FogIntroCompleted)))&&!state.Has(StoryFlag.SchoolReturnedHome);
  var lamp=rt.GetComponent<OilLampController>();bool canReturn=state.Has(StoryFlag.SchoolChaseCompleted)&&lamp&&lamp.IsIlluminating;
  if(schoolBarrier)schoolBarrier.enabled=!state.Has(StoryFlag.FogIntroCompleted)||!canReturn&&SceneManager.GetActiveScene().name=="Scene04_School";
  if(y<.5f||y>4||controls.IsLocked())return;
  if(SceneManager.GetActiveScene().name=="Scene02_Courtyard"&&!state.Has(StoryFlag.FogIntroCompleted)&&state.Has(StoryFlag.Scene03Completed)&&fog&&x>=9.75f&&x<14&&GameplayClock.Now>=retryAt){retryAt=GameplayClock.Now+.25f;fog.TryEnter();}
  if(fog&&fog.Phase==FogIntroPhase.Completed&&state.Has(StoryFlag.FogIntroCompleted)&&!schoolActivated&&!returning&&x>=38.4f)ActivateSchool();
  if(SceneManager.GetActiveScene().name=="Scene04_School"&&school&&school.Ready&&school.layout.CurrentRoom=="Floor01"&&x<=38.7f&&canReturn&&!returning)school.chase.TryBeginWalkingReturn();
 }
 public bool CompleteFog(){return BothResident&&school;}
 public bool BeginReturn(){if(!BothResident||returning)return false;var home=FindObjectsByType<SchoolHomeReturnController>(FindObjectsInactive.Include).FirstOrDefault(a=>a.gameObject.scene.name=="Scene02_Courtyard");if(!home)return false;returning=true;home.enabled=true;StartCoroutine(home.Resume());return true;}
 public void ArriveHome(){var home=FindObjectsByType<P05WorldLink>(FindObjectsInactive.Include).FirstOrDefault(a=>a.gameObject.scene.name=="Scene02_Courtyard");if(!home)return;home.ResidentOnly=false;SceneManager.SetActiveScene(home.gameObject.scene);ApplyResidentFrames();home.HouseRooms(true);if(school){school.enabled=false;school.layout.enabled=false;school.layout.otherworldRoot.SetActive(false);school.layout.memoryRoot.SetActive(false);}
  foreach(var b in home.dormant)if(b)b.enabled=true;home.dormant.Clear();if(homeBarrier)homeBarrier.enabled=false;RoomPresentationFraming.PrepareCurrentView();}
 void OnDestroy(){if(Owner==this){Owner=null;if(loading)PreloadingScene=null;}}
}
