using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Visual children follow the ending's existing moving sets and fixed bed anchor.</summary>
public sealed class EndingArtwork : MonoBehaviour
{
 [SerializeField] PresentationArtLibrary art;
 [SerializeField] Material softForeground;
 Transform decorated; bool laneDecorated,closeupDecorated;
 void LateUpdate(){
  if(!art)return;
  if(!decorated){var root=transform.Find("EndingPresentation");if(!root||!root.Find("HospitalWard/WardBed"))return;Decorate(root);decorated=root;}
  if(!laneDecorated&&decorated.Find("EndingWalkGround")){DecorateLane(decorated);laneDecorated=true;}
  if(!closeupDecorated){var closeup=transform.Find("EndingCloseupAndCompletion/BedCloseup");if(closeup){DecorateCloseup(closeup);closeupDecorated=true;}}
 }
 void Hide(Transform root,System.Func<SpriteRenderer,bool> predicate){
  var old=root.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>!r.name.StartsWith("NewArt_")&&predicate(r)).ToArray();
  if(old.Length==0)return;var g=new GameObject("NewArt_Retired");g.transform.SetParent(root,false);g.AddComponent<RetiredArtVisibility>().SetRenderers(old);
 }
 void Actor(Transform t,string key,float height){
  if(!t)return;
  // Keep hand, arm, pendant and wound gesture nodes visible and animated.
  Hide(t,r=>new[]{"Coat","HeadSilhouette","LeftLeg","RightLeg"}.Contains(r.name));
  art.Add(t,"NewArt_Actor",key,new Vector2(0,-1.055f),0,height,23);
  var arm=t.Find("Arm");var hand=t.Find("Hand");
  if(arm){Hide(arm,r=>r.transform==arm);var sleeve=art.Add(arm,"NewArt_Sleeve",key=="adult_qt"?"dark_sleeve":"uniform_sleeve",new Vector2(-.5f,0),1,1,24);sleeve.transform.localRotation=Quaternion.Euler(0,0,-90);}
  if(hand){Hide(hand,r=>r.transform==hand);art.Add(hand,"NewArt_Hand","reaching_hand",new Vector2(0,-.5f),1.6f,1,25);}
  var necklace=t.Find("PlectrumNecklace");if(necklace){Hide(necklace,r=>true);art.Add(necklace,"NewArt_Pendant","pendant",Vector2.zero,0,.42f,26);}
  var wound=t.Find("CoveredArmWound");if(wound){Hide(wound,r=>true);art.Add(wound,"NewArt_Wound","arm_wound",new Vector2(0,-.5f),1,1,25);}
 }
 void Decorate(Transform root){
  Camera.main.backgroundColor=new Color(.02f,.025f,.03f);
  var letterbox=new GameObject("NewArt_EndingLetterbox",typeof(Canvas));letterbox.transform.SetParent(root,false);var canvas=letterbox.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=-1000;
  for(int i=0;i<2;i++){var bar=new GameObject("Bar",typeof(RectTransform),typeof(Image));bar.transform.SetParent(letterbox.transform,false);var rect=(RectTransform)bar.transform;rect.anchorMin=new Vector2(0,i==0?.88f:0);rect.anchorMax=new Vector2(1,i==0?1:.12f);rect.offsetMin=rect.offsetMax=Vector2.zero;bar.GetComponent<Image>().color=Color.black;bar.GetComponent<Image>().raycastTarget=false;}
  Hide(root,r=>r.name=="ForegroundLane");
  var director=GetComponent<EndingDirector>();if(director&&director.riverLandmarksTemplate)Hide(director.riverLandmarksTemplate.transform,r=>true);
  var player=PersistentRuntime.Instance.GetComponent<GameControlManager>().ActivePlayer;var pain=player.transform.Find("HeadachePose");
  if(pain)Hide(pain,r=>true); // DemoActorArtwork selects the complete authored headache pose.
  var car=root.Find("CarRearSeat");
  Hide(car,r=>true);
  art.Add(car,"NewArt_CarInterior","car_rear",new Vector2(0,-3),16,9,-8);
  art.Add(car,"NewArt_SeatedXiaoPeng","seated_xp",new Vector2(-3.4f,-4.1f),0,7.8f,23);
  art.Add(car,"NewArt_SeatedQiuTian","seated_qt",new Vector2(3.4f,-4.1f),0,7.8f,23);
  // Only the car shot covers the standing adult; original renderer state is
  // restored as soon as the existing car set is disabled for the walk sequence.
  var carCover=new GameObject("NewArt_CarStandingCover");carCover.transform.SetParent(car,false);
  carCover.AddComponent<RetiredArtVisibility>().SetRenderers(player.GetComponentsInChildren<SpriteRenderer>(true).Concat(PersistentRuntime.Instance.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.name=="AdultDefault_Visual")).Distinct().Cast<Renderer>().ToArray());
  foreach(string set in new[]{"ChildhoodFarewell","YouthFarewell"}){
   var t=root.Find(set);bool youth=set=="YouthFarewell";float center=youth?23:9;
   Hide(t,r=>r.name=="MemorySky"||r.name=="River"||r.name=="RiverbankStage"||r.transform.GetComponentsInParent<Transform>(true).Any(p=>p.name=="Scene03RiverVisualsOnly"));
   art.Add(t,"NewArt_River",youth?"river_rain":"river_day",new Vector2(center,-1.5f),32,10.67f,-20);
   for(int n=0;n<4;n++){float height=7.02f*art.Get("floor_n").rect.height/art.Get("floor_n").rect.width;art.Add(t,"NewArt_RiverStage_"+n,"floor_n",new Vector2(center-10.5f+n*7,1.35f-height),7.02f,0,-6).color=new Color(.58f,.63f,.62f,1);}
   art.Add(t,"NewArt_Pavilion","pavilion",new Vector2(center-.8f,1.35f),7,4.6f,-4);
   art.Add(t,"NewArt_Tree","river_tree",new Vector2(center+8,1.35f),7.5f,7.3f,-5);
   Actor(t.Find(youth?"YouthMemoryView":"ChildMemoryView"),youth?"youth_xp":"child_xp",2.05f);
   Actor(t.Find(youth?"FriendMemoryView":"BoyMemoryView"),youth?"youth_qt":"child_qt",2.05f);
  }
  foreach(string set in new[]{"HospitalCorridor","HospitalWard"}){
   var t=root.Find(set);var wall=t.Find("HospitalWall");
   // The wall's original transform follows the player in the last segment.
   // Child-local dimensions account for its existing 28x9 scale.
   Hide(t,r=>r.transform.parent==t&&(r.name=="HospitalWall"||r.name=="WallRail"||r.name=="CeilingLight"));
   var shell=new GameObject("NewArt_HospitalShell").transform;shell.SetParent(wall,false);shell.localScale=new Vector3(1/wall.lossyScale.x,1/wall.lossyScale.y,1);shell.localPosition=new Vector3(0,-3/wall.lossyScale.y,0);
   for(int i=-3;i<=3;i++)Surface(art.Add(shell,"NewArt_Wall_"+i,"hospital_lowwall",new Vector2(i*5.8f,-.92f),5.82f,0,-16));
   if(set=="HospitalCorridor"){
    art.Add(shell,"NewArt_Door","hospital_door",new Vector2(-3.6f,-.94f),0,2.15f,-12);
    var bench=art.Add(shell,"NewArt_WaitingBench","hospital_bench",new Vector2(.7f,-1.02f),0,.8f,-11);Contact(bench);
    art.Add(shell,"NewArt_WallInfo","hospital_info",new Vector2(.7f,.40f),0,.6f,-12);
    var plant=art.Add(shell,"NewArt_FloorPlant","hospital_plant",new Vector2(2.5f,-1.02f),0,1.05f,-11);Contact(plant);
   }
   foreach(Transform bed in t.Cast<Transform>().Where(x=>x.name.StartsWith("AnonymousStretcher_")).ToArray()){
    Hide(bed,r=>r.transform.parent==bed);
    var gurney=art.Add(bed,"NewArt_Gurney","gurney",new Vector2(0,-1.1f-bed.localPosition.y),0,1.0f,21);Contact(gurney);
    var staff=bed.Find("MedicalStaff");
    if(staff){Hide(staff,r=>true);var nurse=art.Add(staff,"NewArt_Staff",bed.name.EndsWith("1")?"nurse":"doctor",new Vector2(0,(-1.1f-staff.position.y)/staff.lossyScale.y),0,1.8f/staff.lossyScale.y,23);Contact(nurse);}
   }
   if(set=="HospitalWard"){
    var bed=t.Find("WardBed");Hide(t,r=>r.transform.parent==t&&r.name.StartsWith("Ward"));
    var bedRoot=new GameObject("NewArt_BedScale").transform;bedRoot.SetParent(bed,false);bedRoot.localScale=new Vector3(1/bed.lossyScale.x,1/bed.lossyScale.y,1);var bedView=art.Add(bedRoot,"NewArt_Bed","ward_side_bed",new Vector2(0,-1.1f),2.3f,0,-2);Contact(bedView);
    float bx=bed.localPosition.x;
    if(softForeground){
     var nearSeat=art.Add(t,"NewArt_NearWaitingSeatSilhouette","hospital_bench",new Vector2(bx-10.0f,-2.0f),0,1.23f,65);
     nearSeat.sharedMaterial=softForeground;nearSeat.color=new Color(.055f,.065f,.065f,.72f);
    }
    // Ward furnishings stay in world space with the bed. Only the plain wall extends.
    art.Add(t,"NewArt_WardEntranceDoor","hospital_door",new Vector2(bx-8.6f,-.94f),0,2.15f,-12);
    var seat=art.Add(t,"NewArt_WardWaitingBench","hospital_bench",new Vector2(bx-6.2f,-1.05f),0,.8f,-11);Contact(seat);
    art.Add(t,"NewArt_WardLeaflets","hospital_info",new Vector2(bx-6.2f,.40f),0,.6f,-12);
    var green=art.Add(t,"NewArt_WardPlant","hospital_plant",new Vector2(bx-4.9f,-1.05f),0,.9f,-11);Contact(green);
    var cart=art.Add(t,"NewArt_WardSupplyCart","ward_cabinet",new Vector2(bx-3.7f,-1.1f),0,.9f,-10);Contact(cart);
    art.Add(t,"NewArt_WardWindow","ward_window",new Vector2(bx-3.3f,.2f),0,1.3f,-10);
    art.Add(t,"NewArt_MedicalPanel","ward_panel",new Vector2(bx,.45f),0,.48f,-9);
    art.Add(t,"NewArt_PrivacyCurtain","ward_curtain",new Vector2(bx+2.4f,-1.1f),0,2.6f,-1);
    var cabinet=art.Add(t,"NewArt_BedsideCabinet","ward_cabinet",new Vector2(bx-1.8f,-1.1f),0,.86f,-2);Contact(cabinet);
   }
  }
 }
 void DecorateLane(Transform root){
  foreach(Transform tile in root.Cast<Transform>().Where(t=>t.name=="EndingWalkGround").ToArray()){
   Hide(tile,r=>r.name=="Ground"||r.name=="GroundSeam");
   for(int i=0;i<2;i++){float height=16.02f*art.Get("hospital_floor").rect.height/art.Get("hospital_floor").rect.width;Surface(art.Add(tile,"NewArt_Floor_"+i,"hospital_floor",new Vector2(-8+i*16,-.92f-height),16.02f,0,-14));}
  }
 }
 static void Surface(SpriteRenderer r){if(r)r.sharedMaterial=Resources.Load<Material>("DemoSurface_"+r.sprite.name.Replace(':','_'));}
 static void Contact(SpriteRenderer r){if(r){var c=r.gameObject.AddComponent<PaintedContactShadow>();c.target=r;c.depth=.10f;}}
 void DecorateCloseup(Transform page){
  var sprite=art.Get("covered_bed");if(!sprite)return;
  // The original permanent featureless face cover and animated focus haze stay above this visual.
  foreach(Transform child in page)if(new[]{"BedFrame","Blanket","Pillow","NearRail","BedLegLeft","BedLegRight"}.Contains(child.name)){var image=child.GetComponent<Image>();if(image)image.enabled=false;}
  var g=new GameObject("NewArt_BedCloseup",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
  var rect=(RectTransform)g.transform;rect.SetParent(page,false);rect.anchorMin=new Vector2(.24f,.17f);rect.anchorMax=new Vector2(.89f,.79f);rect.offsetMin=rect.offsetMax=Vector2.zero;
  var imageView=g.GetComponent<Image>();imageView.sprite=sprite;imageView.preserveAspect=true;imageView.raycastTarget=false;
  var cover=page.Find("PermanentFeaturelessHeadOcclusion");if(cover)rect.SetSiblingIndex(cover.GetSiblingIndex());
 }
}


