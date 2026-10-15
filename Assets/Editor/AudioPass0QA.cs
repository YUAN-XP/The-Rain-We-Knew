using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Explicit, isolated music checks. No startup hook; absent from player builds.
public static class AudioPass0QA
{
    const string W = AudioPass0Build.Work;
    static P01ReplayDriver observer;
    static float nextSample;
    static string previousSignature = "";
    static int serial;
    static readonly Dictionary<string, string> captured = new Dictionary<string, string>();
    [Serializable] public class Voice
    {
        public string name, clip, mixer; public bool playing, loop, ignorePause; public int samples;
        public float time, volume, pan, cutoff;
    }
    [Serializable] public class Observation
    {
        public string stamp, scene, state, requested, reason, ending, chase, world, route;
        public bool focused, paused, listenerPaused, fading, ordinarySilent, rhythm, appreciating, lastLineComplete;
        public float time, fade, duck, master, outputPeak;
        public int managers, runtimes, stage, answer, mazeLayout, doorSampleSpread;
        public Voice[] music, doors, audibleOtherSources;
    }
    [Serializable] public class Check { public string name, result, detail; }
    [Serializable] public class Checks { public string stamp; public Check[] checks; }
    static Voice Describe(AudioSource s)
    {
        if (!s) return null;
        var filter = s.GetComponent<AudioLowPassFilter>();
        return new Voice { name = s.name, clip = s.clip ? AssetDatabase.GetAssetPath(s.clip) : "", mixer = s.outputAudioMixerGroup ? s.outputAudioMixerGroup.name : "unassigned",
            playing = s.isPlaying, loop = s.loop, ignorePause = s.ignoreListenerPause, samples = s.clip ? s.timeSamples : 0,
            time = s.clip ? s.time : 0, volume = s.volume, pan = s.panStereo, cutoff = filter ? filter.cutoffFrequency : 22000 };
    }
    public static void Begin()
    {
        if (!EditorApplication.isPlaying) throw new Exception("Play Mode required");
        if (observer) return;
        Directory.CreateDirectory(W + "runtime"); Directory.CreateDirectory(W + "listening");
        observer = new GameObject("Audio Pass 0 opt-in telemetry").AddComponent<P01ReplayDriver>();
        UnityEngine.Object.DontDestroyOnLoad(observer.gameObject); observer.Tick = Tick; observer.Cleanup = Cleanup;
        nextSample = 0; captured.Clear(); previousSignature = "";
    }
    static void Cleanup()
    {
        foreach (var probe in UnityEngine.Object.FindObjectsByType<AudioPass0OutputProbe>()) UnityEngine.Object.Destroy(probe);
        observer = null;
    }
    public static void End() { if (observer) UnityEngine.Object.Destroy(observer.gameObject); }
    static T Active<T>() where T : Component => AudioPass0Build.FindIn<T>(SceneManager.GetActiveScene());
    static Observation Observe()
    {
        var m = MusicManager.Instance; var doors = Active<ThreeDoorsMusicController>(); var puzzle = Active<ThreeDoorPuzzleController>();
        var school = Active<SchoolLayoutController>(); var chase = Active<SchoolChaseController>(); var ending = Active<EndingDirector>();
        var river = Active<Scene03RiverSequence>(); var maze = Active<Scene03MazeController>(); var rt = PersistentRuntime.Instance;
        var s = new Observation { stamp = DateTime.Now.ToString("O"), scene = SceneManager.GetActiveScene().name, time = Time.realtimeSinceStartup,
            focused = Application.isFocused, paused = GamePause.IsPaused, listenerPaused = AudioListener.pause, master = AudioListener.volume,
            state = m ? m.CurrentState.ToString() : "missing", requested = m ? m.RequestedState.ToString() : "missing",
            reason = m ? m.GetComponent<MusicStateRouter>().ResolutionReason : "", fading = m && m.IsFading, fade = m ? m.FadeProgress : 0,
            duck = m ? m.DuckGain : 0, ordinarySilent = m && m.OrdinaryMusicSilent,
            managers = UnityEngine.Object.FindObjectsByType<MusicManager>().Length, runtimes = UnityEngine.Object.FindObjectsByType<PersistentRuntime>().Length,
            stage = puzzle ? puzzle.Progress : -1, answer = puzzle ? puzzle.CorrectDoor : -1, mazeLayout = maze ? maze.CurrentLayout : -1,
            chase = chase ? chase.Step.ToString() : "", world = school ? school.Variant.ToString() : "", ending = ending ? ending.Step.ToString() : "",
            lastLineComplete = rt && rt.GetComponent<StoryStateManager>().Has(StoryFlag.EndingFinalLineCompleted),
            rhythm = river && river.rhythm && river.rhythm.IsRunning, appreciating = river && river.Appreciating,
            music = m ? new[] { Describe(m.SourceA), Describe(m.SourceB), Describe(m.ExternalSource) }.Where(v => v != null).ToArray() : new Voice[0],
            doors = doors ? doors.doorAudio.Select(Describe).ToArray() : new Voice[0] };
        if (s.doors.Length == 3) s.doorSampleSpread = s.doors.Max(v => v.samples) - s.doors.Min(v => v.samples);
        s.audibleOtherSources = UnityEngine.Object.FindObjectsByType<AudioSource>().Where(a => a.isPlaying && a.volume > .0001f && (!m || a != m.SourceA && a != m.SourceB && a != m.ExternalSource) && (!doors || !doors.doorAudio.Contains(a))).Select(Describe).ToArray();
        var buffer = new float[256]; AudioListener.GetOutputData(buffer, 0); s.outputPeak = buffer.Max(v => Mathf.Abs(v));
        return s;
    }
    static void Tick()
    {
        if (Time.realtimeSinceStartup < nextSample) return; nextSample = Time.realtimeSinceStartup + .2f;
        var s = Observe(); File.WriteAllText(W + "runtime/music-status.json", JsonUtility.ToJson(s, true));
        File.AppendAllText(W + "runtime/music-observations.jsonl", JsonUtility.ToJson(s) + "\n");
        string signature = s.scene + "|" + s.state + "|" + s.stage + "|" + s.chase + "|" + s.world + "|" + s.ending + "|" + s.rhythm + "|" + s.appreciating;
        if (signature != previousSignature)
        {
            previousSignature = signature; File.AppendAllText(W + "runtime/music-transitions.jsonl", JsonUtility.ToJson(s) + "\n");
        }
        if (s.focused && (!s.paused || s.state == "MainTheme") && !s.fading && s.duck > .8f && (s.music.Any(v => v.playing && v.volume > .001f) || s.doors.Any(v => v.playing && v.volume > .001f)))
        {
            string key = s.state == "ThreeDoors" ? s.state + "_stage" + s.stage : s.state;
            if (!captured.ContainsKey(key))
            {
                var listener = UnityEngine.Object.FindAnyObjectByType<AudioListener>();
                if (listener)
                {
                    string label = (++serial).ToString("D2") + "_" + key;
                    var probe = listener.GetComponent<AudioPass0OutputProbe>() ?? listener.gameObject.AddComponent<AudioPass0OutputProbe>();
                    probe.Begin(W + "listening/" + label + ".wav", 12); captured[key] = label;
                    ScreenCapture.CaptureScreenshot(W + "listening/" + label + ".png");
                    File.WriteAllText(W + "listening/" + label + "_state.json", JsonUtility.ToJson(s, true));
                }
            }
        }
    }
    public static void StaticAudit()
    {
        var checks = new List<Check>();
        void Check(string name, bool pass, string detail) => checks.Add(new Check { name = name, result = pass ? "PASS" : "FAIL", detail = detail });
        var lib = Resources.Load<MusicLibrary>("AudioPass0/MusicLibrary");
        Check("single_config", lib && lib.cues.Length == 12, lib ? "cues=" + lib.cues.Length : "missing library");
        if (lib)
        {
            Check("mixer_hierarchy", new[] { "Music", "Ambience", "SFX", "UI" }.All(n => lib.mixer.FindMatchingGroups(n).Length == 1), AssetDatabase.GetAssetPath(lib.mixer));
            foreach (var cue in lib.cues)
            {
                string path = AssetDatabase.GetAssetPath(cue.clip); var importer = AssetImporter.GetAtPath(path) as AudioImporter;
                Check("cue_" + cue.state, cue.clip && cue.clip.length > 20 && cue.clip.channels == 2 && importer && !importer.forceToMono && importer.defaultSampleSettings.loadType == AudioClipLoadType.Streaming && importer.defaultSampleSettings.compressionFormat == AudioCompressionFormat.Vorbis,
                    path + " seconds=" + cue.clip.length + " channels=" + cue.clip.channels + " gain=" + cue.baseVolume);
            }
            var main = lib.Cue(MusicState.MainTheme); var end = lib.Cue(MusicState.EndTheme);
            Check("separate_theme_cues", main != end && main.clip == end.clip && main.startOffset == 0 && end.startOffset == 0 && main.loop && end.loop, "same full song; separate assets; zero offset and loop");
            Check("instrumental_reserve_only", lib.themeInstrumentalReserve && lib.cues.All(c => c.clip != lib.themeInstrumentalReserve), "imported, not assigned to a cue");
        }
        var scene = SceneManager.GetActiveScene(); string original = scene.path;
        try
        {
            foreach (var entry in EditorBuildSettings.scenes.Where(e => e.enabled))
            {
                scene = EditorSceneManager.OpenScene(entry.path); var profile = AudioPass0Build.FindIn<SceneMusicProfile>(scene);
                Check("profile_" + scene.name, profile != null, profile ? profile.role + " -> " + profile.defaultState : "missing");
                var puzzle = AudioPass0Build.FindIn<ThreeDoorPuzzleController>(scene);
                if (puzzle)
                {
                    var seq = (int[])typeof(ThreeDoorPuzzleController).GetField("sequence", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(puzzle);
                    var controller = puzzle.GetComponent<ThreeDoorsMusicController>();
                    Check("unchanged_door_answer", seq.SequenceEqual(new[] { 2, 0, 1 }), string.Join(",", seq));
                    Check("three_door_audio_bindings", controller && controller.doorAudio.Length == 3 && controller.doorAudio.All(a => a) && controller.settings && controller.settings.clip,
                        controller ? string.Join(" | ", controller.doorAudio.Select(a => a.name + "@" + a.transform.position)) : "missing");
                }
            }
        }
        finally { if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original); }
        File.WriteAllText(W + "checks/unity-static-audit.json", JsonUtility.ToJson(new Checks { stamp = DateTime.Now.ToString("O"), checks = checks.ToArray() }, true));
    }
    public static void SystemChecks()
    {
        if (!EditorApplication.isPlaying || GamePause.IsPaused || !MusicManager.Instance) throw new Exception("Focused, unpaused Play Mode required for isolated music checks");
        if (!observer) Begin(); observer.StartCoroutine(CheckSystem());
    }
    static IEnumerator CheckSystem()
    {
        var m = MusicManager.Instance; var router = m.GetComponent<MusicStateRouter>();
        var restore = m.RequestedState; var runtime = PersistentRuntime.Instance;
        var checks = new List<Check>();
        void Check(string name, bool pass, string detail) => checks.Add(new Check { name = name, result = pass ? "PASS" : "FAIL", detail = detail });
        router.enabled = false;
        try
        {
            m.SetDucking(1); m.SetState(MusicState.RainExplore); yield return new WaitForSecondsRealtime(3.2f);
            Check("ordinary_single_owner", UnityEngine.Object.FindObjectsByType<MusicManager>().Length == 1 && new[] { m.SourceA, m.SourceB }.Count(s => s.isPlaying) == 1, "one manager, one steady ordinary voice");
            new GameObject("AudioPass0 duplicate-owner test").AddComponent<MusicManager>(); yield return null;
            Check("duplicate_discarded", MusicManager.Instance == m && UnityEngine.Object.FindObjectsByType<MusicManager>().Length == 1 && PersistentRuntime.Instance == runtime, "duplicate removed; original persistent game runtime retained");
            var voice = m.SourceA.isPlaying ? m.SourceA : m.SourceB; float position = voice.time;
            m.SetState(MusicState.RainExplore); m.SetState(MusicState.RainExplore); yield return new WaitForSecondsRealtime(.5f);
            Check("same_state_continuity", voice.time > position + .3f, "position advanced from " + position + " to " + voice.time);
            m.SetState(MusicState.HomeExplore); yield return new WaitForSecondsRealtime(.5f);
            Check("crossfade_overlap_only", m.SourceA.isPlaying && m.SourceB.isPlaying && m.SourceA.volume > 0 && m.SourceB.volume > 0 && m.IsFading, "both transitional voices audible with complementary ramps");
            m.SetState(MusicState.StreetExplore); m.SetState(MusicState.MazeExplore); yield return new WaitForSecondsRealtime(3.2f);
            Check("rapid_request_settles", m.CurrentState == MusicState.MazeExplore && !m.IsFading && new[] { m.SourceA, m.SourceB }.Count(s => s.isPlaying) == 1, "last requested state retained, no third ordinary voice");
            m.SetState(MusicState.None); yield return new WaitForSecondsRealtime(1.8f);
            Check("none_fades_to_silence", m.OrdinaryMusicSilent && !m.SourceA.isPlaying && !m.SourceB.isPlaying, "both sources stopped only at zero gain");
            m.SetState(MusicState.MainTheme); yield return new WaitForSecondsRealtime(1.8f);
            voice = m.SourceA.isPlaying ? m.SourceA : m.SourceB;
            Check("menu_pause_exception_scoped", voice.ignoreListenerPause && voice.loop, "only menu cue ignores listener pause");
            voice.time = voice.clip.length - .7f; yield return new WaitForSecondsRealtime(1.5f);
            Check("full_theme_loop_wrap", voice.isPlaying && voice.time < 3, "test seek near full-song end; wrapped to " + voice.time);
            m.SetState(MusicState.RainExplore); yield return new WaitForSecondsRealtime(1.8f);
            voice = m.SourceA.isPlaying ? m.SourceA : m.SourceB;
            Check("exploration_honors_pause", !voice.ignoreListenerPause, "ordinary cue retains original global pause behavior");
        }
        finally
        {
            if (m) { m.SetState(restore); router.enabled = true; }
            File.WriteAllText(W + "checks/isolated-music-system.json", JsonUtility.ToJson(new Checks { stamp = DateTime.Now.ToString("O"), checks = checks.ToArray() }, true));
        }
    }
    public static void ReleaseBuild()
    {
        if (EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty) throw new Exception("Clean edit mode required");
        Directory.CreateDirectory(W + "build_release"); EditorUtility.UnloadUnusedAssetsImmediate(); GC.Collect();
        var scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
        var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = scenes, locationPathName = W + "build_release/ProjectMystery.exe", target = BuildTarget.StandaloneWindows64, options = BuildOptions.None });
        File.WriteAllText(W + "checks/release-build.txt", DateTime.Now.ToString("O") + " " + result.summary.result + " errors=" + result.summary.totalErrors + " warnings=" + result.summary.totalWarnings + " bytes=" + result.summary.totalSize);
        File.WriteAllLines(W + "checks/release-build-messages.txt", result.steps.SelectMany(s => s.messages.Select(m => s.name + " | " + m.type + " | " + m.content)));
        if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded) throw new Exception("Release build failed");
    }
}
