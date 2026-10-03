using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

// Opt-in editor QA supplement: uses existing UI callbacks; snapshots never overwrite user saves.
// It does not change pause/focus handling, story flags, transforms or puzzle results.
public static class P02MainChainReview
{
    const string Work="D:/游戏设计/Codex工作区/work/2026-10-03-P02/main_chain_PV20/";
    static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
    static void RequireReplay(){if(!EditorApplication.isPlaying||GameplayInput.Replay==null)throw new InvalidOperationException("Isolated input replay required");}
    public static void ClickButton(){
        RequireReplay();
        if(GamePause.IsPaused)throw new InvalidOperationException("Original game pause must be resumed in the foreground");
        string path=File.ReadAllText(Work+"button_request.txt").Trim();
        var found=UnityEngine.Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude).Where(b=>b.isActiveAndEnabled&&b.interactable&&PathOf(b.transform)==path).ToArray();
        if(found.Length!=1)throw new InvalidOperationException("Expected one authored active button: "+path);
        File.AppendAllText(Work+"ui_supplement.txt",DateTime.Now.ToString("O")+" original Button.onClick "+path+"\n");
        found[0].onClick.Invoke();P01InputReplay.Snapshot();
    }
    public static void EnterPuzzleText(){
        RequireReplay();if(GamePause.IsPaused)throw new InvalidOperationException("Original pause is active");
        var found=UnityEngine.Object.FindObjectsByType<TMP_InputField>(FindObjectsInactive.Exclude).Where(f=>f.isActiveAndEnabled&&f.interactable).ToArray();
        if(found.Length!=1)throw new InvalidOperationException("Expected one active in-game puzzle input");
        found[0].text=File.ReadAllText(Work+"puzzle_text.txt").Trim();
        File.AppendAllText(Work+"ui_supplement.txt",DateTime.Now.ToString("O")+" original in-game puzzle input "+PathOf(found[0].transform)+"\n");
        P01InputReplay.Snapshot();
    }
    public static void CaptureSnapshot(){
        RequireReplay();var saves=PersistentRuntime.Instance.GetComponent<GameSaveService>();
        var label=SceneManager.GetActiveScene().name+"_"+DateTime.Now.ToString("HHmmss");
        var data=saves.Capture(4);File.WriteAllText(Work+label+"_snapshot.json",JsonUtility.ToJson(data,true));
        ScreenCapture.CaptureScreenshot(Work+label+"_full_ui.png");P01InputReplay.Snapshot();
        File.Copy(P01InputReplay.Folder+"/status.txt",Work+label+"_status.txt",true);
    }
    public static void RestoreNaturalSnapshot(){
        RequireReplay();var saves=PersistentRuntime.Instance.GetComponent<GameSaveService>();
        var data=JsonUtility.FromJson<GameSaveData>(File.ReadAllText(Work+"natural_pre_fog_snapshot.json"));
        // A fresh dedicated QA directory: never overwrite any existing slot.
        string directory=Work+"isolated_resume_"+Guid.NewGuid().ToString("N");
        var store=new FourSlotSaveStore(directory);
        typeof(GameSaveService).GetProperty("Store").GetSetMethod(true).Invoke(saves,new object[]{store});
        store.Write(4,data,false);
        if(!saves.Load(4,out var why))throw new InvalidOperationException(why);
        File.AppendAllText(Work+"ui_supplement.txt",DateTime.Now.ToString("O")+" restored captured natural pre-fog state through original load API; new QA directory "+directory+"\n");
    }
}
