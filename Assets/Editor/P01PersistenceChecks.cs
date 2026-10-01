using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class P01PersistenceChecks {
    readonly List<string> report=new List<string>();
    string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Temp/P01Verification"));
    void Check(bool ok,string label){report.Add((ok?"PASS ":"FAIL ")+label);File.WriteAllLines(Path.Combine(Folder,"persistence-runtime.txt"),report);}
    public static void Run(){if(!EditorApplication.isPlaying)throw new Exception("Requires Play Mode");PersistentRuntime.Instance.StartCoroutine(new P01PersistenceChecks().Checks());}
    IEnumerator Checks(){
        Directory.CreateDirectory(Folder);
        var rt=PersistentRuntime.Instance;var pause=rt.GetComponent<GamePause>();var service=rt.GetComponent<GameSaveService>();var controls=rt.GetComponent<GameControlManager>();
        var dir=Path.Combine(Folder,"isolated-slots-"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
        typeof(GameSaveService).GetProperty("Store").GetSetMethod(true).Invoke(service,new object[]{new FourSlotSaveStore(dir)});
        pause.SetPaused(false);float deadline=Time.realtimeSinceStartup+20;
        while(!service.CanSave(out _)&&Time.realtimeSinceStartup<deadline)yield return null;
        Check(service.CanSave(out var reason),"free movement allows save: "+reason);
        if(!service.CanSave(out _))yield break;
        var baseline=service.Capture(1);
        var quest=rt.GetComponent<QuestProgress>();quest.Publish(QuestEvent.CameraObtained);quest.Publish(QuestEvent.PhoneDialogueCompleted);
        Check(quest.CurrentEvent==(int)QuestEvent.CameraObtained,"older task event cannot rewind");
        quest.Publish(QuestEvent.BoyFound);Check(!quest.Visible,"hidden task state");quest.Restore(baseline.quests);
        var history=rt.GetComponent<AcquisitionHistory>();int first=0;history.Obtained+=(id,isFirst)=>{if(isFirst)first++;};history.Record("test:one");history.Record("test:one");Check(first==1,"first acquisition deduplicates");history.Restore(baseline.firstObtained);
        foreach(ControlLockReason r in Enum.GetValues(typeof(ControlLockReason))){if(r==ControlLockReason.GlobalPause)continue;controls.AcquireLock(r,service);Check(!service.CanSave(out _),"save denied for "+r);controls.ReleaseLock(r,service);}
        var before=GameplayClock.Elapsed;var pos=controls.ActivePlayer.transform.position;pause.SetPaused(true);var freeze=GameplayClock.Elapsed;
        var clockWait=GameplayClock.Wait(.4f);float real=Time.realtimeSinceStartup;
        while(Time.realtimeSinceStartup-real<.8f)yield return null;
        Check(GameplayClock.Elapsed==freeze,"gameplay clock frozen");Check(Time.timeScale==0&&AudioListener.pause,"physics and audio pause together");Check(clockWait.keepWaiting,"unscaled gameplay wait frozen");Check(controls.ActivePlayer.transform.position==pos,"player position frozen");Check(service.CanSave(out _),"pause menu can save free-roam state");
        pause.SetPaused(false);real=Time.realtimeSinceStartup;while(Time.realtimeSinceStartup-real<.5f)yield return null;
        Check(GameplayClock.Elapsed>before&&!clockWait.keepWaiting,"clock and waits resume");Check(!controls.IsLockedBy(ControlLockReason.GlobalPause)&&Time.timeScale==1&&!AudioListener.pause,"pause releases only its lock");
        controls.AcquireLock(ControlLockReason.Dialogue,service);pause.SetPaused(true);pause.SetPaused(false);Check(controls.IsLockedBy(ControlLockReason.Dialogue),"resume preserves dialogue owner lock");controls.ReleaseLock(ControlLockReason.Dialogue,service);
        for(int slot=1;slot<=4;slot++)Check(service.Save(slot,false,out var error),"write slot "+slot+": "+error);
        string original=File.ReadAllText(service.Store.SlotPath(1));Check(!service.Save(1,false,out _),"overwrite needs explicit confirmation");Check(File.ReadAllText(service.Store.SlotPath(1))==original,"cancelled overwrite retains bytes");
        Check(service.Save(1,true,out _),"confirmed overwrite succeeds");Check(service.Store.LatestValidSlot()==1,"continue selects most recent valid save");
        File.WriteAllText(service.Store.SlotPath(4),"broken test data");Check(!service.Store.TryRead(4,out _,out var corrupt)&&!string.IsNullOrEmpty(corrupt),"damaged slot explained");Check(service.Store.TryRead(2,out _,out _)&&service.Store.TryRead(3,out _,out _),"damage isolated from other slots");
        Check(!service.Load(4,out _)&&!service.IsRestoring,"damaged slot rejected before world mutation");
        service.Store.TryRead(1,out var saved,out _);controls.ActivePlayer.transform.position+=Vector3.right*1.7f;rt.GetComponent<StoryStateManager>().Set(StoryFlag.CatFed);
        bool loaded=false;service.LoadFinished+=(ok,error)=>{loaded=ok;};Check(service.Load(1,out var loadError),"load accepted: "+loadError);
        deadline=Time.realtimeSinceStartup+30;while(service.IsRestoring&&Time.realtimeSinceStartup<deadline)yield return null;
        Check(loaded,"scene restore completed: "+service.LastError);if(loaded){
            Check(Vector3.Distance(controls.ActivePlayer.transform.position,saved.position)<.05f,"player position restored");
            Check(JsonUtility.ToJson(rt.GetComponent<StoryStateManager>().CaptureSave())==JsonUtility.ToJson(saved.story),"story and outfit restored");
            Check(JsonUtility.ToJson(quest.Capture())==JsonUtility.ToJson(saved.quests),"task state restored");
            Check(!controls.IsLocked(),"restore releases control after initialization");
        }
        Check(report.All(x=>!x.StartsWith("FAIL")),"SUMMARY");
    }
}
