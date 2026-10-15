using UnityEngine;
using UnityEngine.SceneManagement;

// Read existing narrative state; never set flags, positions, answers or control locks.
public sealed class MusicStateRouter : MonoBehaviour
{
    MusicManager music;
    SceneMusicProfile profile;
    ThreeDoorPuzzleController puzzle;
    DistortedCorridorController corridor;
    ChildShadowSequence shadow;
    Scene03MazeController maze;
    Scene03RiverSequence river;
    SchoolLayoutController school;
    SchoolChaseController chase;
    EndingDirector ending;
    float nextRefresh;
    public string ResolutionReason { get; private set; } = "Startup";
    void Awake() { music = GetComponent<MusicManager>(); }
    void OnEnable() { SceneManager.activeSceneChanged += Changed; SceneManager.sceneLoaded += Loaded; }
    void OnDisable() { SceneManager.activeSceneChanged -= Changed; SceneManager.sceneLoaded -= Loaded; }
    void Changed(Scene oldScene, Scene newScene) { profile = null; nextRefresh = 0; }
    void Loaded(Scene scene, LoadSceneMode mode) { nextRefresh = 0; }
    T FindIn<T>(Scene scene) where T : Component
    {
        foreach (var root in scene.GetRootGameObjects())
        { var item = root.GetComponentInChildren<T>(true); if (item) return item; }
        return null;
    }
    void Refresh(Scene scene)
    {
        profile = FindIn<SceneMusicProfile>(scene); puzzle = FindIn<ThreeDoorPuzzleController>(scene);
        corridor = FindIn<DistortedCorridorController>(scene); shadow = FindIn<ChildShadowSequence>(scene);
        maze = FindIn<Scene03MazeController>(scene); river = FindIn<Scene03RiverSequence>(scene);
        school = FindIn<SchoolLayoutController>(scene); chase = FindIn<SchoolChaseController>(scene);
        ending = FindIn<EndingDirector>(scene);
    }
    void LateUpdate()
    {
        if (!music || !music.Ready) return;
        var scene = SceneManager.GetActiveScene(); if (!scene.IsValid() || !scene.isLoaded) return;
        if (!profile || profile.gameObject.scene != scene || Time.unscaledTime >= nextRefresh)
        { nextRefresh = Time.unscaledTime + .5f; Refresh(scene); }
        var state = Resolve(); music.SetState(state);
        float duck = 1;
        if (state != MusicState.MainTheme && state != MusicState.EndTheme)
        {
            var runtime = PersistentRuntime.Instance;
            var dialogue = runtime ? runtime.GetComponentInChildren<DialogueManager>(true) : null;
            if (dialogue && dialogue.IsDialogueActive()) duck = music.Library.dialogueGain;
            // Paged artwork is a visual transition, not a music-silence boundary.
            // Existing dialogue ducking and authored scene/performance cues remain.
        }
        music.SetDucking(duck);
    }
    MusicState Resolved(MusicState value, string reason) { ResolutionReason = reason; return value; }
    public MusicState Resolve()
    {
        if (GamePause.Instance && GamePause.Instance.IsMainMenu) return Resolved(MusicState.MainTheme, "Original main menu");
        if (!profile) return Resolved(MusicState.None, "No music profile in active scene");
        var runtime = PersistentRuntime.Instance;
        var session = runtime ? runtime.GetComponent<EndingSession>() : null;
        if (profile.role == MusicSceneRole.Ending)
        {
            if (!ending || session == null || !session.SequenceStarted) return Resolved(MusicState.None, "No actual boarding session");
            if (ending.Step == EndingStep.Complete) return Resolved(MusicState.EndTheme, "Original completion page, after final line");
            if (ending.Step >= EndingStep.WalkHospital) return Resolved(MusicState.None, "Hospital truth sequence");
            return Resolved(MusicState.Companionship, "Car and child/youth memories");
        }
        if (session != null && (session.IsEndingVisit || session.HandoverStarted))
            return Resolved(MusicState.Companionship, "Original final handover/street visit");
        if (profile.role == MusicSceneRole.Home)
        {
            if (shadow && shadow.enabled && shadow.IsPlaying) return Resolved(MusicState.None, "Child shadow presentation");
            if (corridor && corridor.enabled && corridor.HasJoined || puzzle && puzzle.enabled && puzzle.IsActive)
                return Resolved(MusicState.ThreeDoors, "Existing distorted corridor / puzzle state");
        }
        if (profile.role == MusicSceneRole.Childhood)
        {
            if (river && river.isActiveAndEnabled && (river.Appreciating || river.rhythm && river.rhythm.IsRunning))
                return Resolved(MusicState.BirthdayPerformance, "Authored rhythm / full river song");
            if (maze && maze.isActiveAndEnabled && maze.IsInMaze) return Resolved(MusicState.MazeExplore, "Existing maze entry, layout independent");
        }
        if (profile.role == MusicSceneRole.School && school)
        {
            if (chase && chase.isActiveAndEnabled && (chase.Step == SchoolChaseStep.Opening || chase.Step == SchoolChaseStep.Escaping || chase.Step == SchoolChaseStep.FinalCharge || chase.Step == SchoolChaseStep.Dispersing))
                return Resolved(MusicState.Chase, "Existing dangerous chase state");
            if (chase && chase.isActiveAndEnabled && (chase.Step == SchoolChaseStep.Dead || chase.Step == SchoolChaseStep.Restoring))
                return Resolved(MusicState.None, "Death / checkpoint restore");
            return Resolved(school.Variant == SchoolWorldVariant.Memory ? MusicState.SchoolMemory : MusicState.SchoolOtherworld, "Actual school world variant");
        }
        return Resolved(profile.defaultState, "Active scene default, no movement-key dependency");
    }
}
