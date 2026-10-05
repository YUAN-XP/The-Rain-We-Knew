
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
public static class P07Coverage {
 public static void Opening()=>Run(true);
 public static void Clear(){if(EditorApplication.isPlaying)throw new Exception("Stop Play Mode before clearing fixtures");foreach(var g in Resources.FindObjectsOfTypeAll<GameObject>().Where(g=>!EditorUtility.IsPersistent(g)&&(g.name=="P07_test_floor_reference"||g.name.StartsWith("P07_Isolated")||g.name=="P07_isolated_semantic_observer"||g.name=="P07_InactiveRuleFixture")))UnityEngine.Object.DestroyImmediate(g);}
 public static void AuditLines(){Directory.CreateDirectory(P07Regression.Work+"checks");var lines=Resources.FindObjectsOfTypeAll<LineRenderer>().Where(l=>!EditorUtility.IsPersistent(l)).Select(l=>l.name+" | scene="+l.gameObject.scene.name+" valid="+l.gameObject.scene.IsValid()+" flags="+l.gameObject.hideFlags+" parent="+(l.transform.parent?l.transform.parent.name:"none")+" material="+(l.sharedMaterial?l.sharedMaterial.name:"none"));File.WriteAllLines(P07Regression.Work+"checks/lines-"+DateTime.Now.ToString("HHmmss")+".txt",lines);}

