using UnityEngine;
using UnityEngine.SceneManagement;

// The original puzzle owns the fixed right -> left -> middle answer and reset logic.
public sealed class ThreeDoorsMusicController : MonoBehaviour
{
    public ThreeDoorPuzzleController puzzle;
    public DistortedCorridorController corridor;
    public ThreeDoorsMusicSettings settings;
    public AudioSource[] doorAudio;
    public AudioSource legacyCorridorGuide;
    [SerializeField] int currentStage, correctDoor;
    [SerializeField] string leftClarity, middleClarity, rightClarity;
    [SerializeField] double scheduledStart;
    AudioLowPassFilter[] filters;
    AudioReverbFilter[] reverbs;
    bool started;
    float completedElapsed;
    public bool OwnsAudio => isActiveAndEnabled && settings && settings.clip && settings.stages != null && settings.stages.Length > 0 && doorAudio != null && doorAudio.Length == 3 && doorAudio[0] && doorAudio[1] && doorAudio[2];
    public double ScheduledStart => scheduledStart;
    public int CurrentStage => currentStage;
    public string[] Clarity => new[] { leftClarity, middleClarity, rightClarity };
    void Awake()
    {
        if (!OwnsAudio) return;
        filters = new AudioLowPassFilter[3]; reverbs = new AudioReverbFilter[3];
        for (int i = 0; i < 3; i++)
        {
            var source = doorAudio[i]; source.Stop(); source.playOnAwake = false; source.volume = 0;
            source.clip = settings.clip; source.loop = true;
            // Unity may return an editor fake-null object for a missing component.
            // Use Unity's object validity check rather than CLR null coalescing.
            filters[i] = source.GetComponent<AudioLowPassFilter>();
            if (!filters[i]) filters[i] = source.gameObject.AddComponent<AudioLowPassFilter>();
            reverbs[i] = source.GetComponent<AudioReverbFilter>();
            if (!reverbs[i]) reverbs[i] = source.gameObject.AddComponent<AudioReverbFilter>();
        }
        if (legacyCorridorGuide) { legacyCorridorGuide.Stop(); legacyCorridorGuide.volume = 0; }
    }
    void Update()
    {
        if (!OwnsAudio || !puzzle) return;
        var manager = MusicManager.Instance;
        bool active = manager && manager.CurrentState == MusicState.ThreeDoors && puzzle.enabled && gameObject.scene == SceneManager.GetActiveScene();
        if (!active) { StopVoices(); return; }
        if (!started)
        {
            scheduledStart = AudioSettings.dspTime + settings.scheduleLead;
            for (int i = 0; i < 3; i++)
            {
                doorAudio[i].outputAudioMixerGroup = manager.Library.musicGroup;
                doorAudio[i].clip = settings.clip; doorAudio[i].timeSamples = 0; doorAudio[i].PlayScheduled(scheduledStart);
            }
            started = true;
        }
        if (GamePause.IsPaused) return;
        currentStage = Mathf.Clamp(puzzle.Progress, 0, 3); correctDoor = puzzle.CorrectDoor;
        completedElapsed = currentStage == 3 ? completedElapsed + Time.unscaledDeltaTime : 0;
        float completionGain = 1 - Mathf.Clamp01((completedElapsed - settings.completedHoldSeconds) / Mathf.Max(.01f, settings.completedFadeSeconds));
        var recovery = settings.stages[Mathf.Clamp(currentStage, 0, settings.stages.Length - 1)];
        var player = PersistentRuntime.Instance ? PersistentRuntime.Instance.GetComponent<GameControlManager>()?.ActivePlayer : null;
        string[] labels = new string[3]; float dt = Time.unscaledDeltaTime;
        for (int i = 0; i < 3; i++)
        {
            var clarity = i == correctDoor ? settings.clear : i == (correctDoor + 1) % 3 ? settings.medium : settings.distant;
            labels[i] = i == correctDoor ? "Clear" : i == (correctDoor + 1) % 3 ? "Medium" : "Distant";
            float volume = clarity.volume * recovery.volumeMultiplier;
            if (!puzzle.IsActive) volume = i == correctDoor ? settings.corridorStartVolume + (corridor ? corridor.LampStage : 0) * settings.corridorLampIncrease : 0;
            volume *= manager.Library.Cue(MusicState.ThreeDoors).baseVolume * completionGain;
            float offset = player ? doorAudio[i].transform.position.x - player.transform.position.x : 0;
            volume /= 1 + Mathf.Abs(offset) * settings.distanceAttenuation;
            doorAudio[i].volume = Mathf.Lerp(doorAudio[i].volume, volume * manager.DuckGain, 1 - Mathf.Exp(-dt / Mathf.Max(.01f, settings.smoothingSeconds)));
            doorAudio[i].panStereo = Mathf.Clamp(offset / settings.panDistance, -.8f, .8f);
            doorAudio[i].spatialBlend = clarity.spatialBlend;
            filters[i].cutoffFrequency = Mathf.Lerp(filters[i].cutoffFrequency, clarity.lowPass * recovery.cutoffMultiplier, 1 - Mathf.Exp(-dt / Mathf.Max(.01f, settings.smoothingSeconds)));
            reverbs[i].enabled = clarity.reverb;
            if (clarity.reverb) { reverbs[i].reverbPreset = AudioReverbPreset.User; reverbs[i].reverbLevel = clarity.reverbLevel; }
        }
        leftClarity = labels[0]; middleClarity = labels[1]; rightClarity = labels[2];
    }
    void StopVoices()
    {
        if (!started) return;
        foreach (var source in doorAudio) if (source) { source.Stop(); source.volume = 0; }
        started = false;
    }
    void OnDisable() { StopVoices(); }
}
