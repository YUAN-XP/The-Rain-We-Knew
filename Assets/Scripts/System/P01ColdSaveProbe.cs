#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Reflection;
using UnityEngine;

// Opt-in development probe only. Never activates or touches saves without both arguments.
public sealed class P01ColdSaveProbe : MonoBehaviour {
    string mode,folder;bool failed;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot(){var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-p01ColdSave");if(at<0||at+2>=args.Length)return;var go=new GameObject("P01ColdSaveProbe");DontDestroyOnLoad(go);var p=go.AddComponent<P01ColdSaveProbe>();p.mode=args[at+1];p.folder=Path.GetFullPath(args[at+2]);p.StartCoroutine(p.Guarded());}
    void Result(bool ok,string label){failed|=!ok;File.AppendAllText(Path.Combine(folder,mode+"-report.txt"),(ok?"PASS ":"FAIL ")+label+"\n");}
    IEnumerator Guarded(){Directory.CreateDirectory(folder);File.WriteAllText(Path.Combine(folder,mode+"-report.txt"),"Process="+System.Diagnostics.Process.GetCurrentProcess().Id+" UTC="+DateTime.UtcNow.ToString("O")+"\n");var test=Run();while(true){bool more=false;object next=null;try{more=test.MoveNext();if(more)next=test.Current;}catch(Exception e){Result(false,e.ToString());}if(!more||failed)break;yield return next;}Result(!failed,"SUMMARY");Application.Quit(failed?2:0);}
    IEnumerator Run(){
        yield return null;var rt=PersistentRuntime.Instance;var service=rt.GetComponent<GameSaveService>();var pause=rt.GetComponent<GamePause>();
        typeof(GameSaveService).GetProperty("Store").GetSetMethod(true).Invoke(service,new object[]{new FourSlotSaveStore(Path.Combine(folder,"slots"))});
        pause.SetPaused(false);float deadline=Time.realtimeSinceStartup+30;while(!service.CanSave(out _)&&Time.realtimeSinceStartup<deadline)yield return null;
        Result(service.CanSave(out var reason),"initial free roam: "+reason);if(failed)yield break;
        if(mode=="write"){
            var registry=SaveAssetRegistry.Load();var inv=rt.GetComponent<InventoryManager>();var files=rt.GetComponent<FileArchiveManager>();
            foreach(var item in registry.entries.Select(x=>x.asset).OfType<ItemData>().Where(x=>!string.IsNullOrEmpty(x.itemId)).Take(3)){inv.AddItem(item);inv.SetRuntimePresentation(item,"冷启动验证："+item.itemName,"状态应在另一进程中恢复");}
            foreach(var file in registry.entries.Select(x=>x.asset).OfType<FileData>().Where(x=>!string.IsNullOrEmpty(x.fileId)).Take(3))files.AddFile(file);
            var story=rt.GetComponent<StoryStateManager>();story.Set(StoryFlag.OverallsUnlocked);story.SetOutfit(AdultOutfit.Overalls);
            rt.GetComponent<QuestProgress>().Publish(QuestEvent.GateKeyObtained);
            Result(service.Save(1,false,out var initialError),"save rain slot: "+initialError);
            var seed=service.Capture(1);
            for(int slot=2;slot<=4;slot++){
                var fixture=JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(seed));fixture.slot=4;fixture.companions=new SavedCompanion[0];
                if(slot==2){fixture.scene="Scene02_Courtyard";fixture.actor="adult";fixture.position=new Vector3(70,1.41f,0);fixture.story.flags=new[]{StoryFlag.SchoolReturnedHome,StoryFlag.SchoolEmailRead,StoryFlag.EndingEmailReactionCompleted,StoryFlag.EndingHandoverCompleted}.Select(x=>x.ToString()).ToArray();}
                if(slot==3){fixture.scene="Scene03_Childhood";fixture.actor="child";fixture.position=new Vector3(70,1.23f,0);fixture.scale=Vector3.one;fixture.story.flags=new[]{StoryFlag.Scene03Started,StoryFlag.Scene03IntroCompleted,StoryFlag.Scene03PhoneAnswered,StoryFlag.Scene03CameraTaken,StoryFlag.Scene03ReturnedHome,StoryFlag.Scene03FamilySetupComplete}.Select(x=>x.ToString()).ToArray();}
                if(slot==4){fixture.scene="Scene04_School";fixture.actor="youth";fixture.position=new Vector3(272,17.41f,0);fixture.story.flags=new[]{StoryFlag.FogIntroCompleted,StoryFlag.SchoolNameRevealed,StoryFlag.Scene05OpeningPlayed,StoryFlag.SchoolYouthConversationCompleted}.Select(x=>x.ToString()).ToArray();fixture.school=new SavedSchool{present=true,room="Class3_1",variant=1,actor=1,phase=3};}
                service.Store.Write(4,fixture,true);Result(service.Load(4,out var fixtureError),"diagnostic fixture "+slot+": "+fixtureError);deadline=Time.realtimeSinceStartup+35;while(service.IsRestoring&&Time.realtimeSinceStartup<deadline)yield return null;Result(!service.IsRestoring&&service.LastError==null,"fixture restored "+slot);if(failed)yield break;
                for(int frame=0;frame<20;frame++)yield return new WaitForFixedUpdate();
                if(slot==2){var home=FindAnyObjectByType<EndingHomeController>();home.RestoreFollowDirection(-1);home.guide.position=EndingSession.CompanionAnchor(home.guide,rt.GetComponent<GameControlManager>().ActivePlayer,1.4f,home.data.guideVisualGroundOffset);}
                if(slot==3){var river=FindAnyObjectByType<Scene03RiverSequence>(FindObjectsInactive.Include);var family=FindAnyObjectByType<Scene03FamilySequence>(FindObjectsInactive.Include);river.boy.SetParent(river.chapter.livingRoomRoot.transform,true);river.boy.position=family.roomBoyPoint.position;}
                Result(service.Save(slot,slot==4,out var saveError),"save chapter slot "+slot+": "+saveError);
            }
        }else if(mode=="read"){
            for(int slot=1;slot<=4;slot++){
                Result(service.Store.TryRead(slot,out var expected,out var readError),"read disk slot "+slot+": "+readError);if(failed)yield break;
                bool loaded=false;Action<bool,string> handler=(ok,error)=>{loaded=ok;};service.LoadFinished+=handler;
                Result(service.Load(slot,out var loadError),"load slot "+slot+": "+loadError);deadline=Time.realtimeSinceStartup+35;
                while(service.IsRestoring&&Time.realtimeSinceStartup<deadline)yield return null;service.LoadFinished-=handler;
                Result(loaded,"restore completed "+slot+": "+service.LastError);if(failed)yield break;
                var actual=service.Capture(slot);
                Result(JsonUtility.ToJson(actual.story)==JsonUtility.ToJson(expected.story),"story and outfit "+slot);
                Result(actual.items.Length==expected.items.Length&&actual.items.Zip(expected.items,(a,b)=>JsonUtility.ToJson(a)==JsonUtility.ToJson(b)).All(x=>x),"inventory and descriptions "+slot);
                Result(actual.files.Length==expected.files.Length&&actual.files.Zip(expected.files,(a,b)=>a.key==b.key&&a.id==b.id).All(x=>x),"file variants "+slot);
                Result(JsonUtility.ToJson(actual.quests)==JsonUtility.ToJson(expected.quests)&&actual.firstObtained.SequenceEqual(expected.firstObtained),"task and acquisition history "+slot);
                Result(Vector3.Distance(actual.position,expected.position)<.06f&&actual.actor==expected.actor&&actual.scene==expected.scene,"actor and position "+slot);
                Result(JsonUtility.ToJson(actual.school)==JsonUtility.ToJson(expected.school),"school world room and phase "+slot);
                Result(actual.companions.Length==expected.companions.Length&&actual.companions.Zip(expected.companions,(a,b)=>a.role==b.role&&a.parent==b.parent&&a.active==b.active&&a.direction==b.direction&&Vector3.Distance(a.position,b.position)<.04f&&a.scale==b.scale).All(x=>x),"companion room position and direction "+slot);
                Result(actual.playSeconds>=expected.playSeconds&&actual.playSeconds-expected.playSeconds<2,"play clock restore "+slot);
            }
        }else Result(false,"unknown mode");
    }
}
#endif
