using UnityEngine;
using System.Linq;

/// <summary>Presentation follows the current actor and outfit; original bounds remain usable by story scripts.</summary>
[DefaultExecutionOrder(1200)]
public sealed class DemoActorArtwork : MonoBehaviour
{
 SpriteRenderer body,paint;
 PlayerController actor;
 StoryStateManager story;
 Vector3 previous;
 bool left;
 string prefix;
 float phase;
 bool originalForceOff;
 string poseKey;float poseHeight,poseLift;
 public bool HasExplicitPose => !string.IsNullOrEmpty(poseKey);
 public bool OwnsPaintedBody => paint&&paint.enabled&&paint.sprite&&!paint.forceRenderingOff;
 public void SetPose(string key,float height=1,float lift=0){poseKey=key;poseHeight=height;poseLift=lift;}

 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
 static void Install(){UnityEngine.SceneManagement.SceneManager.sceneLoaded-=OnSceneLoaded;UnityEngine.SceneManagement.SceneManager.sceneLoaded+=OnSceneLoaded;Attach();}
 static void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene,UnityEngine.SceneManagement.LoadSceneMode mode)=>Attach();
 static void Attach(){foreach(var p in FindObjectsByType<PlayerController>(FindObjectsInactive.Include))if(!p.GetComponent<DemoActorArtwork>())p.gameObject.AddComponent<DemoActorArtwork>();}
 void Awake(){actor=GetComponent<PlayerController>();body=GetComponent<SpriteRenderer>();if(!body)body=GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r=>r.name=="NewArt_Actor");if(!body)return;originalForceOff=body.forceRenderingOff;var g=new GameObject("Demo2026_PlayerPainting");g.transform.SetParent(transform,false);paint=g.AddComponent<SpriteRenderer>();var contact=g.AddComponent<PaintedContactShadow>();contact.target=paint;contact.minimumWidth=.38f;contact.depth=.13f;previous=transform.position;}
 void LateUpdate()
 {
  if(!body||!paint)return;
  var native=GetComponent<P02ActorPresentation>();
  if(native&&native.OwnsBody){paint.enabled=false;body.forceRenderingOff=true;
   HideLegacyOutfit();
   if(PersistentRuntime.Instance){var old=PersistentRuntime.Instance.transform.Find("AdultDefault_Visual");if(old&&old.TryGetComponent<SpriteRenderer>(out var legacy))legacy.forceRenderingOff=true;}
   return;}

  if(!story&&PersistentRuntime.Instance)story=PersistentRuntime.Instance.GetComponent<StoryStateManager>();
  bool adult=GetComponent<AdultOutfitController>()!=null;
  prefix=adult?(story&&story.Outfit==AdultOutfit.Overalls?"CHR_XP_ADULT_OVERALL":story&&story.Outfit==AdultOutfit.SchoolUniform?"CHR_XP_ADULT_SCHOOL":"CHR_XP_ADULT_DEFAULT"):(name.Contains("Youth")?"CHR_XP_YOUTH":"CHR_XP_CHILD");
  float dx=transform.position.x-previous.x;previous=transform.position;
  bool moving=Mathf.Abs(dx)>.0001f&&Mathf.Abs(dx)<.8f;
  if(moving)left=dx<0;
  string key=prefix+(prefix=="CHR_XP_ADULT_DEFAULT"?"_TURN3_CLEAN_V02:":"_TURN3:")+(left?2:1);
  // Existing right-facing cycle drawings are selected by travelled distance, not frame rate.
  if(moving){phase+=Mathf.Abs(dx)*8f;int f=Mathf.FloorToInt(phase)%8;key=prefix+"_WALK_0"+(f/4+1)+":"+(f%4);}
  var headache=transform.Find("HeadachePose");
  if(adult&&headache&&headache.gameObject.activeInHierarchy)key="CHR_XP_ADULT_DEFAULT_ACTION4:3";
  if(!string.IsNullOrEmpty(poseKey))key=poseKey;
  var sprite=DemoArtCatalog.Find(key)??DemoArtCatalog.Find(prefix+"_BASE:0");if(!sprite)return;
  paint.sprite=sprite;paint.flipX=string.IsNullOrEmpty(poseKey)&&moving&&left;paint.flipY=false;paint.sortingLayerID=body.sortingLayerID;paint.sortingOrder=body.sortingOrder+2;
  float height=adult?1.8f:prefix.Contains("CHILD")?1.35f:1.72f;
  var foot=body.sprite?body.transform.TransformPoint(new Vector3(0,body.sprite.bounds.min.y,0)):transform.position-new Vector3(0,height*.5f);
  if(!string.IsNullOrEmpty(poseKey)){height=poseHeight;foot+=Vector3.up*poseLift;}
  paint.transform.position=foot;var scale=transform.lossyScale;float unit=height/sprite.bounds.size.y;paint.transform.localScale=new Vector3(unit/Mathf.Abs(scale.x),unit/Mathf.Abs(scale.y),1);
  paint.color=new Color(1,1,1,body.color.a);paint.enabled=gameObject.activeInHierarchy;
  body.forceRenderingOff=true;
  HideLegacyOutfit();
  if(PersistentRuntime.Instance){var old=PersistentRuntime.Instance.transform.Find("AdultDefault_Visual");if(old&&old.TryGetComponent<SpriteRenderer>(out var r))r.forceRenderingOff=true;}
 }
 void HideLegacyOutfit(){
  foreach(var r in GetComponentsInChildren<SpriteRenderer>(true)){
   if(r==paint||r==body||r.GetComponentInParent<P02CharacterRig>())continue;
   bool legacy=r.name=="BlueBib"||r.name=="Strap";
   for(var t=r.transform;t&&t!=transform;t=t.parent)
    legacy|=t.name.Contains("Placeholder")||t.name.Contains("SchoolUniform");
   if(legacy)r.forceRenderingOff=true;
  }
 }
 void OnDisable(){if(body)body.forceRenderingOff=originalForceOff;if(paint)paint.enabled=false;}
}
