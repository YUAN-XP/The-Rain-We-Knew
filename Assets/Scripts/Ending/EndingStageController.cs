using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Common ownership for ending-local clocks, dialogue and pause. Never clears other locks.
public abstract class EndingStageController : MonoBehaviour
{
    public EndingData data;
    protected PersistentRuntime runtime;
    protected EndingSession session;
    protected StoryStateManager story;
    protected GameControlManager controls;
    protected DialogueManager dialogue;
    protected InventoryManager inventory;
    protected FileArchiveManager files;
    protected bool ready, busy, paused, sequenceLock;
    protected DialogueData ownedDialogue;
    protected bool speechCompleted;
    GameObject pausePage;
    AudioSource[] pausedSounds;
    protected float Delta => paused?0:Time.unscaledDeltaTime;
    public bool IsPaused=>paused;
    public bool IsBusy=>busy;
    protected virtual void OnEnable(){StartCoroutine(Initialize());}
    IEnumerator Initialize(){
        yield return null;
        runtime=PersistentRuntime.Instance;
        if(runtime==null||data==null){Debug.LogWarning("Ending requires the original runtime and its authored data.",this);yield break;}
        controls=runtime.GetComponent<GameControlManager>();story=runtime.GetComponent<StoryStateManager>();
        inventory=runtime.GetComponent<InventoryManager>();files=runtime.GetComponent<FileArchiveManager>();dialogue=runtime.GetComponentInChildren<DialogueManager>(true);
        session=runtime.GetComponent<EndingSession>()??runtime.gameObject.AddComponent<EndingSession>();session.Configure(data);
        var c=RuntimePanelUI.Canvas("EndingPause",transform,600);pausePage=c.gameObject;
        RuntimePanelUI.Panel("暂停",c,Vector2.zero,Vector2.one,new Color(0,0,0,.87f));
        RuntimePanelUI.Text("暂停",c,data.font,new Vector2(.25f,.55f),new Vector2(.75f,.7f),42);
        RuntimePanelUI.Button("继续",c,data.font,new Vector2(.4f,.38f),new Vector2(.6f,.49f),()=>Pause(false));
        pausePage.SetActive(false);ready=true;yield return Begin();
    }
    protected abstract IEnumerator Begin();
    protected virtual bool PauseAvailable=>busy;
    protected virtual void Update(){if(ready&&PauseAvailable&&Input.GetKeyDown(KeyCode.Escape))Pause(!paused);}
    protected virtual void Pause(bool value){
        if(paused==value||!ready)return;paused=value;pausePage.SetActive(value);dialogue.SetInputPaused(this,value);
        if(value){controls.AcquireLock(ControlLockReason.EndingPause);var list=new List<AudioSource>();foreach(var a in GetComponentsInChildren<AudioSource>(true))if(a.isPlaying){a.Pause();list.Add(a);}pausedSounds=list.ToArray();}
        else {controls.ReleaseLock(ControlLockReason.EndingPause);if(pausedSounds!=null)foreach(var a in pausedSounds)if(a!=null)a.UnPause();pausedSounds=null;}
    }
    void OnApplicationFocus(bool focus){if(!focus&&ready&&PauseAvailable)Pause(true);}
    protected void Lock(){
        runtime.GetComponentInChildren<InventoryUI>(true)?.CloseInventory();
        var camera=runtime.GetComponent<CameraModeController>();camera?.AbortCapture();camera?.Lower();runtime.GetComponent<OilLampController>()?.LowerLamp();
        controls.AcquireLock(ControlLockReason.EndingSequence);sequenceLock=true;busy=true;
    }
    protected void Unlock(){if(sequenceLock&&controls!=null)controls.ReleaseLock(ControlLockReason.EndingSequence);sequenceLock=false;busy=false;}
    protected IEnumerator Wait(float seconds){float t=0;while(t<seconds){t+=Delta;yield return null;}}
    protected IEnumerator FreshKeys(){yield return null;while(paused||Input.GetKey(KeyCode.E)||Input.GetKey(KeyCode.Space))yield return null;yield return null;}
    protected IEnumerator Say(string id){
        speechCompleted=false;var line=data.Find(id);
        if(line==null){Debug.LogError("Missing ending semantic cue: "+id,this);yield break;}
        while(dialogue.IsDialogueActive()||paused)yield return null;
        yield return FreshKeys();ownedDialogue=line;int before=dialogue.CompletedCount;
        dialogue.StartDialogue(line);yield return null;
        while(dialogue.IsDialogueActive()&&dialogue.CurrentDialogue==line)yield return null;
        speechCompleted=dialogue.CompletedCount>before&&dialogue.LastCompletedDialogue==line;ownedDialogue=null;
    }
    protected IEnumerator Move(Transform actor,Vector3 target,float seconds){
        Vector3 start=actor.position;float t=0;while(t<seconds){t+=Delta;actor.position=Vector3.Lerp(start,target,Mathf.SmoothStep(0,1,Mathf.Clamp01(t/seconds)));yield return null;}actor.position=target;
    }
    protected AudioSource Sound(AudioClip clip,bool loop=false,float volume=.25f){
        if(clip==null){Debug.LogWarning("Ending Alpha audio slot missing; timed presentation continues.",this);return null;}
        var a=gameObject.AddComponent<AudioSource>();a.clip=clip;a.loop=loop;a.volume=volume;a.Play();return a;
    }
    protected virtual void OnDisable(){
        StopAllCoroutines();if(dialogue!=null){dialogue.SetInputPaused(this,false);dialogue.CancelDialogue(ownedDialogue);}ownedDialogue=null;
        if(paused&&controls!=null)controls.ReleaseLock(ControlLockReason.EndingPause);paused=false;
        Unlock();foreach(var a in GetComponents<AudioSource>())a.Stop();
        if(pausePage!=null)Destroy(pausePage);ready=false;
    }
}
