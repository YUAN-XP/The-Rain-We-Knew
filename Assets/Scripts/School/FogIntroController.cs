using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum FogIntroPhase { Idle, Entering, FreeWalk, Encircled, Inquiry, Charging, Dispersing, Dead, Restoring, Completed }

// Scene02's separate same-scene fog block. Only the initial dispersal consumes
// the lamp; all deaths restore CP_FOG_OUT without reloading inventory or story.
public partial class FogIntroController : MonoBehaviour
{
    [Header("Existing runtime / same-scene entry")]
    [SerializeField] private ScreenFadeController screenFade;
    [SerializeField] private Transform fogSpawn;
    [SerializeField] private GameObject fogVisualRoot;
    [SerializeField] private Vector2 fogCameraLimits = new Vector2(-192, -138);
    [SerializeField] private float cameraY = 2f, cameraSize = 5f;
    [SerializeField] private string schoolSceneName = "Scene04_School";
    [SerializeField] private TMP_FontAsset font;
    [Header("Proximity staging, increasing X")]
    [SerializeField] private float studentRevealX = -187, firstStandingX = -180, encircleX = -172, inquiryX = -163;
    [SerializeField] private GameObject seatedStudent, firstStandingStudent, crowd;
    [SerializeField] private Transform approachingCrowd;
    [SerializeField] private Vector3 crowdApproachOffset = new Vector3(-3, 0, 0);
    [SerializeField, Min(.1f)] private float crowdApproachSpeed = .6f;
    [SerializeField] private DialogueData entryDialogue, inquiryDialogue;
    [Header("Alpha timings; no additional charge-death timer")]
    [SerializeField, Min(.1f)] private float darknessSeconds = 3f;
    [SerializeField, Min(.1f)] private float dispersalSeconds = .65f;
    private GameControlManager controls;
    private InventoryManager inventory;
    private StoryStateManager story;
    private StoryNoticeUI notice;
    private DialogueManager dialogue;
    private OilLampController lamp;
    private CameraModeController cameraTool;
    private SprintAbility sprint;
    private FogCheckpoint checkpoint = new FogCheckpoint();
    private AudioSource heartbeat;
    private AudioClip heartbeatClip;
    private GameObject uiRoot, deathPanel;
    private Image whiteMask;
    private TMP_Text dangerText, chargeText, bullyingLeft, bullyingRight;
    private Vector3 crowdStart;
    private float darkTime;
    private bool ownsCutscene, ownsTransition, spaceArmed, retryArmed, warningShown;
    private int completedFrame, retryFrame;
    public FogIntroPhase Phase { get; private set; }
    public float DarknessElapsed => darkTime;
    public FogCheckpoint Checkpoint => checkpoint;
    public bool IsActive => Phase != FogIntroPhase.Idle && Phase != FogIntroPhase.Completed;
    public int DeathCount { get; private set; }
    public int RetryCount { get; private set; }

