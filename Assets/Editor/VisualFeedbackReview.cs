using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.U2D.Animation;
using UnityEngine.SceneManagement;

// Explicit, isolated artwork review. No startup hook, saved scenes, input
// replay, progression flags, inventory grants, or desktop publication.
public static class VisualFeedbackReview
{
    public const string Work = "D:/游戏设计/Codex工作区/work/2026-10-15-VisualFeedback/";
    static Camera camera;
    static readonly MethodInfo deform = typeof(SpriteSkin).GetMethod("DeformForPreviewUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
    static P02MotionLibrary.Entry[] entries;
    static int entryIndex,poseIndex,waitFrames;
    static P02CharacterRig liveRig;
    const string SessionKey="VisualFeedbackReview.Active";
    static string liveFolder;
    static string[] poses={"idle","walk","run"};
    [Serializable] class Part { public string joint,path,name; public Vector2 pivot; public Rect rect; public float paintedHeight,angle; public int order; public bool calibrated,waist; }
    [Serializable] class Skin { public string actor,direction,path; public Part[] parts; }
    [Serializable] class Inventory { public Skin[] skins; }

    public static void Before() { BeforeLive(); }
    public static void After() { AfterLive(); }
    public static void BeforeLive() { BeginLive("Before"); }
    public static void AfterLive() { BeginLive("After"); }
    public static void ApplyAndAfter() { P02VisualAssetRules.ApplyInstalled(); BeginLive("After"); }
    [InitializeOnLoadMethod] static void ResumeLive()
    {
        if(!string.IsNullOrEmpty(SessionState.GetString(SessionKey,"")))EditorApplication.update+=LiveTick;
    }
    static void BeginLive(string revision)
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode is required.");
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        SessionState.SetString(SessionKey,revision);EditorApplication.update-=LiveTick;EditorApplication.update+=LiveTick;
        EditorApplication.EnterPlaymode();
    }
    static void LiveTick()
    {
        string revision=SessionState.GetString(SessionKey,"");
        if(string.IsNullOrEmpty(revision)){EditorApplication.update-=LiveTick;return;}
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling){EditorApplication.QueuePlayerLoopUpdate();return;}
        try {
            if(entries==null){
                entries=P02MotionLibrary.Current.entries;entryIndex=poseIndex=waitFrames=0;
                liveFolder=Work+revision+"/";Directory.CreateDirectory(liveFolder);
                camera=new GameObject("ReviewCamera").AddComponent<Camera>();camera.tag="MainCamera";
                camera.orthographic=true;camera.orthographicSize=1.08f;camera.transform.position=new Vector3(0,.91f,-10);
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.26f,.28f,.30f);
            }
            if(!liveRig){
                var e=entries[entryIndex];var actor=new GameObject(e.actor+"_"+e.direction);
                liveRig=actor.AddComponent<P02CharacterRig>();liveRig.definition=e.rig;liveRig.targetHeight=e.rig.targetHeight;
                liveRig.sampleMode=true;liveRig.samplePaused=true;liveRig.Build();SetLivePose();waitFrames=0;
            }
            if(++waitFrames<6){EditorApplication.QueuePlayerLoopUpdate();return;}
            var entry=entries[entryIndex];
            string label=poseIndex<3?entry.actor+"_"+entry.direction+"_"+poses[poseIndex]:"opening_"+(poseIndex-3).ToString("D2");
            RenderCamera(liveFolder+label+".png",poseIndex<3?640:800,poseIndex<3?800:700);
            poseIndex++;waitFrames=0;
            bool opening=entry.actor=="adult_default"&&entry.direction=="right";
            if(poseIndex<(opening?14:3))SetLivePose();
            else {liveRig.gameObject.SetActive(false);UnityEngine.Object.Destroy(liveRig.gameObject);liveRig=null;entryIndex++;poseIndex=0;}
            if(entryIndex>=entries.Length){
                File.WriteAllText(liveFolder+"complete.txt","Actual Play Mode SpriteSkin updates and camera output in an isolated empty scene. Fixed poses are separate from continuous motion and game-context checks.");
                SessionState.EraseString(SessionKey);EditorApplication.update-=LiveTick;EditorApplication.isPlaying=false;EditorApplication.Exit(0);
            }
            EditorApplication.QueuePlayerLoopUpdate();
        } catch(Exception ex){File.WriteAllText(Work+"review-error.txt",ex.ToString());SessionState.EraseString(SessionKey);EditorApplication.Exit(1);}
    }
    static void SetLivePose()
    {
        liveRig.OpeningRise=-1;liveRig.SetDiagnosticPose(.35f,poseIndex>0&&poseIndex<3,poseIndex==2);
        if(poseIndex>=3){liveRig.SetDiagnosticPose(0,false,false);liveRig.OpeningRise=(poseIndex-3)/10f;liveRig.ApplyPose(0);}
    }
    static void Capture(string revision)
    {
        if (EditorApplication.isPlaying) throw new Exception("Edit mode is required.");
        string folder = Work + revision + "/"; Directory.CreateDirectory(folder);
        var previous = EditorSceneManager.GetSceneManagerSetup();
        try
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            camera = new GameObject("ReviewCamera").AddComponent<Camera>(); camera.tag="MainCamera";
            camera.orthographic=true; camera.orthographicSize=1.08f;
            camera.transform.position=new Vector3(0,.91f,-10); camera.clearFlags=CameraClearFlags.SolidColor;
            camera.backgroundColor=new Color(.26f,.28f,.30f);
            var library=P02MotionLibrary.Current;
            File.WriteAllText(folder+"skins.json",JsonUtility.ToJson(new Inventory { skins=library.entries.Select(e=>new Skin {
                actor=e.actor,direction=e.direction,path=AssetDatabase.GetAssetPath(e.rig),parts=e.rig.parts.Select(p=>new Part {
                    joint=p.joint,path=AssetDatabase.GetAssetPath(p.sprite),name=p.sprite.name,pivot=p.sprite.pivot,rect=p.sprite.rect,
                    paintedHeight=p.paintedHeight,angle=p.artworkAngle,order=p.sortingOffset,calibrated=p.calibratedLimb,waist=p.stabilizedWaist
                }).ToArray() }).ToArray() },true));
            foreach(var e in library.entries)
            {
                var actor=new GameObject(e.actor+"_"+e.direction);
                try {
                    var rig=actor.AddComponent<P02CharacterRig>();rig.definition=e.rig;rig.targetHeight=e.rig.targetHeight;rig.sampleMode=true;rig.samplePaused=true;rig.Build();
                    foreach(string pose in new[]{"idle","walk","run"}) {
                        rig.SetDiagnosticPose(.35f,pose!="idle",pose=="run");
                        Render(rig,folder+e.actor+"_"+e.direction+"_"+pose+".png",640,800);
                    }
                    if(e.actor=="adult_default"&&e.direction=="right")for(int n=0;n<=10;n++) {
                        rig.SetDiagnosticPose(0,false,false);rig.OpeningRise=n/10f;rig.ApplyPose(0);
                        Render(rig,folder+"opening_"+n.ToString("D2")+".png",800,700);
                    }
                } finally { UnityEngine.Object.DestroyImmediate(actor); }
            }
            // Native source sprite crops; source textures and import settings are read-only.
            if(revision=="Before")foreach(var e in library.entries.Where(e=>(e.actor=="adult_default"||e.actor=="qt_adult")&&(e.direction=="left"||e.direction=="right")))
                foreach(var p in e.rig.parts) Export(p.sprite,folder+e.actor+"_"+e.direction+"_source_"+p.joint+".png");
        }
        finally { EditorSceneManager.RestoreSceneManagerSetup(previous); }
        File.WriteAllText(folder+"complete.txt","Native SpriteSkin deformation and actual Unity camera renders. Fixed poses are separate from continuous playback and game-context checks.");
    }
    public static void Render(P02CharacterRig rig,string path,int width,int height)
    {
        foreach(var skin in rig.GetComponentsInChildren<SpriteSkin>())deform.Invoke(skin,null);
        RenderCamera(path,width,height);
    }
    static void RenderCamera(string path,int width,int height)
    {
        var rt=RenderTexture.GetTemporary(width,height,24);var old=RenderTexture.active;var previous=camera.targetTexture;
        var image=new Texture2D(width,height,TextureFormat.RGB24,false);
        try {camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
        finally {camera.targetTexture=previous;RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);}
    }
    static void Export(Sprite sprite,string path)
    {
        var texture=sprite.texture;var rt=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGB32);
        var old=RenderTexture.active;var image=new Texture2D((int)sprite.rect.width,(int)sprite.rect.height,TextureFormat.RGBA32,false);
        try {Graphics.Blit(texture,rt);RenderTexture.active=rt;image.ReadPixels(sprite.rect,0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
        finally {RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);UnityEngine.Object.DestroyImmediate(image);}
    }
}
