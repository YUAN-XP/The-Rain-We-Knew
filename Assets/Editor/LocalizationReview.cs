using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Narrow title/language review. No gameplay replay, quest progression, inventory
// grants, scene rewriting, input override, or desktop/GitHub publication.
public static class LocalizationReview
{
    const string W = LocalizationProjectAudit.Work;
    static readonly List<string> checks = new List<string>();
    static readonly Regex han = new Regex("[\\u3400-\\u9fff]");
    static void Check(bool ok, string name) { checks.Add((ok ? "PASS " : "FAIL ") + name); }
    static string Rendered(TMP_Text text)
    {
        GameLocalization.Bind(text);
        text.ForceMeshUpdate(true, true);
        return text.textPreprocessor.PreprocessText(text.text);
    }

    public static void Run()
    {
        Directory.CreateDirectory(W);
        checks.Clear();
        LocalizationProjectAudit.Export();
        Verify();
        File.WriteAllLines(W + "language-checks.txt", checks);
        if (checks.Any(c => c.StartsWith("FAIL "))) throw new Exception("Language review failed. See language-checks.txt.");
        Build();
    }

    public static void VerifyOnly()
    {
        Directory.CreateDirectory(W);
        checks.Clear();
        LocalizationProjectAudit.Export();
        Verify();
        File.WriteAllLines(W + "language-checks.txt", checks);
        if (checks.Any(c => c.StartsWith("FAIL "))) throw new Exception("Language review failed. See language-checks.txt.");
    }

