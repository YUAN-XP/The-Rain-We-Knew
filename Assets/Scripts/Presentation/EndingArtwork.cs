using System.Linq;
using UnityEngine;
using UnityEngine.UI;
/// <summary>Ending-local art. All walking sets are fixed; actor gesture ownership is native and singular.</summary>
public sealed class EndingArtwork:MonoBehaviour {
 [SerializeField] PresentationArtLibrary art;
 [SerializeField] Material softForeground;
 Transform decorated;bool laneDecorated;
 public Sprite PendantSprite=>art?art.Get("pendant"):null;
 void LateUpdate(){
  if(!art)return;if(!decorated){var r=transform.Find("EndingPresentation");if(!r||!r.Find("HospitalWard/WardBed"))return;Decorate(r);decorated=r;}
  if(!laneDecorated&&decorated.Find("EndingWalkGround")){DecorateLane(decorated);laneDecorated=true;}
 }
 void Hide(Transform root){var old=root.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>!r.name.StartsWith("NewArt_")).ToArray();if(old.Length==0)return;var g=new GameObject("NewArt_Retired");g.transform.SetParent(root,false);g.AddComponent<RetiredArtVisibility>().SetRenderers(old);}
 static SpriteRenderer NativeArt(Transform parent,string name,string key,Vector2 foot,float height,int order){
  var sprite=DemoArtCatalog.Find(key);if(!sprite){Debug.LogError("P06 missing art: "+key);return null;}var r=new GameObject(name).AddComponent<SpriteRenderer>();r.transform.SetParent(parent,false);r.sprite=sprite;r.sortingOrder=order;float scale=height/sprite.bounds.size.y;r.transform.localScale=Vector3.one*scale;r.transform.localPosition=new Vector3(foot.x-sprite.bounds.center.x*scale,foot.y-sprite.bounds.min.y*scale,0);return r;
 }
 void Actor(Transform t,string key,float height,bool anonymous=false){
  if(!t)return;Hide(t);t.localScale=Vector3.one;var p=t.position;p.y=-1.1f+1.055f;t.position=p;
  var r=NativeArt(t,"NewArt_Actor",key=="child"?"CHR_XP_CHILD_BASE:0":key=="youth"?"CHR_XP_YOUTH_BASE:0":key=="boy_child"?"CHR_BOY_CHILD_BASE:0":"CHR_GUIDE_BASE_V02:0",new Vector2(0,-1.055f),height,23);
  var native=P02ActorPresentation.AttachCompanion(t,r,key,height);native.RestoreFacing(anonymous);
  var actor=t.GetComponent<EndingActorView>();if(actor)actor.nativePose=native;
  if(anonymous)t.gameObject.AddComponent<P06AnonymousActor>();
  if(actor&&t.name=="BoyMemoryView"){var pendant=art.Add(t,"NewArt_PlectrumNecklace","pendant",new Vector2(0,0),0,.16f,26);actor.pendant=pendant.transform;pendant.gameObject.SetActive(false);}
 }
 void Decorate(Transform root){
  Camera.main.backgroundColor=new Color(.055f,.065f,.07f);
  var letterbox=new GameObject("NewArt_EndingLetterbox",typeof(Canvas));letterbox.transform.SetParent(root,false);var canvas=letterbox.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=-1000;
  for(int i=0;i<2;i++)RuntimePanelUI.Panel("Bar",letterbox.transform,new Vector2(0,i==0?.88f:0),new Vector2(1,i==0?1:.12f),Color.black,false);
  var foreground=root.Find("ForegroundLane");if(foreground)Hide(foreground);
  var player=PersistentRuntime.Instance.GetComponent<GameControlManager>().ActivePlayer;var pain=player.transform.Find("HeadachePose");if(pain)Hide(pain);
  var car=root.Find("CarRearSeat");Hide(car);art.Add(car,"NewArt_CarInterior","car_rear",new Vector2(0,-3),16,9,-8);
  var story=PersistentRuntime.Instance.GetComponent<StoryStateManager>();string outfit=story.Outfit==AdultOutfit.Overalls?"1":story.Outfit==AdultOutfit.SchoolUniform?"2":"0";
  NativeArt(car,"NewArt_SeatedXiaoPeng","P06_CarSeated:"+outfit,new Vector2(-2.05f,-3.0f),6f,23);NativeArt(car,"NewArt_SeatedQiuTian","P06_CarSeated:3",new Vector2(2.05f,-3.0f),6f,23);
  var carCover=new GameObject("NewArt_CarStandingCover");carCover.transform.SetParent(car,false);carCover.AddComponent<RetiredArtVisibility>().SetRenderers(player.GetComponentsInChildren<SpriteRenderer>(true));
  foreach(string set in new[]{"ChildhoodFarewell","YouthFarewell"}){
   var t=root.Find(set);Hide(t);bool youth=set=="YouthFarewell";float centre=youth?23:9;
   EndingActorView.Part("NewArt_AnonymousBackdrop",t,GetComponent<EndingDirector>().data.bodySprite,new Vector2(centre,2.25f),new Vector2(28,7),new Color(.16f,.20f,.22f),-25);
   var xp=t.Find(youth?"YouthMemoryView":"ChildMemoryView");var other=t.Find(youth?"FriendMemoryView":"BoyMemoryView");other.localPosition=new Vector3(xp.localPosition.x+.42f,other.localPosition.y,0);
   Actor(xp,youth?"youth":"child",youth?1.8f:1.50f);Actor(other,youth?"qt_adult":"boy_child",youth?1.80f:1.55f,true);
  }
  var corridor=root.Find("HospitalCorridor");Hide(corridor);
  EndingActorView.Part("NewArt_AnonymousBackdrop",corridor,GetComponent<EndingDirector>().data.bodySprite,new Vector2(38,2.25f),new Vector2(28,7),new Color(.20f,.24f,.25f),-25);
  foreach(Transform bed in corridor.Cast<Transform>().Where(t=>t.name.StartsWith("AnonymousStretcher_")).ToArray()){
   var gurney=art.Add(bed,"NewArt_AnonymousGurney","gurney",new Vector2(0,-1.1f-bed.localPosition.y),0,1.0f,21);gurney.color=new Color(.07f,.085f,.09f,.92f);
   var staff=bed.Find("MedicalStaff");Actor(staff,"qt_adult",1.8f,true);
  }
  var ward=root.Find("HospitalWard");Hide(ward);
  var plate=NativeArt(ward,"NewArt_CurtainedWard","P06_WardWide",new Vector2(49,-1.56f),4.4f,-8);
  ward.gameObject.AddComponent<P06WardArtworkBounds>().Initialize(plate,GetComponent<EndingDirector>());
  // Nothing is rendered behind the opaque curtain. The plate and its quiet floor share a fixed owner.

 }
 void DecorateLane(Transform root){
  var tile=root.Find("EndingWalkGround");Hide(tile);
  var r=NativeArt(tile,"NewArt_QuietGround","P04_QuietMemory",new Vector2(28,-3.4f),2.3f,6);
  if(r){r.drawMode=SpriteDrawMode.Tiled;r.transform.localScale=Vector3.one;r.size=new Vector2(128,2.3f);r.transform.localPosition=new Vector3(28,0,0);r.transform.position+=Vector3.up*(-1.1f-r.bounds.max.y);r.gameObject.AddComponent<P06EndingGroundVisibility>();r.color=new Color(.68f,.73f,.75f,1);}
 }
}
[DefaultExecutionOrder(220)]
public sealed class P06AnonymousActor:MonoBehaviour {
 void LateUpdate(){foreach(var r in GetComponentsInChildren<SpriteRenderer>(true))if(r.GetComponentInParent<P02CharacterRig>())r.color=new Color(.055f,.07f,.08f,.91f);}
}