 public static void Tasks()=>Run(false);
 static void Run(bool opening){if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");if(GameplayInput.Replay!=null)throw new Exception("Replay already active");var g=new GameObject("P07_IsolatedCoverage"){hideFlags=HideFlags.DontSave};g.AddComponent<P07CoverageDriver>().Begin(opening);}
}
public sealed class P07CoverageDriver:MonoBehaviour,GameplayInput.IReplaySource {
 string folder;bool opening,cleaned;readonly List<string> results=new List<string>();GameObject observer;QuestSnapshot before;P02CharacterRig rig;int down;KeyCode key=KeyCode.None;
 PersistentRuntime Rt=>PersistentRuntime.Instance;QuestProgress Quest=>Rt.GetComponent<QuestProgress>();GameControlManager Controls=>Rt.GetComponent<GameControlManager>();
 public bool Key(KeyCode k)=>k==key;public bool KeyDown(KeyCode k)=>k==key&&Time.frameCount==down;public float Axis(string a)=>0;public bool Mouse(int b)=>false;public bool MouseDown(int b)=>false;public bool MouseUp(int b)=>false;public Vector3 Pointer=>Input.mousePosition;
 public void Begin(bool isOpening){opening=isOpening;folder=P07Regression.Work+"runtime/"+(opening?"opening":"tasks")+"-"+DateTime.Now.ToString("HHmmss");Directory.CreateDirectory(folder);before=Quest.Capture();GameplayInput.Replay=this;Rt.GetComponent<GamePause>().SetPaused(false);StartCoroutine(Guard(opening?Rise():Tasks()));}
 IEnumerator Guard(IEnumerator task){var stack=new Stack<IEnumerator>();stack.Push(task);while(stack.Count>0){var a=stack.Peek();bool more=false;object v=null;try{more=a.MoveNext();if(more)v=a.Current;}catch(Exception e){Check(false,"EXCEPTION "+e);break;}if(!more){stack.Pop();continue;}if(v is IEnumerator b)stack.Push(b);else yield return v;}Cleanup();File.WriteAllText(folder+"/status.txt","DONE checks="+results.Count+" failures="+results.Count(x=>x.StartsWith("FAIL"))); }
 void Cleanup(){if(cleaned)return;cleaned=true;key=KeyCode.None;P07EvidenceRecorder.Stop();if(rig){rig.OpeningRise=-1;rig.ResetTravel();}if(observer)Destroy(observer);if(Rt){Controls.ReleaseLock(ControlLockReason.Cutscene,this);Controls.ReleaseLock(ControlLockReason.Dialogue,this);Rt.GetComponent<GamePause>().Thaw(this);Quest.Restore(before);}if(ReferenceEquals(GameplayInput.Replay,this))GameplayInput.Replay=null;}
 void OnDestroy()=>Cleanup();
 void Check(bool ok,string s){results.Add((ok?"PASS ":"FAIL ")+s);File.WriteAllLines(folder+"/results.txt",results);}
 IEnumerator Wait(float n){float t=0;while(t<n){while(!Application.isFocused){File.WriteAllText(folder+"/status.txt","WAIT Unity foreground; original focus pause preserved");yield return null;}var pause=Rt.GetComponent<GamePause>();if(pause.MenuOpen)pause.SetPaused(false);t+=Time.unscaledDeltaTime;yield return null;}}
 IEnumerator Shot(string name){P07EvidenceRecorder.ReserveStillFrame();yield return new WaitForEndOfFrame();ScreenCapture.CaptureScreenshot(folder+"/"+name+".png");yield return Wait(.1f);}
 IEnumerator Rise(){Controls.AcquireLock(ControlLockReason.Cutscene,this);var actor=Controls.ActivePlayer.GetComponent<P02ActorPresentation>();while(!actor.Rig)yield return null;rig=actor.Rig;rig.ResetTravel();
 // Ground contact is reviewed against the actual painted street; no diagnostic object is added.
 var stats=new List<string>{"u,nearSoleError,farSoleError,nearEndpointGap,farEndpointGap,nearKneeY,farKneeY,nearAngle,farAngle"};
 foreach(float u in new[]{0f,.12f,.22f,.31f,.34f,.42f,.62f,.70f,.82f,.95f,1f}){rig.OpeningRise=u;yield return Wait(.14f);yield return new WaitForEndOfFrame();var joints=rig.GetComponentsInChildren<Transform>();float kneeN=joints.First(x=>x.name=="near_shin").position.y-rig.transform.position.y,kneeF=joints.First(x=>x.name=="far_shin").position.y-rig.transform.position.y;
 stats.Add(string.Join(",",u,rig.nearPaintedSoleY-rig.transform.position.y,rig.farPaintedSoleY-rig.transform.position.y,rig.nearLegEndpointGap,rig.farLegEndpointGap,kneeN,kneeF,rig.nearSupportAngle,rig.farSupportAngle));Check(kneeN>=-.002f&&kneeF>=-.002f,"knees above painted floor at "+u);Check(rig.nearLegEndpointGap<.015f&&rig.farLegEndpointGap<.015f,"no unreachable leg stretch at "+u);if(u>=.34f)Check(Mathf.Abs(rig.nearSupportAngle)<.1f,"front foot flat through weight transfer at "+u);yield return Shot("Rise_"+Mathf.RoundToInt(u*100).ToString("D3"));}
 File.WriteAllLines(folder+"/pose-metrics.csv",stats);foreach(float duration in new[]{6.5f,13f}){P07EvidenceRecorder.Begin(this,folder+"/"+(duration<10?"RiseNormal":"RiseSlow")+".mp4",duration+.4f);float t=0;while(t<duration){while(!Application.isFocused||Rt.GetComponent<GamePause>().MenuOpen){yield return Wait(.03f);}rig.OpeningRise=t/duration;t+=GameplayClock.Delta;yield return null;}rig.OpeningRise=1;yield return Wait(.2f);P07EvidenceRecorder.Stop();}
 }
 TMP_Text HUD=>Rt.GetComponent<P07QuestDisplay>().GetComponentsInChildren<TMP_Text>(true).Single(t=>t.transform.parent.name=="UpperBlackBarTask");
 GameObject Panel=>HUD.transform.parent.gameObject;
 P07InkGraphic Strike=>HUD.GetComponentInChildren<P07InkGraphic>(true);
 IEnumerator Tasks(){Quest.Restore(new QuestSnapshot());yield return Wait(.7f);observer=new GameObject("P07_isolated_semantic_observer"){hideFlags=HideFlags.DontSave};var story=observer.AddComponent<StoryStateManager>();var q=observer.AddComponent<QuestProgress>();yield return null;
 int[] ids={10,20,30,40,50,60,70,80,85,90,100,110,115,120,130,140,150,160,170,175,180,190,200,205,210,220,230,235,240};
 string[] labels={"探索周围","寻找铁门钥匙","探索家属院","追上阴影","接电话","前往客厅拿上相机","探索周围，前往河畔寻找男孩","","回家","探索客厅","将相机放至机架","","探索二楼次卧","穿越黑雾","探索学校","寻找钥匙","探索体育器材室","探索学校","","前往二楼一班","探索一楼","前往三楼教师办公室","","取得灯油，逃离学校","穿越黑雾","探索二楼次卧","","探索屋外",""};
 var hooks=new Dictionary<int,StoryFlag>{{30,StoryFlag.Scene01GateKeyCollected},{40,StoryFlag.ChildShadowSequencePlayed},{50,StoryFlag.Scene03Started},{60,StoryFlag.Scene03PhoneAnswered},{70,StoryFlag.Scene03CameraTaken},{90,StoryFlag.Scene03ReturnedHome},{100,StoryFlag.Scene03FamilySetupComplete},{115,StoryFlag.Scene03ReturnReactionPlayed},{120,StoryFlag.OilLampObtained},{130,StoryFlag.FogIntroCompleted},{140,StoryFlag.EquipmentVoicePlayed},{150,StoryFlag.BasketKeyCollected},{160,StoryFlag.PencilOwned},{170,StoryFlag.SchoolNameRevealed},{175,StoryFlag.SchoolYouthConversationCompleted},{180,StoryFlag.SchoolUsbReceived},{200,StoryFlag.SchoolOfficeHandoffReady},{205,StoryFlag.SchoolChaseStarted},{210,StoryFlag.SchoolChaseCompleted},{220,StoryFlag.SchoolReturnedHome},{230,StoryFlag.SchoolEmailRead},{235,StoryFlag.EndingHandoverCompleted},{240,StoryFlag.EndingBoardingCompleted}};
 P07EvidenceRecorder.Begin(this,folder+"/TaskTransitions.mp4",16);
 for(int i=0;i<ids.Length;i++){int id=ids[i];if(hooks.TryGetValue(id,out var flag))story.Set(flag);else if(id==190){story.Set(StoryFlag.SchoolEvidenceObtained);Check(q.CurrentEvent<190,"Q19 waits for changed password");story.Set(StoryFlag.SchoolMediaPasswordChanged);}else q.Publish((QuestEvent)id);
 Check(q.CurrentEvent==id&&q.TaskName==labels[i],"semantic observer / label "+id);Quest.Publish((QuestEvent)id);yield return Wait(1.7f);Check(HUD.text==labels[i]&&Panel.activeSelf==(!string.IsNullOrEmpty(labels[i])),"rendered HUD after transition "+id);if(new[]{10,60,70,115,190,235,240}.Contains(id))yield return Shot("Task_"+id);
 if(id==20){Check(HUD.GetComponent<RectTransform>().rect.width>0&&HUD.GetComponentInParent<Canvas>(),"actual upper-bar UI exists");Controls.AcquireLock(ControlLockReason.Dialogue,this);yield return Wait(.1f);Check(!Panel.activeSelf,"dialogue hides task HUD");Controls.ReleaseLock(ControlLockReason.Dialogue,this);yield return Wait(.1f);Check(Panel.activeSelf,"HUD returns after modal without another quest");}
 if(id==30){Quest.Publish(QuestEvent.OpeningControlGranted);Quest.Publish(QuestEvent.GateKeyObtained);yield return Wait(.1f);Check(Quest.CurrentEvent==30&&HUD.text==labels[i],"earlier / duplicate events do not regress HUD");}
 if(id==40){var snap=Quest.Capture();Quest.Publish(QuestEvent.ChildhoodEntered);yield return Wait(.16f);float beforeFreeze=Strike.progress;Rt.GetComponent<GamePause>().Freeze(this);yield return Wait(.25f);Check(Mathf.Abs(Strike.progress-beforeFreeze)<.0001f,"completion stroke freezes during pause");Rt.GetComponent<GamePause>().Thaw(this);Quest.Restore(snap);yield return Wait(1.7f);Check(Quest.CurrentEvent==40&&HUD.text=="追上阴影","snapshot restores matching current task");}
 if(id==235){Quest.Publish(QuestEvent.BoardingTriggered);Controls.AcquireLock(ControlLockReason.Cutscene,this);yield return Wait(.2f);Check(Panel.activeSelf&&Strike.progress>0,"boarding triggers completion stroke while original boarding cutscene owns controls");yield return Shot("BoardingStroke");yield return Wait(1.1f);Check(!Panel.activeSelf,"boarding removes final task after stroke");Controls.ReleaseLock(ControlLockReason.Cutscene,this);Quest.Restore(q.Capture());yield return Wait(.8f);}
 }
 P07EvidenceRecorder.Stop();Check(QuestProgress.Valid(q.Capture()),"all semantic events form valid monotonic save snapshot");}
}
