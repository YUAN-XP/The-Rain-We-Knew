using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Editor diagnostics only. Never added to scenes, prefabs or release builds.
public static class P02FinalRegression
{
    const string Work=P02ProductionInputReview.Work;
    public static void CaptureOpening()
    {
        var rt=PersistentRuntime.Instance;var saves=rt.GetComponent<GameSaveService>();
        if(GameplayInput.Replay==null)throw new InvalidOperationException("Isolated replay required");
        var snapshot=saves.Capture(4);
        File.WriteAllText(Work+"opening_before.json",JsonUtility.ToJson(snapshot,true));
        ReportOpening("before");
    }
    public static void VerifyOpening()=>ReportOpening("after");
    public static void VerifyStageCleanup()
    {
        var rt=PersistentRuntime.Instance;var controls=rt.GetComponent<GameControlManager>();
        var saves=rt.GetComponent<GameSaveService>();var story=rt.GetComponent<StoryStateManager>();
        var tool=rt.GetComponent<P02ToolMotion>();var cam=Camera.main;
        var cameraTool=rt.GetComponent<CameraModeController>();var lamp=rt.GetComponent<OilLampController>();
        var actors=UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
        var testObjects=actors.Where(x=>x&&(x.GetType().Name=="P01ReplayDriver"||x.GetType().Name=="P02ProductionInputDriver"||x.GetType().Name=="P02RiverKeyDriver"||x.GetType().Name=="P02StandaloneCheck")).Select(x=>x.GetType().Name).ToArray();
        bool Owns(Component c){if(!c)return false;var item=c.GetType().GetField("requiredItem",BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(c) as ItemData;return item&&rt.GetComponent<InventoryManager>().HasItem(item);}
        var result=new Cleanup{scene=SceneManager.GetActiveScene().name,replayActive=GameplayInput.Replay!=null,
            testObjects=testObjects,saveDirectory=saves.Store.DirectoryPath,
            expectedSaveDirectory=Path.GetFullPath(Path.Combine(Application.persistentDataPath,"Saves")),flags=story.CaptureSave().flags,
            ownsCamera=Owns(cameraTool),ownsLamp=Owns(lamp),cameraRaised=cameraTool&&cameraTool.IsRaised,lampRaised=lamp&&lamp.IsRaised,
            tool=tool?tool.Current.ToString():"None",cameraFollowEnabled=cam&&cam.GetComponent<CameraFollow>()&&cam.GetComponent<CameraFollow>().enabled,
            actualWalkSpeed=controls.ActivePlayer.WalkSpeed,paused=GamePause.IsPaused,originalPauseLock=controls.IsLockedBy(ControlLockReason.GlobalPause)};
        // Original GlobalPause deliberately disables CameraFollow through GameControlManager.
        // A minimized editor must retain that pause; this probe never resumes or bypasses it.
        result.passed=result.scene=="Scene01_Rain"&&!result.replayActive&&testObjects.Length==0&&result.saveDirectory==result.expectedSaveDirectory&&result.flags.Length==0&&!result.ownsCamera&&!result.ownsLamp&&!result.cameraRaised&&!result.lampRaised&&result.tool=="None"&&(result.cameraFollowEnabled||(result.paused&&result.originalPauseLock))&&Mathf.Abs(result.actualWalkSpeed-2.2f)<.001f;
        File.WriteAllText(Work+"stage_close_cleanup.json",JsonUtility.ToJson(result,true));
        ScreenCapture.CaptureScreenshot(Work+"stage_close_normal_opening.png");
        if(!result.passed)throw new InvalidOperationException("Stage cleanup differs from fresh authored opening");
    }
    [Serializable] class Cleanup{public string scene,saveDirectory,expectedSaveDirectory,tool;public string[] testObjects,flags;public bool replayActive,ownsCamera,ownsLamp,cameraRaised,lampRaised,cameraFollowEnabled,paused,originalPauseLock,passed;public float actualWalkSpeed;}
    static void ReportOpening(string step)
    {
        var rt=PersistentRuntime.Instance;var controls=rt.GetComponent<GameControlManager>();
        var cameraTool=rt.GetComponent<CameraModeController>();var lamp=rt.GetComponent<OilLampController>();
        var inventory=rt.GetComponent<InventoryManager>();var motion=rt.GetComponent<P02ToolMotion>();
        bool Owns(Component c){if(!c)return false;var item=c.GetType().GetField("requiredItem",BindingFlags.NonPublic|BindingFlags.Instance)?.GetValue(c) as ItemData;return item&&inventory.HasItem(item);}
        var camera=Camera.main;var follow=camera?camera.GetComponent<CameraFollow>():null;
        var result=new Opening{step=step,scene=SceneManager.GetActiveScene().name,
            ownsCamera=Owns(cameraTool),ownsLamp=Owns(lamp),cameraRaised=cameraTool&&cameraTool.IsRaised,lampRaised=lamp&&lamp.IsRaised,
            tool=motion?motion.Current.ToString():"none",canBeginCamera=cameraTool&&cameraTool.CanBeginPresentation,
            cameraFollowEnabled=follow&&follow.enabled,player=controls.ActivePlayer.transform.position,
            camera=camera?camera.transform.position:Vector3.zero,flags=rt.GetComponent<StoryStateManager>().CaptureSave().flags};
        File.WriteAllText(Work+"opening_"+step+"_check.json",JsonUtility.ToJson(result,true));
        if(step=="after"&&(result.ownsCamera||result.ownsLamp||result.cameraRaised||result.lampRaised||result.tool!="None"))throw new InvalidOperationException("Opening tool restriction changed");
        ScreenCapture.CaptureScreenshot(Work+"opening_"+step+"_full_ui.png");
        if(camera)P02MotionSampleProbe.Render(Work+"opening_"+step+"_world.png",1920,1080);
    }
    [Serializable] class Opening{public string step,scene,tool;public bool ownsCamera,ownsLamp,cameraRaised,lampRaised,canBeginCamera,cameraFollowEnabled;public Vector3 player,camera;public string[] flags;}
}
