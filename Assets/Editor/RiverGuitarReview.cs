using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Explicit opt-in empty-scene Play Mode regression; never rewrites scenes or story state.
public static class RiverGuitarReview
{
    public const string Work="D:/游戏设计/Codex工作区/work/2026-10-15-GuitarAssist/";
    const string Session="RiverGuitarReview.Assist";
    static readonly BindingFlags Hidden=BindingFlags.Instance|BindingFlags.NonPublic;
    static readonly MethodInfo StartPhrase=typeof(GuitarRhythmGame).GetMethod("StartPhrase",Hidden);
    static readonly MethodInfo Play=typeof(GuitarRhythmGame).GetMethod("PlayFrame",Hidden);
    static readonly MethodInfo Held=typeof(GuitarRhythmGame).GetMethod("HeldCorrectly",Hidden);
    static readonly FieldInfo Transition=typeof(GuitarRhythmGame).GetField("transitioning",Hidden);
    static readonly string[] Cases={"Centered","EarlyWithinWindow","LateWithinWindow","EarlyStrumThenRelease","ShortChordGap","NoteDuringBriefRelease","RecoverLongChordGap","OneMissAllowed","TwoMissesRetry","WrongChord","ExtraEarlyKey","ExtraKeyWithCorrectNote","NoteKeyHeldAtStrum","BothArrowsOverlap","StrumTooEarly","StrumTooLate","NoStrum","NoNotes","ChordCorrectedWithinWindow","SecondPhraseRetryRecovery","FeedbackPersistence","ReleaseJustBeforeStrum"};
    static readonly List<string> Checks=new List<string>(),Events=new List<string>();
    static GameObject fixture;
    static GuitarRhythmGame guitar;
    static Replay replay;
    static int current,note,lastPhrase=-1,lastRetry=-1,hits,misses;
    static double clock;
    static bool tapped,extraDone,retapped,captured,checkedLateToken;
    static string savedPreference;
    static bool hadPreference;
    sealed class Replay:GameplayInput.IReplaySource
    {
        public readonly HashSet<KeyCode> held=new HashSet<KeyCode>(),down=new HashSet<KeyCode>();
        public bool Key(KeyCode key)=>held.Contains(key);public bool KeyDown(KeyCode key)=>down.Contains(key);
        public float Axis(string axis)=>0;public bool Mouse(int b)=>false;public bool MouseDown(int b)=>false;public bool MouseUp(int b)=>false;public Vector3 Pointer=>Vector3.zero;
    }
    public static void After(){Run();}
    public static void Run()
    {
        if(EditorApplication.isPlaying)throw new Exception("Edit mode required.");
        Directory.CreateDirectory(Work+"Results");Directory.CreateDirectory(Work+"Previews");
        hadPreference=PlayerPrefs.HasKey(GameLocalization.PreferenceKey);savedPreference=PlayerPrefs.GetString(GameLocalization.PreferenceKey,"");
        SessionState.SetBool(Session+".HadPreference",hadPreference);SessionState.SetString(Session+".Preference",savedPreference);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        SessionState.SetBool(Session,true);EditorApplication.update-=Tick;EditorApplication.update+=Tick;EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Resume(){if(SessionState.GetBool(Session,false))EditorApplication.update+=Tick;}
    static void Check(bool ok,string message){Checks.Add((ok?"PASS ":"FAIL ")+Cases[current]+" "+message);}
    static string Feedback=>((TMP_Text)typeof(GuitarRhythmGame).GetField("feedback",Hidden).GetValue(guitar)).text;
    static void Setup()
    {
        if(!Camera.main){var camera=new GameObject("Isolated review camera").AddComponent<Camera>();camera.tag="MainCamera";camera.gameObject.AddComponent<CameraFollow>();camera.gameObject.AddComponent<AudioListener>();}
        fixture=new GameObject("Guitar assist case "+Cases[current]);
        var controls=fixture.AddComponent<GameControlManager>();var chapter=fixture.AddComponent<Scene03Chapter>();chapter.enabled=false;chapter.font=P07UI.Font;
        typeof(Scene03Chapter).GetProperty("Controls").GetSetMethod(true).Invoke(chapter,new object[]{controls});
        guitar=fixture.AddComponent<GuitarRhythmGame>();guitar.chapter=chapter;guitar.instrument=fixture.AddComponent<AudioSource>();guitar.enabled=false;
        guitar.phrases=Enumerable.Range(1,4).Select(i=>AssetDatabase.LoadAssetAtPath<RhythmPhraseData>("Assets/GameData/Scene03/RhythmPhrase"+i+".asset")).ToArray();
        if(!guitar.ValidateConfiguration())throw new Exception("Authored chart configuration invalid.");
        replay=new Replay();GameplayInput.Replay=replay;clock=0;GameplayClock.Restore(clock);
        lastPhrase=lastRetry=-1;hits=misses=0;checkedLateToken=false;
        guitar.Judged+=(key,ok,time,reason)=>{if(ok)hits++;else misses++;Events.Add(Cases[current]+" phrase="+guitar.PhraseIndex+" key="+key+" hit="+ok+" t="+time.ToString("F4")+" reason="+reason);};
        guitar.Begin();typeof(GuitarRhythmGame).GetProperty("WaitingForStart").GetSetMethod(true).Invoke(guitar,new object[]{false});StartPhrase.Invoke(guitar,null);
    }
    static void Tick()
    {
        if(!SessionState.GetBool(Session,false)){EditorApplication.update-=Tick;return;}
        if(!EditorApplication.isPlaying||EditorApplication.isCompiling){EditorApplication.QueuePlayerLoopUpdate();return;}
        try
        {
            if(!guitar)Setup();
            for(int step=0;step<12;step++)
            {
                if(Done()){Teardown();current++;if(current==Cases.Length){Finish();return;}Setup();}
                clock+=1d/120;GameplayClock.Restore(clock);
                if((bool)Transition.GetValue(guitar)||guitar.PhraseIndex>=4)continue;
                if(lastPhrase!=guitar.PhraseIndex||lastRetry!=guitar.RetryCount){lastPhrase=guitar.PhraseIndex;lastRetry=guitar.RetryCount;note=0;tapped=extraDone=retapped=false;Events.Add(Cases[current]+" START phrase="+lastPhrase+" retries="+lastRetry);}
                var phrase=guitar.phrases[guitar.PhraseIndex];float time=guitar.PhraseTime;
                replay.held.Clear();replay.down.Clear();
                bool held=!(current==3&&tapped)&&current!=9;
                if(current==4&&time>3.10f&&time<3.60f||current==5&&time>1.90f&&time<2.30f||current==6&&time>3.0f&&time<4.0f||current==18&&time>1.95f&&time<2.20f||current==21&&time>phrase.notes[phrase.notes.Length-1].time-.15f)held=false;
                if(held)replay.held.Add(phrase.heldChord);
                if(current==9||current==13)replay.held.Add(phrase.heldChord==KeyCode.LeftArrow?KeyCode.RightArrow:KeyCode.LeftArrow);
                if((current==10||current==11)&&!extraDone&&time>=(current==10?.5f:2f)){replay.down.Add(KeyCode.D);extraDone=true;}
                if(current==18&&!retapped&&time>=2.25f){replay.down.Add(KeyCode.W);retapped=true;}
                if(note<phrase.notes.Length)
                {
                    var n=phrase.notes[note];float offset=current==1?-.50f:current==2?.50f:0;
                    if(n.key==KeyCode.Space)offset=current==1||current==3?-.80f:current==2?.80f:current==14?-1f:current==15?1f:0;
                    if(time>=n.time+offset)
                    {
                        bool skip=current==17||((current==7||current==20)&&note==0)||(current==8&&note<2)||(current==19&&lastPhrase==1&&lastRetry==0&&note<2)||(current==16&&n.key==KeyCode.Space);
                        if(!skip){replay.down.Add(n.key);replay.held.Add(n.key);}
                        if(n.key==KeyCode.Space){tapped=true;if(current==12)replay.held.Add(KeyCode.W);}
                        note++;
                    }
                }
                Play.Invoke(guitar,new object[]{(bool)Held.Invoke(guitar,null),time,1f/120});
                if(current==2&&!checkedLateToken&&time>2.10f&&time<2.40f){var tokens=(RectTransform[])typeof(GuitarRhythmGame).GetField("tokens",Hidden).GetValue(guitar);Check(tokens[0].gameObject.activeSelf&&Mathf.Abs(tokens[0].anchorMin.x-.18f)<.001f,"late note stays visible at judgement line");checkedLateToken=true;}
                if(current==20&&!captured&&lastPhrase==0&&time>2.95f){Check(Feedback.Contains("W 漏拍"),"specific missed-key hint survives the next successful notes");CapturePreviews();captured=true;}
            }
            EditorApplication.QueuePlayerLoopUpdate();
        }
        catch(Exception ex){File.WriteAllText(Work+"Results/error.txt",ex.ToString());Exit(1);}
    }
    static bool Done()
    {
        bool retry=current==8||current==9||current>=14&&current<=17;
        if(!guitar.Completed&&!(retry&&guitar.RetryCount>0)&&clock<180)return false;
        Check(retry?guitar.RetryCount==1&&guitar.PhraseIndex==0&&!guitar.Completed:guitar.Completed&&!guitar.IsRunning&&guitar.PhraseIndex==4&&guitar.RetryCount==(current==19?1:0),"completion="+guitar.Completed+" phrase="+guitar.PhraseIndex+" retries="+guitar.RetryCount+" hits="+hits+" misses="+misses);
        if(!retry&&current!=6&&current!=7&&current!=19&&current!=20)Check(hits==29&&misses==0,"all authored notes accepted without hidden extra misses");
        Check(((TMP_Text)typeof(GuitarRhythmGame).GetField("feedback",Hidden).GetValue(guitar)).enabled,"feedback label is enabled");
        if(guitar.Completed)Check(!fixture.GetComponent<GameControlManager>().IsLockedBy(ControlLockReason.CloseUp),"completion releases CloseUp lock");
        if(current==8)Check(Feedback.Contains("2")&&Feedback.Contains("容错"),"retry explains excess melody misses");
        if(current>=14&&current<=16)Check(Feedback.Contains("最后的空格"),"retry identifies the missing final strum");
        return true;
    }
    static void CapturePreviews()
    {
        var previous=GameLocalization.Current;
        var root=((RectTransform)typeof(GuitarRhythmGame).GetField("canvas",Hidden).GetValue(guitar)).gameObject;
        foreach(var language in new[]{GameLanguage.SimplifiedChinese,GameLanguage.English})
        {
            GameLocalization.SetLanguage(language);Canvas.ForceUpdateCanvases();
            var texts=root.GetComponentsInChildren<TMP_Text>(true);
            foreach(var text in texts){text.ForceMeshUpdate(true,true);Check(!text.isTextOverflowing,"guidance fits: "+language+" / "+text.text);if(language==GameLanguage.English)Check(!Regex.IsMatch(text.GetParsedText(),"[\\u3400-\\u9fff]"),"English guidance is translated: "+text.text);}
            Capture(root,Work+"Previews/"+(language==GameLanguage.English?"english":"chinese")+"-miss-guidance.png");
        }
        GameLocalization.SetLanguage(previous);
    }
    static void Capture(GameObject root,string path)
    {
        var cameraObject=new GameObject("Guidance capture camera");var camera=cameraObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=5;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.10f,.12f);camera.cullingMask=1<<31;
        var target=new RenderTexture(1600,900,24);target.Create();camera.targetTexture=target;
        var canvases=root.GetComponentsInChildren<Canvas>(true);var oldModes=canvases.Select(c=>c.renderMode).ToArray();var oldCameras=canvases.Select(c=>c.worldCamera).ToArray();var oldDistances=canvases.Select(c=>c.planeDistance).ToArray();
        var transforms=root.GetComponentsInChildren<Transform>(true);var oldLayers=transforms.Select(t=>t.gameObject.layer).ToArray();
        foreach(var c in canvases){c.renderMode=RenderMode.ScreenSpaceCamera;c.worldCamera=camera;c.planeDistance=1;}foreach(var t in transforms)t.gameObject.layer=31;
        Canvas.ForceUpdateCanvases();foreach(var t in root.GetComponentsInChildren<TMP_Text>(true))t.ForceMeshUpdate(true,true);camera.Render();
        var active=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());RenderTexture.active=active;
        for(int i=0;i<canvases.Length;i++){canvases[i].renderMode=oldModes[i];canvases[i].worldCamera=oldCameras[i];canvases[i].planeDistance=oldDistances[i];}for(int i=0;i<transforms.Length;i++)transforms[i].gameObject.layer=oldLayers[i];
        UnityEngine.Object.DestroyImmediate(image);camera.targetTexture=null;target.Release();UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(cameraObject);
    }
    static void Teardown(){GameplayInput.Replay=null;if(fixture)UnityEngine.Object.DestroyImmediate(fixture);fixture=null;guitar=null;}
    static void Finish(){File.WriteAllLines(Work+"Results/checks.txt",Checks);File.WriteAllLines(Work+"Results/judgements.txt",Events);File.WriteAllText(Work+"Results/summary.json","{\"cases\":"+Cases.Length+",\"passed\":"+Checks.Count(s=>s.StartsWith("PASS "))+",\"failed\":"+Checks.Count(s=>s.StartsWith("FAIL "))+",\"physicalKeyboard\":false,\"fullStoryPlaythrough\":false}");Exit(Checks.Any(s=>s.StartsWith("FAIL "))?1:0);}
    static void Exit(int code)
    {
        SessionState.EraseBool(Session);EditorApplication.update-=Tick;Teardown();
        bool had=SessionState.GetBool(Session+".HadPreference",false);string prior=SessionState.GetString(Session+".Preference","");GameLocalization.SetLanguage(GameLocalization.ResolvePreference(prior));if(had)PlayerPrefs.SetString(GameLocalization.PreferenceKey,prior);else PlayerPrefs.DeleteKey(GameLocalization.PreferenceKey);PlayerPrefs.Save();
        EditorApplication.isPlaying=false;EditorApplication.Exit(code);
    }
}
