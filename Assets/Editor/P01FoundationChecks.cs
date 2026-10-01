using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;
using TMPro;

// Focused checks for the P01 foundation. Does not save or rebuild any scene.
public static class P01FoundationChecks
{
    static string Work => Path.GetFullPath(Path.Combine(Application.dataPath,"../Temp/P01Verification"))+"/";
    static void Write(string name,string text){Directory.CreateDirectory(Work);File.WriteAllText(Work+name,text);}
    public static void Snapshot()
    {
        var lines=new List<string>();
        lines.Add("scene="+SceneManager.GetActiveScene().name+" play="+EditorApplication.isPlaying);
        foreach(var p in UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude))
            lines.Add("player="+p.name+" pos="+p.transform.position+" collider="+p.GetComponent<Collider2D>()?.bounds);
        foreach(var c in UnityEngine.Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Exclude))
            if(c.GetComponent<IInteractable>()!=null)lines.Add("target="+c.name+" pos="+c.transform.position+" bounds="+c.bounds+" trigger="+c.isTrigger);
        foreach(var cam in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Exclude))lines.Add("camera="+cam.name+" pos="+cam.transform.position+" size="+cam.orthographicSize+" pixel="+cam.pixelRect);
        Write("snapshot.txt",string.Join("\n",lines));
    }
    [MenuItem("Tools/ProjectMystery/P01/Identity Mapping")]
    public static void Identity()
    {
        var lines=new List<string>();int count=0;
        foreach(string guid in AssetDatabase.FindAssets("t:DialogueData"))
        {
            string path=AssetDatabase.GUIDToAssetPath(guid);
            var d=AssetDatabase.LoadAssetAtPath<DialogueData>(path);
            if(d.lines==null)continue;
            foreach(var line in d.lines)
            {
                count++;
                if(line.characterId==DialogueCharacterId.Unassigned)lines.Add("FAIL unassigned: "+path);
                if(line.speaker=="赵炳程"&&line.characterId!=DialogueCharacterId.ZhaoBingCheng)lines.Add("FAIL Zhao: "+path);
                if(line.speaker=="张若媛"&&line.characterId!=DialogueCharacterId.ZhangRuoYuan)lines.Add("FAIL Zhang: "+path);
                if(line.speaker=="孙付"&&line.characterId!=DialogueCharacterId.SunFu)lines.Add("FAIL Sun: "+path);
                if(line.speaker=="王平"&&line.characterId!=DialogueCharacterId.WangPing)lines.Add("FAIL Wang: "+path);
                if(path.Contains("D_Fog_Inquiry")&&line.speaker=="???"&&line.characterId!=DialogueCharacterId.Unknown)lines.Add("FAIL fog unknown");
                if(line.portraitMode==DialoguePortraitMode.Character)
                {
                    var key=DialogueIdentity.PortraitKey(line.characterId,line.age);
                    if(key!=null&&!DemoArtCatalog.Find(key))lines.Add("FAIL missing portrait: "+key+" "+path);
                }
            }
        }
        lines.Insert(0,"Inspected dialogue lines="+count);
        lines.Add(lines.Any(l=>l.StartsWith("FAIL"))?"RESULT FAIL":"RESULT PASS");
        Write("identity-check.txt",string.Join("\n",lines));
    }
    public static void PuddleReach()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Requires Play Mode from Scene01.");
        var p=PersistentRuntime.Instance.GetComponent<GameControlManager>().ActivePlayer;
        var interaction=p.GetComponent<PlayerInteraction>();
        var target=UnityEngine.Object.FindObjectsByType<Collider2D>(FindObjectsInactive.Exclude).First(c=>c.name=="Puddle_Placeholder"&&c.GetComponent<IInteractable>()!=null);
        var rb=p.GetComponent<Rigidbody2D>();var original=p.transform.position;bool simulated=rb.simulated;rb.simulated=false;
        var report=new List<string>();
        try
        {
            foreach(float gap in new[]{1.2f,.7f,.3f})
            {
                var surface=target.bounds;target.GetComponent<HandDrawnInteractionHighlight>().TryGetInteractionBounds(out surface);
                var pos=p.transform.position;pos.x=surface.min.x-gap; p.transform.position=pos;Physics2D.SyncTransforms();
                typeof(PlayerInteraction).GetMethod("DetectInteractable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(interaction,null);
                bool selected=ReferenceEquals(interaction.CurrentVisualTarget,target.GetComponent<IInteractable>());
                bool expected=gap<.55f;
                report.Add((selected==expected?"PASS":"FAIL")+" puddle gap="+gap+" selected="+selected);
            }
        }
        finally{p.transform.position=original;rb.simulated=simulated;Physics2D.SyncTransforms();}
        Write("reach-check.txt",string.Join("\n",report));
    }
    public static void NearPuddle()
    {
        var p=PersistentRuntime.Instance.GetComponent<GameControlManager>().ActivePlayer;
        var puddle=GameObject.Find("Puddle_Placeholder");
        puddle.GetComponent<HandDrawnInteractionHighlight>().TryGetInteractionBounds(out var surface);
        p.transform.position=new Vector3(surface.min.x-.3f,p.transform.position.y,0);
        Physics2D.SyncTransforms();Camera.main.GetComponent<CameraFollow>().SnapToTarget();
    }
    public static void GuideDialogue()
    {
        var d=AssetDatabase.LoadAssetAtPath<DialogueData>("Assets/ScriptableObjects/D_S01_QiuTian_First.asset");
        UnityEngine.Object.FindAnyObjectByType<DialogueManager>().StartDialogue(d);
    }

    [MenuItem("Tools/ProjectMystery/P01/Interaction and Locks (Scene01 Play Mode)")]
    public static void RuntimeRules()
    {
        if(!EditorApplication.isPlaying)throw new Exception("Requires Play Mode.");
        var controls=PersistentRuntime.Instance.GetComponent<GameControlManager>();
        if(controls.IsLocked())throw new Exception("Test needs an idle player.");
        var p=controls.ActivePlayer;var interaction=p.GetComponent<PlayerInteraction>();
        var ui=UnityEngine.Object.FindAnyObjectByType<InteractionUI>();
        var dialogue=UnityEngine.Object.FindAnyObjectByType<DialogueManager>();
        var inventory=UnityEngine.Object.FindAnyObjectByType<InventoryUI>();
        var reports=new List<string>();
        Action<bool,string> check=(ok,label)=>reports.Add((ok?"PASS ":"FAIL ")+label);
        var ownerA=new GameObject("P01_TestOwnerA");var ownerB=new GameObject("P01_TestOwnerB");
        var original=p.transform.position;GameObject wall=null;DialogueData testDialogue=null;
        var detect=typeof(PlayerInteraction).GetMethod("DetectInteractable",BindingFlags.Instance|BindingFlags.NonPublic);
        try
        {
            controls.AcquireLock(ControlLockReason.Cutscene,ownerA);
            controls.AcquireLock(ControlLockReason.Cutscene,ownerB);
            controls.ReleaseLock(ControlLockReason.Cutscene,ownerA);
            check(controls.IsLockedBy(ControlLockReason.Cutscene)&&!interaction.enabled,"releasing owner A keeps owner B locked");
            controls.ReleaseLock(ControlLockReason.Cutscene);
            check(controls.IsLocked(),"legacy release cannot release another owner's lock");
            controls.ReleaseLock(ControlLockReason.Cutscene,ownerB);
            check(!controls.IsLocked()&&interaction.enabled,"last owner release restores control");
            NearPuddle();detect.Invoke(interaction,null);
            check(interaction.CurrentVisualTarget!=null,"near target selected before modal");
            typeof(InventoryUI).GetMethod("OpenInventory",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(inventory,null);
            check(inventory.IsOpen&&!interaction.enabled&&!ui.IsVisible&&interaction.CurrentVisualTarget==null,"inventory hides prompt and blocks scene interaction");
            inventory.CloseInventory();detect.Invoke(interaction,null);
            check(interaction.CurrentVisualTarget!=null,"closing inventory restores target");
            wall=new GameObject("P01_TestSolidWall");wall.transform.position=new Vector3(p.transform.position.x+.15f,interaction.Feet.y+.65f,0);
            wall.AddComponent<BoxCollider2D>().size=new Vector2(.08f,2);Physics2D.SyncTransforms();detect.Invoke(interaction,null);
            check(interaction.CurrentVisualTarget==null,"solid wall blocks nearby target");
            UnityEngine.Object.DestroyImmediate(wall);wall=null;Physics2D.SyncTransforms();
            p.transform.position+=Vector3.up*5;Physics2D.SyncTransforms();detect.Invoke(interaction,null);
            check(interaction.CurrentVisualTarget==null,"other-floor target is not eligible");
            NearPuddle();
            testDialogue=ScriptableObject.CreateInstance<DialogueData>();
            testDialogue.lines=new[]{new DialogueLine{speaker="肖鹏",characterId=DialogueCharacterId.XiaoPeng,text="P01 interaction regression"}};
            dialogue.StartDialogue(testDialogue);
            check(dialogue.IsDialogueActive()&&!interaction.enabled&&!ui.IsVisible,"dialogue blocks scene interaction");
            dialogue.CancelDialogue(testDialogue);detect.Invoke(interaction,null);
            check(dialogue.DidDialogueEndThisFrame()&&interaction.CurrentVisualTarget==null,"dialogue-ending frame cannot reuse E target");
        }
        finally
        {
            inventory.CloseInventory();if(testDialogue)dialogue.CancelDialogue(testDialogue);
            controls.ReleaseLock(ControlLockReason.Cutscene,ownerA);controls.ReleaseLock(ControlLockReason.Cutscene,ownerB);
            if(wall)UnityEngine.Object.DestroyImmediate(wall);
            UnityEngine.Object.DestroyImmediate(ownerA);UnityEngine.Object.DestroyImmediate(ownerB);
            if(testDialogue)UnityEngine.Object.DestroyImmediate(testDialogue);
            p.transform.position=original;Physics2D.SyncTransforms();
        }
        Write("runtime-rules-check.txt",string.Join("\n",reports));
    }
    public static void PromptGeometry()
    {
        var ui=UnityEngine.Object.FindAnyObjectByType<InteractionUI>();
        var p=PersistentRuntime.Instance.GetComponent<GameControlManager>().ActivePlayer;
        var paint=p.GetComponentsInChildren<SpriteRenderer>().First(r=>r.name=="Demo2026_PlayerPainting");
        var root=(GameObject)typeof(InteractionUI).GetField("promptRoot",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(ui);
        var text=root.GetComponent<TMP_Text>();var canvas=root.GetComponentInParent<Canvas>();
        var screen=RectTransformUtility.WorldToScreenPoint(canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera,root.transform.position);
        var head=Camera.main.WorldToScreenPoint(new Vector3(paint.bounds.center.x,paint.bounds.max.y,0));
        bool pass=ui.IsVisible&&Mathf.Abs(screen.x-head.x)<5&&screen.y>head.y;
        Write("prompt-geometry.txt",(pass?"PASS":"FAIL")+" head="+head+" prompt="+screen+" font="+text.fontSize+" scale="+canvas.scaleFactor);
    }


    public static void LoadCourtyard()
    {
        if(!Application.isPlaying || !PersistentRuntime.Instance)throw new Exception("Start from Scene01 first.");
        var d=UnityEngine.Object.FindAnyObjectByType<DialogueManager>();
        if(d&&d.IsDialogueActive())d.CancelDialogue(d.CurrentDialogue);
        SceneManager.LoadScene("Scene02_Courtyard");
    }
    public static void MasterBedroom()
    {
        if(SceneManager.GetActiveScene().name!="Scene02_Courtyard")throw new Exception("Scene02 required");
        var p=PersistentRuntime.Instance.GetComponent<GameControlManager>().ActivePlayer;
        EndingSession.Place(p,new Vector3(70,1.42f,0));
        Physics2D.SyncTransforms();Camera.main.GetComponent<CameraFollow>().SnapToTarget();
    }
    public static void SpatialSnapshot()
    {
        var rows=new List<string>();
        rows.Add("scene="+SceneManager.GetActiveScene().name);
        var p=PersistentRuntime.Instance.GetComponent<GameControlManager>().ActivePlayer;
        rows.Add("player="+p.transform.position+" feet="+p.GetComponent<PlayerInteraction>().Feet+" speed="+p.WalkSpeed);
        foreach(var t in ArtRebuildEditor.All())
        {
            if(t.position.x<57||t.position.x>83)continue;
            var c=t.GetComponent<Collider2D>();var r=t.GetComponent<Renderer>();
            if(c)rows.Add("COLLIDER "+ArtRebuildEditor.PathOf(t)+" bounds="+c.bounds+" trigger="+c.isTrigger+" enabled="+c.enabled);
            if(r&&r.enabled&&!r.forceRenderingOff)rows.Add("RENDERER "+ArtRebuildEditor.PathOf(t)+" bounds="+r.bounds+" order="+r.sortingOrder);
        }
        foreach(var room in UnityEngine.Object.FindObjectsByType<ContinuousRoomArtwork>(FindObjectsInactive.Include))
            if(room.name.Contains("Master"))rows.Add("PAINTING "+JsonUtility.ToJson(room));
        Write("spatial-master-before.txt",string.Join("\n",rows));
    }


    [MenuItem("Tools/ProjectMystery/P01/Bedroom Targets (Scene02 Play Mode)")]
    public static void BedroomReach()
    {
        var p=PersistentRuntime.Instance.GetComponent<GameControlManager>().ActivePlayer;
        var interaction=p.GetComponent<PlayerInteraction>();var rb=p.GetComponent<Rigidbody2D>();
        var original=p.transform.position;bool simulation=rb.simulated;rb.simulated=false;
        var rows=new List<string>();
        var root=GameObject.Find("MasterBedroomInterior");
        var detect=typeof(PlayerInteraction).GetMethod("DetectInteractable",BindingFlags.Instance|BindingFlags.NonPublic);
        try
        {
            foreach(var name in new[]{"Wardrobe_Placeholder","Bed_Placeholder","Cabinet_Placeholder","Lamp_Placeholder","CameraItem_Placeholder","ExitDoor_Point"})
            {
                var t=root.transform.Find(name);var target=t.GetComponent<IInteractable>();
                var r=t.GetComponentsInChildren<SpriteRenderer>().First(v=>v.name=="NewArt_Visual");
                float x=name=="ExitDoor_Point"?r.bounds.max.x+.3f:r.bounds.center.x;
                p.transform.position=new Vector3(x,original.y,0);Physics2D.SyncTransforms();detect.Invoke(interaction,null);
                bool ok=ReferenceEquals(interaction.CurrentVisualTarget,target);
                rows.Add((ok?"PASS ":"FAIL ")+name+" at x="+x+" selected="+(interaction.CurrentVisualTarget as Component)?.name);
            }
            var wardrobe=root.transform.Find("Wardrobe_Placeholder");var visual=wardrobe.Find("NewArt_Visual").GetComponent<SpriteRenderer>();
            p.transform.position=new Vector3(visual.bounds.min.x-.7f,original.y,0);Physics2D.SyncTransforms();detect.Invoke(interaction,null);
            rows.Add((!ReferenceEquals(interaction.CurrentVisualTarget,wardrobe.GetComponent<IInteractable>())?"PASS":"FAIL")+" outside painted wardrobe despite oversized old sensor");
        }
        finally{p.transform.position=original;rb.simulated=simulation;Physics2D.SyncTransforms();}
        Write("bedroom-reach-check.txt",string.Join("\n",rows));
    }


    [MenuItem("Tools/ProjectMystery/P01/Portrait Rules (Play Mode)")]
    public static void PortraitRules()
    {
        var d=UnityEngine.Object.FindAnyObjectByType<DialogueManager>();
        if(d.IsDialogueActive())throw new Exception("Requires idle dialogue manager.");
        var art=d.GetComponent<DemoDialogueArtwork>();
        var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var update=typeof(DemoDialogueArtwork).GetMethod("LateUpdate",flags);
        var rows=new List<string>();int completed=d.CompletedCount;
        Action<bool,string> check=(ok,label)=>rows.Add((ok?"PASS ":"FAIL ")+label);
        Func<string,DialogueData> load=p=>AssetDatabase.LoadAssetAtPath<DialogueData>(p);
        try
        {
            var guide=load("Assets/ScriptableObjects/D_S01_QiuTian_First.asset");
            d.StartDialogue(guide);update.Invoke(art,null);
            var right=(UnityEngine.UI.Image)typeof(DemoDialogueArtwork).GetField("right",flags).GetValue(art);
            check(right.enabled&&right.sprite,"guide portrait appears for explicitly assigned anonymous guide");
            var root=(GameObject)typeof(DemoDialogueArtwork).GetField("root",flags).GetValue(art);
            check(root.GetComponentsInChildren<DialoguePortraitMask>(true).Length==2,"both portrait frames use the same curved mask");
            d.CancelDialogue(guide);
            var fog=AssetDatabase.FindAssets("D_Fog_Inquiry t:DialogueData").Select(AssetDatabase.GUIDToAssetPath).Select(load).First();
            d.StartDialogue(fog);update.Invoke(art,null);
            check(right.sprite==null,"unknown fog voice does not inherit guide portrait");d.CancelDialogue(fog);
            foreach(var cue in new[]{"F01","F02","F03","F04","F05","E01"})
            {
                var data=load("Assets/GameData/Ending/Stage6/D_"+cue+".asset");
                d.StartDialogue(data);update.Invoke(art,null);
                check(d.IsDialogueActive()&&d.PresentationBackground.gameObject.activeInHierarchy&&!root.activeSelf,cue+" retains subtitle while hiding all portraits");
                if(cue=="F02"||cue=="F04")check(data.lines.All(l=>l.speaker=="？？？"),cue+" identity remains anonymous in subtitle");
                d.CancelDialogue(data);
            }
            d.StartDialogue(guide);update.Invoke(art,null);
            check(root.activeSelf&&right.sprite,"normal portrait returns after no-portrait exception");d.CancelDialogue(guide);
            check(d.CompletedCount==completed,"diagnostic cancellation never advances story completion");
        }
        finally{if(d.IsDialogueActive())d.CancelDialogue(d.CurrentDialogue);update.Invoke(art,null);}
        Write("portrait-rules-check.txt",string.Join("\n",rows));
    }

}
