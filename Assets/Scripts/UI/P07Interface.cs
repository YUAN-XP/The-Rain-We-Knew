using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Fresh runtime owns the opening once. Disk load uses the existing runtime and
// never traverses this entry. Session restart destroys all chapter-local state.
[DisallowMultipleComponent]
public sealed class P07Interface:MonoBehaviour {
 public static bool NewGamePending;
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetStartup(){NewGamePending=false;}
 public bool OpeningActive {get;private set;}
 public string OpeningPhase {get;private set;}
 GameControlManager controls;GameObject page;Image cover;CameraFollow openingFollow;bool priorFollow;
 void Awake(){controls=GetComponent<GameControlManager>();if(!GetComponent<P07QuestDisplay>())gameObject.AddComponent<P07QuestDisplay>();if(!GetComponent<P07AcquisitionPresentation>())gameObject.AddComponent<P07AcquisitionPresentation>();P07SettingsPanel.ApplySaved();controls.AcquireLock(ControlLockReason.Cutscene,this);}
 IEnumerator Start(){yield return null;yield return null;
  if(SceneManager.GetActiveScene().name!="Scene01_Rain"){controls.ReleaseLock(ControlLockReason.Cutscene,this);yield break;}
  if(NewGamePending){NewGamePending=false;yield return Opening();}else{GetComponent<GamePause>().ShowMainMenu();controls.ReleaseLock(ControlLockReason.Cutscene,this);}
 }
 IEnumerator Fade(Color from,Color to,float duration){for(float t=0;t<duration;t+=GameplayClock.Delta){cover.color=Color.Lerp(from,to,t/duration);yield return null;}cover.color=to;}
 IEnumerator Opening(){OpeningActive=true;OpeningPhase="white";page=RuntimePanelUI.Canvas("P07Opening",transform,31000).gameObject;cover=RuntimePanelUI.Panel("OpeningPaper",page.transform,Vector2.zero,Vector2.one,Color.black);
  yield return Fade(Color.black,Color.white,1.6f);
  string[] lines={"So we beat on, boats against the current,","borne back ceaselessly into the past.","F. Scott Fitzgerald — The Great Gatsby","于是我们继续向前，逆流而行，","却又不断被推回往昔。","——《了不起的盖茨比》"};
  TMP_Text[] texts=new TMP_Text[6];float[] ys={.77f,.66f,.55f,.40f,.29f,.18f};
  for(int i=0;i<6;i++){texts[i]=P07UI.Text(lines[i],page.transform,new Vector2(.13f,ys[i]),new Vector2(.91f,ys[i]+.075f),i==2||i==5?28:34,true);texts[i].maxVisibleCharacters=0;}
  OpeningPhase="paired_quote";
  for(int pair=0;pair<3;pair++){float duration=pair==2?2.2f:2.6f;for(float t=0;t<duration;t+=GameplayClock.Delta){texts[pair].maxVisibleCharacters=Mathf.CeilToInt(lines[pair].Length*t/duration);texts[pair+3].maxVisibleCharacters=Mathf.CeilToInt(lines[pair+3].Length*t/duration);yield return null;}texts[pair].maxVisibleCharacters=texts[pair+3].maxVisibleCharacters=int.MaxValue;}
  OpeningPhase="hold_three_seconds";yield return GameplayClock.Wait(3);
  OpeningPhase="ink_melt";var melt=RuntimePanelUI.Rect("QuoteInkMelt",page.transform,Vector2.zero,Vector2.one).gameObject.AddComponent<P07InkMelt>();melt.raycastTarget=false;
  for(float t=0;t<2.4f;t+=GameplayClock.Delta){melt.progress=t/2.4f;melt.SetVerticesDirty();foreach(var x in texts)x.color=new Color(0,0,0,Mathf.Clamp01(1-t/.8f));yield return null;}cover.color=Color.black;Destroy(melt.gameObject);foreach(var x in texts)x.gameObject.SetActive(false);
  OpeningPhase="headache_no_portrait";var line=P07UI.Text("头好痛，这是哪里？",page.transform,new Vector2(.20f,.16f),new Vector2(.80f,.27f),33);line.alignment=TextAlignmentOptions.Center;yield return GameplayClock.Wait(2.8f);line.gameObject.SetActive(false);
  var player=controls.ActivePlayer;GroundedPlacement.Settle(player);var actor=player.GetComponent<P02ActorPresentation>();actor.RestoreDirection("right");
  RoomPresentationFraming.PrepareCurrentView();var camera=Camera.main;Vector3 finalPosition=camera.transform.position;float finalSize=camera.orthographicSize;
  openingFollow=camera.GetComponent<CameraFollow>();priorFollow=openingFollow&&openingFollow.enabled;if(openingFollow)openingFollow.enabled=false;
  while(!actor.Rig)yield return null;actor.Rig.OpeningRise=0;actor.Rig.ApplyPose(0);OpeningPhase="rise_and_camera";
  float distantSize=finalSize*1.48f;Vector3 distant=finalPosition+Vector3.up*.15f;camera.orthographicSize=distantSize;camera.transform.position=distant;
  for(float t=0;t<6.5f;t+=GameplayClock.Delta){float u=Mathf.Clamp01(t/6.5f),s=Mathf.SmoothStep(0,1,u);actor.Rig.OpeningRise=u;camera.orthographicSize=Mathf.Lerp(distantSize,finalSize,s);camera.transform.position=Vector3.Lerp(distant,finalPosition,s);cover.color=new Color(0,0,0,1-Mathf.Clamp01(t/1.6f));yield return null;}
  actor.Rig.OpeningRise=-1;actor.ResetTravel();camera.orthographicSize=finalSize;camera.transform.position=finalPosition;Destroy(page);if(openingFollow)openingFollow.enabled=priorFollow;OpeningActive=false;OpeningPhase="free";GameplayInput.ConsumeFrame();controls.ReleaseLock(ControlLockReason.Cutscene,this);GetComponent<QuestProgress>().Publish(QuestEvent.OpeningControlGranted);
 }
 void OnDestroy(){if(openingFollow)openingFollow.enabled=priorFollow;if(controls)controls.ReleaseLock(ControlLockReason.Cutscene,this);}
}
[RequireComponent(typeof(CanvasRenderer))]
public sealed class P07InkMelt:MaskableGraphic {
 public float progress;
 protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();var r=rectTransform.rect;float[] y={.8f,.69f,.58f,.43f,.32f,.21f};
  for(int row=0;row<6;row++)for(int n=0;n<14;n++){var center=new Vector2(r.xMin+r.width*(.145f+n*.047f),r.yMin+r.height*y[row]);float radius=Mathf.Lerp(1,r.width*.65f,Mathf.Pow(progress,2.4f))*(.90f+.1f*Mathf.Sin(n*3.1f+row));int v=vh.currentVertCount;vh.AddVert(center,Color.black,Vector2.zero);
   for(int k=0;k<=24;k++){float a=k*Mathf.PI/12;float wiggle=1+.05f*Mathf.Sin(a*5+progress*6+n);vh.AddVert(center+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius*wiggle,Color.black,Vector2.zero);if(k>0)vh.AddTriangle(v,v+k,v+k+1);}
  }
 }
}
