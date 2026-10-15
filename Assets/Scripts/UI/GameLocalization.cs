using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameLanguage { English, SimplifiedChinese }

// Localizes rendered text. The original TMP text, item IDs, dialogue identities,
// quest events and saved story state remain the values authored by the game.
public static class GameLocalization
{
    public const string PreferenceKey = "PM.Language.v1";
    public const string TitleChinese = "旧雨";
    public const string TitleEnglish = "The Rain We Knew";
    public static event Action LanguageChanged;

    [Serializable] public sealed class Row { public string zh, en; }
    [Serializable] public sealed class Alias { public string source, zh; }
    [Serializable] public sealed class Catalog { public int version; public Row[] entries, templates; public Alias[] aliases; }
    sealed class Pattern
    {
        public Regex regex;
        public string english;
        public int[] slots;
        public int specificity;
    }
    static bool initialized;
    static GameLanguage current;
    static readonly Dictionary<string, string> english = new Dictionary<string, string>(StringComparer.Ordinal);
    static readonly Dictionary<string, string> chinese = new Dictionary<string, string>(StringComparer.Ordinal);
    static readonly Dictionary<string, string> cache = new Dictionary<string, string>(StringComparer.Ordinal);
    static readonly HashSet<string> missing = new HashSet<string>(StringComparer.Ordinal);
    static readonly HashSet<TMP_Text> labels = new HashSet<TMP_Text>();
    static readonly List<Pattern> patterns = new List<Pattern>();
    static readonly Regex han = new Regex("[\\u3400-\\u9fff]", RegexOptions.CultureInvariant);
    static readonly Regex placeholder = new Regex(@"\{(\d+)\}", RegexOptions.CultureInvariant);
    static Regex fragments;
    static Catalog catalog;

