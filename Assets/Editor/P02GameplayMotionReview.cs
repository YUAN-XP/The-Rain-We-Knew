using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Media;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

/// <summary>Editor-only, opt-in attachment with explicit temporary speed override and complete cleanup.</summary>
public static class P02GameplayMotionReview
{
    const string Work="D:/游戏设计/Codex工作区/work/2026-10-02-P02/";
    public static void Begin()
    {
        if(!EditorApplication.isPlaying||!PersistentRuntime.Instance||SceneManager.GetActiveScene().name!="Scene01_Rain")
            throw new InvalidOperationException("Review starts at the normal Scene01 bootstrap in Play Mode.");
        if(UnityEngine.Object.FindAnyObjectByType<P02GameplayMotionProbe>())throw new InvalidOperationException("Review already active");
        var controls=PersistentRuntime.Instance.GetComponent<GameControlManager>();var player=controls.ActivePlayer;
        if(!player)throw new InvalidOperationException("No active player");
        var body=player.GetComponent<SpriteRenderer>();if(!body||!body.sprite)throw new InvalidOperationException("Original physical sprite/ground reference required");
        var before=CaptureLiveState();
        var originalRenderers=player.GetComponentsInChildren<SpriteRenderer>(true);
        var flags=originalRenderers.Select(r=>r.forceRenderingOff).ToArray();
        var artwork=player.GetComponent<DemoActorArtwork>();bool artworkEnabled=artwork&&artwork.enabled;
        float oldSpeed=player.WalkSpeed;
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/P02Motion/P02_XP_Default_Right.prefab");
        if(!prefab)throw new InvalidOperationException("Candidate prefab missing");
        var actor=UnityEngine.Object.Instantiate(prefab);actor.name="P02 temporary gameplay candidate";
        var rig=actor.GetComponent<P02CharacterRig>();rig.sampleMode=false;rig.motionSource=player.transform;
        rig.targetHeight=1.8f;rig.transform.position=body.transform.TransformPoint(new Vector3(0,body.sprite.bounds.min.y,0));rig.ResetTravel();
        var group=actor.GetComponent<SortingGroup>();group.sortingLayerID=body.sortingLayerID;group.sortingOrder=body.sortingOrder+2;
        if(artwork)artwork.enabled=false;
        foreach(var r in originalRenderers)r.forceRenderingOff=true;
        var serialized=new SerializedObject(player);serialized.FindProperty("moveSpeed").floatValue=rig.definition.sampleWalkSpeed;serialized.ApplyModifiedPropertiesWithoutUndo();
        var probe=new GameObject("P02 opt-in gameplay motion review").AddComponent<P02GameplayMotionProbe>();
        probe.player=player;probe.originalBody=body;probe.rig=rig;probe.gameCamera=Camera.main;
        probe.originalWalkSpeed=oldSpeed;probe.reviewWalkSpeed=player.WalkSpeed;
        probe.directory=Work+"gameplay_runs/"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+"/";
        Directory.CreateDirectory(probe.directory);
        File.WriteAllText(probe.directory+"before_state.json",JsonUtility.ToJson(before,true));
        File.WriteAllText(Work+"active_gameplay_review.txt",probe.directory);
        File.Copy(Work+"sample_source_manifest.json",probe.directory+"source_manifest.json");
        probe.restore=()=>{
            if(player){var s=new SerializedObject(player);s.FindProperty("moveSpeed").floatValue=oldSpeed;s.ApplyModifiedPropertiesWithoutUndo();}
            for(int i=0;i<originalRenderers.Length;i++)if(originalRenderers[i])originalRenderers[i].forceRenderingOff=flags[i];
            if(artwork)artwork.enabled=artworkEnabled;
        };
        var encoder=new MediaEncoder(probe.directory+"gameplay_motion.mp4",new VideoTrackAttributes{width=1920,height=1080,frameRate=new MediaRational(60),includeAlpha=false,bitRateMode=VideoBitrateMode.High});
        var slow=new MediaEncoder(probe.directory+"gameplay_motion_slow.mp4",new VideoTrackAttributes{width=1920,height=1080,frameRate=new MediaRational(60),includeAlpha=false,bitRateMode=VideoBitrateMode.High});
        probe.writeMovie=(texture,seconds)=>{bool a=encoder.AddFrame(texture,new MediaTime((long)(seconds*1000000),1000000,1));bool b=slow.AddFrame(texture,new MediaTime((long)(seconds*4000000),1000000,1));return a&&b;};
        probe.closeMovie=()=>{encoder.Dispose();slow.Dispose();};probe.Begin();
    }
    public static void End()
    {
        var probe=UnityEngine.Object.FindAnyObjectByType<P02GameplayMotionProbe>();
        if(probe)UnityEngine.Object.Destroy(probe.gameObject);
    }
    public static void BeginAndReplay()
    {
        if(GameplayInput.Replay==null)throw new InvalidOperationException("Normal input replay must be enabled first.");
        if(File.Exists(P01InputReplay.Folder+"/queue.json"))throw new InvalidOperationException("Previous input batch still pending.");
        Begin();
        // Record and enqueue in the same editor operation so no external
        // interaction delay can consume the movie's validation window.
        File.Copy(Work+"gameplay_review_queue.json",P01InputReplay.Folder+"/queue.json");
    }
    [Serializable] public sealed class CleanupState
    {
        public string timestamp,scene;
        public float walkSpeed;
        public bool artworkEnabled,replayActive,paused;
        public int candidateRigs,reviewProbes;
        public string[] rendererStates;
    }
    static CleanupState CaptureLiveState()
    {
        if(!EditorApplication.isPlaying||!PersistentRuntime.Instance)
            throw new InvalidOperationException("Cleanup verification requires the same active game.");
        var controls=PersistentRuntime.Instance.GetComponent<GameControlManager>();
        var player=controls.ActivePlayer;
        var artwork=player.GetComponent<DemoActorArtwork>();
        var result=new CleanupState{timestamp=DateTime.Now.ToString("O"),scene=SceneManager.GetActiveScene().name,
            walkSpeed=player.WalkSpeed,artworkEnabled=artwork&&artwork.enabled,replayActive=GameplayInput.Replay!=null,
            paused=GamePause.IsPaused,candidateRigs=UnityEngine.Object.FindObjectsByType<P02CharacterRig>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length,
            reviewProbes=UnityEngine.Object.FindObjectsByType<P02GameplayMotionProbe>(FindObjectsInactive.Include,FindObjectsSortMode.None).Length,
            rendererStates=player.GetComponentsInChildren<SpriteRenderer>(true).Select(r=>r.name+" enabled="+r.enabled+" forceOff="+r.forceRenderingOff).ToArray()};
        return result;
    }
    public static void VerifyCleanup()
    {
        var result=CaptureLiveState();
        string directory=File.ReadAllText(Work+"active_gameplay_review.txt").Trim();
        File.WriteAllText(directory+"cleanup_state.json",JsonUtility.ToJson(result,true));
        string beforePath=directory+"before_state.json";
        if(!File.Exists(beforePath))return; // Older run: state is evidence, not an exact before/after certification.
        var before=JsonUtility.FromJson<CleanupState>(File.ReadAllText(beforePath));
        if(Mathf.Abs(result.walkSpeed-before.walkSpeed)>.00001f||result.artworkEnabled!=before.artworkEnabled
            ||!result.rendererStates.SequenceEqual(before.rendererStates)||result.replayActive||result.paused
            ||result.candidateRigs!=before.candidateRigs||result.reviewProbes!=before.reviewProbes)
            throw new InvalidOperationException("Gameplay review cleanup differs from its recorded pre-review state.");
    }
}

