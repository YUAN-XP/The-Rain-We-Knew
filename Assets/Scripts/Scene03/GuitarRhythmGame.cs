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
    public float PhraseTime => GameplayClock.Now - phraseStarted;
    public event Action<KeyCode,bool,float,string> Judged;
    public event Action<int> PhraseStarted;
    RectTransform canvas, page, track;
    CanvasGroup group;
    TMP_Text title, chord, feedback, instruction;
    Image chordPlate;
    RectTransform[] tokens;
    bool[] judged;
    bool transitioning, ownsLock, hadFocus = true, finalStrokeAccepted;
    int missedMelodyNotes;
    float phraseStarted, lastChordHeldAt, feedbackUntil;
    string retryAdvice;
    RectTransform sustainTrack,sustainBar;
    Image sustainInk;
    readonly RhythmHoldTracker sustain = new RhythmHoldTracker();
    [Min(.65f)] public float releaseGraceSeconds=.65f;
    // Older scenes serialized 0.18s; use the relaxed rule without resaving scenes.
    public float ReleaseGraceSeconds => Mathf.Max(.65f, releaseGraceSeconds);
    public const int AllowedMelodyMisses = 1;
    [Min(2)] public float spaceApproachSeconds=6;
    public float ChordApproachSeconds=>approachSeconds+2;
    public float SpaceLeadSeconds=>ChordApproachSeconds+2+(phrases[Mathf.Clamp(PhraseIndex,0,3)].notes[phrases[Mathf.Clamp(PhraseIndex,0,3)].notes.Length-1].time-phrases[Mathf.Clamp(PhraseIndex,0,3)].notes[0].time);
    public float PreRollSeconds=>Mathf.Max(0,ChordApproachSeconds-phrases[PhraseIndex].notes[0].time,SpaceLeadSeconds-phrases[PhraseIndex].notes[phrases[PhraseIndex].notes.Length-1].time);
    P04GuitarHands hands;
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
        title.text="";
        instruction.text="按住右侧箭头；音符靠近金线时按对应键，最后按空格。";
        feedback.text="按E开始";chord.text="←";
    }
    void Build()
    {
        if(canvas!=null)Destroy(canvas.gameObject);
        canvas=RuntimePanelUI.Canvas("GuitarRhythmCanvas",transform,140);
        page=RuntimePanelUI.Panel("RiverGuitarCloseup",canvas,new Vector2(.045f,.045f),new Vector2(.955f,.955f),new Color(.17f,.25f,.23f,.98f)).rectTransform;
        group=page.gameObject.AddComponent<CanvasGroup>();
        RuntimePanelUI.Panel("RiverView",page,new Vector2(.025f,.65f),new Vector2(.72f,.98f),new Color(.46f,.66f,.62f));
        for(int i=0;i<4;i++)RuntimePanelUI.Panel("WaterGlint",page,new Vector2(.04f+i*.16f,.72f+(i%2)*.06f),new Vector2(.15f+i*.16f,.724f+(i%2)*.06f),new Color(.74f,.82f,.68f),false);
        var art=RuntimePanelUI.Rect("GuitarAndHands",page,new Vector2(.12f,.65f),new Vector2(.68f,.99f));hands=art.gameObject.AddComponent<P04GuitarHands>();hands.rhythm=this;
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
            var arrowLabel=RuntimePanelUI.Text(i==0?"←":"→",sustainTrack,chapter.font,new Vector2(x,0),new Vector2(x+.5f,.115f),26);
            arrowLabel.enableAutoSizing=true;arrowLabel.fontSizeMin=20;arrowLabel.fontSizeMax=26;
        }
        RuntimePanelUI.Panel("ChordJudgementLine",sustainTrack,new Vector2(.02f,.14f),new Vector2(.98f,.15f),new Color(.96f,.84f,.53f),false);
        sustainInk=RuntimePanelUI.Panel("ContinuousChordNote",sustainTrack,Vector2.zero,Vector2.one,new Color(.60f,.72f,.68f),false);
        sustainBar=sustainInk.rectTransform;sustainBar.gameObject.SetActive(false);
        DemoPaintedFrame.Add(page,new Color(.68f,.73f,.64f));
        feedback=Text("",.035f,.09f,.965f,.20f,26);
        feedback.enableAutoSizing=true;feedback.fontSizeMin=18;feedback.fontSizeMax=26;
        instruction=Text("",.035f,.015f,.965f,.09f,23);
        instruction.enableAutoSizing=true;instruction.fontSizeMin=17;instruction.fontSizeMax=23;
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
            RuntimePanelUI.Text(note.key==KeyCode.Space?"Space":note.key.ToString(),token,chapter.font,Vector2.zero,Vector2.one,25).color=new Color(.19f,.22f,.18f);
        }
        sustain.Reset(p.notes[0].time, p.notes[p.notes.Length-1].time, ReleaseGraceSeconds);
        missedMelodyNotes=0;transitioning=false;finalStrokeAccepted=false;phraseStarted=GameplayClock.Now+PreRollSeconds;
        lastChordHeldAt=float.NegativeInfinity;feedbackUntil=0;
        title.text="";
        chord.text=p.heldChord==KeyCode.LeftArrow?"←":"→";
        // Current review explicitly requests visible guidance, superseding the old hidden-feedback rule.
        feedback.enabled=true;
        instruction.text="按住 {0}；音符靠近金线时按键，最后按空格。".Replace("{0}",chord.text);
        if(!string.IsNullOrEmpty(retryAdvice))ShowFeedback(retryAdvice,true);
        else ShowFeedback("第 {0}/4 段 · 每段允许漏 1 个旋律音符。".Replace("{0}",(PhraseIndex+1).ToString()));
        retryAdvice=null;
        PhraseStarted?.Invoke(PhraseIndex);
    }
    // Overlapping direction keys during a chord change no longer rejects the required key.
    bool HeldCorrectly()=>GameplayInput.GetKey(phrases[PhraseIndex].heldChord);
    void ShowFeedback(string text,bool important=false)
    {
        if(!important&&GameplayClock.Now<feedbackUntil)return;
        feedback.enabled=true;feedback.text=text;
        feedback.color=important?new Color(1,.79f,.60f):new Color(.85f,.95f,.73f);
        if(important)feedbackUntil=GameplayClock.Now+2.6f;
    }
    string HoldAdvice()=>"请按住 {0}，再按音符键；这一拍还可以补。".Replace("{0}",chord.text);
    void Update()
    {
        if(GameplayInput.Blocked||!IsRunning||transitioning||!Application.isFocused)return;
        if(WaitingForStart){if(GameplayInput.GetKeyDown(KeyCode.E)){WaitingForStart=false;StartPhrase();}return;}
        if(!hadFocus){hadFocus=true;StartPhrase();}
        PlayFrame(HeldCorrectly(),PhraseTime,GameplayClock.Delta);
    }
    // The focused, unpaused Update above owns input admission. Keeping the
    // phrase step separate also permits isolated deterministic input regression.
    void PlayFrame(bool held,float time,float delta)
    {
        if(held){lastChordHeldAt=time;if(sustain.Failed)sustain.Reset(phrases[PhraseIndex].notes[0].time,phrases[PhraseIndex].notes[phrases[PhraseIndex].notes.Length-1].time,ReleaseGraceSeconds);}
        bool chordAccepted=held||time-lastChordHeldAt<=ReleaseGraceSeconds;
        chordPlate.color=held?new Color(.37f,.52f,.33f):new Color(.37f,.31f,.23f);
        if(!finalStrokeAccepted&&sustain.Tick(time,delta,held)){
            ShowFeedback("和弦松开太久：重新按住 {0}，接着弹即可。".Replace("{0}",chord.text),true);
        }
        DrawSustain(time,held);
        Tick(time);
        if(transitioning)return;
        foreach(var key in Keys)if(GameplayInput.GetKeyDown(key))Hit(key,chordAccepted,time);
    }
    void DrawSustain(float time,bool held)
    {
        if(finalStrokeAccepted){sustainBar.gameObject.SetActive(false);return;}
        var p=phrases[PhraseIndex];float head=.145f+(p.notes[0].time-time)/ChordApproachSeconds*.825f;
        float tail=.145f+(p.notes[p.notes.Length-1].time-time)/ChordApproachSeconds*.825f;
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
            float lead=p.notes[i].key==KeyCode.Space?SpaceLeadSeconds:approachSeconds;
            float normalized=.18f+(p.notes[i].time-time)/lead*.82f;
            // Keep a late note at the line while its forgiving window is still open.
            tokens[i].anchorMin=tokens[i].anchorMax=new Vector2(Mathf.Max(.18f,normalized),.90f-Array.IndexOf(Keys,p.notes[i].key)*.18f);
            tokens[i].anchoredPosition=Vector2.zero;tokens[i].gameObject.SetActive(!judged[i]&&normalized<=1);
            if(!judged[i]&&time>p.notes[i].time+p.Window(p.notes[i].key))Judge(i,false,time,"Miss · 漏拍");
        }
        if(time>p.notes[p.notes.Length-1].time+p.Window(KeyCode.Space)+p.tail)StartCoroutine(FinishPhrase());
    }
    void Hit(KeyCode key,bool held,float time)
    {
        var p=phrases[PhraseIndex];int nearest=-1;float delta=float.MaxValue;
        bool remaining=false;
        for(int i=0;i<p.notes.Length;i++)if(!judged[i])
        {
            remaining=true;
            if(p.notes[i].key!=key)continue;
            float distance=Mathf.Abs(p.notes[i].time-time);
            // Wider windows can overlap repeated W notes. Credit the oldest
            // eligible note so a late tap cannot steal the following beat.
            if(distance<=p.Window(key)){nearest=i;delta=distance;break;}
            if(distance<delta){nearest=i;delta=distance;}
        }
        if(!remaining)return;
        if(nearest<0){ShowFeedback("刚才多按的键不扣分，跟着还亮着的音符继续。",true);return;}
        if(delta>p.Window(key)){
            ShowFeedback((time<p.notes[nearest].time?"{0} 按早了：等它靠近金线再按，这次不扣分。":"{0} 已错过：跟着下一个音符继续。每段允许漏 1 个旋律音符。").Replace("{0}",key.ToString()),true);
            return;
        }
        // A mistimed chord can be corrected and tapped again within this note's window.
        if(!held){ShowFeedback(HoldAdvice(),true);return;}
        Judge(nearest,true,time,delta<=p.perfectWindow?"Perfect":"Good");
    }
    void Judge(int index,bool success,float time,string text)
    {
        judged[index]=true;tokens[index].gameObject.SetActive(false);
        var key=phrases[PhraseIndex].notes[index].key;
        if(!success&&key!=KeyCode.Space)missedMelodyNotes++;
        // The final strum accepts the same early/late Good window as every note.
        // Once accepted, releasing the chord must not add a new sustain miss
        // before the nominal end time. Melody misses are counted separately.
        if(success&&index==phrases[PhraseIndex].notes.Length-1&&phrases[PhraseIndex].notes[index].key==KeyCode.Space)
            finalStrokeAccepted=true;
        if(!success)ShowFeedback(key==KeyCode.Space?"空格拍弦漏掉了：按住箭头，空格靠近金线时再按。":"{0} 漏拍：接着弹即可，本段允许漏 1 个旋律音符。".Replace("{0}",key.ToString()),true);
        else ShowFeedback(key==KeyCode.Space?"拍弦成功，可以松开和弦了。":text=="Perfect"?"完美":"良好");
        if(success&&phrases[PhraseIndex].notes[index].tone!=null)instrument.PlayOneShot(phrases[PhraseIndex].notes[index].tone,.65f);
        Judged?.Invoke(phrases[PhraseIndex].notes[index].key,success,time,text);
    }
    IEnumerator FinishPhrase()
    {
        transitioning=true;
        if(!finalStrokeAccepted||missedMelodyNotes>AllowedMelodyMisses)
        {
            RetryCount++;
            retryAdvice=!finalStrokeAccepted?"本段重试：最后的空格没有拍到。按住箭头，在空格靠近金线时按下。":"本段漏了 {0} 个旋律音符，已超过 1 个的容错。再试一次，前面的段落保留。".Replace("{0}",missedMelodyNotes.ToString());
            ShowFeedback(retryAdvice,true);
            yield return GameplayClock.Wait(2.8f);StartPhrase();yield break;
        }
        PhraseIndex++;
        if(PhraseIndex<phrases.Length)
        {feedbackUntil=0;ShowFeedback(missedMelodyNotes>0?"这一段通过了，已容错 1 个漏拍。":"这一段弹好了");yield return GameplayClock.Wait(1.2f);StartPhrase();yield break;}
        feedbackUntil=0;ShowFeedback("让这首歌继续吧。");
        for(float t=0;t<.7f;t+=GameplayClock.Delta){group.alpha=1-t/.7f;yield return null;}
        page.gameObject.SetActive(false);Completed=true;IsRunning=false;transitioning=false;Release();
    }
    void OnApplicationFocus(bool focus){ /* Global pause preserves the exact phrase instead of restarting it. */ }
    void Release(){if(ownsLock&&chapter!=null&&chapter.Controls!=null)chapter.Controls.ReleaseLock(ControlLockReason.CloseUp);ownsLock=false;}
    void OnDisable(){StopAllCoroutines();IsRunning=false;Release();if(canvas!=null)canvas.gameObject.SetActive(false);}
}