    public static GameLanguage Current { get { Initialize(); return current; } }
    public static int EntryCount { get { Initialize(); return english.Count; } }
    public static string[] MissingTranslations => missing.ToArray();
    public static string GameTitle => Current == GameLanguage.English ? TitleEnglish : TitleChinese;
    public static GameLanguage ResolvePreference(string value) => value == "zh-CN" ? GameLanguage.SimplifiedChinese : GameLanguage.English;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset()
    {
        initialized = false;
        english.Clear(); chinese.Clear(); cache.Clear(); missing.Clear(); labels.Clear(); patterns.Clear();
        catalog = null; fragments = null; LanguageChanged = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void StartPresentation()
    {
        Initialize();
        var root = new GameObject("LanguagePresentation");
        UnityEngine.Object.DontDestroyOnLoad(root);
        root.AddComponent<LocalizationPresentation>();
    }

    static string Normalize(string value) => (value ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n');
    static void Initialize()
    {
        if (initialized) return;
        initialized = true;
        current = ResolvePreference(PlayerPrefs.GetString(PreferenceKey, "en"));
        var data = Resources.Load<TextAsset>("Localization/GameText");
        if (!data) { Debug.LogError("Game localization catalog is missing."); return; }
        catalog = JsonUtility.FromJson<Catalog>(data.text);
        foreach (var row in catalog.entries ?? Array.Empty<Row>())
        {
            if (string.IsNullOrEmpty(row.zh) || row.en == null) continue;
            string source = Normalize(row.zh), target = Normalize(row.en);
            english[source] = target;
            if (!string.IsNullOrEmpty(target) && !chinese.ContainsKey(target)) chinese.Add(target, source);
        }
        // Exact legacy aliases take precedence over ambiguous reverse translations.
        foreach (var alias in catalog.aliases ?? Array.Empty<Alias>())
            if (!string.IsNullOrEmpty(alias.source)) chinese[Normalize(alias.source)] = Normalize(alias.zh);
        foreach (var row in catalog.templates ?? Array.Empty<Row>())
        {
            string source = Normalize(row.zh);
            var found = placeholder.Matches(source);
            if (found.Count == 0) continue;
            int offset = 0;
            var regex = "\\A";
            var slots = new List<int>();
            foreach (Match match in found)
            {
                regex += Regex.Escape(source.Substring(offset, match.Index - offset)) + "([\\s\\S]*?)";
                slots.Add(int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));
                offset = match.Index + match.Length;
            }
            regex += Regex.Escape(source.Substring(offset)) + "\\z";
            patterns.Add(new Pattern { regex = new Regex(regex, RegexOptions.CultureInvariant), english = Normalize(row.en), slots = slots.ToArray(), specificity = source.Length - found.Count * 3 });
        }
        patterns.Sort((a, b) => b.specificity.CompareTo(a.specificity));
        var parts = english.Keys.Where(s => s.Length <= 220 && han.IsMatch(s)).OrderByDescending(s => s.Length).ThenBy(s => s, StringComparer.Ordinal).ToArray();
        if (parts.Length > 0) fragments = new Regex(string.Join("|", parts.Select(Regex.Escape)), RegexOptions.CultureInvariant);
    }

    // Kept separate from English presentation so names used by existing UI
    // composition and game rules are never changed into translated identifiers.
    public static string ToChinese(string value)
    {
        Initialize();
        string source = Normalize(value);
        return chinese.TryGetValue(source, out var result) ? result : source;
    }

    public static string Display(string value)
    {
        Initialize();
        if (string.IsNullOrEmpty(value)) return value ?? string.Empty;
        string source = ToChinese(value);
        if (current == GameLanguage.SimplifiedChinese) return source;
        if (cache.TryGetValue(source, out var result)) return result;
        result = TranslateEnglish(source, 0);
        if (cache.Count > 4096) cache.Clear();
        cache[source] = result;
        return result;
    }

    static string TranslateEnglish(string source, int depth)
    {
        source = ToChinese(source);
        if (english.TryGetValue(source, out var exact)) return exact;
        if (depth < 4)
            foreach (var pattern in patterns)
            {
                var match = pattern.regex.Match(source);
                if (!match.Success) continue;
                var values = new object[pattern.slots.Max() + 1];
                for (int n = 0; n < pattern.slots.Length; n++) values[pattern.slots[n]] = TranslateEnglish(match.Groups[n + 1].Value, depth + 1);
                return string.Format(CultureInfo.InvariantCulture, pattern.english, values);
            }
        if (!han.IsMatch(source)) return source;
        // Concatenated legacy labels are covered by their reviewed text fragments.
        // This does not translate IDs, save fields or any gameplay condition.
        string translated = fragments != null ? fragments.Replace(source, m => english[m.Value]) : source;
        if (han.IsMatch(translated)) missing.Add(source);
        return translated;
    }

    public static void SetLanguage(GameLanguage language)
    {
        Initialize();
        current = language == GameLanguage.SimplifiedChinese ? GameLanguage.SimplifiedChinese : GameLanguage.English;
        PlayerPrefs.SetString(PreferenceKey, current == GameLanguage.SimplifiedChinese ? "zh-CN" : "en");
        PlayerPrefs.Save();
        cache.Clear();
        labels.RemoveWhere(t => !t);
        foreach (var text in labels.ToArray())
        {
            text.havePropertiesChanged = true;
            text.SetLayoutDirty(); text.SetVerticesDirty();
            if (text.gameObject.activeInHierarchy) text.ForceMeshUpdate(false, true);
        }
        LanguageChanged?.Invoke();
    }

    public static void Bind(TMP_Text text)
    {
        if (!text) return;
        Initialize();
        if (!(text.textPreprocessor is LocalizedTextPreprocessor))
        {
            text.textPreprocessor = new LocalizedTextPreprocessor(text.textPreprocessor);
            text.havePropertiesChanged = true;
            text.SetLayoutDirty(); text.SetVerticesDirty();
        }
        labels.Add(text);
    }

    public static void BindLoadedText()
    {
        // A new-game session recreates PersistentRuntime during Awake and moves
        // its serialized UI to DontDestroyOnLoad before sceneLoaded. Scanning
        // only the loaded scene's roots misses all of that replacement UI.
        foreach (var text in UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include)) Bind(text);
    }

    public static int VisibleLength(string source) => Regex.Replace(Display(source ?? ""), "<[^>]+>", "").Length;

    public sealed class LocalizedTextPreprocessor : ITextPreprocessor
    {
        readonly ITextPreprocessor inner;
        public LocalizedTextPreprocessor(ITextPreprocessor existing = null) { inner = existing; }
        public string PreprocessText(string text) => Display(inner == null ? text : inner.PreprocessText(text));
    }
}

// Presentation-only attachment; it never reads player input, awards items,
// advances a quest, changes a camera, or changes the game's pause rules.
public sealed class LocalizationPresentation : MonoBehaviour
{
    void Awake() { SceneManager.sceneLoaded += Loaded; }
    void Start() { GameLocalization.BindLoadedText(); }
    void Loaded(Scene scene, LoadSceneMode mode) { GameLocalization.BindLoadedText(); }
    void OnDestroy() { SceneManager.sceneLoaded -= Loaded; }
}