    private void Awake()
    {
        if (approachingCrowd != null) crowdStart = approachingCrowd.position;
        ResetFigures(); if (fogVisualRoot != null) fogVisualRoot.SetActive(false);
    }
    private bool Resolve()
    {
        var runtime = PersistentRuntime.Instance;
        if (runtime == null) return false;
        controls = runtime.GetComponent<GameControlManager>(); inventory = runtime.GetComponent<InventoryManager>();
        story = runtime.GetComponent<StoryStateManager>(); notice = runtime.GetComponent<StoryNoticeUI>();
        dialogue = runtime.GetComponentInChildren<DialogueManager>(true); lamp = runtime.GetComponent<OilLampController>();
        cameraTool = runtime.GetComponent<CameraModeController>();
        return controls != null && controls.ActivePlayer != null && inventory != null && story != null && lamp != null && dialogue != null;
    }
    public bool TryEnter(string destinationOverride = null)
    {
        if (Phase != FogIntroPhase.Idle || Time.frameCount <= retryFrame || !Resolve() || controls.IsLocked()) return false;
        if (story.Has(StoryFlag.FogIntroCompleted) || !lamp.StoryUsable) { lamp.ShowExhausted(); return false; }
        if (!lamp.IsIlluminating) { if (notice != null) notice.Show("先按 R 举起小油灯，再进入黑雾。"); return false; }
        if (!string.IsNullOrWhiteSpace(destinationOverride)) schoolSceneName = destinationOverride;
        if (screenFade == null || fogSpawn == null || Camera.main == null || Camera.main.GetComponent<CameraFollow>() == null ||
            !Application.CanStreamedLevelBeLoaded(schoolSceneName))
        { Warn("FogIntroController needs its fade, safe spawn, camera and enabled school scene."); return false; }
        checkpoint.Capture(controls, lamp, inventory);
        if (!checkpoint.IsValid) { Warn("CP_FOG_OUT could not capture the current actor and camera."); return false; }
        if (uiRoot == null) BuildUI();
        sprint = controls.ActivePlayer.GetComponent<SprintAbility>();
        lamp.ChargeCompleted -= ChargeCompleted; lamp.ChargeCompleted += ChargeCompleted;
        ResetTransient(); deathPanel.SetActive(false); Phase = FogIntroPhase.Entering;
        StartCoroutine(Enter()); return true;
    }
    private IEnumerator Enter()
    {
        OwnTransition();
        yield return screenFade.FadeThroughBlack(() =>
        {
            if (fogVisualRoot != null) fogVisualRoot.SetActive(true);
            uiRoot.SetActive(true);
            var player = controls.ActivePlayer; var body = player.GetComponent<Rigidbody2D>();
            player.transform.position = fogSpawn.position;
            if (body != null) { body.position = fogSpawn.position; body.linearVelocity = Vector2.zero; body.angularVelocity = 0; }
            var camera = Camera.main; var follow = camera.GetComponent<CameraFollow>();
            camera.orthographicSize = cameraSize; follow.SetTarget(player.transform);
            follow.SetLimits(fogCameraLimits.x, fogCameraLimits.y); follow.SetVerticalLimits(true, 0, cameraY, cameraY);
            follow.SnapToTarget(); Physics2D.SyncTransforms();
        });
        ReleaseTransition();
        yield return Say(entryDialogue);
        Phase = FogIntroPhase.FreeWalk;
    }
    private void Update()
    {
        if(GamePause.IsPaused)return;
        if (!IsActive || controls == null || controls.ActivePlayer == null) return;
        if(schoolReturnMode){UpdateSchoolReturn();return;}
        if (Phase == FogIntroPhase.Dead)
        {
            if (!GameplayInput.GetKey(KeyCode.E)) retryArmed = true;
            if (retryArmed && GameplayInput.GetKeyDown(KeyCode.E) && Time.frameCount > completedFrame) StartCoroutine(Retry());
            return;
        }
        if (Phase == FogIntroPhase.FreeWalk || Phase == FogIntroPhase.Encircled)
        {
            UpdateDarkness();
            if (Phase == FogIntroPhase.Dead || !CanStageAdvance()) return;
            float x = controls.ActivePlayer.transform.position.x;
            if (x >= studentRevealX && seatedStudent != null) seatedStudent.SetActive(true);
            if (x >= firstStandingX && firstStandingStudent != null) firstStandingStudent.SetActive(true);
            if (x >= encircleX)
            {
                Phase = FogIntroPhase.Encircled;
                if (crowd != null) crowd.SetActive(true);
                if (sprint != null) sprint.SetTemporarilySuppressed(this, true);
                bullyingLeft.text = "你装什么"; bullyingRight.text = "真把自己当回事了";
            }
            if (x >= inquiryX && lamp.IsIlluminating) { Phase = FogIntroPhase.Inquiry; StartCoroutine(Inquiry()); }
        }
        if (Phase == FogIntroPhase.Charging)
        {
            if (approachingCrowd != null)
                approachingCrowd.position = Vector3.MoveTowards(approachingCrowd.position, crowdStart + crowdApproachOffset, crowdApproachSpeed * GameplayClock.Delta);
            // Require release after the last dialogue key, then a new held input.
            if (!GameplayInput.GetKey(KeyCode.Space)) spaceArmed = true;
            bool responsive = !controls.IsLockedBy(ControlLockReason.Dialogue) && !controls.IsLockedBy(ControlLockReason.Inventory)
                && !controls.IsLockedBy(ControlLockReason.CloseUp) && !controls.IsLockedBy(ControlLockReason.SceneTransition) && Time.timeScale > 0;
            lamp.TickCharge(spaceArmed && responsive && GameplayInput.GetKey(KeyCode.Space), responsive ? GameplayClock.Delta : 0);
            chargeText.text = "长按空格" + (lamp.Charge01 > 0 ? "  " + Mathf.RoundToInt(lamp.Charge01 * 100) + "%" : "");
        }
        if (Phase == FogIntroPhase.Encircled || Phase == FogIntroPhase.Inquiry || Phase == FogIntroPhase.Charging)
        {
            float a = .7f + .18f * Mathf.Sin(GameplayClock.Now * 2.4f);
            bullyingLeft.alpha = a; bullyingRight.alpha = a;
        }
    }
    private bool CanStageAdvance()
    { return !controls.IsLocked() && !screenFade.IsFading && Time.timeScale > 0; }
    private bool DangerClockRuns()
    {
        // Camera preview is deliberately dangerous. Modal menus, dialogue and
        // the shutter animation pause danger because R cannot respond then.
        return lamp.CanRespondThisFrame && !OilLampController.HasLockOtherThanCamera(controls) && !screenFade.IsFading
            && (cameraTool == null || !cameraTool.IsBusy) && Time.timeScale > 0;
    }
    private void UpdateDarkness()
    {
        if (lamp.IsIlluminating) { ClearDanger(); return; }
        if (!DangerClockRuns()) { if (heartbeat != null) heartbeat.Pause(); return; }
        darkTime += GameplayClock.Delta;
        dangerText.text = "这里太黑了，有些令人不安...";
        if (heartbeat != null)
        {
            heartbeat.volume = Mathf.Lerp(.12f, .6f, darkTime / darknessSeconds);
            heartbeat.pitch = Mathf.Lerp(.9f, 1.45f, darkTime / darknessSeconds);
            if (!heartbeat.isPlaying) heartbeat.Play();
        }
        if (darkTime >= darknessSeconds) Die();
    }
    private void ClearDanger()
    {
        darkTime = 0; if (dangerText != null) dangerText.text = "";
        if (heartbeat != null) heartbeat.Stop();
    }
    private IEnumerator Inquiry()
    {
        OwnCutscene(); ClearDanger();
        yield return Say(inquiryDialogue);
        // No automatic refuelling or tool raise in this scene: arrival here
        // required actual light and the cutscene keeps that lamp pose.
        if (!lamp.IsIlluminating) { ReleaseCutscene(); Phase = FogIntroPhase.Encircled; yield break; }
        spaceArmed = false;
        while (GameplayInput.GetKey(KeyCode.Space)) yield return null;
        if (Phase != FogIntroPhase.Inquiry) yield break;
        spaceArmed = true; lamp.BeginCharge(); Phase = FogIntroPhase.Charging;
        chargeText.text = "长按空格";
    }
    private void ChargeCompleted()
    {
        if (Phase != FogIntroPhase.Charging || story.Has(StoryFlag.FogIntroCompleted)) return;
        Phase = FogIntroPhase.Dispersing; StartCoroutine(Disperse());
    }
    private IEnumerator Disperse()
    {
        ClearDanger(); chargeText.text = "";
        for (float t = 0; t < dispersalSeconds; t += GameplayClock.Delta)
        {
            float alpha = Mathf.Clamp01(t / dispersalSeconds); whiteMask.color = new Color(1, 1, 1, alpha);
            bullyingLeft.alpha = bullyingRight.alpha = 1 - alpha;
            yield return null;
        }
        whiteMask.color = Color.white; ResetFigures();
        if (!WhiteSceneTransition.Begin(schoolSceneName, controls))
        {
            whiteMask.color = Color.clear; Warn("Fog dispersal could not load its configured school. The lamp was not consumed.");
            lamp.BeginCharge(); spaceArmed = false; Phase = FogIntroPhase.Charging; yield break;
        }
        story.Set(StoryFlag.FogIntroCompleted); lamp.SetStoryUsable(false);
        if (sprint != null) sprint.SetTemporarilySuppressed(this, false);
        Phase = FogIntroPhase.Completed; ReleaseCutscene();
    }
    private void Die()
    {
        if (Phase == FogIntroPhase.Dead || Phase == FogIntroPhase.Restoring) return;
        StopAllCoroutines(); Phase = FogIntroPhase.Dead; DeathCount++; OwnCutscene();
        if (cameraTool != null) cameraTool.Lower(); lamp.LowerLamp(); ClearDanger();
        if (sprint != null) sprint.SetTemporarilySuppressed(this, false);
        chargeText.text = ""; deathPanel.SetActive(true); retryArmed = false; completedFrame = Time.frameCount;
        if (notice != null) notice.Tutorial("");
    }
    private IEnumerator Retry()
    {
        Phase = FogIntroPhase.Restoring; retryArmed = false; RetryCount++;
        // Keep the black death mask until both physical and camera restoration
        // have rendered; consume the retry edge until E has really been released.
        checkpoint.Restore(controls, lamp); ResetTransient();
        if (fogVisualRoot != null) fogVisualRoot.SetActive(false);
        yield return null; yield return new WaitForFixedUpdate();
        while (GameplayInput.GetKey(KeyCode.E)) yield return null;
        retryFrame = Time.frameCount + 1; uiRoot.SetActive(false); Phase = FogIntroPhase.Idle;
        ReleaseCutscene();
    }
    private void ResetTransient()
    {
        ClearDanger(); ResetFigures(); spaceArmed = retryArmed = false;
        if (sprint != null) sprint.SetTemporarilySuppressed(this, false);
        if (chargeText != null) chargeText.text = "";
        if (bullyingLeft != null) bullyingLeft.text = ""; if (bullyingRight != null) bullyingRight.text = "";
        if (whiteMask != null) whiteMask.color = Color.clear;
    }
    private void ResetFigures()
    {
        if (seatedStudent != null) seatedStudent.SetActive(false); if (firstStandingStudent != null) firstStandingStudent.SetActive(false);
        if (crowd != null) crowd.SetActive(false); if (approachingCrowd != null) approachingCrowd.position = crowdStart;
    }
    private IEnumerator Say(DialogueData data)
    {
        if (data == null) yield break;
        while (dialogue.IsDialogueActive()) yield return null;
        dialogue.StartDialogue(data); yield return null;
        while (dialogue.IsDialogueActive()) yield return null;
        yield return null;
    }
    private void BuildUI()
    {
        var canvas = RuntimePanelUI.Canvas("FogIntroCanvas", transform, 240); uiRoot = canvas.gameObject;
        dangerText = RuntimePanelUI.Text("", canvas, font, new Vector2(.15f, .8f), new Vector2(.85f, .9f), 30);
        chargeText = RuntimePanelUI.Text("", canvas, font, new Vector2(.3f, .14f), new Vector2(.7f, .23f), 34);
        bullyingLeft = RuntimePanelUI.Text("", canvas, font, new Vector2(.04f, .46f), new Vector2(.35f, .66f), 34);
        bullyingRight = RuntimePanelUI.Text("", canvas, font, new Vector2(.66f, .58f), new Vector2(.97f, .76f), 31);
        deathPanel = RuntimePanelUI.Panel("FogDeath", canvas, Vector2.zero, Vector2.one, Color.black).gameObject;
        RuntimePanelUI.Text("你的意识消散了", deathPanel.transform, font, new Vector2(.2f, .43f), new Vector2(.8f, .58f), 40);
        RuntimePanelUI.Text("[E] 重试", deathPanel.transform, font, new Vector2(.3f, .27f), new Vector2(.7f, .36f), 26);
        deathPanel.SetActive(false);
        whiteMask = RuntimePanelUI.Panel("FogDispersalWhite", canvas, Vector2.zero, Vector2.one, Color.clear, false);
        heartbeat = gameObject.AddComponent<AudioSource>(); heartbeat.loop = true; heartbeat.playOnAwake = false; heartbeat.spatialBlend = 0;
        // Generated low double thump, explicitly a synthetic Alpha placeholder.
        const int rate = 22050; var samples = new float[rate];
        for (int i = 0; i < rate; i++)
        {
            float t = i / (float)rate;
            float pulse = Mathf.Exp(-t * 35) + (t >= .2f ? .7f * Mathf.Exp(-(t - .2f) * 40) : 0);
            samples[i] = Mathf.Sin(2 * Mathf.PI * 65 * t) * pulse * .55f;
        }
        heartbeatClip = AudioClip.Create("SyntheticFogHeartbeat_Alpha", rate, 1, rate, false); heartbeatClip.SetData(samples, 0); heartbeat.clip = heartbeatClip;
        uiRoot.SetActive(false);
    }
    private void OwnCutscene() { if (!ownsCutscene) { controls.AcquireLock(ControlLockReason.Cutscene); ownsCutscene = true; } }
    private void ReleaseCutscene() { if (ownsCutscene && controls != null) controls.ReleaseLock(ControlLockReason.Cutscene); ownsCutscene = false; }
    private void OwnTransition() { if (!ownsTransition) { controls.AcquireLock(ControlLockReason.SceneTransition); ownsTransition = true; } }
    private void ReleaseTransition() { if (ownsTransition && controls != null) controls.ReleaseLock(ControlLockReason.SceneTransition); ownsTransition = false; }
    private void Warn(string text) { if (!warningShown) { Debug.LogWarning(text, this); warningShown = true; } }
    private void OnDisable()
    {
        StopAllCoroutines(); if (lamp != null) { lamp.ChargeCompleted -= ChargeCompleted; lamp.CancelCharge(); }
        if (heartbeat != null) heartbeat.Stop(); if (sprint != null) sprint.SetTemporarilySuppressed(this, false);
        ReleaseCutscene(); ReleaseTransition(); if (uiRoot != null) uiRoot.SetActive(false);
    }
    private void OnDestroy() { if (heartbeatClip != null) Destroy(heartbeatClip); }
}

