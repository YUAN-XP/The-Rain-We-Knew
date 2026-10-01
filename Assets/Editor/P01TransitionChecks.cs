using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public sealed class P01TransitionChecks {
    static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Temp/P01Transitions"));
    readonly List<string> lines=new List<string>();GameSaveService saves;GameSaveData baseline;
    void Check(bool ok,string name){lines.Add((ok?"PASS ":"FAIL ")+name);File.WriteAllLines(Path.Combine(Folder,"results.txt"),lines);}
    public static void Run(){if(!EditorApplication.isPlaying)throw new Exception("Requires Play Mode");PersistentRuntime.Instance.StartCoroutine(new P01TransitionChecks().Tests());}
    static Transform Destination(RoomTransitionInteractable door)=>(Transform)typeof(RoomTransitionInteractable).GetField("destination",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(door);
    IEnumerator Fixture(string scene,Vector3 at,SavedSchool school=null){
        var d=JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(baseline));d.scene=scene;d.position=at;d.actor=school!=null&&school.variant==1?"youth":"adult";d.companions=new SavedCompanion[0];d.story.flags=scene=="Scene04_School"?school!=null&&school.variant==1?new[]{StoryFlag.FogIntroCompleted.ToString(),StoryFlag.SchoolNameRevealed.ToString(),StoryFlag.Scene05OpeningPlayed.ToString(),StoryFlag.SchoolYouthConversationCompleted.ToString()}:new[]{StoryFlag.FogIntroCompleted.ToString()}:new string[0];d.school=school??new SavedSchool();
        saves.Store.Write(1,d,true);Check(saves.Load(1,out var error),"fixture "+scene+": "+error);float until=Time.realtimeSinceStartup+30;while(saves.IsRestoring&&Time.realtimeSinceStartup<until)yield return null;Check(saves.LastError==null,"fixture settled");for(int i=0;i<20;i++)yield return new WaitForFixedUpdate();
    }
    IEnumerator Traverse(RoomTransitionInteractable door,IInteractable action,string label,bool pauseFade=false){
        var rt=PersistentRuntime.Instance;var controls=rt.GetComponent<GameControlManager>();var p=controls.ActivePlayer;var pause=rt.GetComponent<GamePause>();var fade=UnityEngine.Object.FindAnyObjectByType<ScreenFadeController>();
        var at=p.transform.position;at.x=door.transform.position.x;EndingSession.Place(p,at);GroundedPlacement.Settle(p);RoomPresentationFraming.PrepareCurrentView();
        bool arrived=false,moved=false,black=false;Action<RoomTransitionInteractable> after=x=>arrived=true;Action<RoomTransitionInteractable> under=x=>{moved=true;black=fade.GetComponent<CanvasGroup>().alpha>=.999f;};door.Arrived+=after;door.MovedUnderFade+=under;
        action.Interact();Check(!saves.CanSave(out _),label+" transition forbids save");
        if(pauseFade){yield return null;pause.SetPaused(true);float a=fade.GetComponent<CanvasGroup>().alpha;var before=p.transform.position;float end=Time.realtimeSinceStartup+.6f;while(Time.realtimeSinceStartup<end)yield return null;Check(fade.GetComponent<CanvasGroup>().alpha==a&&p.transform.position==before,label+" fade and position freeze together");pause.SetPaused(false);}
        float until=Time.realtimeSinceStartup+10;while(!arrived&&Time.realtimeSinceStartup<until)yield return null;door.Arrived-=after;door.MovedUnderFade-=under;
        Check(arrived&&moved&&black,label+" placement occurs under opaque curtain");Check(GroundedPlacement.IsSupported(p),label+" already grounded at curtain completion");
        float foot=p.GetComponent<Collider2D>().bounds.min.y;for(int i=0;i<8;i++)yield return new WaitForFixedUpdate();Check(Mathf.Abs(p.GetComponent<Collider2D>().bounds.min.y-foot)<.025f,label+" no visible landing drop after arrival");Check(!controls.IsLocked(),label+" control released");
        ScreenCapture.CaptureScreenshot(Path.Combine(Folder,label+".png"));yield return null;
    }
    IEnumerator Tests(){
        Directory.CreateDirectory(Folder);var rt=PersistentRuntime.Instance;saves=rt.GetComponent<GameSaveService>();rt.GetComponent<GamePause>().SetPaused(false);typeof(GameSaveService).GetProperty("Store").GetSetMethod(true).Invoke(saves,new object[]{new FourSlotSaveStore(Path.Combine(Folder,"slots"))});
        float until=Time.realtimeSinceStartup+20;while(!saves.CanSave(out _)&&Time.realtimeSinceStartup<until)yield return null;if(!saves.CanSave(out var reason)){Check(false,reason);yield break;}baseline=saves.Capture(1);
        yield return Fixture("Scene02_Courtyard",new Vector3(70,1.41f,0));
        var exit=UnityEngine.Object.FindObjectsByType<RoomTransitionInteractable>().First(x=>x.transform.IsChildOf(GameObject.Find("MasterBedroomInterior").transform));
        yield return Traverse(exit,exit,"BedroomExit",true);
        var entry=UnityEngine.Object.FindObjectsByType<RoomTransitionInteractable>().First(x=>x!=exit&&Destination(x)&&Destination(x).position.x>64&&Destination(x).position.x<76&&Destination(x).position.y<5);
        yield return Traverse(entry,entry,"BedroomEnter");
        yield return Fixture("Scene04_School",new Vector3(208,17.41f,0),new SavedSchool{present=true,room="Class3_2",variant=0,actor=0,phase=0});
        var outDoors=UnityEngine.Object.FindObjectsByType<SchoolPortal>().Where(x=>x.Definition.sourceRoom=="Class3_2"&&x.CanPass()).OrderBy(x=>x.transform.position.x).ToArray();
        Check(outDoors.Length==1,"otherworld classroom preserves its single open rear door");
        var blocked=UnityEngine.Object.FindObjectsByType<SchoolPortal>().Single(x=>x.Definition.sourceRoom=="Class3_2"&&!x.CanPass());
        var before=rt.GetComponent<GameControlManager>().ActivePlayer.transform.position;blocked.Interact();yield return null;
        Check(rt.GetComponent<GameControlManager>().ActivePlayer.transform.position==before&&!rt.GetComponent<GameControlManager>().IsLocked(),"blocked classroom front door cannot transition");
        foreach(var portal in outDoors){
            yield return Traverse(portal.transition,portal,"ClassExit_"+portal.portalId);
            var back=UnityEngine.Object.FindObjectsByType<SchoolPortal>().Where(x=>x.Definition.sourceRoom==portal.Definition.targetRoom&&x.Definition.targetRoom=="Class3_2"&&x.CanPass()).OrderBy(x=>Vector3.Distance(x.transform.position,PersistentRuntime.Instance.GetComponent<GameControlManager>().ActivePlayer.transform.position)).First();
            yield return Traverse(back.transition,back,"ClassEnter_"+back.portalId);
        }
        yield return Fixture("Scene04_School",new Vector3(208,17.41f,0),new SavedSchool{present=true,room="Class3_2",variant=1,actor=1,phase=3});
        var memoryDoors=UnityEngine.Object.FindObjectsByType<SchoolPortal>().Where(x=>x.Definition.sourceRoom=="Class3_2"&&x.CanPass()).OrderBy(x=>x.transform.position.x).ToArray();
        Check(memoryDoors.Length==2,"memory classroom has both authored doors open");
        foreach(var portal in memoryDoors){
            yield return Traverse(portal.transition,portal,"MemoryExit_"+portal.portalId);
            var back=UnityEngine.Object.FindObjectsByType<SchoolPortal>().Where(x=>x.Definition.sourceRoom==portal.Definition.targetRoom&&x.Definition.targetRoom=="Class3_2"&&x.CanPass()).OrderBy(x=>Vector3.Distance(x.transform.position,PersistentRuntime.Instance.GetComponent<GameControlManager>().ActivePlayer.transform.position)).First();
            yield return Traverse(back.transition,back,"MemoryEnter_"+back.portalId);
        }
        Check(lines.All(x=>!x.StartsWith("FAIL")),"SUMMARY diagnostic interaction calls, not native keyboard play");
    }
}
