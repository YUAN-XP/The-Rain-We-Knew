using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Diagnostic fixtures exercise production restore/interaction code, not a manual playthrough.
public sealed class P01RestoreEdgeChecks {
    static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Temp/P01RestoreEdges"));
    readonly List<string> lines=new List<string>();GameSaveService saves;GameSaveData baseline;
    void Check(bool ok,string name){lines.Add((ok?"PASS ":"FAIL ")+name);File.WriteAllLines(Path.Combine(Folder,"results.txt"),lines);}
    public static void Run(){if(!EditorApplication.isPlaying)throw new Exception("Requires Scene01 Play Mode");PersistentRuntime.Instance.StartCoroutine(new P01RestoreEdgeChecks().RunTests());}
    IEnumerator Load(GameSaveData data){saves.Store.Write(1,data,true);Check(saves.Load(1,out var error),"load accepted "+data.scene+": "+error);float until=Time.realtimeSinceStartup+30;while(saves.IsRestoring&&Time.realtimeSinceStartup<until)yield return null;Check(!saves.IsRestoring&&saves.LastError==null,"load settled: "+saves.LastError);for(int i=0;i<20;i++)yield return new WaitForFixedUpdate();}
    GameSaveData Fixture(string scene,Vector3 pos,params StoryFlag[] flags){var d=JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(baseline));d.scene=scene;d.position=pos;d.actor=scene=="Scene03_Childhood"?"child":"adult";d.scale=d.actor=="child"?Vector3.one:baseline.scale;d.story.flags=flags.Select(x=>x.ToString()).ToArray();d.school=new SavedSchool();d.companions=new SavedCompanion[0];d.items=new SavedItem[0];d.files=new SavedFile[0];return d;}
    IEnumerator RunTests(){
        Directory.CreateDirectory(Folder);var rt=PersistentRuntime.Instance;saves=rt.GetComponent<GameSaveService>();var controls=rt.GetComponent<GameControlManager>();var pause=rt.GetComponent<GamePause>();pause.SetPaused(false);
        typeof(GameSaveService).GetProperty("Store").GetSetMethod(true).Invoke(saves,new object[]{new FourSlotSaveStore(Path.Combine(Folder,"slots"))});
        float until=Time.realtimeSinceStartup+20;while(!saves.CanSave(out _)&&Time.realtimeSinceStartup<until)yield return null;if(!saves.CanSave(out var reason)){Check(false,reason);yield break;}baseline=saves.Capture(1);
        var player=controls.ActivePlayer;var at=player.transform.position;EndingSession.Place(player,at+Vector3.up*2);Physics2D.SyncTransforms();
        Check(!saves.CanSave(out _),"zero-velocity airborne state cannot save");pause.SetPaused(true);Check(!saves.CanSave(out _),"pausing airborne state cannot bypass support check");pause.SetPaused(false);EndingSession.Place(player,at);Physics2D.SyncTransforms();
        yield return Load(Fixture("Scene01_Rain",baseline.position,StoryFlag.Scene01NewspaperTaken));
        var paper=UnityEngine.Object.FindAnyObjectByType<NewspaperFragmentInteractable>(FindObjectsInactive.Include);var key=UnityEngine.Object.FindAnyObjectByType<GateKeyInteractable>(FindObjectsInactive.Include);
        Check(paper&&paper.IsCollected&&!paper.GetComponent<Collider2D>().enabled,"collected newspaper stays collected");Check(key&&key.gameObject.activeInHierarchy&&!key.IsCollected&&key.GetComponent<Collider2D>().enabled,"uncollected key is revealed after newspaper restore");
        yield return Load(Fixture("Scene01_Rain",baseline.position,StoryFlag.Scene01NewspaperTaken,StoryFlag.Scene01GateKeyCollected));
        key=UnityEngine.Object.FindAnyObjectByType<GateKeyInteractable>(FindObjectsInactive.Include);Check(key&&key.IsCollected&&!key.GetComponent<Collider2D>().enabled&&!key.GetComponent<SpriteRenderer>().enabled,"collected key cannot respawn");
        yield return Load(Fixture("Scene01_Rain",baseline.position,StoryFlag.Scene01NewspaperTaken,StoryFlag.Scene01GateOpened));
        key=UnityEngine.Object.FindAnyObjectByType<GateKeyInteractable>(FindObjectsInactive.Include);Check(key&&key.IsCollected,"consumed key stays removed after gate opened");
        yield return Load(Fixture("Scene03_Childhood",new Vector3(70,1.23f,0),StoryFlag.Scene03Started,StoryFlag.Scene03IntroCompleted,StoryFlag.Scene03PhoneAnswered,StoryFlag.Scene03CameraTaken,StoryFlag.Scene03ReturnedHome,StoryFlag.Scene03FamilySetupComplete));
        var river=UnityEngine.Object.FindAnyObjectByType<Scene03RiverSequence>(FindObjectsInactive.Include);var family=UnityEngine.Object.FindAnyObjectByType<Scene03FamilySequence>(FindObjectsInactive.Include);
        river.boy.SetParent(river.chapter.livingRoomRoot.transform,true);river.boy.position=family.roomBoyPoint.position;
        var snap=saves.Capture(1);var expected=snap.companions.Single(x=>x.role=="childhood-boy");Check(expected.parent=="living","living-room companion captured by role");
        yield return Load(snap);river=UnityEngine.Object.FindAnyObjectByType<Scene03RiverSequence>(FindObjectsInactive.Include);
        Check(river.boy.IsChildOf(river.chapter.livingRoomRoot.transform)&&Vector3.Distance(river.boy.position,expected.position)<.001f,"companion room and position survive scene reload");
        Check(rt.GetComponent<QuestProgress>().CurrentEvent==snap.quests.currentEvent,"restore does not publish new quest milestones");
        Check(lines.All(x=>!x.StartsWith("FAIL")),"SUMMARY");
    }
}