    static void Verify()
    {
        bool hadLanguage = PlayerPrefs.HasKey(GameLocalization.PreferenceKey);
        string priorLanguage = PlayerPrefs.GetString(GameLocalization.PreferenceKey, "");
        var oldLanguage = GameLocalization.Current;
        float audioPreference = PlayerPrefs.GetFloat("P07.MasterVolume", 1);
        int widthPreference = PlayerPrefs.GetInt("P07.Width", 0), heightPreference = PlayerPrefs.GetInt("P07.Height", 0);
        try
        {
            Check(GameLocalization.ResolvePreference(null) == GameLanguage.English && GameLocalization.ResolvePreference("") == GameLanguage.English, "Fresh language preference defaults to English");
            Check(GameLocalization.ResolvePreference("zh-CN") == GameLanguage.SimplifiedChinese && GameLocalization.ResolvePreference("unsupported") == GameLanguage.English, "Saved Chinese preference and invalid-value fallback");
            Check(GameLocalization.EntryCount >= 800, "Runtime translation catalog loaded");
            Check(Resources.Load<P07PresentationData>("P07Presentation").gameTitle == GameLocalization.TitleChinese, "Authored game title is the confirmed title");
            Check(PlayerSettings.productName == "ProjectMystery" && PlayerSettings.companyName == "DefaultCompany", "Existing save and preferences namespace retained");

            GameLocalization.SetLanguage(GameLanguage.English);
            Check(GameLocalization.GameTitle == "The Rain We Knew", "English title");
            Check(GameLocalization.Display("载入存档 3？未保存的进度将丢失。") == "Load Save Slot 3? Unsaved progress will be lost.", "Dynamic numbered save confirmation");
            Check(GameLocalization.Display("旧相机已放入背包") == "Old Camera added to inventory", "Dynamic acquisition toast");
            Check(GameLocalization.Display("城门前的小骑士 · 第 2 幕") == "The Little Knight at the City Gate - Act 2", "Dynamic puppet act title");
            Check(GameLocalization.Display("剩余 3 次  ·  已套中 2") == "Attempts left: 3 | Targets hit: 2", "Dynamic ring-toss counters");
            Check(GameLocalization.Display("XPZT0715") == "XPZT0715" && GameLocalization.Display("0715") == "0715", "Password clues and digits unchanged");
            Check(GameLocalization.VisibleLength("探索周围") == "Explore the surroundings".Length, "Typewriter uses displayed-language length");
            VerifyLateInteractionPrompt();
            VerifyLateDialoguePresentation();

            var inventory = JsonUtility.FromJson<LocalizationProjectAudit.Inventory>(File.ReadAllText(W + "serialized_strings.json"));
            var untranslated = new List<string>();
            var untranslatedEnglish = new List<string>();
            foreach (var entry in inventory.entries)
            {
                string result = GameLocalization.Display(entry.value);
                if (han.IsMatch(result) && result != "简体中文") untranslated.Add(entry.asset + " | " + entry.property + " | " + entry.value + " => " + result);
                if (entry.property.EndsWith(".speaker") && entry.value == "Xiao Peng")
                    Check(DialogueIdentity.LegacyName(entry.value) == DialogueCharacterId.XiaoPeng, "Legacy speaker identity preserved");
            }
            File.WriteAllLines(W + "untranslated-chinese-in-english.txt", untranslated.Distinct());
            Check(untranslated.Count == 0, "Authored Chinese text has English display coverage");

            GameLocalization.SetLanguage(GameLanguage.SimplifiedChinese);
            Check(GameLocalization.GameTitle == "旧雨", "Chinese title");
            Check(GameLocalization.Display("Who are you?") == "你是谁？", "Legacy English dialogue has Chinese display");
            Check(GameLocalization.Display("[E] Pick Up") == "[E] 拾取", "Legacy English interaction has Chinese display");
            foreach (var entry in inventory.entries.Where(e => e.property.EndsWith(".text") || e.property == "m_text"))
                if (Regex.IsMatch(entry.value, "[a-zA-Z]{3,}") && !han.IsMatch(entry.value) && !han.IsMatch(GameLocalization.Display(entry.value)) &&
                    !Regex.IsMatch(entry.value, @"\A(?:[0-9\s:.%×-]+|[WASDQRE]+|XPZT0715|Space|Tab|Esc|Shift|DialogueText\.\.\.|New Text)\z")) untranslatedEnglish.Add(entry.asset + " | " + entry.property + " | " + entry.value);
            File.WriteAllLines(W + "english-source-coverage-exceptions.txt", untranslatedEnglish.Distinct());
            Check(untranslatedEnglish.Count == 0, "Authored pure-English subtitles and labels covered in Chinese");
            Check(PlayerPrefs.GetString(GameLocalization.PreferenceKey) == "zh-CN", "Language choice saved to preferences");

            Preview();
            Check(PlayerPrefs.GetFloat("P07.MasterVolume", 1) == audioPreference && PlayerPrefs.GetInt("P07.Width", 0) == widthPreference && PlayerPrefs.GetInt("P07.Height", 0) == heightPreference, "Audio and resolution preferences untouched by language selection");
        }
        finally
        {
            GameLocalization.SetLanguage(oldLanguage);
            if (hadLanguage) PlayerPrefs.SetString(GameLocalization.PreferenceKey, priorLanguage);
            else PlayerPrefs.DeleteKey(GameLocalization.PreferenceKey);
            PlayerPrefs.Save();
        }
    }

