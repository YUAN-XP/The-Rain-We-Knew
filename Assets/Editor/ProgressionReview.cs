using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Explicit QA only. Never saves scenes, sets story facts, grants items, or changes focus rules.
public static class ProgressionReview
{
    public const string Work="D:/游戏设计/Codex工作区/work/2026-10-15-ProgressionAudit/";
    static readonly BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    [Serializable] public class Surface { public string path,type,prompt,detail; public Vector3 position,colliderCenter,colliderSize,visualCenter,visualSize; public bool active,enabled,trigger,painted; public int layer; public string[] visuals; }
    [Serializable] public class Probe { public string path,variant,room,error; public float floor; public int selections; public string[] selectedInstead; public Vector3[] selectableFeet; }
    [Serializable] class Export { public string stamp,scene,phase; public Surface[] targets; public Probe[] probes; }
    static Transform[] All()=>SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();
    public static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
    public static void Audit()=>Scan("before");
    public static void Verify()=>Scan("after");
    public static void ExitClean()
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit mode required before closing task-owned editor");
        EditorApplication.Exit(0);
    }
    public static void ApplyAndFullFlow(){ApplySchoolBindings();FullFlow();}
    public static void ApplySchoolBindings()
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit mode required");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Scene04_School.unity");
        var layout=All().Select(t=>t.GetComponent<SchoolLayoutController>()).First(v=>v);
        var changes=new List<string>();
        foreach(var variant in new[]{layout.otherworldRoot,layout.memoryRoot}) {
            bool memory=variant==layout.memoryRoot;
            foreach(var room in layout.data.rooms) {
                var root=variant.transform.Find(room.id);if(!root)continue;
                var art=root.Find("P05_Artwork");if(!art)continue;
                foreach(var target in root.GetComponentsInChildren<MonoBehaviour>(true).Where(m=>m is SchoolPortal||m is SchoolInteractable)) {
                    var visual=art.Find("Gateway_"+target.name);if(!visual||target.name.StartsWith("Stair_"))continue;
                    var sr=visual.GetComponent<SpriteRenderer>();if(!sr)continue;
                    P03ProductionBuild.Paint(sr,DemoArtCatalog.Find("MOD_SCHOOL_DOORS:"+(memory?0:2)),target.transform.position.x,room.origin.y+.25f,2.35f);
                    Bind(target,sr);changes.Add("CLOSED_DOOR_AND_BIND "+PathOf(target.transform));
                }
                if(room.id=="Class3_2"||room.id=="Class3_1") {
                    for(int i=0;i<4;i++) {
                        var seat=root.Find("Desk_"+(i+1));if(!seat)throw new Exception("Missing authored semantic seat "+room.id+"/"+(i+1));
                        var t=art.Find("Desk_"+i);if(!t){t=new GameObject("Desk_"+i).transform;t.SetParent(art,false);}
                        var sr=t.GetComponent<SpriteRenderer>();if(!sr)sr=t.gameObject.AddComponent<SpriteRenderer>();
                        P03ProductionBuild.Paint(sr,DemoArtCatalog.Find("MOD_SCHOOL_DESK_CHAIR_PAIR_V02:"+(memory?0:1)),seat.position.x,room.origin.y+.23f,1.05f);sr.sortingOrder=-10;
                        var shadow=t.GetComponent<PaintedContactShadow>();if(!shadow)shadow=t.gameObject.AddComponent<PaintedContactShadow>();shadow.target=sr;shadow.depth=.045f;shadow.widthFactor=.7f;
                        t.gameObject.SetActive(!(memory&&room.id=="Class3_2"&&i==0));
                    }
                    if(room.id=="Class3_2") {
                        foreach(var action in new[]{SchoolAction.OwnDesk,SchoolAction.ScratchedDesk}) {
                            var target=root.GetComponentsInChildren<SchoolInteractable>(true).Single(t=>t.action==action);
                            var sr=art.Find(action==SchoolAction.OwnDesk?"Desk_1":"Desk_2").GetComponent<SpriteRenderer>();
                            Bind(target,sr);var reach=target.GetComponent<InteractionReach>();if(!reach)reach=target.gameObject.AddComponent<InteractionReach>();reach.anchor=null;reach.visualSurface=sr;reach.horizontalReach=.55f;reach.verticalReach=1.8f;
                            changes.Add("DESK_SEMANTIC_BIND "+PathOf(target.transform)+" -> "+PathOf(sr.transform));
                        }
                        var seated=art.Find("Seated_S03");if(memory&&seated)seated.position=new Vector3(root.Find("Desk_1").position.x,seated.position.y,seated.position.z);
                    }
                }
            }
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
        File.WriteAllLines(Work+"Results/applied-school-bindings.txt",changes);
        EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");
    }
    static void Bind(MonoBehaviour target,SpriteRenderer visual)
    {
        var highlight=target.GetComponent<HandDrawnInteractionHighlight>();if(!highlight)highlight=target.gameObject.AddComponent<HandDrawnInteractionHighlight>();
        var so=new SerializedObject(highlight);so.FindProperty("interactionSource").objectReferenceValue=target;
        var list=so.FindProperty("visuals");list.arraySize=1;list.GetArrayElementAtIndex(0).objectReferenceValue=visual;
        so.FindProperty("outerContour").arraySize=0;
        if(!so.FindProperty("outlineMaterial").objectReferenceValue)so.FindProperty("outlineMaterial").objectReferenceValue=AssetDatabase.FindAssets("HandDrawnInteraction t:Material").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Material>).First();
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    public static void FullFlow()
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit mode required");
        Directory.CreateDirectory(Work+"Runtime");
        started=false;
        EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");
        SessionState.SetBool("ProgressionReview.Flow",true);
        EditorApplication.update-=FlowTick;EditorApplication.update+=FlowTick;
        EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Resume(){if(SessionState.GetBool("ProgressionReview.Flow",false))EditorApplication.update+=FlowTick;}
    static bool started;
    static void FlowTick()
    {
        if(!SessionState.GetBool("ProgressionReview.Flow",false)){EditorApplication.update-=FlowTick;return;}
        if(EditorApplication.isPlaying&&!EditorApplication.isCompiling&&PersistentRuntime.Instance&&!started&&GamePause.Instance&&GamePause.Instance.MenuOpen&&!PersistentRuntime.Instance.GetComponent<GameControlManager>().IsLockedBy(ControlLockReason.Cutscene)){started=true;ProgressionFullReplay.Begin();File.WriteAllText(Work+"Runtime/ready.txt",DateTime.Now.ToString("O"));}
        if(File.Exists(Work+"Runtime/stop-review.txt")){File.Delete(Work+"Runtime/stop-review.txt");SessionState.SetBool("ProgressionReview.Flow",false);ProgressionFullReplay.End();EditorApplication.isPlaying=false;EditorApplication.update-=FlowTick;if(Application.isBatchMode)EditorApplication.Exit(0);return;}
        EditorApplication.QueuePlayerLoopUpdate();
    }
    static void Scan(string phase)
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Clean edit mode required");
        Directory.CreateDirectory(Work+"Results/"+phase);
        try {
            foreach(var entry in EditorBuildSettings.scenes.Where(e=>e.enabled)) {
                var scene=EditorSceneManager.OpenScene(entry.path);var all=All();
                foreach(var region in all.Select(t=>t.GetComponent<SpriteArtRegion>()).Where(v=>v))region.Rebuild();
                var sources=all.SelectMany(t=>t.GetComponents<MonoBehaviour>()).Where(m=>m is IInteractable).ToArray();
                var surfaces=new List<Surface>();
                foreach(var m in sources) {
                    var c=m.GetComponent<Collider2D>();var h=m.GetComponent<HandDrawnInteractionHighlight>();var reach=m.GetComponent<InteractionReach>();
                    Bounds b=c?c.bounds:default;bool painted=h&&h.TryGetInteractionBounds(out b);
                    string prompt="",detail="";try{prompt=((IInteractable)m).GetInteractionPrompt();}catch(Exception e){detail=e.GetType().Name;}
                    if(m is SchoolInteractable si)detail+=" action="+si.action;
                    if(m is SchoolPortal portal&&portal.Definition!=null)detail+=" gate="+portal.Gate+" ->"+portal.Definition.targetRoom;
                    if(reach)detail+=" reach="+reach.horizontalReach+"/"+reach.verticalReach+" anchor="+(reach.anchor?PathOf(reach.anchor):"none");
                    var v=new List<string>();if(h){var a=new SerializedObject(h).FindProperty("visuals");for(int i=0;i<a.arraySize;i++){var sr=a.GetArrayElementAtIndex(i).objectReferenceValue as SpriteRenderer;if(sr)v.Add(PathOf(sr.transform)+" active="+sr.gameObject.activeInHierarchy+" enabled="+sr.enabled+" off="+sr.forceRenderingOff+" sprite="+(sr.sprite?sr.sprite.name:"none")+" bounds="+sr.bounds.ToString("F3"));}}
                    surfaces.Add(new Surface{path=PathOf(m.transform),type=m.GetType().Name,prompt=prompt,detail=detail,position=m.transform.position,layer=m.gameObject.layer,active=m.gameObject.activeInHierarchy,enabled=c&&c.enabled,trigger=c&&c.isTrigger,colliderCenter=c?c.bounds.center:default,colliderSize=c?c.bounds.size:default,painted=painted,visualCenter=b.center,visualSize=b.size,visuals=v.ToArray()});
                }
                var probes=new List<Probe>();
                if(scene.name=="Scene04_School") {
                    var layout=all.Select(t=>t.GetComponent<SchoolLayoutController>()).First(v=>v);
                    var fake=new GameObject("unsaved selection probe");fake.layer=Mathf.Max(0,LayerMask.NameToLayer("Player"));
                    var body=fake.AddComponent<CapsuleCollider2D>();body.size=new Vector2(.28f,1.7f);body.offset=new Vector2(0,.85f);body.isTrigger=true;
                    var interaction=fake.AddComponent<PlayerInteraction>();typeof(PlayerInteraction).GetField("playerCollider",Hidden).SetValue(interaction,body);
                    var serialized=new SerializedObject(interaction);serialized.FindProperty("interactableLayer").intValue=64;serialized.ApplyModifiedPropertiesWithoutUndo();
                    var detect=typeof(PlayerInteraction).GetMethod("DetectInteractable",Hidden);
                    foreach(var variant in new[]{layout.otherworldRoot,layout.memoryRoot}) {
                        layout.otherworldRoot.SetActive(variant==layout.otherworldRoot);layout.memoryRoot.SetActive(variant==layout.memoryRoot);Physics2D.SyncTransforms();
                        foreach(var room in layout.data.rooms) {
                            var root=variant.transform.Find(room.id);if(!root)continue;
                            var floors=root.GetComponentsInChildren<Collider2D>(true).Where(c=>c.enabled&&!c.isTrigger&&c.bounds.size.x>room.width*.5f&&c.bounds.max.y<room.origin.y+1).ToArray();
                            float floor=floors.Length>0?floors.Max(c=>c.bounds.max.y):room.origin.y+.23f;
                            foreach(var target in root.GetComponentsInChildren<MonoBehaviour>(true).Where(m=>m is IInteractable)) {
                                var p=new Probe{path=PathOf(target.transform),variant=variant.name,room=room.id,floor=floor};var feet=new List<Vector3>();var instead=new HashSet<string>();
                                for(float x=Mathf.Max(room.origin.x+.2f,target.transform.position.x-2);x<=Mathf.Min(room.origin.x+room.width-.2f,target.transform.position.x+2);x+=.1f) {
                                    fake.transform.position=new Vector3(x,floor+.001f,0);Physics2D.SyncTransforms();
                                    try { detect.Invoke(interaction,null);var got=interaction.CurrentVisualTarget as Component;if(got==target)feet.Add(interaction.Feet);else if(got)instead.Add(PathOf(got.transform)); }
                                    catch(Exception e){p.error=e.InnerException?.Message??e.Message;break;}
                                }
                                p.selections=feet.Count;p.selectableFeet=feet.ToArray();p.selectedInstead=instead.ToArray();probes.Add(p);
                            }
                        }
                        var cam=Camera.main;if(cam){cam.orthographicSize=2.5f;cam.aspect=16f/9;cam.transform.position=new Vector3(207,17.35f,-10);P03SceneEvidence.Render(cam,Work+"Results/"+phase+"/"+variant.name+"_Class3_2.png",1280,720);}
                    }
                    UnityEngine.Object.DestroyImmediate(fake);
                }
                File.WriteAllText(Work+"Results/"+phase+"/"+scene.name+".json",JsonUtility.ToJson(new Export{stamp=DateTime.Now.ToString("O"),scene=scene.name,phase=phase,targets=surfaces.ToArray(),probes=probes.ToArray()},true));
            }
        } finally { EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity"); }
        File.WriteAllText(Work+"Results/"+phase+"/complete.txt",DateTime.Now.ToString("O"));
    }
}
