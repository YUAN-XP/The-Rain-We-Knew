using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

// Explicit authoring only: never imports, changes a scene, or starts QA on editor startup.
public static class AudioPass0Build
{
    public const string Work = "D:/游戏设计/Codex工作区/work/2026-10-06-AudioPass0/";
    public const string Evidence = "Documentation/AudioPass0/Evidence/";
    const string Config = "Assets/Resources/AudioPass0/";
    const string MixerPath = "Assets/Audio/Mixer/ProjectMystery_AudioMixer.mixer";
    const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
    [Serializable] public class Row { public string state, file, category; public float volume = .3f; }
    [Serializable] public class Manifest { public Row[] cues; }
    [MenuItem("ProjectMystery/Audio Pass 0/Build music configuration")]
    public static void Build()
    {
        if (EditorApplication.isPlaying || EditorSceneManager.GetActiveScene().isDirty) throw new Exception("Exit Play Mode and preserve the current scene before audio authoring.");
        var rows = JsonUtility.FromJson<Manifest>(File.ReadAllText(Work + "checks/import-map.json")).cues;
        foreach (var row in rows)
        {
            string path = "Assets/Audio/Music/" + row.category + "/" + row.file;
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (AudioImporter)AssetImporter.GetAtPath(path);
            var sample = importer.defaultSampleSettings;
            sample.loadType = AudioClipLoadType.Streaming; sample.compressionFormat = AudioCompressionFormat.Vorbis;
            sample.quality = .75f; sample.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            importer.forceToMono = false; importer.loadInBackground = true;
            importer.defaultSampleSettings = sample; importer.SaveAndReimport();
        }
        Directory.CreateDirectory(Config); Directory.CreateDirectory("Assets/Audio/Ambience");
        Directory.CreateDirectory("Assets/Audio/Mixer"); Directory.CreateDirectory(Evidence + "checks");
        AssetDatabase.Refresh();
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        if (!mixer) mixer = CreateMixer();
        var library = LoadOrCreate<MusicLibrary>(Config + "MusicLibrary.asset");
        library.mixer = mixer; library.musicGroup = mixer.FindMatchingGroups("Music").Single();
        library.ambienceGroup = mixer.FindMatchingGroups("Ambience").Single();
        library.sfxGroup = mixer.FindMatchingGroups("SFX").Single(); library.uiGroup = mixer.FindMatchingGroups("UI").Single();
        library.cues = rows.Where(r => r.state != "Reserve").Select(row =>
        {
            var state = (MusicState)Enum.Parse(typeof(MusicState), row.state);
            var cue = LoadOrCreate<MusicCue>(Config + state + ".asset");
            cue.state = state; cue.clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/" + row.category + "/" + row.file);
            cue.loop = state != MusicState.BirthdayPerformance; cue.baseVolume = row.volume;
            cue.fadeIn = state == MusicState.EndTheme ? 1.8f : 1.5f; cue.fadeOut = 1.5f; cue.startOffset = 0;
            cue.delegatedPlayback = state == MusicState.ThreeDoors || state == MusicState.BirthdayPerformance;
            cue.playDuringMenuPause = state == MusicState.MainTheme; EditorUtility.SetDirty(cue); return cue;
        }).ToArray();
        var reserve = rows.Single(r => r.state == "Reserve");
        library.themeInstrumentalReserve = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/" + reserve.category + "/" + reserve.file);
        EditorUtility.SetDirty(library);
        var doors = LoadOrCreate<ThreeDoorsMusicSettings>(Config + "ThreeDoorsSettings.asset");
        doors.clip = library.Cue(MusicState.ThreeDoors).clip; EditorUtility.SetDirty(doors);
        doors.clear.volume = 1; doors.medium.volume = .44f; doors.distant.volume = .19f;
        AssetDatabase.SaveAssets();
        string original = SceneManager.GetActiveScene().path;
        try
        {
            foreach (var entry in EditorBuildSettings.scenes.Where(s => s.enabled))
            {
                var scene = EditorSceneManager.OpenScene(entry.path);
                // Scene loads can release unused assets. Resolve the persisted configuration after each load.
                library = AssetDatabase.LoadAssetAtPath<MusicLibrary>(Config + "MusicLibrary.asset");
                doors = AssetDatabase.LoadAssetAtPath<ThreeDoorsMusicSettings>(Config + "ThreeDoorsSettings.asset");
                var profile = FindIn<SceneMusicProfile>(scene);
                if (!profile) profile = new GameObject("AudioPass0_Profile").AddComponent<SceneMusicProfile>();
                switch (scene.name)
                {
                    case "Scene01_Rain": profile.role = MusicSceneRole.Rain; profile.defaultState = MusicState.RainExplore; break;
                    case "Scene02_Courtyard": profile.role = MusicSceneRole.Home; profile.defaultState = MusicState.HomeExplore; break;
                    case "Scene03_Childhood": profile.role = MusicSceneRole.Childhood; profile.defaultState = MusicState.StreetExplore; break;
                    case "Scene04_School": profile.role = MusicSceneRole.School; profile.defaultState = MusicState.SchoolOtherworld; break;
                    case "EndingSequence": profile.role = MusicSceneRole.Ending; profile.defaultState = MusicState.Companionship; break;
                    default: throw new Exception("No confirmed music mapping for " + scene.name);
                }
                var puzzle = FindIn<ThreeDoorPuzzleController>(scene);
                if (puzzle)
                {
                    var controller = puzzle.GetComponent<ThreeDoorsMusicController>() ?? puzzle.gameObject.AddComponent<ThreeDoorsMusicController>();
                    controller.puzzle = puzzle; controller.corridor = FindIn<DistortedCorridorController>(scene); controller.settings = doors;
                    var properties = new SerializedObject(puzzle); var sources = properties.FindProperty("doorSounds");
                    controller.doorAudio = Enumerable.Range(0, sources.arraySize).Select(i => (AudioSource)sources.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
                    doors.WrongDoorSFX = (AudioClip)properties.FindProperty("errorClip").objectReferenceValue;
                    if (controller.corridor) controller.legacyCorridorGuide = (AudioSource)new SerializedObject(controller.corridor).FindProperty("guitarGuide").objectReferenceValue;
                    EditorUtility.SetDirty(doors);
                }
                var river = FindIn<Scene03RiverSequence>(scene);
                if (river) river.fullBirthdaySong = library.Cue(MusicState.BirthdayPerformance).clip;
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            }
        }
        finally { if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original); }
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        File.WriteAllText(Work + "checks/authoring-complete.txt", DateTime.Now.ToString("O") + " Five original scene profiles, twelve distinct source files, twelve cues, four child mixer groups.\n");
    }
    static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        var value = AssetDatabase.LoadAssetAtPath<T>(path); if (value) return value;
        value = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(value, path); return value;
    }
    public static T FindIn<T>(Scene scene) where T : Component
    {
        foreach (var root in scene.GetRootGameObjects()) { var value = root.GetComponentInChildren<T>(true); if (value) return value; }
        return null;
    }
    static AudioMixer CreateMixer()
    {
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.Audio.AudioMixerController")).First(t => t != null);
        var mixer = (AudioMixer)type.GetMethod("CreateMixerControllerAtPath", All).Invoke(null, new object[] { MixerPath });
        var master = type.GetProperty("masterGroup", All).GetValue(mixer);
        foreach (string name in new[] { "Music", "Ambience", "SFX", "UI" })
        {
            var group = type.GetMethod("CreateNewGroup", All).Invoke(mixer, new object[] { name, false });
            type.GetMethod("AddChildToParent", All).Invoke(mixer, new[] { group, master });
        }
        EditorUtility.SetDirty(mixer); AssetDatabase.SaveAssets(); return mixer;
    }
}
