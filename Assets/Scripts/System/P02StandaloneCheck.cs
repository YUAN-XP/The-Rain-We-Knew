#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

// Opt-in QA only. Release builds exclude this type and its replay hook.
// All fixture/slot data stays in the explicitly named test output directory.
[DefaultExecutionOrder(2000)]
public sealed class P02StandaloneCheck : MonoBehaviour,GameplayInput.IReplaySource
{
    [Serializable] public class Plan {public string[] fixtures;public int frameRate;}
    string folder;readonly HashSet<KeyCode> held=new HashSet<KeyCode>(),previous=new HashSet<KeyCode>();
    readonly List<string> rows=new List<string>();bool failed;
    int caseIndex,walkFrames,runFrames;float lastPhase;Vector3 lastPosition;bool hasPrevious, previousMoving;
    string direction;bool running;float phaseError;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot(){var args=Environment.GetCommandLineArgs();int n=Array.IndexOf(args,"-p02Probe");if(n<0||n+1>=args.Length)return;
        var go=new GameObject("P02 explicit isolated standalone QA");DontDestroyOnLoad(go);var p=go.AddComponent<P02StandaloneCheck>();p.folder=Path.GetFullPath(args[n+1]);p.StartCoroutine(p.Guarded());}
    void Check(bool ok,string label){failed|=!ok;rows.Add((ok?"PASS ":"FAIL ")+label);File.WriteAllLines(Path.Combine(folder,"report.txt"),rows);}
    IEnumerator Guarded(){Directory.CreateDirectory(folder);rows.Add("Process "+System.Diagnostics.Process.GetCurrentProcess().Id+" resolution "+Screen.width+"x"+Screen.height+"; isolated saved-state QA, not natural story completion");
        var routine=Run();while(true){bool more=false;object next=null;try{more=routine.MoveNext();if(more)next=routine.Current;}catch(Exception e){Check(false,e.ToString());}if(!more)break;yield return next;}
        GameplayInput.Replay=null;Check(!failed,"SUMMARY");Application.Quit(failed?2:0);}
    IEnumerator Run(){
        yield return null;var rt=PersistentRuntime.Instance;var save=rt.GetComponent<GameSaveService>();
        typeof(GameSaveService).GetProperty("Store").GetSetMethod(true).Invoke(save,new object[]{new FourSlotSaveStore(Path.Combine(folder,"isolated-slots"))});
        GameplayInput.Replay=this;Application.runInBackground=true;
        var plan=JsonUtility.FromJson<Plan>(File.ReadAllText(Path.Combine(folder,"plan.json")));
        if(plan.frameRate>0){QualitySettings.vSyncCount=0;Application.targetFrameRate=plan.frameRate;rows.Add("Requested diagnostic frame rate "+plan.frameRate);}
        foreach(var path in plan.fixtures){
            var data=JsonUtility.FromJson<GameSaveData>(File.ReadAllText(path));rt.GetComponent<GamePause>().SetPaused(false);
            float limit=Time.realtimeSinceStartup+30;while(!save.CanSave(out _)&&Time.realtimeSinceStartup<limit)yield return null;
            save.Store.Write(4,data,true);Check(save.Load(4,out var why),"load fixture "+Path.GetFileName(path)+" "+why);
            limit=Time.realtimeSinceStartup+35;while(save.IsRestoring&&Time.realtimeSinceStartup<limit)yield return null;
            Check(!save.IsRestoring&&save.LastError==null,"restore fixture "+caseIndex);for(int i=0;i<45;i++)yield return null;
            while(rt.GetComponent<GameControlManager>().IsLocked()&&Time.realtimeSinceStartup<limit)yield return null;
            rt.GetComponent<GamePause>().SetPaused(false);walkFrames=runFrames=0;phaseError=0;hasPrevious=false;
            var activePlayer=rt.GetComponent<GameControlManager>().ActivePlayer;
            bool runAllowed=activePlayer.GetComponent<SprintAbility>()?.GetMultiplier(true)>1.01f;
            rows.Add("Authored sprint permission "+caseIndex+" allowed="+runAllowed+" unlocked="+rt.GetComponent<StoryStateManager>().Has(StoryFlag.SprintUnlocked));
            foreach(var keys in new[]{"D","D,LeftShift","A","A,LeftShift"}){
                Set(keys);yield return new WaitForSecondsRealtime(1.3f);Set("");
                float releasedAt=Time.realtimeSinceStartup;bool settled=false;
                do{yield return new WaitForEndOfFrame();var observed=rt.GetComponent<GameControlManager>().ActivePlayer.GetComponent<P02ActorPresentation>().Rig;
                    settled=observed.actualSpeed<.025f&&Mathf.Abs(observed.phase)<.00001f&&Mathf.Abs(observed.bodyLean)<.00001f;
                }while(!settled&&Time.realtimeSinceStartup-releasedAt<.8f);
                var stopped=rt.GetComponent<GameControlManager>().ActivePlayer.GetComponent<P02ActorPresentation>().Rig;
                rows.Add("Idle observation "+caseIndex+" "+keys+" settled="+settled+" seconds="+(Time.realtimeSinceStartup-releasedAt)+" phase="+stopped.phase+" lean="+stopped.bodyLean+" speed="+stopped.actualSpeed);
                Check(settled,"canonical idle measured after LateUpdate "+caseIndex+" "+keys);
                CaptureWorld(caseIndex+"_"+keys.Replace(',','_'));yield return new WaitForEndOfFrame();
            }
            Check(walkFrames>15&&(runAllowed?runFrames>15:runFrames==0),"actual motion follows authored sprint permission "+caseIndex+" "+walkFrames+"/"+runFrames+" allowed="+runAllowed);
            Check(phaseError<.02f,"phase follows actual displacement "+caseIndex+" error="+phaseError);
            var p=rt.GetComponent<GameControlManager>().ActivePlayer;var view=p.GetComponent<P02ActorPresentation>();
            var native=view.Rig;Check(Mathf.Abs(native.phase)<.00001f&&Mathf.Abs(native.bodyLean)<.00001f,"canonical idle after release "+caseIndex);Check(view.OwnsBody&&p.GetComponentsInChildren<P02CharacterRig>(true).Count(x=>x.gameObject.activeInHierarchy)==1,"one visible native body "+caseIndex);
            Check(view.Direction=="left","left-facing restored after actual movement "+caseIndex);
            Check(p.WalkSpeed>1.8f,"original serialized player speed retained "+caseIndex+" speed="+p.WalkSpeed);
            Check(save.Save(3,true,out why),"save free roam "+caseIndex+" "+why);
            Check(save.Load(3,out why),"load free roam "+caseIndex+" "+why);
            limit=Time.realtimeSinceStartup+35;while(save.IsRestoring&&Time.realtimeSinceStartup<limit)yield return null;for(int i=0;i<30;i++)yield return null;
            Check(rt.GetComponent<GameControlManager>().ActivePlayer.GetComponent<P02ActorPresentation>().Direction=="left","saved facing remains left "+caseIndex);
            CaptureWorld(caseIndex+"_restored");yield return new WaitForEndOfFrame();caseIndex++;
        }
    }
    void CaptureWorld(string label){
        var camera=Camera.main;if(!camera){Check(false,"capture camera missing "+label);return;}
        var target=new RenderTexture(Screen.width,Screen.height,24,RenderTextureFormat.ARGB32);
        var pixels=new Texture2D(Screen.width,Screen.height,TextureFormat.RGBA32,false);
        var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;
        try{camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            pixels.ReadPixels(new Rect(0,0,Screen.width,Screen.height),0,0);pixels.Apply(false);
            File.WriteAllBytes(Path.Combine(folder,label+"_world.png"),pixels.EncodeToPNG());
        }finally{camera.targetTexture=previousTarget;RenderTexture.active=previousActive;target.Release();Destroy(target);Destroy(pixels);}
    }
    void Set(string keys){held.Clear();foreach(var key in keys.Split(new[]{','},StringSplitOptions.RemoveEmptyEntries))held.Add((KeyCode)Enum.Parse(typeof(KeyCode),key));}
    void LateUpdate(){
        previous.Clear();previous.UnionWith(held);var p=PersistentRuntime.Instance?PersistentRuntime.Instance.GetComponent<GameControlManager>().ActivePlayer:null;
        var view=p?p.GetComponent<P02ActorPresentation>():null;var rig=view?view.Rig:null;
        if(!rig||GamePause.IsPaused){hasPrevious=false;return;}
        if(hasPrevious&&previousMoving&&direction==view.Direction&&running==rig.running&&rig.actualSpeed>.05f){
            float travelled=Vector3.Distance(lastPosition,p.transform.position);
            if(travelled<.8f){float stride=(rig.running?rig.definition.runningStride:rig.definition.walkingStride)*rig.targetHeight/rig.definition.referenceHeight;
                phaseError=Mathf.Max(phaseError,Mathf.Abs(Mathf.Repeat(rig.phase-lastPhase,1)-travelled/stride));}
        }
        if(rig.actualSpeed>.05f){if(rig.running)runFrames++;else walkFrames++;}
        lastPhase=rig.phase;lastPosition=p.transform.position;direction=view.Direction;running=rig.running;previousMoving=rig.actualSpeed>.05f;hasPrevious=true;
    }
    public bool Key(KeyCode key)=>held.Contains(key);
    public bool KeyDown(KeyCode key)=>held.Contains(key)&&!previous.Contains(key);
    public float Axis(string axis)=>axis=="Horizontal"?((Key(KeyCode.D)?1:0)-(Key(KeyCode.A)?1:0)):0;
    public bool Mouse(int n)=>false;public bool MouseDown(int n)=>false;public bool MouseUp(int n)=>false;public Vector3 Pointer=>Vector3.zero;
}
#endif
