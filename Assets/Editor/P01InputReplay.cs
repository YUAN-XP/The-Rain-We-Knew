using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

// Opt-in editor-only input replay. It never changes story flags, actor transforms or puzzle results.
public sealed class P01InputReplay : GameplayInput.IReplaySource {
    [Serializable] public class Step { public string label; public float seconds=.2f; public string keys=""; public float x,y; public bool pointer,mouse,capture; }
    [Serializable] public class Batch { public Step[] steps; }
    public static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Temp/P01InputReplay"));
    static P01InputReplay instance;
    readonly Queue<Step> queue=new Queue<Step>(); readonly HashSet<KeyCode> held=new HashSet<KeyCode>(),previous=new HashSet<KeyCode>();
    P01ReplayDriver driver; GameSaveService isolatedService; FourSlotSaveStore originalStore;
    Step active; float until,nextRead,nextStatus; bool mouse,oldMouse; Vector3 pointer; int serial;
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    public static void Begin(){
        if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");
        if(instance!=null)return;
        var go=new GameObject("P01 editor input replay");UnityEngine.Object.DontDestroyOnLoad(go);instance=new P01InputReplay();var driver=go.AddComponent<P01ReplayDriver>();driver.Tick=instance.Update;driver.Cleanup=instance.Cleanup;instance.driver=driver;
        Directory.CreateDirectory(Folder);GameplayInput.Replay=instance;
        var saves=PersistentRuntime.Instance.GetComponent<GameSaveService>();
        instance.isolatedService=saves;instance.originalStore=saves.Store;
        typeof(GameSaveService).GetProperty("Store").GetSetMethod(true).Invoke(saves,new object[]{new FourSlotSaveStore(Path.Combine(Folder,"isolated-slots"))});
        File.AppendAllText(Path.Combine(Folder,"events.txt"),"BEGIN "+DateTime.Now.ToString("O")+" normal gameplay input; isolated save slots\n");
    }
    void Cleanup(){
        if(isolatedService!=null&&originalStore!=null)typeof(GameSaveService).GetProperty("Store").GetSetMethod(true).Invoke(isolatedService,new object[]{originalStore});
        if(ReferenceEquals(GameplayInput.Replay,this))GameplayInput.Replay=null;
        if(ReferenceEquals(instance,this))instance=null;
    }
    public static void Snapshot(){if(instance!=null)instance.Report();}
    public static void End(){if(instance!=null)UnityEngine.Object.Destroy(instance.driver.gameObject);}
    public bool Key(KeyCode key)=>held.Contains(key);
    public bool KeyDown(KeyCode key)=>held.Contains(key)&&!previous.Contains(key);
    public float Axis(string axis)=>axis=="Horizontal"?((Key(KeyCode.D)||Key(KeyCode.RightArrow)?1:0)-(Key(KeyCode.A)||Key(KeyCode.LeftArrow)?1:0)):axis=="Vertical"?((Key(KeyCode.W)||Key(KeyCode.UpArrow)?1:0)-(Key(KeyCode.S)||Key(KeyCode.DownArrow)?1:0)):0;
    public bool Mouse(int b)=>b==0&&mouse; public bool MouseDown(int b)=>b==0&&mouse&&!oldMouse; public bool MouseUp(int b)=>b==0&&!mouse&&oldMouse; public Vector3 Pointer=>pointer;
    void Update(){
        previous.Clear();previous.UnionWith(held);oldMouse=mouse;
        float now=Time.realtimeSinceStartup;
        if(now>=nextRead){nextRead=now+.1f;string path=Path.Combine(Folder,"queue.json");if(File.Exists(path)){try{var batch=JsonUtility.FromJson<Batch>(File.ReadAllText(path));File.Delete(path);foreach(var step in batch.steps)queue.Enqueue(step);}catch(Exception e){Debug.LogException(e);}}}
        if(active==null||now>=until){
            if(active!=null){File.AppendAllText(Path.Combine(Folder,"events.txt"),"END "+active.label+" "+Summary()+"\n");if(active.capture)ScreenCapture.CaptureScreenshot(Path.Combine(Folder,(++serial).ToString("D4")+"_"+active.label+".png"));}
            active=queue.Count>0?queue.Dequeue():null;held.Clear();mouse=false;
            if(active!=null){foreach(var name in active.keys.Split(new[]{',',' '},StringSplitOptions.RemoveEmptyEntries))held.Add((KeyCode)Enum.Parse(typeof(KeyCode),name));mouse=active.mouse;if(active.pointer)pointer=new Vector3(active.x,active.y,0);until=now+Mathf.Max(.035f,active.seconds);File.AppendAllText(Path.Combine(Folder,"events.txt"),"INPUT "+JsonUtility.ToJson(active)+" "+Summary()+"\n");}
        }
        if(now>=nextStatus){nextStatus=now+.4f;Report();}
    }
    string Summary(){var rt=PersistentRuntime.Instance;var p=rt?rt.GetComponent<GameControlManager>().ActivePlayer:null;return "t="+Time.realtimeSinceStartup.ToString("F2")+" scene="+SceneManager.GetActiveScene().name+" p="+(p?p.transform.position.ToString("F3"):"none")+" paused="+GamePause.IsPaused;}
    void Report(){
        var rows=new List<string>{Summary(),"active="+(active?.label??"idle")+" queued="+queue.Count+" size="+Screen.width+"x"+Screen.height};
        var rt=PersistentRuntime.Instance;if(!rt)return;var controls=rt.GetComponent<GameControlManager>();var p=controls.ActivePlayer;
        rows.Add("locked="+controls.IsLocked()+" save="+rt.GetComponent<GameSaveService>().CanSave(out var why)+" reason="+why);
        if(p){var target=p.GetComponent<PlayerInteraction>()?.CurrentVisualTarget as Component;rows.Add("target="+(target?PathOf(target.transform):"none"));}
        var story=rt.GetComponent<StoryStateManager>();if(story)rows.Add("flags="+string.Join(",",Enum.GetValues(typeof(StoryFlag)).Cast<StoryFlag>().Where(story.Has)));
        var dialogue=UnityEngine.Object.FindAnyObjectByType<DialogueManager>();if(dialogue){rows.Add("dialogue="+dialogue.IsDialogueActive());foreach(var field in typeof(DialogueManager).GetFields(Hidden))if(field.Name=="currentDialogue"||field.Name=="currentLineIndex")rows.Add(field.Name+"="+field.GetValue(dialogue));}
        foreach(var t in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude))if(t.isActiveAndEnabled&&!string.IsNullOrWhiteSpace(t.text))rows.Add("TEXT "+t.text.Replace("\n"," / "));
        foreach(var button in UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude))if(button.isActiveAndEnabled&&button.interactable){var rect=button.transform as RectTransform;var c=button.GetComponentInParent<Canvas>();var v=RectTransformUtility.WorldToScreenPoint(c&&c.renderMode!=RenderMode.ScreenSpaceOverlay?c.worldCamera:null,rect.TransformPoint(rect.rect.center));rows.Add("BUTTON "+PathOf(button.transform)+" screen="+v+" text="+button.GetComponentInChildren<TMP_Text>()?.text);}
        if(p)foreach(var m in UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Exclude))if(m is IInteractable i&&Mathf.Abs(m.transform.position.y-p.transform.position.y)<4)rows.Add("HOT "+PathOf(m.transform)+" p="+m.transform.position+" prompt="+i.GetInteractionPrompt());
        File.WriteAllLines(Path.Combine(Folder,"status.txt"),rows);
    }
    static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
}
