using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GuitarRhythmGame : MonoBehaviour
{
    public RhythmPhraseData[] phrases = new RhythmPhraseData[4];
    public Scene03Chapter chapter;
    public AudioSource instrument;
    [Min(.5f)] public float approachSeconds = 2;
    public bool IsRunning {get;private set;}
    public bool Completed {get;private set;}
    public bool WaitingForStart {get;private set;}
    public int PhraseIndex {get;private set;}
    public int RetryCount {get;private set;}
    public float PhraseTime => Time.unscaledTime - phraseStarted;
    public event Action<KeyCode,bool,float,string> Judged;
    public event Action<int> PhraseStarted;
    RectTransform canvas, page, track;
    CanvasGroup group;
    TMP_Text title, chord, feedback, instruction;
    Image chordPlate;
    RectTransform[] tokens;
    bool[] judged;
    bool missed, transitioning, ownsLock, hadFocus = true;
    float phraseStarted;
    RectTransform sustainTrack,sustainBar;
    Image sustainInk;
    readonly RhythmHoldTracker sustain = new RhythmHoldTracker();
    [Min(.1f)] public float releaseGraceSeconds=.18f;
    [Min(2)] public float spaceApproachSeconds=3.2f;
    public bool SustainedChordFailed => sustain.Failed;
    static readonly KeyCode[] Keys={KeyCode.W,KeyCode.S,KeyCode.A,KeyCode.D,KeyCode.Space};

    public bool ValidateConfiguration()
    {
        if(chapter==null || instrument==null || phrases==null || phrases.Length!=4)return false;
        for(int i=0;i<4;i++)if(phrases[i]==null||!phrases[i].IsValid()||phrases[i].heldChord!=(i==0||i==3?KeyCode.LeftArrow:KeyCode.RightArrow))return false;
        return true;
    }
    public void Begin()
    {
        if(IsRunning)return;
        if(!ValidateConfiguration()){Debug.LogWarning("Guitar rhythm needs four ordered, valid phrases and an instrument source.",this);return;}
        IsRunning=true;Completed=false;PhraseIndex=0;RetryCount=0;
        chapter.Controls.AcquireLock(ControlLockReason.CloseUp);ownsLock=true;
        Build();WaitingForStart=true;
        title.text="河堤边 · 生日快乐";
        instruction.text="右侧长条到横线后，持续按住对应方向键\n左侧音符到竖线时按键；段尾松开 W/S/A/D，再按 Space";
        feedback.text="按 [E] 开始";chord.text="第一段：持续按住 ←";
    }
    void Build()
    {
        if(canvas!=null)Destroy(canvas.gameObject);
        canvas=RuntimePanelUI.Canvas("GuitarRhythmCanvas",transform,140);
        page=RuntimePanelUI.Panel("RiverGuitarCloseup",canvas,new Vector2(.045f,.045f),new Vector2(.955f,.955f),new Color(.17f,.25f,.23f,.98f)).rectTransform;
        group=page.gameObject.AddComponent<CanvasGroup>();
        RuntimePanelUI.Panel("RiverView",page,new Vector2(.025f,.65f),new Vector2(.72f,.98f),new Color(.46f,.66f,.62f));
        for(int i=0;i<4;i++)RuntimePanelUI.Panel("WaterGlint",page,new Vector2(.04f+i*.16f,.72f+(i%2)*.06f),new Vector2(.15f+i*.16f,.724f+(i%2)*.06f),new Color(.74f,.82f,.68f),false);
        var art=RuntimePanelUI.Rect("GuitarAndHands",page,new Vector2(.12f,.65f),new Vector2(.68f,.99f));art.gameObject.AddComponent<GuitarCloseupGraphic>().raycastTarget=false;
        title=Text("",.74f,.9f,.98f,.98f,28);
        chordPlate=RuntimePanelUI.Panel("HeldChord",page,new Vector2(.75f,.72f),new Vector2(.97f,.89f),new Color(.27f,.34f,.29f));
        chord=RuntimePanelUI.Text("",chordPlate.transform,chapter.font,new Vector2(.02f,.05f),new Vector2(.98f,.95f),30);
        track=RuntimePanelUI.Rect("FiveLanes",page,new Vector2(.06f,.20f),new Vector2(.70f,.65f));
        for(int i=0;i<5;i++)
        {
            float y=.90f-i*.18f;
            RuntimePanelUI.Panel("Lane",track,new Vector2(.10f,y-.055f),new Vector2(1,y+.055f),new Color(.28f,.35f,.31f),false);
            RuntimePanelUI.Text(i==4?"Space":Keys[i].ToString(),track,chapter.font,new Vector2(-.03f,y-.065f),new Vector2(.085f,y+.065f),i==4?23:30);
        }
        RuntimePanelUI.Panel("JudgementLine",track,new Vector2(.177f,.09f),new Vector2(.18f,.97f),new Color(.96f,.84f,.53f),false);
        sustainTrack=RuntimePanelUI.Rect("SustainedChordLanes",page,new Vector2(.75f,.20f),new Vector2(.96f,.65f));
        for(int i=0;i<2;i++){
            float x=i*.5f;
            RuntimePanelUI.Panel("ChordLane",sustainTrack,new Vector2(x+.045f,.12f),new Vector2(x+.455f,.97f),new Color(.20f,.27f,.28f),false);
            RuntimePanelUI.Text(i==0?"←":"→",sustainTrack,chapter.font,new Vector2(x,0),new Vector2(x+.5f,.115f),30);
        }
        RuntimePanelUI.Panel("ChordJudgementLine",sustainTrack,new Vector2(.02f,.14f),new Vector2(.98f,.15f),new Color(.96f,.84f,.53f),false);
        sustainInk=RuntimePanelUI.Panel("ContinuousChordNote",sustainTrack,Vector2.zero,Vector2.one,new Color(.60f,.72f,.68f),false);
        sustainBar=sustainInk.rectTransform;sustainBar.gameObject.SetActive(false);
        DemoPaintedFrame.Add(page,new Color(.68f,.73f,.64f));
        feedback=Text("",.05f,.105f,.95f,.20f,30);
        instruction=Text("",.04f,.015f,.96f,.105f,24);
    }
    TMP_Text Text(string text,float x,float y,float xx,float yy,float size)=>RuntimePanelUI.Text(text,page,chapter.font,new Vector2(x,y),new Vector2(xx,yy),size);
    void StartPhrase()
    {
        if(tokens!=null)foreach(var t in tokens)if(t!=null)Destroy(t.gameObject);
        var p=phrases[PhraseIndex];tokens=new RectTransform[p.notes.Length];judged=new bool[p.notes.Length];
        for(int i=0;i<tokens.Length;i++)
        {
            var note=p.notes[i];int lane=Array.IndexOf(Keys,note.key);
            var token=RuntimePanelUI.Panel(note.key.ToString(),track,Vector2.zero,Vector2.zero,new Color(.91f,.72f,.37f),false).rectTransform;
            token.sizeDelta=new Vector2(note.key==KeyCode.Space?76:44,42);tokens[i]=token;
            RuntimePanelUI.Text(note.key==KeyCode.Space?"拍":note.key.ToString(),token,chapter.font,Vector2.zero,Vector2.one,25).color=new Color(.19f,.22f,.18f);
        }
        sustain.Reset(p.notes[0].time, p.notes[p.notes.Length-1].time, releaseGraceSeconds);
        missed=false;transitioning=false;phraseStarted=Time.unscaledTime;
        title.text=$"第 {PhraseIndex+1} / 4 段";
        chord.text="持续按住\n"+(p.heldChord==KeyCode.LeftArrow?"←":"→");
        feedback.text="跟着琴弦慢慢来";
        instruction.text="音符到竖线时按键 · 段尾松开 W/S/A/D 后按 Space · Good / Perfect 均可通过";
        PhraseStarted?.Invoke(PhraseIndex);
    }
    bool HeldCorrectly()=>Input.GetKey(phrases[PhraseIndex].heldChord)&&!Input.GetKey(phrases[PhraseIndex].heldChord==KeyCode.LeftArrow?KeyCode.RightArrow:KeyCode.LeftArrow);
    void Update()
    {
        if(!IsRunning||transitioning||!Application.isFocused)return;
        if(WaitingForStart){if(Input.GetKeyDown(KeyCode.E)){WaitingForStart=false;StartPhrase();}return;}
        if(!hadFocus){hadFocus=true;StartPhrase();}
        bool held=HeldCorrectly();chordPlate.color=held?new Color(.37f,.52f,.33f):new Color(.37f,.31f,.23f);
        if(sustain.Tick(PhraseTime,Time.unscaledDeltaTime,held)){
            missed=true;feedback.text="Miss · 长条结束前请持续按住和弦";
            feedback.color=new Color(1,.70f,.55f);Judged?.Invoke(phrases[PhraseIndex].heldChord,false,PhraseTime,"Miss · 和弦中断");
        }
        DrawSustain(PhraseTime,held);
        Tick(PhraseTime);
        if(transitioning)return;
        foreach(var key in Keys)if(Input.GetKeyDown(key))Hit(key,held,PhraseTime);
    }
    void DrawSustain(float time,bool held)
    {
        var p=phrases[PhraseIndex];float head=.145f+(p.notes[0].time-time)/approachSeconds*.80f;
        float tail=.145f+(p.notes[p.notes.Length-1].time-time)/approachSeconds*.80f;
        float x=p.heldChord==KeyCode.LeftArrow?.09f:.59f;
        sustainBar.gameObject.SetActive(head<=.97f&&tail>=.145f);
        sustainBar.anchorMin=new Vector2(x,Mathf.Clamp(head,.145f,.97f));
        sustainBar.anchorMax=new Vector2(x+.32f,Mathf.Clamp(tail,.145f,.97f));
        sustainBar.offsetMin=sustainBar.offsetMax=Vector2.zero;
        sustainInk.color=sustain.Failed?new Color(.66f,.34f,.32f):held?new Color(.84f,.86f,.65f):new Color(.51f,.67f,.68f);
    }
    void Tick(float time)
    {
        var p=phrases[PhraseIndex];
        for(int i=0;i<p.notes.Length;i++)
        {
            float lead=p.notes[i].key==KeyCode.Space?Mathf.Max(approachSeconds,spaceApproachSeconds):approachSeconds;
            float normalized=.18f+(p.notes[i].time-time)/lead*.78f;
            tokens[i].anchorMin=tokens[i].anchorMax=new Vector2(normalized,.90f-Array.IndexOf(Keys,p.notes[i].key)*.18f);
            tokens[i].anchoredPosition=Vector2.zero;tokens[i].gameObject.SetActive(!judged[i]&&normalized>=.04f&&normalized<=1);
            if(!judged[i]&&time>p.notes[i].time+p.goodWindow)Judge(i,false,time,"Miss · 漏拍");
        }
        if(time>p.notes[p.notes.Length-1].time+p.goodWindow+p.tail)StartCoroutine(FinishPhrase());
    }
    void Hit(KeyCode key,bool held,float time)
    {
        var p=phrases[PhraseIndex];int nearest=-1;float delta=float.MaxValue;
        bool remaining=false;
        for(int i=0;i<p.notes.Length;i++)if(!judged[i])
        {
            remaining=true;
            if(p.notes[i].key==key&&Mathf.Abs(p.notes[i].time-time)<delta){nearest=i;delta=Mathf.Abs(p.notes[i].time-time);}
        }
        if(!remaining)return;
        if(nearest<0||delta>p.goodWindow)
        {missed=true;feedback.text="Miss · 跟着当前音符";feedback.color=new Color(1,.70f,.55f);Judged?.Invoke(key,held,time,"Miss · 错键或时机");return;}
        bool released=true;
        if(key==KeyCode.Space)foreach(var noteKey in Keys)if(noteKey!=KeyCode.Space&&Input.GetKey(noteKey))released=false;
        if(!released){Judge(nearest,false,time,"Miss · 拍弦前请松开 W/S/A/D");return;}
        bool ok=held;
        Judge(nearest,ok,time,!held?"Miss · 请持续按住正确和弦":delta<=p.perfectWindow?"Perfect":"Good");
    }
    void Judge(int index,bool success,float time,string text)
    {
        judged[index]=true;missed|=!success;tokens[index].gameObject.SetActive(false);
        feedback.text=text;feedback.color=success?new Color(.80f,.94f,.63f):new Color(1,.70f,.55f);
        if(success&&phrases[PhraseIndex].notes[index].tone!=null)instrument.PlayOneShot(phrases[PhraseIndex].notes[index].tone,.65f);
        Judged?.Invoke(phrases[PhraseIndex].notes[index].key,success,time,text);
    }
    IEnumerator FinishPhrase()
    {
        transitioning=true;
        if(missed)
        {
            RetryCount++;feedback.text="这一段再来一次，前面弹好的都还在。";
            yield return new WaitForSecondsRealtime(1.4f);StartPhrase();yield break;
        }
        PhraseIndex++;
        if(PhraseIndex<phrases.Length)
        {feedback.text="这一段弹好了";yield return new WaitForSecondsRealtime(1.2f);StartPhrase();yield break;}
        feedback.text="让这首歌继续吧。";
        for(float t=0;t<.7f;t+=Time.unscaledDeltaTime){group.alpha=1-t/.7f;yield return null;}
        page.gameObject.SetActive(false);Completed=true;IsRunning=false;transitioning=false;Release();
    }
    void OnApplicationFocus(bool focus){if(!focus&&IsRunning&&!WaitingForStart)hadFocus=false;}
    void Release(){if(ownsLock&&chapter!=null&&chapter.Controls!=null)chapter.Controls.ReleaseLock(ControlLockReason.CloseUp);ownsLock=false;}
    void OnDisable(){StopAllCoroutines();IsRunning=false;Release();if(canvas!=null)canvas.gameObject.SetActive(false);}
}
