using UnityEngine;

/// <summary>P03 spatial presentation. The bedroom exit retains its existing fade and child follow-up.</summary>
public class DistortedCorridorController : MonoBehaviour
{
 [SerializeField] GameObject corridorArea;
 [SerializeField] RoomTransitionInteractable bedroomExit;
 [SerializeField] SpriteRenderer familiarDoor;
 [SerializeField] SpriteRenderer[] anomalyLayers=new SpriteRenderer[0];
 [SerializeField] SpriteRenderer[] lamps=new SpriteRenderer[0];
 [SerializeField] float[] lampCrossings=new[]{0f,7f};
 [SerializeField] Collider2D rearBoundary;
 [SerializeField] GameObject sourceExtension,originalRightBoundary;
 [SerializeField] Collider2D joinedRearBoundary;
 [SerializeField] AudioSource guitarGuide;
 [SerializeField] float remarkWorldX=-8f;
 [SerializeField] GameObject[] disabledDuringDistortion=new GameObject[0];
 PlayerController player;StoryStateManager story;StoryNoticeUI notice;GameControlManager controls;
 bool joined;int lampStage;float flickerUntil;Color[] colors,lampColors;
 public bool HasJoined=>joined;
 public int LampStage=>lampStage;
 void Start()
 {
  player=FindAnyObjectByType<PlayerController>();story=FindAnyObjectByType<StoryStateManager>();notice=FindAnyObjectByType<StoryNoticeUI>();controls=FindAnyObjectByType<GameControlManager>();
  colors=new Color[anomalyLayers.Length];for(int i=0;i<colors.Length;i++)if(anomalyLayers[i])colors[i]=anomalyLayers[i].color;
  lampColors=new Color[lamps.Length];for(int i=0;i<lamps.Length;i++)if(lamps[i])lampColors[i]=lamps[i].color;
  if(bedroomExit)bedroomExit.MovedUnderFade+=BedroomExited;
  SetActive(false);
  if(story&&story.Has(StoryFlag.DistortedCorridorStarted)&&!story.Has(StoryFlag.Scene03Completed))RestoreSave(true,0);
 }
 void BedroomExited(RoomTransitionInteractable exit)
 {
  if(story&&story.Has(StoryFlag.ChildShadowSequencePlayed)&&!story.Has(StoryFlag.Scene03Completed))
  {
   story.Set(StoryFlag.DistortedCorridorStarted);joined=true;SetActive(true);
   // No position offset and no additional fade: the destination is the original second floor.
   RoomPresentationFraming.PrepareCurrentView();
  }
 }
 public void RestoreSave(bool value,int stage=0)
 {
  joined=value&&story&&!story.Has(StoryFlag.Scene03Completed);
  lampStage=Mathf.Clamp(stage,0,2);flickerUntil=0;SetActive(joined);ApplyPresentation();
 }
 void SetActive(bool value)
 {
  if(corridorArea)corridorArea.SetActive(value);
  if(rearBoundary)rearBoundary.enabled=value;
  if(joinedRearBoundary)joinedRearBoundary.enabled=value;
  if(sourceExtension)sourceExtension.SetActive(value);
  if(originalRightBoundary)originalRightBoundary.SetActive(!value);
  foreach(var g in disabledDuringDistortion)if(g)g.SetActive(!value);
  if(guitarGuide){guitarGuide.volume=0;if(!value)guitarGuide.Stop();}
 }
 void LateUpdate()
 {
  if(!story||!player||story.Has(StoryFlag.Scene03Completed)||!joined)return;
  if(Mathf.Abs(player.transform.position.y-7.43f)>2)return;
  var body=player.GetComponent<Rigidbody2D>();float x=player.transform.position.x;
  if(x>remarkWorldX&&body&&body.linearVelocity.x>.1f&&!story.Has(StoryFlag.CorridorRemarkShown))
  {story.Set(StoryFlag.CorridorRemarkShown);if(notice)notice.Show("走廊好像不一样了。",3.5f);}
  if(!controls||!controls.IsLocked())
   while(lampStage<Mathf.Min(2,lampCrossings.Length)&&x>=lampCrossings[lampStage])
   {lampStage++;flickerUntil=(float)GameplayClock.Elapsed+1.1f;}
  ApplyPresentation();
 }
 void ApplyPresentation()
 {
  float clock=(float)GameplayClock.Elapsed;
  float strength=lampStage*.5f;
  for(int i=0;i<anomalyLayers.Length;i++)if(anomalyLayers[i])
  {var c=colors!=null&&i<colors.Length?colors[i]:Color.white;c.a*=strength;anomalyLayers[i].color=c;}
  for(int i=0;i<lamps.Length;i++)if(lamps[i])
  {
   var c=lampColors!=null&&i<lampColors.Length?lampColors[i]:Color.white;
   float light=i<lampStage?.34f:1f;
   if(i==lampStage-1&&clock<flickerUntil)light=Mathf.Sin(clock*39f)>.2f?.78f:.06f;
   lamps[i].color=new Color(c.r*light,c.g*light,c.b*light,c.a);
  }
  if(guitarGuide&&player)
  {
   guitarGuide.volume=player.transform.position.x<9?strength*.22f:0;
   if(joined&&!guitarGuide.isPlaying)guitarGuide.Play();
  }
 }
 void OnDestroy(){if(bedroomExit)bedroomExit.MovedUnderFade-=BedroomExited;}
}


