using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// Isolated prototype: never changes P02MotionLibrary or an authored scene.
public static class P02FarLegCandidateReview
{
    const string ImagePath="Assets/Art/P02Motion/Candidates/child_far_leg_PV18_cutout.png";
    const string AssetPath="Assets/Art/P02Motion/Candidates/child_up_left_farleg_PV18.asset";
    public static void Prepare()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Edit Mode required");
        const float width=360,height=1342;
        var hip=new Vector2(520,300);var knee=new Vector2(553,840);var ankle=new Vector2(590,1380);
        float upper=Vector2.Distance(hip,knee),lower=Vector2.Distance(knee,ankle);
        float units=.695f/(upper+lower),paintedHeight=height*units;
        var part=new P02RigFactory.Part{name="far_leg",x=344,y=66,width=(int)width,height=(int)height,
            pivotX=(520-344)/width,pivotY=1-(120-66)/height,paintedHeight=paintedHeight,
            tipY=1-(840-66)/height,order=-8,limb=true,calibratedLimb=true,
            hipX=(hip.x-344)/width,hipY=1-(hip.y-66)/height,
            jointX=(knee.x-344)/width,jointY=1-(knee.y-66)/height,
            endX=(ankle.x-344)/width,endY=1-(ankle.y-66)/height,
            bindUpperLength=upper*units,bindLowerLength=lower*units};
        P02RigFactory.Import(new P02RigFactory.Source{path=ImagePath,width=1024,height=1536,parts=new[]{part}});
        var source=AssetDatabase.LoadAssetAtPath<P02MotionLibrary>("Assets/Resources/P02MotionLibrary.asset").Find("child","up_left");
        var candidate=AssetDatabase.LoadAssetAtPath<P02RigDefinition>(AssetPath);
        if(!candidate){candidate=UnityEngine.Object.Instantiate(source);AssetDatabase.CreateAsset(candidate,AssetPath);}
        else EditorUtility.CopySerialized(source,candidate);
        var binding=candidate.parts.Single(x=>x.joint=="far_leg");
        binding.sprite=AssetDatabase.LoadAllAssetsAtPath(ImagePath).OfType<Sprite>().Single();
        binding.paintedHeight=paintedHeight;binding.bindUpperLength=part.bindUpperLength;binding.bindLowerLength=part.bindLowerLength;
        EditorUtility.SetDirty(candidate);AssetDatabase.SaveAssets();
    }
    public static void Run()
    {
        if(!EditorApplication.isPlaying||!SceneManager.GetActiveScene().name.StartsWith("P02"))throw new InvalidOperationException("Isolated P02 sample required");
        foreach(var rig in UnityEngine.Object.FindObjectsByType<P02CharacterRig>(FindObjectsSortMode.None)){rig.gameObject.SetActive(false);UnityEngine.Object.Destroy(rig.gameObject);}
        var actor=new GameObject("P02 far-leg candidate isolated review");var r=actor.AddComponent<P02CharacterRig>();
        r.definition=AssetDatabase.LoadAssetAtPath<P02RigDefinition>(AssetPath);r.targetHeight=r.definition.targetHeight;r.sampleMode=true;r.Build();
        var probe=actor.AddComponent<P02MotionSampleProbe>();probe.runLabel="child_up_left_farleg_PV18_candidate";
        string directory=probe.PrepareRun();
        File.WriteAllText("D:/游戏设计/Codex工作区/work/2026-10-03-P02/active_farleg_candidate.txt",directory);
        typeof(P02ProductionFactory).GetMethod("BeginRecording",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{probe,directory});
    }
}
