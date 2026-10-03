using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Observes the installed production presentation; never creates an extra actor or changes motion.</summary>
public static class P02ProductionInputReview
{
    public const string Work="D:/游戏设计/Codex工作区/work/2026-10-02-P02/";
    public const string Evidence="D:/GitHub Desktop/ProjectMystery/ProjectMystery/Documentation/FirstLargeTest_20261001/P02Evidence/";
    [Serializable] public class Request {public string label="native_input",scope="Normal gameplay input; installed production actors observed without speed or transform overrides";public float seconds=32;public int width=1920,height=1080;}
    public static void BeginAndReplay()
    {
        if(!EditorApplication.isPlaying||!PersistentRuntime.Instance||GameplayInput.Replay==null)
            throw new InvalidOperationException("Bootstrap Play Mode and isolated input replay are required.");
        if(UnityEngine.Object.FindAnyObjectByType<P02ProductionInputDriver>())throw new InvalidOperationException("Previous native capture is still active.");
        if(File.Exists(P01InputReplay.Folder+"/queue.json"))throw new InvalidOperationException("Input batch is still pending.");
        var request=JsonUtility.FromJson<Request>(File.ReadAllText(Work+"production_input_request.json"));
        var driver=new GameObject("P02 production observation only").AddComponent<P02ProductionInputDriver>();
        UnityEngine.Object.DontDestroyOnLoad(driver.gameObject);
        driver.Begin(request);
        File.Copy(Work+"production_input_queue.json",P01InputReplay.Folder+"/queue.json");
    }
    public static void End(){var d=UnityEngine.Object.FindAnyObjectByType<P02ProductionInputDriver>();if(d){d.Finish();UnityEngine.Object.Destroy(d.gameObject);}}
    public static void Resume(){EditorWindow.GetWindow(typeof(Editor).Assembly.GetType("UnityEditor.GameView")).Focus();PersistentRuntime.Instance.GetComponent<GamePause>().SetPaused(false);PersistentRuntime.Instance.GetComponent<P02ToolMotion>()?.Clear(true);}
    // These preparations are isolated diagnostic cases, never reported as natural story progression.
    public static void AdultDefault()=>PrepareAdult(AdultOutfit.Default);
    public static void AdultOveralls()=>PrepareAdult(AdultOutfit.Overalls);
    public static void AdultSchool()=>PrepareAdult(AdultOutfit.SchoolUniform);
    public static void PrepareCameraInventory()=>AddRequiredItem(PersistentRuntime.Instance.GetComponent<CameraModeController>());
    static void PrepareAdult(AdultOutfit outfit)
    {
        var rt=PersistentRuntime.Instance;var controls=rt.GetComponent<GameControlManager>();
        if(!controls.ActivePlayer||!controls.AllowsAdultOutfits)throw new InvalidOperationException("An existing adult player is required.");
        var motion=rt.GetComponent<P02ToolMotion>();motion?.Clear(true);
        var story=rt.GetComponent<StoryStateManager>();story.Set(StoryFlag.SprintUnlocked);story.Set(StoryFlag.OverallsUnlocked);story.Set(StoryFlag.SchoolUniformOwned);story.Set(StoryFlag.OilLampExhausted,false);story.SetOutfit(outfit);
        var lamp=rt.GetComponent<OilLampController>();
        if(!lamp){lamp=rt.gameObject.AddComponent<OilLampController>();lamp.ConfigureItem(AssetDatabase.LoadAssetAtPath<ItemData>("Assets/GameData/Items/Scene02/I_S02_OilLamp.asset"));}
        AddRequiredItem(rt.GetComponent<CameraModeController>());AddRequiredItem(lamp);
        rt.GetComponent<CameraModeController>().ContextAvailable=true;
        // A no-op authored context exercises real exposure timing without starting Scene01's reveal.
        var context=rt.GetComponent<P02DiagnosticCaptureContext>()??rt.gameObject.AddComponent<P02DiagnosticCaptureContext>();
        rt.GetComponent<CameraModeController>().SetCaptureContext(context);
        File.AppendAllText(Work+"diagnostic_preparations.txt",DateTime.Now.ToString("O")+" isolated adult outfit, sprint and inventory preparation: "+outfit+"\n");
    }
    static void AddRequiredItem(Component controller)
    {
        if(!controller)throw new InvalidOperationException("Required authored controller is not installed.");
        var field=controller.GetType().GetField("requiredItem",BindingFlags.Instance|BindingFlags.NonPublic);
        var item=field?.GetValue(controller) as ItemData;if(!item)throw new InvalidOperationException("Authored required item missing.");
        var inventory=PersistentRuntime.Instance.GetComponent<InventoryManager>();if(!inventory.HasItem(item))inventory.AddItem(item);
    }
}

