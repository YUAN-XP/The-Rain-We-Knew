using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Explicit, disposable runtime review. Uses the shipped scene and its real
// DialogueManager. No scene saves, simulated input, quest or inventory grants.
public static class SubtitleLocalizationReview
{
    const string W = "D:/游戏设计/Codex工作区/work/2026-10-15-SubtitleLocalization/";
    const string Key = "SubtitleLocalizationReview.Active";
    static int wait, index, lineIndex, lineWait, languageIndex, checks, failures;
    static bool lineStarted;
    static PersistentRuntime originalRuntime;
    static bool restarted;
    static DialogueManager manager;
    static TMP_Text body, speaker;
    static readonly List<string> log = new List<string>();
    static DialogueData[] data;
    static readonly Regex han = new Regex("[\\u3400-\\u9fff]");
    static readonly BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;
    [Serializable] public sealed class Sample { public string asset; public DialogueLine[] lines; }
    [Serializable] public sealed class Corpus { public Sample[] samples; }

    public static void Before() { Begin("Before"); }
    public static void After() { Begin("After"); }
    static void Begin(string revision)
    {
        if (EditorApplication.isPlaying) throw new Exception("Edit mode required.");
        Directory.CreateDirectory(W + revision);
        SessionState.SetBool(Key + ".HadPreference", PlayerPrefs.HasKey(GameLocalization.PreferenceKey));
        SessionState.SetString(Key + ".Preference", PlayerPrefs.GetString(GameLocalization.PreferenceKey, ""));
        PlayerPrefs.SetString(GameLocalization.PreferenceKey, "en"); PlayerPrefs.Save();
        SessionState.SetString(Key, revision);
        EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity", OpenSceneMode.Single);
        EditorApplication.update -= Tick; EditorApplication.update += Tick;
        EditorApplication.EnterPlaymode();
    }
    [InitializeOnLoadMethod] static void Resume()
    {
        if (!string.IsNullOrEmpty(SessionState.GetString(Key, ""))) EditorApplication.update += Tick;
    }
    static void Tick()
    {
        string revision = SessionState.GetString(Key, "");
        if (string.IsNullOrEmpty(revision)) { EditorApplication.update -= Tick; return; }
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling) { EditorApplication.QueuePlayerLoopUpdate(); return; }
        try
        {
            if (++wait < 35) { EditorApplication.QueuePlayerLoopUpdate(); return; }
            if (!originalRuntime && !restarted)
            {
                originalRuntime=PersistentRuntime.Instance;
                if (!originalRuntime) throw new Exception("Initial runtime not found.");
                GamePause.Instance.StartNew(); restarted=true;
                EditorApplication.QueuePlayerLoopUpdate(); return;
            }
            if (PersistentRuntime.Instance==originalRuntime) { EditorApplication.QueuePlayerLoopUpdate(); return; }
            if (!manager)
            {
                manager = UnityEngine.Object.FindAnyObjectByType<DialogueManager>(FindObjectsInactive.Include);
                if (!manager) throw new Exception("Shipped Scene01 DialogueManager not found.");
                body = (TMP_Text) typeof(DialogueManager).GetField("dialogueText", fields).GetValue(manager);
                speaker = (TMP_Text) typeof(DialogueManager).GetField("speakerText", fields).GetValue(manager);
                log.Add("Runtime body bound at startup=" + (body.textPreprocessor != null));
                log.Add("Runtime speaker bound at startup=" + (speaker.textPreprocessor != null));
                log.Add("Runtime body scene=" + body.gameObject.scene.name);
                data = AssetDatabase.FindAssets("t:DialogueData", new[] { "Assets" })
                    .Select(g => AssetDatabase.LoadAssetAtPath<DialogueData>(AssetDatabase.GUIDToAssetPath(g)))
                    .Where(d => d && d.lines != null && d.lines.Length > 0).ToArray();
                log.Add("Cold start language=" + GameLocalization.Current);
                log.Add("Real DialogueData assets=" + data.Length);
                File.WriteAllText(W+"DialogueCorpus.json",JsonUtility.ToJson(new Corpus { samples=data.Select(d=>new Sample {asset=AssetDatabase.GetAssetPath(d),lines=d.lines}).ToArray()},true));
                VerifyPersistentBindings();
            }
            // Actual display call; neither this path nor Capture binds labels.
            if (index < data.Length)
            {
                var source = data[index];
                if (!lineStarted)
                {
                    if (manager.CurrentDialogue) manager.CancelDialogue(manager.CurrentDialogue);
                    manager.StartDialogue(source); lineIndex=0; lineWait=0; lineStarted=true;
                }
                if (++lineWait < 5) { EditorApplication.QueuePlayerLoopUpdate(); return; }
                // Read after real player loops; do not force parsing or bind
                // the label here, because that would mask a rendering defect.
                {
                    int n=lineIndex;
                    string parsed = body.GetParsedText();
                    bool ok = parsed == Strip(GameLocalization.Display(source.lines[n].text)); checks++;
                    if (!ok) failures++;
                    log.Add((ok ? "PASS " : "FAIL ") + AssetDatabase.GetAssetPath(source) + " line=" + n +
                        " language="+GameLocalization.Current+" | source=" + source.lines[n].text + " | rendered=" + parsed);
                    string expectedName=Strip(GameLocalization.Display(source.lines[n].speaker));
                    bool nameOk=string.IsNullOrWhiteSpace(source.lines[n].speaker)?!speaker.gameObject.activeSelf:speaker.GetParsedText()==expectedName;
                    checks++;if(!nameOk)failures++;
                    if(!nameOk)log.Add("FAIL speaker "+source.name+" line="+n+" rendered="+speaker.GetParsedText());
                    if (GameLocalization.Current==GameLanguage.English && han.IsMatch(parsed)) log.Add("CHINESE " + parsed);
                    if(n==0&&(source.name=="D_S02_StainedPhoto_First"||source.name=="D_C01"||source.name=="D_S01_FirstDialogue"))
                        Capture(W+revision+"/"+source.name+"_"+GameLocalization.Current+".png");
                }
                if (++lineIndex < source.lines.Length)
                {
                    typeof(DialogueManager).GetMethod("ShowNextLine", fields).Invoke(manager,null); lineWait=0;
                }
                else { index++; lineStarted=false; }
                EditorApplication.QueuePlayerLoopUpdate(); return;
            }
            if(++languageIndex<3)
            {
                GameLocalization.SetLanguage(languageIndex==1?GameLanguage.SimplifiedChinese:GameLanguage.English);
                // Validate the currently visible line before displaying another.
                bool ok=body.GetParsedText()==Strip(GameLocalization.Display(body.text));checks++;if(!ok)failures++;
                log.Add((ok?"PASS ":"FAIL ")+"Live subtitle refresh to "+GameLocalization.Current);
                index=0;lineStarted=false;EditorApplication.QueuePlayerLoopUpdate();return;
            }
            File.WriteAllLines(W + revision + "/runtime-lines.txt", log);
            File.WriteAllText(W+revision+"/summary.json","{\"assets\":"+data.Length+",\"checks\":"+checks+",\"failures\":"+failures+",\"newGameRestart\":true,\"sceneWrites\":false}");
            Finish(failures==0?0:1);
        }
        catch (Exception ex) { File.WriteAllText(W + revision + "/error.txt", ex.ToString()); Finish(1); }
    }
    static string Strip(string s) => Regex.Replace(s ?? "", "<[^>]+>", "");
    static void VerifyPersistentBindings()
    {
        var labels=PersistentRuntime.Instance.GetComponentsInChildren<TMP_Text>(true);
        var unbound=labels.Where(t=>!(t.textPreprocessor is GameLocalization.LocalizedTextPreprocessor)).ToArray();
        checks++;if(unbound.Length>0)failures++;
        log.Add((unbound.Length==0?"PASS ":"FAIL ")+"All replacement persistent labels bound before display: "+labels.Length+" labels; missing="+string.Join(",",unbound.Select(t=>t.name)));
    }
    static void Capture(string path)
    {
        var canvas=manager.PresentationCanvas;
        var oldMode=canvas.renderMode;var oldCamera=canvas.worldCamera;float oldDistance=canvas.planeDistance;
        var nodes=canvas.GetComponentsInChildren<Transform>(true);var layers=nodes.Select(t=>t.gameObject.layer).ToArray();
        var camera=new GameObject("Subtitle evidence camera").AddComponent<Camera>();
        camera.orthographic=true;camera.orthographicSize=5;camera.clearFlags=CameraClearFlags.SolidColor;
        camera.backgroundColor=new Color(.08f,.1f,.12f);camera.cullingMask=1<<31;
        var target=new RenderTexture(1600,900,24);target.Create();camera.targetTexture=target;
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        foreach(var node in nodes)node.gameObject.layer=31;
        // Canvas layout/render only; no localization or ForceMeshUpdate call.
        Canvas.ForceUpdateCanvases();camera.Render();
        var prior=RenderTexture.active;RenderTexture.active=target;
        var image=new Texture2D(1600,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1600,900),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
        RenderTexture.active=prior;canvas.renderMode=oldMode;canvas.worldCamera=oldCamera;canvas.planeDistance=oldDistance;
        for(int i=0;i<nodes.Length;i++)nodes[i].gameObject.layer=layers[i];
        UnityEngine.Object.Destroy(image);camera.targetTexture=null;target.Release();UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(camera.gameObject);
    }
    static void Finish(int exit)
    {
        if (SessionState.GetBool(Key + ".HadPreference", false))
            PlayerPrefs.SetString(GameLocalization.PreferenceKey, SessionState.GetString(Key + ".Preference", ""));
        else PlayerPrefs.DeleteKey(GameLocalization.PreferenceKey);
        PlayerPrefs.Save();
        SessionState.EraseString(Key); EditorApplication.update -= Tick;
        EditorApplication.isPlaying = false; EditorApplication.Exit(exit);
    }
}
