using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.SceneManagement;

/// <summary>Builds explicit skins from measured native sprite references, never edits source pixels.</summary>
public static class P02ProductionFactory
{
    static P02MotionLibrary.Entry[] batch;
    static int batchIndex;
    static string batchCurrent, batchDirectory;
    static GameObject batchActor;
    static readonly List<string> batchReports=new List<string>();
    const string Work="D:/游戏设计/Codex工作区/work/2026-10-02-P02/";
    const string Root="Assets/Art/P02Motion/Production";
    [Serializable] public sealed class Skin
    {
        public string actor, direction, viewKind="profile",nearAnatomicalSide="right";
        public float targetHeight=1.8f, headHeight=.40092f,torsoHeight=.58f;
        public float waistToHip=.115f,thigh=.29f,shin=.405f,upperArm=.28f,forearm=.25f;
        public float shoulderX=-.013f,shoulderY=.455f,neckX=.001f,neckY=.521f;
        public bool projectedPlacement;
        public float farShoulderX,farShoulderY,nearHipX,nearHipY,farHipX,farHipY,nearFootX,nearFootY,farFootX,farFootY;
        public float pelvisIdle=.914f,pelvisMoving=.902f,hipSeparation=.028f,footSeparation=.028f;
        public float walkStride=1.2f,runStride=1.35f,walkSpeed=1.35f;
        public P02RigFactory.Source[] sources;
    }
    [Serializable] public sealed class Manifest { public string revision; public Skin[] skins; }
    public static void Build()=>BuildInternal(false);
    public static void RebuildDefaultLeft()=>BuildInternal(true);
    public static void RebuildChildQuarters()=>BuildInternal(false,true);
    static void BuildInternal(bool defaultLeftOnly,bool quartersOnly=false)
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Saved edit scene required");
        var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Work+"production_skin_manifest.json"));
        var current=AssetDatabase.LoadAssetAtPath<P02MotionLibrary>("Assets/Resources/P02MotionLibrary.asset");
        var entries=(defaultLeftOnly||quartersOnly)&&current?current.entries.ToList():new List<P02MotionLibrary.Entry>();
        var right=AssetDatabase.LoadAssetAtPath<P02RigDefinition>("Assets/Art/P02Motion/XP_Default_Right.asset");
        if(!right)throw new InvalidOperationException("Frozen R13 reference missing");
        if(!manifest.skins.Any(s=>s.actor=="adult_default"&&s.direction=="right"))
            entries.Add(new P02MotionLibrary.Entry{actor="adult_default",direction="right",rig=DefaultProduction(right)});
        foreach(var input in manifest.skins.Where(v=>quartersOnly?v.actor=="child"&&v.projectedPlacement:!defaultLeftOnly||v.actor=="adult_default"&&v.direction=="left"))
        {
            entries.RemoveAll(e=>e.actor==input.actor&&e.direction==input.direction);
            var bindings=new List<P02RigDefinition.P02PartBinding>();
            var allSprites=new Dictionary<string,Sprite>();
            foreach(var src in input.sources){P02RigFactory.Import(src);foreach(var sprite in AssetDatabase.LoadAllAssetsAtPath(src.path).OfType<Sprite>())allSprites.Add(sprite.name,sprite);}
            foreach(var source in input.sources)
            {
                foreach(var p in source.parts.Where(p=>string.IsNullOrEmpty(p.skinDirection)||p.skinDirection==input.direction))
                {
                    if(p.name.EndsWith("_run"))continue;
                    var sprite=allSprites[p.name];
                    var variant=input.sources.SelectMany(src=>src.parts).FirstOrDefault(v=>v.name==p.name+"_run");
                    bindings.Add(new P02RigDefinition.P02PartBinding{joint=p.garment?"shorts":p.name,pelvisGarment=p.garment,sprite=sprite,
                        runSprite=variant!=null?allSprites[variant.name]:null,runPaintedHeight=variant!=null?variant.paintedHeight:0,
                        paintedHeight=p.paintedHeight,artworkAngle=p.artworkAngle,runArtworkAngle=variant!=null?variant.artworkAngle:0,sortingOffset=p.order,bendInfluence=p.bend,
                        articulatedLimb=p.limb,stabilizedWaist=p.name.EndsWith("_leg"),
                        calibratedLimb=p.calibratedLimb,bindUpperLength=p.bindUpperLength,bindLowerLength=p.bindLowerLength,
                        paintedSole=p.name.EndsWith("_shoe")?P02RigFactory.ReadPaintedSole(sprite,p.paintedHeight):null});
                }
            }
            if(bindings.Count!=(input.projectedPlacement?11:10))throw new InvalidOperationException("Unexpected visible parts "+input.actor+"/"+input.direction+": "+bindings.Count);
            var path=Root+"/"+input.actor+"_"+input.direction+".asset";
            var definition=AssetDatabase.LoadAssetAtPath<P02RigDefinition>(path);
            if(!definition){definition=ScriptableObject.CreateInstance<P02RigDefinition>();AssetDatabase.CreateAsset(definition,path);}
            definition.actorKey=input.actor;definition.identity=input.actor;definition.direction=input.direction;definition.viewKind=input.viewKind;
            definition.targetHeight=input.targetHeight;definition.nearAnatomicalSide=input.direction=="left"?"left":input.nearAnatomicalSide;
            definition.referenceHeight=1.8f;definition.parts=bindings.ToArray();
            definition.shoulderOffset=new Vector2(input.shoulderX,input.shoulderY);definition.neckOffset=new Vector2(input.neckX,input.neckY);
            definition.projectedPlacement=input.projectedPlacement;
            definition.farShoulderOffset=new Vector2(input.farShoulderX,input.farShoulderY);
            definition.nearHipOffset=new Vector2(input.nearHipX,input.nearHipY);definition.farHipOffset=new Vector2(input.farHipX,input.farHipY);
            definition.nearFootOffset=new Vector2(input.nearFootX,input.nearFootY);definition.farFootOffset=new Vector2(input.farFootX,input.farFootY);
            definition.idlePelvisHeight=input.pelvisIdle;definition.movingPelvisHeight=input.pelvisMoving;
            definition.hipSeparation=input.hipSeparation;definition.idleFootSeparation=input.footSeparation;
            definition.waistToHip=input.waistToHip;definition.thighLength=input.thigh;definition.shinLength=input.shin;
            definition.upperArmLength=input.upperArm;definition.forearmLength=input.forearm;
            definition.cameraHandHeight=1.56f;
            definition.walkingStride=input.walkStride;definition.runningStride=input.runStride;
            definition.sampleWalkSpeed=input.walkSpeed;definition.sampleRunSpeed=input.walkSpeed*1.7f;
            definition.walkingLean=right.walkingLean;definition.runningLean=right.runningLean;
            definition.walkLift=new AnimationCurve(right.walkLift.keys);definition.runLift=new AnimationCurve(right.runLift.keys);
            definition.walkSwing=new AnimationCurve(right.walkSwing.keys);definition.runSwing=new AnimationCurve(right.runSwing.keys);
            EditorUtility.SetDirty(definition);
            var actor=new GameObject("P02_"+input.actor+"_"+input.direction);
            try{var rig=actor.AddComponent<P02CharacterRig>();rig.definition=definition;rig.targetHeight=input.targetHeight;rig.sampleMode=true;rig.Build();
                PrefabUtility.SaveAsPrefabAsset(actor,Root+"/"+actor.name+".prefab");}
            finally{UnityEngine.Object.DestroyImmediate(actor);}
            entries.Add(new P02MotionLibrary.Entry{actor=input.actor,direction=input.direction,rig=definition});
        }
        const string libraryPath="Assets/Resources/P02MotionLibrary.asset";
        var library=AssetDatabase.LoadAssetAtPath<P02MotionLibrary>(libraryPath);
        if(!library){library=ScriptableObject.CreateInstance<P02MotionLibrary>();AssetDatabase.CreateAsset(library,libraryPath);}
        library.entries=entries.ToArray();EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
        File.WriteAllText(Work+"production_native_import.json",JsonUtility.ToJson(manifest,true));
    }
    static P02RigDefinition DefaultProduction(P02RigDefinition reference)
    {
        string path=Root+"/adult_default_right.asset";
        var definition=AssetDatabase.LoadAssetAtPath<P02RigDefinition>(path);
        if(!definition){definition=UnityEngine.Object.Instantiate(reference);AssetDatabase.CreateAsset(definition,path);}
        definition.actorKey="adult_default";definition.direction="right";definition.viewKind="profile";
        definition.targetHeight=1.8f;definition.nearAnatomicalSide="right";
        definition.sampleRunSpeed=definition.sampleWalkSpeed*1.7f;EditorUtility.SetDirty(definition);return definition;
    }
    public static void AlignDefaultProduction()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit Mode required");
        var library=AssetDatabase.LoadAssetAtPath<P02MotionLibrary>("Assets/Resources/P02MotionLibrary.asset");
        var reference=AssetDatabase.LoadAssetAtPath<P02RigDefinition>("Assets/Art/P02Motion/XP_Default_Right.asset");
        library.entries.First(e=>e.actor=="adult_default"&&e.direction=="right").rig=DefaultProduction(reference);
        EditorUtility.SetDirty(library);AssetDatabase.SaveAssets();
    }
    public static void OpenAssembly()
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Saved edit scene required");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;
        camera.orthographicSize=1.35f;camera.transform.position=new Vector3(0,.85f,-10);camera.backgroundColor=new Color(.28f,.29f,.30f);
        camera.clearFlags=CameraClearFlags.SolidColor;
        var library=AssetDatabase.LoadAssetAtPath<P02MotionLibrary>("Assets/Resources/P02MotionLibrary.asset");
        int n=library.entries.Length;
        for(int i=0;i<n;i++)
        {
            var e=library.entries[i];var actor=new GameObject("P02_"+e.actor+"_"+e.direction);
            actor.transform.position=new Vector3((i-(n-1)*.5f)*.75f,0,0);
            var rig=actor.AddComponent<P02CharacterRig>();rig.definition=e.rig;rig.targetHeight=e.rig.targetHeight;rig.sampleMode=true;rig.sampleSpeed=0;rig.Build();
        }
        string path="Assets/Scenes/Validation/P02AssemblyReview.unity";
        EditorSceneManager.SaveScene(scene,path);
    }
    public static void CaptureAssembly()
    {
        if(!EditorApplication.isPlaying)throw new InvalidOperationException("Play assembly first");
        Directory.CreateDirectory(Work+"production_runs");
        P02MotionSampleProbe.Render(Work+"production_runs/PV01_assembly_standing.png",1920,1080);
    }
    [Serializable] sealed class Selection {public string actor,direction;}
    public static void OpenSelected()
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Saved edit scene required");
        var selected=JsonUtility.FromJson<Selection>(File.ReadAllText(Work+"selected_skin.json"));
        var library=AssetDatabase.LoadAssetAtPath<P02MotionLibrary>("Assets/Resources/P02MotionLibrary.asset");
        var definition=library.Find(selected.actor,selected.direction);if(!definition)throw new InvalidOperationException("Missing selected skin");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;
        camera.orthographicSize=1.55f;camera.transform.position=new Vector3(.1f,.9f,-10);camera.backgroundColor=new Color(.28f,.29f,.30f);camera.clearFlags=CameraClearFlags.SolidColor;
        var actor=new GameObject("P02_"+selected.actor+"_"+selected.direction);
        var rig=actor.AddComponent<P02CharacterRig>();rig.definition=definition;rig.targetHeight=definition.targetHeight;rig.sampleMode=true;rig.Build();
        actor.AddComponent<P02MotionSampleProbe>().runLabel=selected.actor+"_"+selected.direction;
        EditorSceneManager.SaveScene(scene,"Assets/Scenes/Validation/P02SelectedReview.unity");
    }
    public static void RunBatch()
    {
        if(!EditorApplication.isPlaying||!SceneManager.GetActiveScene().name.StartsWith("P02"))throw new InvalidOperationException("Isolated P02 Play scene required");
        if(batch!=null)throw new InvalidOperationException("Batch already running");
        foreach(var rig in UnityEngine.Object.FindObjectsByType<P02CharacterRig>(FindObjectsSortMode.None)){rig.gameObject.SetActive(false);UnityEngine.Object.Destroy(rig.gameObject);}
        batch=P02MotionLibrary.Current.entries;
        string filter=Work+"batch_selection.json";
        if(File.Exists(filter)){
            var selection=JsonUtility.FromJson<BatchSelection>(File.ReadAllText(filter));
            batch=batch.Where(e=>selection.keys.Contains(e.actor+"_"+e.direction)).ToArray();
            if(batch.Length!=selection.keys.Length)throw new InvalidOperationException("Batch selection did not match library");
        }
        batchIndex=0;batchReports.Clear();
        batchDirectory=P02ProductionInputReview.Evidence+"production_runs/batch_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+"/";Directory.CreateDirectory(batchDirectory);
        File.WriteAllText(Work+"active_production_batch.txt",batchDirectory);
        EditorApplication.update+=BatchTick;batchCurrent=null;
    }
    static void BatchTick()
    {
        if(!EditorApplication.isPlaying){EditorApplication.update-=BatchTick;batch=null;return;}
        if(batchCurrent!=null&&!File.Exists(batchCurrent+"sample_motion_probe.json"))return;
        if(batchCurrent!=null){batchReports.Add(batchCurrent+"sample_motion_probe.json");batchCurrent=null;
            if(batchActor){batchActor.SetActive(false);UnityEngine.Object.Destroy(batchActor);}batchIndex++;}
        if(batchIndex>=batch.Length){File.WriteAllText(batchDirectory+"completed.json",JsonUtility.ToJson(new BatchReport{scope="independent native skins; synthetic actual Transform travel; visual review and production input review separate",reports=batchReports.ToArray()},true));EditorApplication.update-=BatchTick;batch=null;return;}
        var e=batch[batchIndex];batchActor=new GameObject("P02_"+e.actor+"_"+e.direction);
        var rig=batchActor.AddComponent<P02CharacterRig>();rig.definition=e.rig;rig.targetHeight=e.rig.targetHeight;rig.sampleMode=true;rig.Build();
        var probe=batchActor.AddComponent<P02MotionSampleProbe>();probe.runLabel=e.actor+"_"+e.direction;
        batchCurrent=probe.PrepareRun();
        File.WriteAllText(batchDirectory+"progress.txt",(batchIndex+1)+"/"+batch.Length+" "+probe.runLabel);
        BeginRecording(probe,batchCurrent);
    }
    [Serializable] sealed class BatchReport {public string scope;public string[] reports;}
    [Serializable] sealed class BatchSelection {public string[] keys;}
    static void BeginRecording(P02MotionSampleProbe probe,string directory)
    {
        var normal=directory+"continuous_motion.mp4";var slow=directory+"continuous_motion_slow.mp4";
        var encoder=new UnityEditor.Media.MediaEncoder(normal,new UnityEditor.Media.VideoTrackAttributes{width=1920,height=1080,frameRate=new UnityEditor.Media.MediaRational(60),includeAlpha=false,bitRateMode=VideoBitrateMode.High});
        probe.StartChecks((texture,seconds)=>encoder.AddFrame(texture,new UnityEditor.Media.MediaTime((long)(seconds*1000000),1000000,1)),()=>{encoder.Dispose();},normal,"");
    }

    static P02MotionLibrary.Entry[] diagnosticEntries;
    static GameObject diagnosticActor;
    static int diagnosticIndex,diagnosticPose,diagnosticWait;
    static string diagnosticDirectory;
    public static void CaptureCalibratedAssembly()=>CaptureAssemblyInternal(false);
    public static void CaptureQuarterAssembly()=>CaptureAssemblyInternal(true);
    static void CaptureAssemblyInternal(bool quartersOnly)
    {
        if(!EditorApplication.isPlaying||!SceneManager.GetActiveScene().name.StartsWith("P02"))
            throw new InvalidOperationException("Isolated P02 Play scene required");
        if(diagnosticEntries!=null)throw new InvalidOperationException("Diagnostic captures already running");
        foreach(var rig in UnityEngine.Object.FindObjectsByType<P02CharacterRig>(FindObjectsSortMode.None)){
            rig.gameObject.SetActive(false);UnityEngine.Object.Destroy(rig.gameObject);
        }
        diagnosticEntries=P02MotionLibrary.Current.entries.Where(e=>!quartersOnly||e.rig.projectedPlacement).ToArray();
        diagnosticIndex=0;diagnosticPose=0;diagnosticWait=0;
        diagnosticDirectory=P02ProductionInputReview.Evidence+"production_runs/assembly_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+"/";
        Directory.CreateDirectory(diagnosticDirectory);
        File.WriteAllText(Work+"active_assembly_review.txt",diagnosticDirectory);
        EditorApplication.update+=DiagnosticTick;
    }
    static void DiagnosticTick()
    {
        if(!EditorApplication.isPlaying){EditorApplication.update-=DiagnosticTick;diagnosticEntries=null;return;}
        if(diagnosticIndex>=diagnosticEntries.Length){
            File.WriteAllText(diagnosticDirectory+"completed.json","{\"scope\":\"native Unity render of fixed diagnostic poses; not continuous-motion acceptance\"}");
            EditorApplication.update-=DiagnosticTick;diagnosticEntries=null;return;
        }
        if(!diagnosticActor){
            var e=diagnosticEntries[diagnosticIndex];diagnosticActor=new GameObject("P02_"+e.actor+"_"+e.direction);
            var r=diagnosticActor.AddComponent<P02CharacterRig>();r.definition=e.rig;r.targetHeight=e.rig.targetHeight;r.sampleMode=true;r.Build();
            r.SetDiagnosticPose(0,false,false);diagnosticWait=0;
            Camera.main.orthographicSize=1.20f;Camera.main.transform.position=new Vector3(0,.88f,-10);
        }
        if(++diagnosticWait<6){EditorApplication.QueuePlayerLoopUpdate();return;}
        var entry=diagnosticEntries[diagnosticIndex];
        string key=entry.actor+"_"+entry.direction;
        P02MotionSampleProbe.Render(diagnosticDirectory+key+"_"+new[]{"idle","walk","run","walk_cross","run_cross"}[diagnosticPose]+".png",1920,1080);
        diagnosticPose++;diagnosticWait=0;
        if(diagnosticPose>=5){diagnosticActor.SetActive(false);UnityEngine.Object.Destroy(diagnosticActor);diagnosticActor=null;diagnosticIndex++;diagnosticPose=0;}
        else diagnosticActor.GetComponent<P02CharacterRig>().SetDiagnosticPose(diagnosticPose>=3?.72f:.35f,true,diagnosticPose==2||diagnosticPose==4);
        EditorApplication.QueuePlayerLoopUpdate();
    }
}

