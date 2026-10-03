#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

// Explicit command-line QA. Never installed by a normal game launch.
public sealed class P02StateProbe : MonoBehaviour {
    [Serializable] public class Plan {public string fixture;public bool cold;public string[] directions;public bool lamp;public bool legacy;}
    string folder;Plan plan;bool failed;readonly List<string> rows=new List<string>();
    GameSaveService save;PersistentRuntime rt;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot(){var a=Environment.GetCommandLineArgs();int n=Array.IndexOf(a,"-p02StateProbe");if(n<0||n+1>=a.Length)return;var go=new GameObject("P02 opt-in state QA");DontDestroyOnLoad(go);var p=go.AddComponent<P02StateProbe>();p.folder=Path.GetFullPath(a[n+1]);p.StartCoroutine(p.RunGuarded());}
    void Check(bool ok,string what){failed|=!ok;rows.Add((ok?"PASS ":"FAIL ")+what);File.WriteAllLines(Path.Combine(folder,plan.cold?"cold-report.txt":"state-report.txt"),rows);}
    IEnumerator RunGuarded(){
        Directory.CreateDirectory(folder);plan=JsonUtility.FromJson<Plan>(File.ReadAllText(Path.Combine(folder,"state-plan.json")));
        rows.Add("PV17 isolated state QA pid="+System.Diagnostics.Process.GetCurrentProcess().Id+" resolution="+Screen.width+"x"+Screen.height+" cold="+plan.cold);
        var stack=new Stack<IEnumerator>();stack.Push(Run());
        while(stack.Count>0){object value=null;bool more=false;try{more=stack.Peek().MoveNext();if(more)value=stack.Peek().Current;}catch(Exception e){Check(false,e.ToString());stack.Clear();break;}if(!more){stack.Pop();continue;}if(value is IEnumerator inner)stack.Push(inner);else yield return value;}
        Check(!failed,"SUMMARY");Application.Quit(failed?2:0);
    }
    IEnumerator Restore(int slot){
        Check(save.Load(slot,out var why),"load slot "+slot+" "+why);float end=Time.realtimeSinceStartup+35;
        while(save.IsRestoring&&Time.realtimeSinceStartup<end)yield return null;
        Check(!save.IsRestoring&&save.LastError==null,"load finished "+save.LastError);
        for(int i=0;i<45;i++)yield return null;
    }
    void Compare(GameSaveData expected,string label){
        var p=rt.GetComponent<GameControlManager>().ActivePlayer;var view=p.GetComponent<P02ActorPresentation>();var d=save.Capture(3);
        var expectedDirection=string.IsNullOrEmpty(expected.actorFacing)?(expected.facing<0?"left":"right"):expected.actorFacing;
        Check(view.Direction==expectedDirection,label+" direction "+view.Direction+" expected "+expectedDirection);
        Check(view.OwnsBody&&p.GetComponentsInChildren<P02CharacterRig>(true).Count(x=>x.gameObject.activeInHierarchy)==1,label+" one native body");
        Check(Vector3.Distance(p.transform.position,expected.position)<.03f,label+" position");
        Check(d.lampRaised==expected.lampRaised,label+" lamp restored="+d.lampRaised);
        Check(JsonUtility.ToJson(d.story)==JsonUtility.ToJson(expected.story),label+" story unchanged");
        Check(d.items.Select(x=>x.key).SequenceEqual(expected.items.Select(x=>x.key)),label+" inventory unchanged");
        Check(d.companions.Length==expected.companions.Length,label+" companion count");
        if(expected.lampRaised){var tool=rt.GetComponent<P02ToolMotion>();Check(tool.Current==P02ToolMotion.Tool.Lamp&&tool.State==P02ToolMotion.Motion.Holding,label+" lamp visual adopted");}
    }
    IEnumerator Run(){
        Application.runInBackground=true;yield return null;rt=PersistentRuntime.Instance;save=rt.GetComponent<GameSaveService>();
        typeof(GameSaveService).GetProperty("Store").GetSetMethod(true).Invoke(save,new object[]{new FourSlotSaveStore(Path.Combine(folder,"isolated-slots"))});
        if(plan.cold){Check(save.Store.TryRead(3,out var expected,out var why),"read previous process save "+why);yield return Restore(3);Compare(expected,"cold process");P02MotionSampleProbe.Render(Path.Combine(folder,"cold-restored-world.png"),Screen.width,Screen.height);yield return new WaitForEndOfFrame();yield break;}
        var seed=JsonUtility.FromJson<GameSaveData>(File.ReadAllText(plan.fixture));seed.slot=4;save.Store.Write(4,seed,true);yield return Restore(4);
        foreach(string direction in plan.directions){
            var player=rt.GetComponent<GameControlManager>().ActivePlayer;player.GetComponent<P02ActorPresentation>().RestoreDirection(direction);for(int i=0;i<5;i++)yield return null;
            if(plan.lamp){var lamp=rt.GetComponent<OilLampController>();Check(lamp&&lamp.RaiseLamp(),"raise owned usable lamp");yield return new WaitForSecondsRealtime(.6f);}
            var expected=save.Capture(3);Check(expected.actorFacing==direction,"capture requested direction "+direction);Check(save.Save(3,true,out var why),"save "+direction+" "+why);yield return Restore(3);Compare(expected,direction);
            P02MotionSampleProbe.Render(Path.Combine(folder,direction+"_restored-world.png"),Screen.width,Screen.height);yield return new WaitForEndOfFrame();
        }
        if(plan.legacy){
            var data=save.Capture(4);data.actorFacing=null;data.facing=-1;data.lampRaised=false;save.Store.Write(4,data,true);
            // Remove the optional P02 field from the actual payload, preserving the production checksum envelope.
            string file=save.Store.SlotPath(4);var envelope=JsonUtility.FromJson<SaveEnvelope>(File.ReadAllText(file));
            envelope.payload=System.Text.RegularExpressions.Regex.Replace(envelope.payload,"\\\"actorFacing\\\":(?:null|\\\"[^\\\"]*\\\"),?","");
            using(var sha=System.Security.Cryptography.SHA256.Create())envelope.sha256=BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(envelope.payload))).Replace("-","").ToLowerInvariant();
            File.WriteAllText(file,JsonUtility.ToJson(envelope));yield return Restore(4);Compare(data,"legacy missing actorFacing");
        }
    }
}
#endif