public sealed class P02DiagnosticCaptureContext:CameraCaptureContext
{
    public override bool CanRaise=>true;
    public override void Preview(Camera view){}
    public override void CancelPreview(Camera view){}
    public override void CommitCapture(Camera view){File.AppendAllText(P02ProductionInputReview.Work+"diagnostic_preparations.txt",DateTime.Now.ToString("O")+" diagnostic exposure completed through CameraModeController\n");}
}

[DefaultExecutionOrder(2000)]
public sealed class P02ProductionInputDriver:MonoBehaviour
{
    [Serializable] public class ActorFrame
    {
        public string actor,direction;public Vector3 position,foot,leftSole,rightSole,hand;public float speed,phase,lean,walkSpeed;
        public bool ownsBody,staticPose,running,leftContact,rightContact,following,catchingUp;public Vector2 velocity;
        public int bodyRoots,visibleParts;public string[] foreignSprites;
    }
    [Serializable] public class Frame
    {
        public double time;public string scene,image,tool,state,desired,saveReason,keys,dialogue;public string[] flags;
        public bool rhythmRunning,songPlaying,focused;public float songTime;
        public float blend,gameDelta;public bool paused,canSave,locked,cameraRaised,exposing,lampRaised;
        public int ownedProps;public ActorFrame[] actors;
    }
    [Serializable] public class Report
    {
        public string timestamp,label,scope,movie,slowMovie;public string[] codeHashes,errors;public Frame[] frames;
    }
    readonly List<Frame> frames=new List<Frame>();readonly List<string> errors=new List<string>();int savedImages;
    P02ProductionInputReview.Request request;RenderTexture target;Texture2D pixels;MediaEncoder normal;
    string folder;double start,nextImage;bool active,background,separated;
    public void Begin(P02ProductionInputReview.Request value)
    {
        request=value;folder=P02ProductionInputReview.Evidence+"native_gameplay_runs/"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+"_"+request.label+"/";
        Directory.CreateDirectory(folder+"frames/");File.WriteAllText(P02ProductionInputReview.Work+"active_native_input_review.txt",folder);
        File.Copy(P02ProductionInputReview.Work+"production_skin_manifest.json",folder+"source_manifest.json");
        File.Copy(P02ProductionInputReview.Work+"production_input_queue.json",folder+"input_queue.json");
        File.WriteAllText(folder+"request.json",JsonUtility.ToJson(request,true));
        target=new RenderTexture(request.width,request.height,24,RenderTextureFormat.ARGB32);
        pixels=new Texture2D(request.width,request.height,TextureFormat.RGBA32,false);
        var attributes=new VideoTrackAttributes{width=(uint)request.width,height=(uint)request.height,frameRate=new MediaRational(60),includeAlpha=false,bitRateMode=VideoBitrateMode.High};
        normal=new MediaEncoder(folder+"native_gameplay.mp4",attributes);
        background=Application.runInBackground;Application.runInBackground=true;start=Time.realtimeSinceStartupAsDouble;active=true;
    }
    void LateUpdate()
    {
        if(!active)return;double elapsed=Time.realtimeSinceStartupAsDouble-start;
        var rt=PersistentRuntime.Instance;if(!rt){Finish();return;}
        var controls=rt.GetComponent<GameControlManager>();var tool=rt.GetComponent<P02ToolMotion>();
        var saves=rt.GetComponent<GameSaveService>();bool canSave=saves.CanSave(out string reason);
        var observed=new List<ActorFrame>();
        foreach(var a in FindObjectsByType<P02ActorPresentation>(FindObjectsInactive.Exclude))
        {
            var rig=a.Rig;var p=a.GetComponent<PlayerController>();var follow=a.GetComponent<P02CompanionFollow>();var body=p?p.GetComponent<Rigidbody2D>():null;
            var foreign=a.GetComponentsInChildren<SpriteRenderer>(true).Where(r=>r.gameObject.activeInHierarchy&&r.enabled&&!r.forceRenderingOff&&r.color.a>.01f&&r.sprite&&!r.GetComponentInParent<P02CharacterRig>()&&r.bounds.size.y>.18f).Select(r=>r.name+":"+r.sprite.name).ToArray();
            int roots=a.GetComponentsInChildren<P02CharacterRig>(true).Count(r=>r.gameObject.activeInHierarchy);
            observed.Add(new ActorFrame{actor=a.ActorKey??a.name,direction=a.Direction,position=a.transform.position,
                foot=rig?rig.transform.position:a.transform.position,speed=rig?rig.actualSpeed:0,phase=rig?rig.phase:0,lean=rig?rig.bodyLean:0,walkSpeed=p?p.WalkSpeed:0,
                ownsBody=a.OwnsBody,staticPose=rig&&!rig.gameObject.activeInHierarchy,running=rig&&rig.running,velocity=body?body.linearVelocity:Vector2.zero,
                leftContact=rig&&rig.leftContact,rightContact=rig&&rig.rightContact,
                leftSole=rig?rig.leftSole:Vector3.zero,rightSole=rig?rig.rightSole:Vector3.zero,hand=rig?rig.HandCamera.position:Vector3.zero,
                following=follow&&follow.IsFollowing,catchingUp=follow&&follow.CatchingUp,bodyRoots=roots,foreignSprites=foreign,
                visibleParts=rig?rig.GetComponentsInChildren<SpriteRenderer>().Count(r=>r.enabled&&!r.forceRenderingOff&&r.color.a>.01f):0});
            if(roots>1)Error("Multiple active native bodies: "+a.name);
            if(a.OwnsBody&&foreign.Length>0)Error("Visible non-native actor artwork: "+a.name+" "+string.Join(",",foreign));
        }
        int props=FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Exclude).Count(r=>r.name=="P02_OwnedTool"&&r.enabled&&!r.forceRenderingOff&&r.color.a>.01f);
        if(props>1)Error("Multiple visible owned tools");
        var cameraTool=rt.GetComponent<CameraModeController>();var lamp=rt.GetComponent<OilLampController>();
        var river=FindAnyObjectByType<Scene03RiverSequence>();var rhythm=FindAnyObjectByType<GuitarRhythmGame>();var dialogue=rt.GetComponentInChildren<DialogueManager>(true);
        var frame=new Frame{time=elapsed,scene=SceneManager.GetActiveScene().name,paused=GamePause.IsPaused,gameDelta=GameplayClock.Delta,locked=controls.IsLocked(),canSave=canSave,saveReason=reason,keys=string.Join(",",new[]{KeyCode.A,KeyCode.D,KeyCode.W,KeyCode.S,KeyCode.LeftShift,KeyCode.Q,KeyCode.R,KeyCode.Space}.Where(GameplayInput.GetKey)),
            tool=tool?tool.Current.ToString():"missing",state=tool?tool.State.ToString():"missing",desired=tool?tool.Desired.ToString():"missing",blend=tool?tool.Blend:0,
            cameraRaised=cameraTool&&cameraTool.IsRaised,exposing=cameraTool&&cameraTool.IsBusy,lampRaised=lamp&&lamp.IsRaised,ownedProps=props,actors=observed.ToArray(),
            flags=rt.GetComponent<StoryStateManager>().CaptureSave().flags,dialogue=dialogue&&dialogue.CurrentDialogue?dialogue.CurrentDialogue.name:"",focused=Application.isFocused,rhythmRunning=rhythm&&rhythm.IsRunning,songPlaying=river&&river.music&&river.music.isPlaying,songTime=river&&river.music&&river.music.clip?river.music.time:0};
        if(frame.cameraRaised&&frame.lampRaised)Error("Camera and lamp controllers raised together");
        var camera=Camera.main;
        if(camera){var old=camera.targetTexture;var oldActive=RenderTexture.active;try{
            camera.targetTexture=target;camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,request.width,request.height),0,0);pixels.Apply(false);
            if(!normal.AddFrame(pixels,new MediaTime((long)(elapsed*1000000),1000000,1)))Error("Native recording frame rejected");
            if(elapsed>=nextImage&&savedImages<20){nextImage=elapsed+Mathf.Max(.35f,request.seconds/18);frame.image=folder+"frames/"+frames.Count.ToString("D5")+".png";File.WriteAllBytes(frame.image,pixels.EncodeToPNG());savedImages++;}
            if(!separated&&tool&&tool.CameraReady&&controls.ActivePlayer){
                separated=true;CaptureLayerDiagnosis(camera,controls.ActivePlayer,frame,oldActive);
            }
        }finally{camera.targetTexture=old;RenderTexture.active=oldActive;}}
        frames.Add(frame);if(elapsed>=request.seconds&&ReplayIdle())Finish();
    }
    static bool ReplayIdle(){var replay=GameplayInput.Replay;if(replay==null)return true;var f=replay.GetType().GetField("active",BindingFlags.Instance|BindingFlags.NonPublic);return f?.GetValue(replay)==null;}
    void Error(string message){if(!errors.Contains(message))errors.Add(message);}
    // Same pose and camera, renderer visibility only; never used as gameplay evidence.
    // Separates source pixels, native ownership and world marks before any art repair.
    void CaptureLayerDiagnosis(Camera camera,PlayerController player,Frame frame,RenderTexture oldActive)
    {
        var all=FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
        var owned=player.GetComponentsInChildren<Renderer>(true);
        var enabled=all.Select(r=>r.enabled).ToArray();
        var clear=camera.clearFlags;var color=camera.backgroundColor;
        var lines=new List<string>{"Diagnostic visibility separation; actor pose, physics and camera transform unchanged",JsonUtility.ToJson(frame)};
        foreach(var r in owned)lines.Add(r.name+" enabled="+r.enabled+" forceOff="+r.forceRenderingOff+" bounds="+r.bounds+" type="+r.GetType().Name+(r is SpriteRenderer s?" sprite="+(s.sprite?s.sprite.name:"none"):""));
        try{
            for(int i=0;i<all.Length;i++)if(owned.Contains(all[i]))all[i].enabled=false;
            ReadLayer("diagnostic_background_only.png");
            for(int i=0;i<all.Length;i++)all[i].enabled=owned.Contains(all[i])&&enabled[i];
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.22f,.26f,.29f,1);
            ReadLayer("diagnostic_actor_only.png");
        }finally{
            for(int i=0;i<all.Length;i++)if(all[i])all[i].enabled=enabled[i];
            camera.clearFlags=clear;camera.backgroundColor=color;RenderTexture.active=oldActive;
        }
        File.WriteAllLines(folder+"diagnostic_renderers.txt",lines);
        void ReadLayer(string name){camera.Render();RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,request.width,request.height),0,0);pixels.Apply(false);File.WriteAllBytes(folder+name,pixels.EncodeToPNG());}
    }
    public void Finish()
    {
        if(!active)return;active=false;normal.Dispose();normal=null;
        var hashes=new List<string>();using(var sha=SHA256.Create())foreach(var path in Directory.GetFiles(Application.dataPath+"/Scripts/Presentation","P02*.cs"))hashes.Add(Path.GetFileName(path)+" "+BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-",""));
        File.WriteAllText(folder+"native_input_report.json",JsonUtility.ToJson(new Report{timestamp=DateTime.Now.ToString("O"),label=request.label,scope=request.scope+"; single master recording, preview at 0.25x for slow review; world camera video does not certify overlay UI; label screenshots in isolated replay supply UI evidence",movie=folder+"native_gameplay.mp4",slowMovie="",codeHashes=hashes.ToArray(),errors=errors.ToArray(),frames=frames.ToArray()},true));
        frames.Clear();target.Release();Destroy(pixels);Destroy(target);pixels=null;target=null;Application.runInBackground=background;Destroy(gameObject);
    }
    void OnDestroy(){if(active)Finish();}
}
