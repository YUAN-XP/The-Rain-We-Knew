using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Isolated saved-state setups. All subsequent motion is real controller input.</summary>
public static class P02ScenarioChecks
{
    static GameSaveData seed;
    public static void PrepareLateStory(){var story=PersistentRuntime.Instance.GetComponent<StoryStateManager>();foreach(var f in new[]{StoryFlag.DeskEventTriggered,StoryFlag.ChildShadowSequencePlayed,StoryFlag.ChildShadowFollowupPlayed,StoryFlag.ParentShadowEventPlayed})story.Set(f);}
    public static void SprintLocked()=>PersistentRuntime.Instance.GetComponent<StoryStateManager>().Set(StoryFlag.SprintUnlocked,false);
    public static void SaveRoundtrip()=>PersistentRuntime.Instance.StartCoroutine(Roundtrip());
    public static void ExportStandaloneFixture(){
        var saves=PersistentRuntime.Instance.GetComponent<GameSaveService>();
        if(!saves.CanSave(out var why))throw new InvalidOperationException(why);
        var d=saves.Capture(4);string folder=P02ProductionInputReview.Work+"standalone_fixtures/";
        Directory.CreateDirectory(folder);var actor=PersistentRuntime.Instance.GetComponent<GameControlManager>().ActivePlayer.GetComponent<P02ActorPresentation>();
        File.WriteAllText(folder+d.scene+"_"+actor.ActorKey+".json",JsonUtility.ToJson(d,true));
    }
    static IEnumerator Roundtrip(){
        var rt=PersistentRuntime.Instance;var saves=rt.GetComponent<GameSaveService>();var p=rt.GetComponent<GameControlManager>().ActivePlayer;
        var before=p.GetComponent<P02ActorPresentation>();string direction=before.Direction;var at=p.transform.position;
        bool lamp=rt.GetComponent<OilLampController>()?.IsRaised==true;
        var companions=CompanionSaveState.Capture();var saved=saves.Capture(3);
        if(!saves.Save(3,true,out var error))throw new InvalidOperationException(error);
        if(!saves.Load(3,out error))throw new InvalidOperationException(error);
        float until=Time.realtimeSinceStartup+30;while(saves.IsRestoring&&Time.realtimeSinceStartup<until)yield return null;
        if(saves.IsRestoring||saves.LastError!=null)throw new InvalidOperationException(saves.LastError);
        for(int n=0;n<30;n++)yield return null;
        var actor=rt.GetComponent<GameControlManager>().ActivePlayer;var after=actor.GetComponent<P02ActorPresentation>();
        string path=P02ProductionInputReview.Work+"save_roundtrips/"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".json";Directory.CreateDirectory(Path.GetDirectoryName(path));
        var restored=saves.Capture(3);var currentCompanions=CompanionSaveState.Capture();
        File.WriteAllText(path,JsonUtility.ToJson(new RoundtripResult{directionBefore=direction,directionAfter=after.Direction,positionError=Vector3.Distance(at,actor.transform.position),lampBefore=lamp,lampAfter=rt.GetComponent<OilLampController>()?.IsRaised==true,ownsBody=after.OwnsBody,companionCountBefore=companions.Length,companionCountAfter=currentCompanions.Length,
            storyUnchanged=JsonUtility.ToJson(saved.story)==JsonUtility.ToJson(restored.story),inventoryUnchanged=JsonUtility.ToJson(new Items{items=saved.items})==JsonUtility.ToJson(new Items{items=restored.items}),
            companionsBefore=companions,companionsAfter=currentCompanions},true));
    }
    [Serializable] class RoundtripResult{public string directionBefore,directionAfter;public float positionError;public bool lampBefore,lampAfter,ownsBody,storyUnchanged,inventoryUnchanged;public int companionCountBefore,companionCountAfter;public SavedCompanion[] companionsBefore,companionsAfter;}
    [Serializable] class Items{public SavedItem[] items;}
    public static void DescribeActors(){
        var lines=new System.Collections.Generic.List<string>();
        foreach(var a in UnityEngine.Object.FindObjectsByType<P02ActorPresentation>(FindObjectsInactive.Include)){
            lines.Add(a.name+" active="+a.gameObject.activeInHierarchy+" enabled="+a.enabled+" key="+a.ActorKey+" direction="+a.Direction+" owns="+a.OwnsBody);
            foreach(var name in new[]{"library","original","player","fixedActor","staticPose"}){
                var v=typeof(P02ActorPresentation).GetField(name,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)?.GetValue(a);
                lines.Add("  "+name+"="+v);
            }
            lines.Add("  adultOutfit="+(a.GetComponent<AdultOutfitController>()!=null));
            foreach(var r in a.GetComponentsInChildren<SpriteRenderer>(true))lines.Add("  sprite="+r.name+" active="+r.gameObject.activeInHierarchy+" enabled="+r.enabled+" forceOff="+r.forceRenderingOff+" alpha="+r.color.a+" source="+(r.sprite?r.sprite.name:"none"));
        }
        File.WriteAllLines(P02ProductionInputReview.Work+"visual_binding_diagnosis.txt",lines);
    }
    public static void DescribeScene(){
        var p=PersistentRuntime.Instance.GetComponent<GameControlManager>().ActivePlayer;
        var lines=new System.Collections.Generic.List<string>{"PLAYER "+p.transform.position+" speed="+p.WalkSpeed};
        foreach(var c in UnityEngine.Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Exclude))if(c.bounds.max.x>62&&c.bounds.min.x<77&&c.bounds.min.y<3)lines.Add("COL "+c.name+" enabled="+c.enabled+" trigger="+c.isTrigger+" bounds="+c.bounds);
        foreach(var r in UnityEngine.Object.FindObjectsByType<RoomTransitionInteractable>(FindObjectsInactive.Exclude))lines.Add("DOOR "+r.name+" "+r.transform.position);
        File.WriteAllLines(P02ProductionInputReview.Work+"scene_bindings.txt",lines);
    }
    public static void ChildStreet()=>Start("child_street");
    public static void ChildMaze()=>Start("child_maze");
    public static void River()=>Start("child_river");
    public static void Youth()=>Start("school_youth");
    public static void GuideHome()=>Start("guide_home");
    public static void GuideStreet()=>Start("guide_street");
    public static void GuideHandover()=>Start("guide_handover");
    public static void GuideHandoverRevealed()=>Start("guide_handover_revealed");
    static void Start(string scenario){
        if(!EditorApplication.isPlaying||!PersistentRuntime.Instance||GameplayInput.Replay==null)throw new InvalidOperationException("Bootstrap and isolated replay required");
        PersistentRuntime.Instance.StartCoroutine(Load(scenario));
    }
    static IEnumerator Load(string scenario){
        var rt=PersistentRuntime.Instance;rt.GetComponent<GamePause>().SetPaused(false);
        rt.GetComponent<P02ToolMotion>()?.Clear(true);
        for(int n=0;n<4;n++)yield return null;
        var saves=rt.GetComponent<GameSaveService>();
        if(!saves.CanSave(out var why))throw new InvalidOperationException("Fixture capture rejected: "+why);
        if(seed==null)seed=saves.Capture(4);
        var d=JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(seed));
        string[] flags;d.school=new SavedSchool();d.companions=new SavedCompanion[0];d.lampRaised=false;d.planar=false;
        if(scenario.StartsWith("child")){
            d.scene="Scene03_Childhood";d.actor="child";d.scale=Vector3.one;d.position=new Vector3(67,31.23f,0);d.story.outfit=0;
            flags=new[]{"Scene03Started","Scene03IntroCompleted","Scene03PhoneAnswered","Scene03CameraTaken","Scene03StreetReached","SprintUnlocked"};
            if(scenario=="child_maze"){flags=flags.Concat(new[]{"Scene03MazeEntered"}).ToArray();d.planar=true;d.story.mazeCell=new Vector2Int(14,0);d.position=new Vector3(181.6f,71.6f,0);}
            if(scenario=="child_river"){flags=flags.Concat(new[]{"Scene03MazeEntered","Scene03MazeCompleted"}).ToArray();d.position=new Vector3(300,70.1f,0);}
        }else if(scenario=="school_youth"){
            d.scene="Scene04_School";d.actor="youth";d.scale=new Vector3(.76f,1.7f,1);d.position=new Vector3(272,17.41f,0);
            flags=new[]{"FogIntroCompleted","SchoolNameRevealed","Scene05OpeningPlayed","SchoolYouthConversationCompleted","SprintUnlocked"};
            d.school=new SavedSchool{present=true,room="Class3_1",variant=1,actor=1,phase=3};
        }else{
            d.actor="adult";d.scale=seed.scale;d.story.outfit=0;
            d.scene=scenario=="guide_street"?"Scene01_Rain":"Scene02_Courtyard";
            d.position=scenario=="guide_street"?new Vector3(3,-.095f,0):new Vector3(70,1.41f,0);
            if(scenario=="guide_home")d.position=new Vector3(0,1.41f,0);
            flags=new[]{"SprintUnlocked","FogIntroCompleted","Scene02Fragment2Collected","SchoolReturnedHome","SchoolGuideReturnSpoken","SchoolNameRevealed","Scene05OpeningPlayed","SchoolYouthConversationCompleted","SchoolChaseStarted","SchoolChaseCompleted","SchoolEmailRead","EndingEmailReactionCompleted","EndingHandoverCompleted","EndingIdentityHintIssued"};
            if(scenario=="guide_street")flags=flags.Concat(new[]{"EndingStreetVisit"}).ToArray();
            flags=flags.Concat(new[]{"DeskEventTriggered","ChildShadowSequencePlayed","ChildShadowFollowupPlayed","ParentShadowEventPlayed"}).ToArray();
            if(scenario.StartsWith("guide_handover")){flags=flags.Where(f=>f!="EndingHandoverCompleted"&&f!="EndingIdentityHintIssued").ToArray();d.position=new Vector3(132,7.435f,0);}
        }
        if(scenario.StartsWith("guide_"))flags=flags.Concat(new[]{"Scene03Completed"}).ToArray();
        d.story.flags=flags.OrderBy(x=>x,StringComparer.Ordinal).ToArray();d.cameraAvailable=true;
        d.cameraX=new Vector2(d.position.x-6,d.position.x+6);d.cameraY=new Vector2(d.position.y+1,d.position.y+1);d.cameraPosition=d.position+new Vector3(0,1,-10);d.cameraSize=3;d.followY=true;
        // Isolated handover fixture must see both authored floors. This diagnostic camera is never saved into a scene or used as natural camera evidence.
        if(scenario.StartsWith("guide_handover")){d.cameraX=new Vector2(-33,145);d.cameraY=new Vector2(-10,15);d.cameraOffset=1.1f;}
        if(scenario=="child_maze"){d.cameraSize=9;d.cameraX=new Vector2(196,228);d.cameraY=new Vector2(79,109);d.cameraPosition=new Vector3(196,79,-10);d.cameraOffset=0;}
        saves.Store.Write(4,d,true);
        if(!saves.Load(4,out var error))throw new InvalidOperationException(error);
        var until=Time.realtimeSinceStartup+30;
        while(saves.IsRestoring&&Time.realtimeSinceStartup<until)yield return null;
        if(saves.IsRestoring||saves.LastError!=null)throw new InvalidOperationException("Fixture restore failed: "+saves.LastError);
        // This seed comes from an earlier chapter, so the diagnostic snapshot
        // supplies the authored companion anchor explicitly. Production restore
        // and following controllers keep their original movement decisions.
        if(scenario=="guide_home"){
            var home=UnityEngine.Object.FindAnyObjectByType<EndingHomeController>();
            home.guide.position=EndingSession.CompanionAnchor(home.guide,rt.GetComponent<GameControlManager>().ActivePlayer,-home.data.followDistance,home.data.guideVisualGroundOffset);
            home.guide.GetComponent<P02CompanionFollow>()?.Relocated();
        }
        if(scenario=="guide_street"){
            var street=UnityEngine.Object.FindAnyObjectByType<EndingStreetController>();
            street.guide.position=EndingSession.CompanionAnchor(street.guide,rt.GetComponent<GameControlManager>().ActivePlayer,-street.data.followDistance,street.data.guideVisualGroundOffset);
            street.guide.GetComponent<P02CompanionFollow>()?.Relocated();
        }
        for(int n=0;n<20;n++)yield return null;
        File.WriteAllText(P02ProductionInputReview.Work+"scenario_ready.json",JsonUtility.ToJson(new Ready{timestamp=DateTime.Now.ToString("O"),scenario=scenario,actor=rt.GetComponent<GameControlManager>().ActivePlayer.name,position=rt.GetComponent<GameControlManager>().ActivePlayer.transform.position,scope="Injected isolated saved state; never natural main-chain evidence"},true));
    }
    [Serializable] class Ready{public string timestamp,scenario,actor,scope;public Vector3 position;}
}
