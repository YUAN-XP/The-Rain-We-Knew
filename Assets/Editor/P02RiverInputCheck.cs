#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Explicit editor-only input conductor. Observes authored notes and injects keys;
// never writes story flags, actors, judgement results, camera or sequence time.
public static class P02RiverInputCheck
{
    public static void Begin()=>P02RiverKeyDriver.Begin();
}
[DefaultExecutionOrder(-31000)]
public sealed class P02RiverKeyDriver : MonoBehaviour,GameplayInput.IReplaySource
{
    GameplayInput.IReplaySource previous;
    GuitarRhythmGame rhythm;
    Scene03RiverSequence river;
    readonly HashSet<KeyCode> held=new HashSet<KeyCode>();
    readonly List<string> rows=new List<string>();
    int note,downFrame=-1;KeyCode downKey;float nextDialogue;string lastDialogue;
    string folder;bool saved;
    public static void Begin(){
        if(!PersistentRuntime.Instance||GameplayInput.Replay==null)throw new InvalidOperationException("Isolated editor input required");
        if(FindAnyObjectByType<P02RiverKeyDriver>())throw new InvalidOperationException("Conductor already active");
        var check=new GameObject("P02 river key-input QA only").AddComponent<P02RiverKeyDriver>();
        check.previous=GameplayInput.Replay;check.rhythm=FindAnyObjectByType<GuitarRhythmGame>();check.river=FindAnyObjectByType<Scene03RiverSequence>();
        if(!check.rhythm||!check.river)throw new InvalidOperationException("Authored river components required");
        check.folder=P02ProductionInputReview.Evidence+"river_input/"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+"/";
        Directory.CreateDirectory(check.folder);
        File.WriteAllText(P02ProductionInputReview.Work+"active_river_input.txt",check.folder);
        check.rhythm.PhraseStarted+=check.Phrase;
        check.rhythm.Judged+=check.Judged;
        check.rows.Add("Editor-only scheduled keys through original GuitarRhythmGame; no completion or sequence override");
        GameplayInput.Replay=check;
    }
    void Phrase(int n){note=0;rows.Add("PHRASE "+n+" retries="+rhythm.RetryCount);}
    void Judged(KeyCode key,bool success,float time,string text){rows.Add("JUDGED "+rhythm.PhraseIndex+" "+key+" "+success+" time="+time+" "+text);}
    void Pulse(KeyCode key){held.Add(key);downKey=key;downFrame=Time.frameCount;}
    void Update(){
        held.Clear();
        if(GamePause.IsPaused||!Application.isFocused)return;
        if(rhythm&&rhythm.IsRunning){
            if(rhythm.WaitingForStart){Pulse(KeyCode.E);return;}
            if(rhythm.PhraseIndex>=rhythm.phrases.Length)return;
            var phrase=rhythm.phrases[rhythm.PhraseIndex];held.Add(phrase.heldChord);
            if(note<phrase.notes.Length&&rhythm.PhraseTime>=phrase.notes[note].time){Pulse(phrase.notes[note].key);note++;}
            return;
        }
        if(!river)return;
        var dialogue=FindAnyObjectByType<DialogueManager>();
        if(dialogue&&dialogue.IsDialogueActive()){
            string name=dialogue.CurrentDialogue?dialogue.CurrentDialogue.name:"unknown";
            if(name!=lastDialogue){lastDialogue=name;rows.Add("DIALOGUE "+name);ScreenCapture.CaptureScreenshot(folder+name+".png");}
            if(GameplayClock.Now>=nextDialogue){Pulse(KeyCode.Space);nextDialogue=GameplayClock.Now+.65f;}
        }
        if(river.Appreciating&&river.CanLeaveSong)Pulse(KeyCode.E);
        var story=PersistentRuntime.Instance.GetComponent<StoryStateManager>();
        if(story.Has(StoryFlag.Scene03ReturnedHome)&&!river.Busy){
            rows.Add("FINAL rhythm="+rhythm.Completed+" retries="+rhythm.RetryCount+" sequence="+story.Has(StoryFlag.Scene03RiverSequenceCompleted)+" returned="+story.Has(StoryFlag.Scene03ReturnedHome));
            ScreenCapture.CaptureScreenshot(folder+"returned_home.png");Save();Destroy(gameObject);
        }
    }
    void Save(){if(saved)return;saved=true;File.WriteAllLines(folder+"actual_input_results.txt",rows);}
    void OnDestroy(){if(rhythm){rhythm.PhraseStarted-=Phrase;rhythm.Judged-=Judged;}if(ReferenceEquals(GameplayInput.Replay,this))GameplayInput.Replay=previous;if(folder!=null)Save();}
    public bool Key(KeyCode key)=>held.Contains(key);
    public bool KeyDown(KeyCode key)=>downFrame==Time.frameCount&&downKey==key;
    public float Axis(string axis)=>0;
    public bool Mouse(int b)=>false;public bool MouseDown(int b)=>false;public bool MouseUp(int b)=>false;public Vector3 Pointer=>Vector3.zero;
}
#endif