    static void VerifyLateInteractionPrompt()
    {
        var owner=new GameObject("Late interaction language review");
        try {
            var root=new GameObject("Prompt",typeof(RectTransform));root.transform.SetParent(owner.transform,false);
            var label=root.AddComponent<TextMeshProUGUI>();label.textPreprocessor=null;
            var ui=owner.AddComponent<InteractionUI>();
            typeof(InteractionUI).GetField("promptRoot",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(ui,root);
            typeof(InteractionUI).GetField("promptText",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(ui,label);
            ui.ShowPrompt("[E] 调查",owner.transform);
            Check(label.textPreprocessor is GameLocalization.LocalizedTextPreprocessor,"Late-created interaction prompt binds localization on first display");
            Check(label.text=="[E] 调查" && !han.IsMatch(label.textPreprocessor.PreprocessText(label.text)),"Interaction identity remains Chinese internally while English prompt renders in English");
        } finally {UnityEngine.Object.DestroyImmediate(owner);}
    }

    static void VerifyLateDialoguePresentation()
    {
        var root=new GameObject("Late dialogue language review");
        DialogueData data=null;
        try
        {
            var canvas=RuntimePanelUI.Canvas("DialogueCanvas",root.transform,100);
            var panel=RuntimePanelUI.Panel("DialoguePanel",canvas,Vector2.zero,Vector2.one,Color.black).gameObject;
            var body=RuntimePanelUI.Rect("DialogueText",panel.transform,new Vector2(.1f,.1f),new Vector2(.9f,.7f)).gameObject.AddComponent<TextMeshProUGUI>();
            var speaker=RuntimePanelUI.Rect("SpeakerText",panel.transform,new Vector2(.1f,.7f),new Vector2(.9f,.9f)).gameObject.AddComponent<TextMeshProUGUI>();
            body.font=speaker.font=P07UI.Font;body.textPreprocessor=speaker.textPreprocessor=null;
            var manager=root.AddComponent<DialogueManager>();
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            typeof(DialogueManager).GetField("dialoguePanel",flags).SetValue(manager,panel);
            typeof(DialogueManager).GetField("dialogueText",flags).SetValue(manager,body);
            typeof(DialogueManager).GetField("speakerText",flags).SetValue(manager,speaker);
            data=ScriptableObject.CreateInstance<DialogueData>();
            data.lines=new[]{new DialogueLine {speaker="肖鹏",text="所以你是怎么知道这些情况的？",characterId=DialogueCharacterId.XiaoPeng}};
            typeof(DialogueManager).GetField("currentDialogue",flags).SetValue(manager,data);
            typeof(DialogueManager).GetMethod("DisplayCurrentLine",flags).Invoke(manager,null);
            Check(body.textPreprocessor is GameLocalization.LocalizedTextPreprocessor&&speaker.textPreprocessor is GameLocalization.LocalizedTextPreprocessor,"Late-created dialogue and speaker bind on real display boundary");
            body.ForceMeshUpdate();speaker.ForceMeshUpdate();
            Check(body.GetParsedText()=="So how do you know all this?"&&speaker.GetParsedText()=="Xiao Peng","Late-created dialogue renders English body and speaker");
            Check(body.text==data.lines[0].text&&speaker.text==data.lines[0].speaker&&DialogueIdentity.Resolve(data.lines[0])==DialogueCharacterId.XiaoPeng,"Dialogue source, speaker identity and authored data stay unchanged");
        }
        finally {UnityEngine.Object.DestroyImmediate(root);if(data)UnityEngine.Object.DestroyImmediate(data);}
    }

    static void Preview()
    {
        var previous = SceneManager.GetActiveScene();
        var preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(preview);
        GameObject root = null;
        try
        {
            foreach (var language in new[] { GameLanguage.English, GameLanguage.SimplifiedChinese })
            {
                GameLocalization.SetLanguage(language);
                string suffix = language == GameLanguage.English ? "en" : "zh";
                root = new GameObject("Language menu review");
                var menu = RuntimePanelUI.Canvas("Menu", root.transform, 32000);
                P07UI.Background(menu);
                P07UI.Text("旧雨", menu, new Vector2(.09f, .75f), new Vector2(.60f, .90f), 57);
                foreach (var entry in new[] { ("开始游戏", .38f), ("设置", .295f), ("退出游戏", .21f) })
                    P07UI.Button(entry.Item1, menu, new Vector2(.09f, entry.Item2), new Vector2(.355f, entry.Item2 + .063f), () => { });
                Capture(root, W + "menu_" + suffix + ".png");
                UnityEngine.Object.DestroyImmediate(root); root = null;

                root = new GameObject("Language settings review");
                var canvas = RuntimePanelUI.Canvas("Settings", root.transform, 32000);
                var settings = canvas.gameObject.AddComponent<P07SettingsPanel>();
                settings.Build(() => { });
                typeof(P07SettingsPanel).GetMethod("Select", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(settings, new object[] { 3 });
                Capture(root, W + "settings_" + suffix + ".png");
                var chineseButton = root.GetComponentsInChildren<Button>(true).Single(b => b.name == "LanguageChinese");
                var englishButton = root.GetComponentsInChildren<Button>(true).Single(b => b.name == "LanguageEnglish");
                Check(chineseButton.interactable == (language != GameLanguage.SimplifiedChinese) && englishButton.interactable == (language != GameLanguage.English), suffix + " current language option is identifiable");
                if (language == GameLanguage.English)
                {
                    chineseButton.onClick.Invoke();
                    Check(GameLocalization.Current == GameLanguage.SimplifiedChinese && PlayerPrefs.GetString(GameLocalization.PreferenceKey) == "zh-CN", "Actual settings button switches and saves Chinese");
                    englishButton = root.GetComponentsInChildren<Button>(true).First(b => b.name == "LanguageEnglish" && b.transform.parent.gameObject.activeInHierarchy);
                    englishButton.onClick.Invoke();
                    Check(GameLocalization.Current == GameLanguage.English && PlayerPrefs.GetString(GameLocalization.PreferenceKey) == "en", "Actual settings button switches back and saves English");
                }
                UnityEngine.Object.DestroyImmediate(root); root = null;

                root = new GameObject("Language dialogue review");
                canvas = RuntimePanelUI.Canvas("Subtitle sample", root.transform, 32000);
                P07UI.PaperPanel(canvas, new Vector2(.05f, .10f), new Vector2(.95f, .90f));
                var title = P07UI.Text("肖鹏", canvas, new Vector2(.10f, .73f), new Vector2(.90f, .85f), 34, true);
                string line = "你什么密码都喜欢用同一个。";
                var subtitle = P07UI.Text(line, canvas, new Vector2(.10f, .52f), new Vector2(.90f, .71f), 32, true);
                var prompt = P07UI.Text("[E] 对话", canvas, new Vector2(.10f, .34f), new Vector2(.90f, .45f), 28, true);
                P07UI.Text("旧相机已放入背包", canvas, new Vector2(.10f, .15f), new Vector2(.90f, .27f), 28, true);
                Capture(root, W + "text_sample_" + suffix + ".png");
                Check(subtitle.text == line, suffix + " rendered translation preserves original text used by existing logic");
                Check(Rendered(prompt) == (language == GameLanguage.English ? "[E] Talk" : "[E] 对话"), suffix + " interaction rendering");
                Check(subtitle.textInfo.characterCount == GameLocalization.VisibleLength(line), suffix + " actual text mesh matches translated character count");
                UnityEngine.Object.DestroyImmediate(root); root = null;
            }
        }
        finally
        {
            if (root) UnityEngine.Object.DestroyImmediate(root);
            SceneManager.SetActiveScene(previous);
            EditorSceneManager.CloseScene(preview, true);
        }
    }

    static void Capture(GameObject root, string path)
    {
        var cameraObject = new GameObject("Language review camera");
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true; camera.orthographicSize = 5; camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.08f, .10f, .12f); camera.cullingMask = 1 << 31;
        var target = new RenderTexture(1600, 900, 24); target.Create();
        camera.targetTexture = target;
        foreach (var canvas in root.GetComponentsInChildren<Canvas>(true)) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1; }
        foreach (var transform in root.GetComponentsInChildren<Transform>(true)) transform.gameObject.layer = 31;
        Canvas.ForceUpdateCanvases();
        foreach (var text in root.GetComponentsInChildren<TMP_Text>(true)) { GameLocalization.Bind(text); text.ForceMeshUpdate(true, true); }
        camera.Render();
        var active = RenderTexture.active; RenderTexture.active = target;
        var image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); image.Apply();
        File.WriteAllBytes(path, image.EncodeToPNG());
        RenderTexture.active = active; camera.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(image); target.Release(); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(cameraObject);
    }

    public static void Build()
    {
        string folder = W + "ReviewBuild";
        Directory.CreateDirectory(folder);
        UnityEditor.EditorUtility.UnloadUnusedAssetsImmediate();
        GC.Collect();
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, locationPathName = folder + "/ProjectMystery.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.CompressWithLz4HC });
        File.WriteAllText(W + "build-result.txt", "Result=" + report.summary.result + "\nErrors=" + report.summary.totalErrors + "\nWarnings=" + report.summary.totalWarnings + "\nBytes=" + report.summary.totalSize + "\nSeconds=" + report.summary.totalTime.TotalSeconds + "\nDevelopment build=" + report.summary.options.HasFlag(BuildOptions.Development));
        File.WriteAllLines(W + "build-messages.txt", report.steps.SelectMany(s => s.messages.Select(m => s.name + " | " + m.type + " | " + m.content)));
        if (report.summary.result != BuildResult.Succeeded) throw new Exception("Review build failed.");
    }
}
