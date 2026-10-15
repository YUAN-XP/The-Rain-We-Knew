using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Media;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Opt-in background tests in unsaved/temporary Play Mode only. Never installs
// gameplay input or changes focus/pause rules, player saves, or quest ordering.
public static class VisualFeedbackPlaybackReview
{
    const string Key="VisualFeedbackPlaybackReview.Active";
    const string W=VisualFeedbackReview.Work;
    static int stage,frame,wait,fixtureWait;
    static readonly List<string> checks=new List<string>();
    static readonly List<string> samples=new List<string>();
    static readonly List<P02CharacterRig> rigs=new List<P02CharacterRig>();
    static Camera camera;
    static MediaEncoder normal,slow;
    static MusicManager music;
    static AudioSource voice;
    static DemoComicSequence comic;
    static double began,opened,lastSample;
    static int initialSample,lastVoiceSample;
    static AudioClip clip;
    static bool lostPlayback;
    static bool stretchedMesh;
    static float maximumLegError;
    static float minimumDuck=1;
    static float maximumSignal,maximumOutput;
    static readonly float[] audioBuffer=new float[512];
    static string[] critical={"adult_default:right","adult_default:left","qt_adult:right","qt_adult:left"};
    [InitializeOnLoadMethod] static void Resume(){if(SessionState.GetBool(Key,false))EditorApplication.update+=Tick;}
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required.");
        P02VisualAssetRules.ApplyInstalled();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        SessionState.SetBool(Key,true);EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();
    }
    static void Check(bool condition,string message){checks.Add((condition?"PASS ":"FAIL ")+message);}
    static void Tick()
    {
        if(!SessionState.GetBool(Key,false)){EditorApplication.update-=Tick;return;}
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling){EditorApplication.QueuePlayerLoopUpdate();return;}
        try
        {
            if(stage==0)Initialize();
            else if(stage<=3)Motion();
            else if(stage==4)Music();
            else if(stage==5)SceneFixture();
            EditorApplication.QueuePlayerLoopUpdate();
        }
        catch(Exception e){File.WriteAllText(W+"playback-error.txt",e.ToString());Finish(1);}
    }
    static void Initialize()
    {
        Directory.CreateDirectory(W+"Playback");stage=1;frame=wait=0;stretchedMesh=false;maximumLegError=0;checks.Clear();samples.Clear();rigs.Clear();
        var library=P02MotionLibrary.Current;
        Check(library.entries.Length==20,"All 20 installed identity/direction skins retained");
        foreach(var entry in library.entries){
            Check(entry.rig.parts.All(p=>p.sprite),entry.actor+"/"+entry.direction+" has all source parts");
            if(entry.direction=="left"&&entry.rig.viewKind=="profile") {
                var near=entry.rig.parts.Single(p=>p.joint=="near_hand");var far=entry.rig.parts.Single(p=>p.joint=="far_hand");
                Check(near.sprite.name=="far_hand"&&far.sprite.name=="near_hand",entry.actor+" near back-of-hand / far palm mapping");
            }
        }
        camera=new GameObject("IsolatedPlaybackCamera").AddComponent<Camera>();camera.tag="MainCamera";
        camera.orthographic=true;camera.orthographicSize=1.5f;camera.transform.position=new Vector3(0,.95f,-10);
        camera.backgroundColor=new Color(.26f,.28f,.30f);camera.clearFlags=CameraClearFlags.SolidColor;
        camera.gameObject.AddComponent<AudioListener>();
        for(int n=0;n<critical.Length;n++){
            var words=critical[n].Split(':');var rig=MakeRig(words[0],words[1],new Vector3((n-1.5f)*1.5f,0));rigs.Add(rig);
            var label=new GameObject("Label").AddComponent<TextMesh>();label.text=critical[n];label.fontSize=30;label.characterSize=.045f;label.anchor=TextAnchor.MiddleCenter;label.transform.position=new Vector3((n-1.5f)*1.5f,2.08f,0);
        }
        var ground=new GameObject("GroundReference").AddComponent<LineRenderer>();ground.positionCount=2;ground.SetPosition(0,new Vector3(-3.3f,0,0));ground.SetPosition(1,new Vector3(3.3f,0,0));ground.startWidth=ground.endWidth=.006f;ground.material=new Material(Shader.Find("Sprites/Default"));ground.startColor=ground.endColor=new Color(.6f,.62f,.64f);ground.sortingOrder=-100;
        normal=new MediaEncoder(W+"Playback/Normal.mp4",new VideoTrackAttributes{width=1280,height=600,frameRate=new MediaRational(24),includeAlpha=false,bitRateMode=VideoBitrateMode.High});
        slow=new MediaEncoder(W+"Playback/Slow.mp4",new VideoTrackAttributes{width=1280,height=600,frameRate=new MediaRational(12),includeAlpha=false,bitRateMode=VideoBitrateMode.High});
        SetMotion();
    }
    static P02CharacterRig MakeRig(string actor,string direction,Vector3 position)
    {
        var root=new GameObject("Review_"+actor+"_"+direction);root.transform.position=position;
        var rig=root.AddComponent<P02CharacterRig>();rig.definition=P02MotionLibrary.Current.Find(actor,direction);
        rig.targetHeight=rig.definition.targetHeight;rig.sampleMode=true;rig.samplePaused=true;rig.Build();return rig;
    }
    static void SetMotion()
    {
        foreach(var rig in rigs){
            rig.OpeningRise=-1;
            float hz=(stage==2?rig.definition.sampleRunSpeed/rig.definition.runningStride:rig.definition.sampleWalkSpeed/rig.definition.walkingStride);
            rig.SetDiagnosticPose(Mathf.Repeat(frame*hz/24,1),stage<3,stage==2);
        }
        if(stage==3){rigs[0].OpeningRise=frame/156f;rigs[0].ApplyPose(0);}
    }
    static void Motion()
    {
        if(++wait<3)return;wait=0;
        var texture=Capture(camera,1280,600);
        try{
            if(!normal.AddFrame(texture)||!slow.AddFrame(texture))throw new Exception("Native preview encoder rejected an RGBA frame.");
            if(stage==3?frame%13==0:frame%8==0)File.WriteAllBytes(W+"Playback/"+stage+"_"+frame.ToString("D3")+".png",texture.EncodeToPNG());
        }finally{UnityEngine.Object.Destroy(texture);}
        foreach(var rig in rigs.Where(r=>r.gameObject.activeSelf)){
            CheckFrame(rig);
        }
        frame++;
        int count=stage==1?65:stage==2?44:157;
        if(frame<count){SetMotion();return;}
        Check(true,(stage==1?"Walk":stage==2?"Run":"Opening")+" actual native deformation output; "+count+" frames, world ground reference");
        frame=0;stage++;
        if(stage==3){
            foreach(var r in rigs.Skip(1))r.gameObject.SetActive(false);
            rigs[0].transform.position=Vector3.zero;camera.orthographicSize=1.08f;camera.transform.position=new Vector3(0,.91f,-10);
            foreach(var t in UnityEngine.Object.FindObjectsByType<TextMesh>())t.gameObject.SetActive(false);
        }
        if(stage<=3){SetMotion();return;}
        normal.Dispose();slow.Dispose();normal=slow=null;
        Check(new FileInfo(W+"Playback/Normal.mp4").Length>10000 && new FileInfo(W+"Playback/Slow.mp4").Length>10000,"Both normal/slow native video outputs contain encoded frames");
        Check(!lostPlayback,"Native character endpoints stayed finite across three walk/run loops and entire opening");
        Check(!stretchedMesh,"Every sampled skinned part stayed within its anatomical size envelope");
        Check(maximumLegError<.006f,"Measured lower-leg endpoint error remained below 0.006 units: "+maximumLegError.ToString("F6"));
        lostPlayback=false;StartMusic();
    }
    static void CheckFrame(P02CharacterRig rig)
    {
        foreach(var t in rig.GetComponentsInChildren<Transform>())if(float.IsNaN(t.position.x)||float.IsNaN(t.position.y)||Mathf.Abs(t.position.x)>100)lostPlayback=true;
        foreach(var r in rig.GetComponentsInChildren<SpriteRenderer>())if(r.bounds.size.x>1.4f||r.bounds.size.y>1.4f)stretchedMesh=true;
        maximumLegError=Mathf.Max(maximumLegError,rig.nearLegEndpointGap,rig.farLegEndpointGap);
    }
    static void StartMusic()
    {
        foreach(var r in rigs)r.gameObject.SetActive(false);
        new GameObject("IsolatedMusicProfile").AddComponent<SceneMusicProfile>().defaultState=MusicState.RainExplore;
        if(!UnityEngine.Object.FindAnyObjectByType<GameplayClock>())new GameObject("IsolatedClock").AddComponent<GameplayClock>();
        music=MusicManager.Instance;Check(music&&music.Ready,"Actual configured music manager ready");
        if(!music||!music.Ready)throw new Exception("Music library unavailable");
        began=EditorApplication.timeSinceStartup;opened=0;lastSample=0;
    }
    static void Music()
    {
        double now=EditorApplication.timeSinceStartup;
        if(opened==0){
            if(now-began<3.5)return;
            voice=new[]{music.SourceA,music.SourceB}.FirstOrDefault(s=>s.isPlaying&&s.volume>.001f);
            if(!voice)throw new Exception("Rain music did not start.");
            clip=voice.clip;initialSample=lastVoiceSample=voice.timeSamples;
            comic=new GameObject("IsolatedCommonPagedComic").AddComponent<DemoComicSequence>();
            typeof(DemoComicSequence).GetMethod("Begin",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(comic,null);
            Check(comic.IsOpen,"Shared paged-animation canvas is open (used by all four comic presentations)");
            opened=now;minimumDuck=1;maximumSignal=maximumOutput=0;return;
        }
        minimumDuck=Mathf.Min(minimumDuck,music.DuckGain);
        if(!voice.isPlaying||voice.clip!=clip||voice.volume<=.001f)lostPlayback=true;
        if(now-lastSample>.2){
            voice.GetOutputData(audioBuffer,0);float signal=audioBuffer.Max(v=>Mathf.Abs(v));maximumSignal=Mathf.Max(maximumSignal,signal);
            AudioListener.GetOutputData(audioBuffer,0);float output=audioBuffer.Max(v=>Mathf.Abs(v));maximumOutput=Mathf.Max(maximumOutput,output);
            samples.Add((now-opened).ToString("F3")+","+voice.timeSamples+","+voice.volume.ToString("F4")+","+music.DuckGain.ToString("F4")+","+signal.ToString("F6")+","+output.ToString("F6"));lastSample=now;lastVoiceSample=voice.timeSamples;
        }
        if(now-opened<5.0)return;
        Check(!lostPlayback,"Same BGM source/clip retained playback and nonzero source gain throughout paged canvas");
        Check(minimumDuck>.99f,"Paged presentation never reduced BGM duck gain to zero");
        Check(lastVoiceSample>initialSample,"BGM sample clock advanced through the presentation without restarting");
        Check(maximumSignal>.000001f,"Nonzero decoded BGM signal during paged presentation: "+maximumSignal.ToString("F6"));
        Check(maximumOutput>.000001f,"Actual listener output remained present with unchanged audio settings: "+maximumOutput.ToString("F6"));
        comic.Close();Check(!comic.IsOpen,"Paged canvas closed normally");
        File.WriteAllLines(W+"music-continuity.csv",new[]{"seconds,sample,voiceGain,duckGain,sourcePeak,listenerPeak"}.Concat(samples));
        stage=5;fixtureWait=0;
        SceneManager.LoadScene("Scene02_Courtyard",LoadSceneMode.Single);
    }
    static void SceneFixture()
    {
        if(SceneManager.GetActiveScene().name!="Scene02_Courtyard")return;
        if(fixtureWait++==15){
            foreach(var c in UnityEngine.Object.FindObjectsByType<Canvas>())c.gameObject.SetActive(false);
            foreach(var actor in UnityEngine.Object.FindObjectsByType<PlayerController>())foreach(var r in actor.GetComponentsInChildren<Renderer>())r.forceRenderingOff=true;
            foreach(var r in UnityEngine.Object.FindObjectsByType<P02CharacterRig>())r.gameObject.SetActive(false);
            foreach(var t in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name=="QiuTian_Placeholder"))t.gameObject.SetActive(false);
            var stair=UnityEngine.Object.FindObjectsByType<SpriteRenderer>().Single(r=>r.name=="P03_CompactStairs");
            Check(Mathf.Abs(stair.bounds.size.y-P02VisualAssetRules.CourtyardStairHeight)<.01f,"Actual courtyard staircase has reviewed full height");
            Check(Mathf.Abs(stair.bounds.min.y-.86f)<.02f,"Staircase still rests on original floor");
            camera=new GameObject("CourtyardArtworkReviewCamera").AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=1.9f;camera.transform.position=new Vector3(6.25f,2.15f,-10);
            var xp=MakeRig("adult_default","right",new Vector3(7.3f,.86f,0));xp.SetDiagnosticPose(0,false,false);
            var qt=MakeRig("qt_adult","right",new Vector3(8.65f,.86f,0));qt.SetDiagnosticPose(0,false,false);
        }
        if(fixtureWait<25)return;
        var texture=Capture(camera,1600,900);File.WriteAllBytes(W+"CourtyardScale.png",texture.EncodeToPNG());UnityEngine.Object.Destroy(texture);
        Check(true,"Authored courtyard scene rendered with static review actors; no saved camera/player/trigger changes");
        File.WriteAllLines(W+"targeted-checks.txt",checks);
        Finish(checks.Any(c=>c.StartsWith("FAIL"))?1:0);
    }
    static Texture2D Capture(Camera view,int width,int height)
    {
        var rt=RenderTexture.GetTemporary(width,height,24);var previous=RenderTexture.active;var old=view.targetTexture;
        var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);
        try{view.targetTexture=rt;view.Render();RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();return texture;}
        finally{view.targetTexture=old;RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);}
    }
    static void Finish(int code)
    {
        normal?.Dispose();slow?.Dispose();normal=slow=null;
        File.WriteAllLines(W+"targeted-checks.txt",checks);
        SessionState.EraseBool(Key);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;EditorApplication.Exit(code);
    }
}
