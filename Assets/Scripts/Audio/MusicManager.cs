using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// One persistent owner, two ordinary BGM voices. The current game runtime is untouched.
[DefaultExecutionOrder(-20000), DisallowMultipleComponent]
public sealed class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }
    [SerializeField] MusicLibrary library;
    [SerializeField] MusicState currentState, requestedState;
    [SerializeField] AudioSource sourceA, sourceB;
    [SerializeField] AudioClip currentClip, nextClip;
    [SerializeField, Range(0, 1)] float fadeProgress = 1, duckGain = 1;
    [SerializeField] string playbackOwner = "None";
    readonly float[] gains = new float[2], starts = new float[2], targets = new float[2], durations = new float[2], levels = new float[2];
    float fadeElapsed, desiredDuck = 1, nextRouteScan, externalGain;
    readonly MusicCue[] voiceCues = new MusicCue[2];
    bool fading, externalFadingOut;
    int incoming = -1;
    MusicCue activeCue;
    AudioSource externalSource;
    public MusicLibrary Library => library;
    public bool Ready => library && library.musicGroup;
    public MusicState CurrentState => currentState;
    public MusicState RequestedState => requestedState;
    public AudioSource SourceA => sourceA;
    public AudioSource SourceB => sourceB;
    public AudioSource ExternalSource => externalSource;
    public float FadeProgress => fadeProgress;
    public float DuckGain => duckGain;
    public bool IsFading => fading;
    public bool OrdinaryMusicSilent => gains[0] < .0001f && gains[1] < .0001f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Instance = null; }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (Instance) return;
        var config = Resources.Load<MusicLibrary>("AudioPass0/MusicLibrary");
        if (!config) return;
        new GameObject("MusicManager").AddComponent<MusicManager>();
    }
    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this; DontDestroyOnLoad(gameObject);
        if (!library) library = Resources.Load<MusicLibrary>("AudioPass0/MusicLibrary");
        sourceA = MakeSource("MusicSource_A"); sourceB = MakeSource("MusicSource_B");
        gameObject.AddComponent<MusicStateRouter>();
        SceneManager.sceneLoaded += SceneLoaded;
    }
    AudioSource MakeSource(string name)
    {
        var child = new GameObject(name); child.transform.SetParent(transform, false);
        var source = child.AddComponent<AudioSource>();
        source.playOnAwake = false; source.spatialBlend = 0; source.volume = 0; source.priority = 64;
        if (library) source.outputAudioMixerGroup = library.musicGroup;
        return source;
    }
    AudioSource Voice(int index) => index == 0 ? sourceA : sourceB;
    void SceneLoaded(Scene scene, LoadSceneMode mode) { nextRouteScan = 0; }
    public void SetDucking(float value) { desiredDuck = Mathf.Clamp01(value); }
    public void SetState(MusicState state)
    {
        if (!Ready || requestedState == state) return;
        var cue = library.Cue(state);
        if (state != MusicState.None && !cue) { Debug.LogWarning("Music cue is not configured: " + state, this); return; }
        requestedState = state;
        // Silence/performance boundaries cannot be delayed behind an old incoming cue.
        if (!fading || state == MusicState.None || cue && cue.delegatedPlayback) BeginTransition();
    }
    void BeginTransition()
    {
        var cue = library.Cue(requestedState);
        float outSeconds = activeCue ? activeCue.fadeOut : 1.5f;
        for (int i = 0; i < 2; i++) { starts[i] = gains[i]; targets[i] = 0; durations[i] = outSeconds; }
        incoming = -1;
        if (cue && !cue.delegatedPlayback && cue.clip)
        {
            incoming = gains[0] <= gains[1] ? 0 : 1;
            var voice = Voice(incoming);
            // A queued change normally reaches this only after the previous fade has settled.
            voice.Stop(); voice.clip = cue.clip; voice.loop = cue.loop;
            voice.ignoreListenerPause = cue.playDuringMenuPause;
            voice.time = Mathf.Clamp(cue.startOffset, 0, Mathf.Max(0, cue.clip.length - .02f));
            levels[incoming] = cue.baseVolume; voiceCues[incoming] = cue; starts[incoming] = gains[incoming] = 0;
            voice.volume = 0; targets[incoming] = 1; durations[incoming] = cue.fadeIn; voice.Play();
        }
        activeCue = cue; currentState = requestedState; fadeElapsed = 0; fadeProgress = 0; fading = true;
        nextClip = incoming >= 0 ? cue.clip : null;
        playbackOwner = cue && cue.delegatedPlayback ? "Authored performance" : incoming >= 0 ? Voice(incoming).name : "None";
    }
    void Update()
    {
        if (!Ready) return;
        bool menu = GamePause.Instance && GamePause.Instance.IsMainMenu;
        if (Time.unscaledTime >= nextRouteScan) { nextRouteScan = Time.unscaledTime + .5f; RouteUnassignedSources(); }
        if (GamePause.IsPaused && !menu) return;
        float dt = Time.unscaledDeltaTime;
        float duckSeconds = desiredDuck < duckGain ? library.duckAttack : library.duckRelease;
        duckGain = Mathf.MoveTowards(duckGain, desiredDuck, dt / Mathf.Max(.01f, duckSeconds));
        if (fading)
        {
            fadeElapsed += dt; float longest = 0; bool complete = true;
            for (int i = 0; i < 2; i++)
            {
                longest = Mathf.Max(longest, durations[i]);
                float p = durations[i] <= 0 ? 1 : Mathf.Clamp01(fadeElapsed / durations[i]);
                // Linear complementary gains avoid boosting correlated material at a crossfade.
                gains[i] = Mathf.Lerp(starts[i], targets[i], p); complete &= p >= 1;
                if (p >= 1 && targets[i] == 0 && Voice(i).clip) { Voice(i).Stop(); Voice(i).clip = null; }
            }
            fadeProgress = longest <= 0 ? 1 : Mathf.Clamp01(fadeElapsed / longest);
            if (complete)
            {
                fading = false; currentClip = incoming >= 0 ? Voice(incoming).clip : null; nextClip = null;
                if (requestedState != currentState) BeginTransition();
            }
        }
        for (int i = 0; i < 2; i++) Voice(i).volume = gains[i] * (voiceCues[i] ? voiceCues[i].baseVolume : levels[i]) * duckGain;
        if (externalSource && externalSource.isPlaying && currentState == MusicState.BirthdayPerformance)
        {
            var cue = library.Cue(MusicState.BirthdayPerformance);
            float seconds = externalFadingOut ? cue.fadeOut : cue.fadeIn;
            externalGain = Mathf.MoveTowards(externalGain, externalFadingOut ? 0 : 1, dt / Mathf.Max(.01f, seconds));
            externalSource.volume = externalGain * cue.baseVolume * duckGain;
            currentClip = externalSource.clip; playbackOwner = externalSource.name;
        }
    }
    public IEnumerator PreparePerformance(AudioSource source)
    {
        if (!Ready || !source) yield break;
        SetState(MusicState.BirthdayPerformance);
        while (!OrdinaryMusicSilent) yield return null;
        externalSource = source; externalGain = 0; externalFadingOut = false;
        source.outputAudioMixerGroup = library.musicGroup; source.volume = 0;
    }
    public IEnumerator FinishPerformance(AudioSource source)
    {
        if (!Ready || !source) yield break;
        if (!source.isPlaying) { if (externalSource == source) externalSource = null; yield break; }
        externalFadingOut = true;
        while (source && source.isPlaying && externalGain > .0001f) yield return null;
        externalSource = null; externalFadingOut = false;
    }
    public void RouteUnassignedSources()
    {
        foreach (var source in FindObjectsByType<AudioSource>(FindObjectsInactive.Include))
            if (source && !source.outputAudioMixerGroup) source.outputAudioMixerGroup = library.sfxGroup;
    }
    void OnDestroy()
    {
        if (Instance == this) { SceneManager.sceneLoaded -= SceneLoaded; Instance = null; }
    }
}
