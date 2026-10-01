using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class P01SceneChecks {
    static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Temp/P01SceneChecks"));
    string reportName="matrix.txt";List<string> lines=new List<string>();GameSaveData seed;GameSaveService saves;
    void Log(bool pass,string s){lines.Add((pass?"PASS ":"FAIL ")+s);File.WriteAllLines(Path.Combine(Folder,reportName),lines);}
    public static void Run(){if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");PersistentRuntime.Instance.StartCoroutine(new P01SceneChecks().Matrix());}
    IEnumerator Matrix(){
        Directory.CreateDirectory(Folder);var rt=PersistentRuntime.Instance;saves=rt.GetComponent<GameSaveService>();rt.GetComponent<GamePause>().SetPaused(false);
        typeof(GameSaveService).GetProperty("Store").GetSetMethod(true).Invoke(saves,new object[]{new FourSlotSaveStore(Path.Combine(Folder,"slots"))});
        if(!saves.CanSave(out var reason)){Log(false,reason);yield break;}seed=saves.Capture(1);
        yield return Load("Scene02_Courtyard",new Vector3(70,1.41f,0),"adult",new StoryFlag[0]);if(saves.LastError!=null)yield break;
        yield return Sweep("Bedroom",new[]{64.8f,67f,70f,73f,75.4f});
        yield return Load("Scene03_Childhood",new Vector3(67,31.23f,0),"child",new[]{StoryFlag.Scene03Started,StoryFlag.Scene03IntroCompleted,StoryFlag.Scene03PhoneAnswered,StoryFlag.Scene03CameraTaken,StoryFlag.Scene03StreetReached});if(saves.LastError!=null)yield break;
        yield return Sweep("Pond",new[]{59f,63f,67f,72f,78f,82f});
        yield return RhythmPause();
        yield return Load("Scene04_School",new Vector3(208,17.41f,0),"adult",new[]{StoryFlag.FogIntroCompleted},new SavedSchool{present=true,room="Class3_2",variant=0,actor=0,phase=0});if(saves.LastError!=null)yield break;
        yield return Sweep("Classroom",new[]{201f,205f,210f,215f,219f});
        yield return Load("Scene04_School",new Vector3(272,17.41f,0),"youth",new[]{StoryFlag.FogIntroCompleted,StoryFlag.SchoolNameRevealed,StoryFlag.Scene05OpeningPlayed,StoryFlag.SchoolYouthConversationCompleted},new SavedSchool{present=true,room="Class3_1",variant=1,actor=1,phase=3});if(saves.LastError!=null)yield break;
        Log(!rt.GetComponent<GameControlManager>().AllowsAdultOutfits,"school youth restored");
        yield return Load("Scene04_School",new Vector3(12,1.41f,0),"adult",new[]{StoryFlag.FogIntroCompleted,StoryFlag.SchoolNameRevealed,StoryFlag.Scene05OpeningPlayed,StoryFlag.SchoolYouthConversationCompleted,StoryFlag.SchoolChaseStarted,StoryFlag.SchoolChaseCompleted},new SavedSchool{present=true,room="Floor01",variant=0,actor=0,phase=7});
        var chase=UnityEngine.Object.FindAnyObjectByType<SchoolChaseController>();Log(chase&&chase.Step==SchoolChaseStep.Exploring&&!chase.EnemyActive&&rt.GetComponent<GameControlManager>().AllowsAdultOutfits,"completed chase resumes adult free exploration");
        Log(!lines.Any(x=>x.StartsWith("FAIL")),"SUMMARY (direct state injection; not natural main-chain play)");
    }
    IEnumerator RhythmPause(){
        var guitar=UnityEngine.Object.FindAnyObjectByType<GuitarRhythmGame>(FindObjectsInactive.Include);
        var pause=PersistentRuntime.Instance.GetComponent<GamePause>();
        Log(guitar&&guitar.ValidateConfiguration(),"guitar configuration remains valid");if(!guitar)yield break;
        guitar.gameObject.SetActive(true);guitar.Begin();typeof(GuitarRhythmGame).GetMethod("StartPhrase",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(guitar,null);
        typeof(GuitarRhythmGame).GetProperty("WaitingForStart").GetSetMethod(true).Invoke(guitar,new object[]{false});
        yield return null;Log(!saves.CanSave(out _),"save forbidden while real guitar phrase is running");
        var tone=guitar.phrases[0].notes.First(n=>n.tone).tone;guitar.instrument.clip=tone;guitar.instrument.Play();yield return null;
        pause.SetPaused(true);float time=guitar.PhraseTime;int retry=guitar.RetryCount,samples=guitar.instrument.timeSamples;float until=Time.realtimeSinceStartup+1.1f;
        while(Time.realtimeSinceStartup<until)yield return null;
        Log(guitar.PhraseTime==time&&guitar.RetryCount==retry,"real guitar phrase and retries frozen while paused");
        Log(guitar.instrument.timeSamples==samples&&!guitar.instrument.ignoreListenerPause,"guitar audio playhead frozen with listener");
        Log(!saves.CanSave(out _),"paused guitar cannot bypass save restriction");
        pause.SetPaused(false);yield return null;yield return null;
        Log(guitar.PhraseTime>time&&guitar.RetryCount==retry,"guitar resumes same phrase without restart");
        Log(PersistentRuntime.Instance.GetComponent<GameControlManager>().IsLockedBy(ControlLockReason.CloseUp),"guitar keeps gameplay lock after resume");
        guitar.gameObject.SetActive(false);
    }
    IEnumerator Load(string scene,Vector3 position,string actor,StoryFlag[] flags,SavedSchool school=null){
        var d=JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(seed));d.scene=scene;d.position=position;d.scale=actor=="child"?Vector3.one:seed.scale;d.actor=actor;d.school=school??new SavedSchool();d.story.flags=flags.Select(f=>f.ToString()).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
        d.story.mazeCell=new Vector2Int(14,0);d.cameraX=new Vector2(position.x-6,position.x+6);d.cameraPosition=new Vector3(position.x,position.y+1,-10);d.cameraY=new Vector2(position.y+1,position.y+1);d.followY=true;d.cameraSize=3;d.cameraAvailable=true;
        if(actor=="youth")d.scale=new Vector3(.76f,1.7f,1);
        saves.Store.Write(1,d,true);Log(saves.Load(1,out var error),"load "+scene+" "+actor+": "+error);float until=Time.realtimeSinceStartup+30;
        while(saves.IsRestoring&&Time.realtimeSinceStartup<until)yield return null;
        Log(!saves.IsRestoring&&saves.LastError==null,"settled "+scene+" "+actor+": "+saves.LastError);
        for(int i=0;i<12;i++)yield return null;
    }
    IEnumerator Sweep(string label,float[] positions){
        var rt=PersistentRuntime.Instance;var controls=rt.GetComponent<GameControlManager>();var p=controls.ActivePlayer;var rb=p.GetComponent<Rigidbody2D>();
        foreach(float x in positions){
            var at=p.transform.position;at.x=x;EndingSession.Place(p,at);Physics2D.SyncTransforms();RoomPresentationFraming.PrepareCurrentView();
            for(int i=0;i<30;i++)yield return new WaitForFixedUpdate();
            var cam=Camera.main;var rect=cam.ViewportToWorldPoint(new Vector3(0,0,10));var top=cam.ViewportToWorldPoint(new Vector3(1,1,10));
            var body=p.GetComponent<Collider2D>().bounds;
            var measurements=new List<string>{"DIAGNOSTIC position sampling, not natural input",$"scene={SceneManager.GetActiveScene().name} player={p.transform.position} feet={body.min.y} speed={p.WalkSpeed} camera={cam.transform.position} size={cam.orthographicSize} aspect={cam.aspect}"};
            foreach(var r in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude))if(r.enabled&&!r.forceRenderingOff&&r.bounds.max.x>=rect.x&&r.bounds.min.x<=top.x&&r.bounds.max.y>=rect.y&&r.bounds.min.y<=top.y){
                string path=r.name;for(var parent=r.transform.parent;parent!=null;parent=parent.parent)path=parent.name+"/"+path;
                measurements.Add($"{path} min={r.bounds.min} max={r.bounds.max} order={r.sortingOrder} sprite={r.sprite?.name} texture={r.sprite?.texture.width}x{r.sprite?.texture.height}");
            }
            string name=label+"_"+x.ToString("0");File.WriteAllLines(Path.Combine(Folder,name+".txt"),measurements);ScreenCapture.CaptureScreenshot(Path.Combine(Folder,name+".png"));yield return null;
        }
        Log(true,label+" snapshots captured; visual review pending");
    }

    public static void RunCoverage(){if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");PersistentRuntime.Instance.StartCoroutine(new P01SceneChecks().Coverage());}
    IEnumerator Coverage(){
        reportName="coverage-runtime.txt";
        Directory.CreateDirectory(Folder);saves=PersistentRuntime.Instance.GetComponent<GameSaveService>();
        typeof(GameSaveService).GetProperty("Store").GetSetMethod(true).Invoke(saves,new object[]{new FourSlotSaveStore(Path.Combine(Folder,"coverage-slots"))});
        if(!saves.CanSave(out var why)){Log(false,why);yield break;}seed=saves.Capture(1);
        yield return Load("Scene03_Childhood",new Vector3(67,31.23f,0),"child",new[]{StoryFlag.Scene03Started,StoryFlag.Scene03IntroCompleted,StoryFlag.Scene03PhoneAnswered,StoryFlag.Scene03CameraTaken,StoryFlag.Scene03StreetReached});
        yield return ScanCoverage("Scene03");
        yield return Load("Scene03_Childhood",new Vector3(70,1.23f,0),"child",new[]{StoryFlag.Scene03Started,StoryFlag.Scene03IntroCompleted});
        yield return ScanCoverage("Scene03Home");
        yield return Load("Scene03_Childhood",new Vector3(200,1.23f,0),"child",new[]{StoryFlag.Scene03Started,StoryFlag.Scene03IntroCompleted,StoryFlag.Scene03PhoneAnswered,StoryFlag.Scene03CameraTaken,StoryFlag.Scene03ReturnedHome,StoryFlag.Scene03FamilySetupComplete});
        yield return ScanCoverage("Scene03ReturnHome");
        yield return Load("Scene04_School",new Vector3(208,17.41f,0),"adult",new[]{StoryFlag.FogIntroCompleted},new SavedSchool{present=true,room="Class3_2",variant=0,actor=0,phase=0});
        yield return ScanCoverage("SchoolOtherworld");
        yield return Load("Scene04_School",new Vector3(208,17.41f,0),"youth",new[]{StoryFlag.FogIntroCompleted,StoryFlag.SchoolNameRevealed,StoryFlag.Scene05OpeningPlayed,StoryFlag.SchoolYouthConversationCompleted},new SavedSchool{present=true,room="Class3_2",variant=1,actor=1,phase=3});
        yield return ScanCoverage("SchoolMemory");
        yield return Load("Scene04_School",new Vector3(208,64.93f,0),"youth",new[]{StoryFlag.FogIntroCompleted,StoryFlag.SchoolNameRevealed,StoryFlag.Scene05OpeningPlayed,StoryFlag.SchoolYouthConversationCompleted},new SavedSchool{present=true,room="ReadingPassage",variant=1,actor=1,phase=3});
        var reading=UnityEngine.Object.FindAnyObjectByType<SchoolReadingChair>();var rt=PersistentRuntime.Instance;var player=rt.GetComponent<GameControlManager>().ActivePlayer;
        reading.Toggle();yield return null;Log(reading.IsOpen&&Mathf.Abs(player.transform.position.x-208)<.1f,"reading interaction uses aligned seat");Log(!saves.CanSave(out _),"reading modal forbids save");
        rt.GetComponent<GamePause>().SetPaused(true);var seated=player.transform.position;float until=Time.realtimeSinceStartup+.3f;while(Time.realtimeSinceStartup<until)yield return null;Log(player.transform.position==seated&&reading.IsOpen,"reading pose stays frozen during pause");rt.GetComponent<GamePause>().SetPaused(false);reading.Close();yield return new WaitForFixedUpdate();
        Log(!reading.IsOpen&&GroundedPlacement.IsSupported(player)&&!rt.GetComponent<GameControlManager>().IsLocked(),"reading close restores supported standing and controls");
        for(int i=0;i<12;i++)yield return null;
        var painter=player.GetComponentsInChildren<SpriteRenderer>(true);var visualReport=new List<string>();foreach(var r in painter)visualReport.Add(r.name+" enabled="+r.enabled+" forceOff="+r.forceRenderingOff+" sprite="+(r.sprite?r.sprite.name:"none")+" rect="+(r.sprite?r.sprite.rect.ToString():"")+" path="+(r.sprite?AssetDatabase.GetAssetPath(r.sprite):"")+" bounds="+r.bounds);File.WriteAllLines(Path.Combine(Folder,"ReadingChairVisuals.txt"),visualReport);
        ScreenCapture.CaptureScreenshot(Path.Combine(Folder,"ReadingChairClosed.png"));
        Log(!lines.Any(x=>x.StartsWith("FAIL")),"SUMMARY isolated coverage; not natural main-chain play");
    }
    IEnumerator ScanCoverage(string label){
        var p=PersistentRuntime.Instance.GetComponent<GameControlManager>().ActivePlayer;var interaction=p.GetComponent<PlayerInteraction>();var rb=p.GetComponent<Rigidbody2D>();var shape=p.GetComponent<Collider2D>();var at=p.transform.position;bool simulate=rb.simulated;rb.simulated=false;float offset=at.y-shape.bounds.min.y;
        var chapter=UnityEngine.Object.FindAnyObjectByType<Scene03Chapter>();bool chapterEnabled=chapter&&chapter.enabled;if(chapter)chapter.enabled=false;
        var report=new List<string>();var detect=typeof(PlayerInteraction).GetMethod("DetectInteractable",BindingFlags.Instance|BindingFlags.NonPublic);
        try{
            foreach(var c in UnityEngine.Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Exclude).Where(x=>x.enabled&&x.GetComponent<IInteractable>()!=null).OrderBy(x=>x.transform.position.x)){
                var target=c.GetComponent<IInteractable>();if(target is Behaviour behavior&&!behavior.enabled)continue;
                var initialGround=Physics2D.RaycastAll(new Vector2(c.bounds.center.x,c.bounds.max.y+1),Vector2.down,12).Where(h=>h.collider&&!h.collider.isTrigger&&!h.collider.attachedRigidbody&&h.normal.y>.65f&&h.collider.GetComponentInParent<IInteractable>()==null).OrderByDescending(h=>h.point.y).ToArray();
                if(initialGround.Length>0){EndingSession.Place(p,new Vector3(c.bounds.center.x,initialGround[0].point.y+.005f+offset,0));Physics2D.SyncTransforms();RoomPresentationFraming.PrepareCurrentView();yield return null;yield return null;}
                string path=c.name;for(var t=c.transform.parent;t!=null;t=t.parent)path=t.name+"/"+path;
                if(string.IsNullOrWhiteSpace(target.GetInteractionPrompt())){report.Add("GATED "+path);continue;}
                var highlight=c.GetComponent<HandDrawnInteractionHighlight>();var surface=c.bounds;bool bound=highlight&&highlight.TryGetInteractionBounds(out surface);if(!bound)surface=c.bounds;
                bool selected=false,supported=false,outlineSeen=false;var selections=new HashSet<string>();
                foreach(float x in new[]{surface.center.x,surface.min.x-.25f,surface.max.x+.25f, surface.center.x-.35f,surface.center.x+.35f}){
                    var ground=Physics2D.RaycastAll(new Vector2(x,c.bounds.max.y+1),Vector2.down,12).Where(h=>h.collider&&!h.collider.isTrigger&&!h.collider.attachedRigidbody&&h.normal.y>.65f&&h.collider.GetComponentInParent<IInteractable>()==null).OrderByDescending(h=>h.point.y).ToArray();
                    if(ground.Length==0)continue;supported=true;p.transform.position=new Vector3(x,ground[0].point.y+.005f+offset,0);rb.position=p.transform.position;Physics2D.SyncTransforms();yield return null;detect.Invoke(interaction,null);
                    if(c.name=="B5_ReadingChair"){var reach=c.GetComponent<InteractionReach>();var pt=reach&&reach.anchor?(Vector2)reach.anchor.position:(Vector2)surface.ClosestPoint(interaction.Feet);report.Add("PROBE x="+x+" transform="+p.transform.position+" shape="+shape.bounds.center+" surface="+surface.center+" feet="+interaction.Feet+" point="+pt+" floor="+ground[0].collider.name+" hits="+string.Join(",",Physics2D.LinecastAll(interaction.Feet+Vector2.up*.65f,new Vector2(pt.x,Mathf.Max(interaction.Feet.y+.15f,pt.y))).Select(h=>h.collider.name+":"+h.collider.isTrigger)));}
                    bool isSelected=ReferenceEquals(interaction.CurrentVisualTarget,target);selected|=isSelected;if(interaction.CurrentVisualTarget is Component chosen)selections.Add(chosen.name);
                    if(isSelected&&highlight&&bound){yield return new WaitForEndOfFrame();var outlines=(SpriteRenderer[])typeof(HandDrawnInteractionHighlight).GetField("outlines",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(highlight);var contours=(LineRenderer[])typeof(HandDrawnInteractionHighlight).GetField("contours",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(highlight);bool drawn=(outlines!=null&&outlines.Any(r=>r&&r.enabled&&r.gameObject.activeInHierarchy))||(contours!=null&&contours.Any(r=>r&&r.enabled&&r.gameObject.activeInHierarchy));
                        if(drawn&&!outlineSeen&&new[]{"S03_ExitHome","S03_Fish","FamilyCameraPlacement","B5_ReadingChair","Basketball_Interaction"}.Contains(c.name))ScreenCapture.CaptureScreenshot(Path.Combine(Folder,"Outline_"+label+"_"+c.name+".png"));outlineSeen|=drawn;}
                }
                report.Add((selected&&(!bound||outlineSeen)?"PASS ":supported?"REVIEW ":"NO_FLOOR ")+path+" type="+target.GetType().Name+" paintedBounds="+bound+" outlineSeen="+outlineSeen+" width="+surface.size.x+" selected="+string.Join(",",selections));
            }
        }finally{EndingSession.Place(p,at);rb.simulated=simulate;if(chapter)chapter.enabled=chapterEnabled;Physics2D.SyncTransforms();RoomPresentationFraming.PrepareCurrentView();}
        File.WriteAllLines(Path.Combine(Folder,"coverage-"+label+".txt"),report);
        Log(!report.Any(x=>x.StartsWith("REVIEW ")||x.StartsWith("NO_FLOOR ")),label+" selectable targets and painted outlines");
    }

    public static void RunContinuous(){
        if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");
        PersistentRuntime.Instance.StartCoroutine(new P01SceneChecks().Continuous());
    }
    IEnumerator Continuous(){
        reportName="continuous-final.txt";Directory.CreateDirectory(Folder);
        saves=PersistentRuntime.Instance.GetComponent<GameSaveService>();
        if(!saves.CanSave(out var why)){Log(false,why);yield break;}
        seed=saves.Capture(1);var original=saves.Store;
        typeof(GameSaveService).GetProperty("Store").GetSetMethod(true).Invoke(saves,new object[]{new FourSlotSaveStore(Path.Combine(Folder,"continuous-slots"))});
        yield return Load("Scene02_Courtyard",new Vector3(64.8f,1.41f,0),"adult",new StoryFlag[0]);
        yield return InputWalk("FinalBedroom",5.2f);
        yield return Load("Scene03_Childhood",new Vector3(58.32f,31.23f,0),"child",new[]{StoryFlag.Scene03Started,StoryFlag.Scene03IntroCompleted,StoryFlag.Scene03PhoneAnswered,StoryFlag.Scene03CameraTaken,StoryFlag.Scene03StreetReached});
        Camera.main.GetComponent<CameraFollow>().SetLimits(-22,135);RoomPresentationFraming.PrepareCurrentView();
        yield return InputWalk("Final2Pond",17.7f);
        yield return Load("Scene04_School",new Vector3(201.18f,17.41f,0),"adult",new[]{StoryFlag.FogIntroCompleted},new SavedSchool{present=true,room="Class3_2",variant=0,actor=0,phase=0});
        UnityEngine.Object.FindAnyObjectByType<SchoolLayoutController>().EnterRoom("Class3_2");
        yield return InputWalk("Final2ClassOther",9f);
        yield return Load("Scene04_School",new Vector3(200.9f,17.41f,0),"youth",new[]{StoryFlag.FogIntroCompleted,StoryFlag.SchoolNameRevealed,StoryFlag.Scene05OpeningPlayed,StoryFlag.SchoolYouthConversationCompleted},new SavedSchool{present=true,room="Class3_2",variant=1,actor=1,phase=3});
        UnityEngine.Object.FindAnyObjectByType<SchoolLayoutController>().EnterRoom("Class3_2");
        yield return InputWalk("Final2ClassMemory",9.8f);
        typeof(GameSaveService).GetProperty("Store").GetSetMethod(true).Invoke(saves,new object[]{original});
        Log(true,"continuous captures complete; diagnostic starting states, actual gameplay walking inputs; visual review required");
    }
    IEnumerator InputWalk(string label,float seconds){
        if(saves.LastError!=null){Log(false,label+" restore failed");yield break;}
        P01InputReplay.Begin();
        var steps=new[]{new P01InputReplay.Step{label=label+"_settle",seconds=.5f},new P01InputReplay.Step{label=label+"_right",keys="D",seconds=seconds},new P01InputReplay.Step{label=label+"_turn",seconds=.3f},new P01InputReplay.Step{label=label+"_left",keys="A",seconds=seconds},new P01InputReplay.Step{label=label+"_done",seconds=.4f}};
        File.WriteAllText(Path.Combine(P01InputReplay.Folder,"queue.json"),JsonUtility.ToJson(new P01InputReplay.Batch{steps=steps}));
        var records=new List<string>{"Diagnostic initial placement; subsequent samples use actual gameplay D/A inputs."};
        float start=Time.realtimeSinceStartup,next=start,end=start+seconds*2+1.8f;int count=0;
        while(Time.realtimeSinceStartup<end){
            if(Time.realtimeSinceStartup>=next){next+=.5f;var p=PersistentRuntime.Instance.GetComponent<GameControlManager>().ActivePlayer;records.Add(count+" t="+(Time.realtimeSinceStartup-start)+" p="+p.transform.position.ToString("F3"));ScreenCapture.CaptureScreenshot(Path.Combine(Folder,label+"_"+(count++).ToString("D3")+".png"));}
            yield return null;
        }
        P01InputReplay.End();yield return null;yield return null;
        File.WriteAllLines(Path.Combine(Folder,label+".txt"),records);Log(true,label+" captured "+count+" frames; visual acceptance pending");
    }

    public static void RunPondFinal(){PersistentRuntime.Instance.StartCoroutine(new P01SceneChecks().PondFinal());}
    IEnumerator PondFinal(){
        reportName="pond-final.txt";Directory.CreateDirectory(Folder);saves=PersistentRuntime.Instance.GetComponent<GameSaveService>();seed=saves.Capture(1);var original=saves.Store;
        typeof(GameSaveService).GetProperty("Store").GetSetMethod(true).Invoke(saves,new object[]{new FourSlotSaveStore(Path.Combine(Folder,"pond-final-slots"))});
        yield return Load("Scene03_Childhood",new Vector3(58.32f,31.205f,0),"child",new[]{StoryFlag.Scene03Started,StoryFlag.Scene03IntroCompleted,StoryFlag.Scene03PhoneAnswered,StoryFlag.Scene03CameraTaken,StoryFlag.Scene03StreetReached});
        Camera.main.GetComponent<CameraFollow>().SetLimits(-22,135);RoomPresentationFraming.PrepareCurrentView();
        yield return InputWalk("Final3Pond",17.7f);
        typeof(GameSaveService).GetProperty("Store").GetSetMethod(true).Invoke(saves,new object[]{original});Log(true,"captures complete; visual review required");
    }
}
