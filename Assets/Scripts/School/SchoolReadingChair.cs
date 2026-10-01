using UnityEngine;
using UnityEngine.UI;

// Optional school rest. This owns one CloseUp lock and never changes story/inventory.
[DisallowMultipleComponent]
public class SchoolReadingChair : MonoBehaviour
{
    public SchoolYouthController flow;
    public Transform seat, standPosition;
    public AudioClip restingMusic;
    [Min(1f)] public float wideSize = 7.2f;
    [Range(0f, 1f)] public float musicVolume = .12f;
    public bool IsOpen { get; private set; }

    GameControlManager controls;
    SchoolLayoutController layout;
    PlayerController player;
    Rigidbody2D body;
    CameraFollow follow;
    Camera view;
    GameObject ui;
    Button exitButton;
    AudioSource music;
    AudioClip generatedMusic;
    bool ownsLock, snapshotValid, warned;
    int openedFrame;
    string roomId;
    Vector3 originalPosition, originalScale, cameraPosition;
    Quaternion originalRotation, cameraRotation;
    Vector2 horizontalLimits, verticalLimits;
    float verticalOffset, facing, originalSize;
    bool followY, followEnabled, bodySimulated;

    public void Toggle()
    {
        if (IsOpen) { Close(); return; }
        if (!isActiveAndEnabled || flow == null || !flow.Ready || !flow.IsYouthMemory || flow.Busy)
            return;
        controls = flow.Controls;
        if (controls == null || controls.IsLocked() || flow.Dialogue == null
            || flow.Dialogue.IsDialogueActive() || flow.Dialogue.DidDialogueEndThisFrame()) return;
        player = controls.ActivePlayer;
        layout = FindAnyObjectByType<SchoolLayoutController>();
        follow = FindAnyObjectByType<CameraFollow>();
        view = follow != null ? follow.GetComponent<Camera>() : null;
        if (player == null || layout == null || follow == null || view == null || !view.orthographic
            || seat == null || flow.font == null)
        { Warn("SchoolReadingChair needs the active youth, school layout, orthographic camera, seat and existing font."); return; }
        if (layout.gameObject.scene != gameObject.scene || seat.gameObject.scene != gameObject.scene)
        { Warn("SchoolReadingChair seat and layout must belong to this school scene."); return; }

        roomId = layout.CurrentRoom;
        originalPosition = player.transform.position;
        originalScale = player.transform.localScale;
        originalRotation = player.transform.rotation;
        body = player.GetComponent<Rigidbody2D>();
        bodySimulated = body != null && body.simulated;
        horizontalLimits = follow.HorizontalLimits;
        verticalLimits = follow.VerticalLimits;
        verticalOffset = follow.VerticalOffset;
        followY = follow.FollowsY;
        facing = follow.Facing;
        followEnabled = follow.enabled;
        cameraPosition = view.transform.position;
        cameraRotation = view.transform.rotation;
        originalSize = view.orthographicSize;
        snapshotValid = true;

        controls.AcquireLock(ControlLockReason.CloseUp);
        ownsLock = true;
        IsOpen = true;
        openedFrame = Time.frameCount;
        if (body != null) { body.linearVelocity = Vector2.zero; body.simulated = false; }
        player.transform.position = seat.position;
        // Placeholder seated pose only; no adult outfit component or permanent scale change.
        player.transform.localScale = new Vector3(originalScale.x, originalScale.y * .82f, originalScale.z);
        follow.enabled = false;
        view.orthographicSize = Mathf.Max(originalSize, wideSize);
        var room = layout.data != null ? layout.data.Room(roomId) : null;
        float centerX = room != null ? room.origin.x + room.width * .5f : seat.position.x;
        float centerY = room != null ? room.CameraY : cameraPosition.y;
        view.transform.position = new Vector3(centerX, centerY, cameraPosition.z);
        Physics2D.SyncTransforms();
        BuildUI();
        ui.SetActive(true);
        PlayMusic();
    }

    void Update()
    {
        if (!IsOpen) return;
        if (flow == null || !flow.IsYouthMemory || controls == null || controls.ActivePlayer != player)
        { Close(); return; }
        if (Time.frameCount > openedFrame && GameplayInput.GetKeyDown(KeyCode.Escape)) Close();
    }

    void BuildUI()
    {
        if (ui != null) return;
        var canvas = RuntimePanelUI.Canvas("SchoolReadingChairUI", transform, 135);
        ui = canvas.gameObject;
        // There is no fullscreen panel/raycast surface; the view remains unobstructed.
        exitButton = RuntimePanelUI.Button("[Esc] 起身", canvas, flow.font,
            new Vector2(.80f, .035f), new Vector2(.975f, .105f), Close);
    }

    public void Close()
    {
        if (!IsOpen && !snapshotValid && !ownsLock) return;
        IsOpen = false;
        if (ui != null) ui.SetActive(false);
        if (music != null) music.Stop();
        bool restoreWorld = snapshotValid && player != null && layout != null && follow != null && view != null
            && flow != null && flow.IsYouthMemory && controls != null && controls.ActivePlayer == player
            && layout.gameObject.scene == gameObject.scene && view.gameObject.scene == gameObject.scene;
        if (snapshotValid && player != null)
        {
            player.transform.localScale = originalScale;
            player.transform.rotation = originalRotation;
            if (restoreWorld)
            {
                Vector3 standing = originalPosition;
                if (standPosition != null && standPosition.gameObject.scene == gameObject.scene
                    && IsSafeStandingPoint(standPosition.position)) standing = standPosition.position;
                player.transform.position = standing;
                if (body != null) body.position = standing;
            }
            if (body != null) { body.linearVelocity = Vector2.zero; body.simulated = bodySimulated; }
        }
        if (restoreWorld)
        {
            // Normally unchanged; restore the logical room too if a local owner changed it.
            if (layout.CurrentRoom != roomId) layout.EnterRoom(roomId);
            follow.SetLimits(horizontalLimits.x, horizontalLimits.y);
            follow.SetVerticalLimits(followY, verticalOffset, verticalLimits.x, verticalLimits.y);
            follow.SetFacing(facing);
            view.orthographicSize = originalSize;
            view.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
        }
        snapshotValid = false;
        if (ownsLock)
        {
            ownsLock = false;
            if (controls != null) controls.ReleaseLock(ControlLockReason.CloseUp);
        }
        // Releasing a control lock updates CameraFollow.enabled. Preserve the captured
        // disabled state, but never enable it through another owner's active lock.
        if (restoreWorld) follow.enabled = followEnabled && !controls.IsLocked();
        Physics2D.SyncTransforms();
    }

    bool IsSafeStandingPoint(Vector3 position)
    {
        var room = layout.data != null ? layout.data.Room(roomId) : null;
        if (room != null && (position.x < room.origin.x + .35f || position.x > room.origin.x + room.width - .35f
            || position.y < room.origin.y || position.y > room.origin.y + 6f)) return false;
        var collider = player.GetComponent<Collider2D>();
        if (collider == null) return true;
        // Body simulation is paused while seated, so Collider.bounds can be empty.
        // Use the standing placeholder size as a conservative query; unsafe authored
        // markers fall back to the original, already reachable interaction position.
        Vector2 size = new Vector2(Mathf.Abs(originalScale.x) * .90f, Mathf.Abs(originalScale.y) * .90f);
        foreach (var hit in Physics2D.OverlapBoxAll(position, size, 0))
            if (hit != null && !hit.isTrigger && hit.transform != player.transform
                && !hit.transform.IsChildOf(player.transform)) return false;
        return true;
    }

    void PlayMusic()
    {
        if (music == null)
        {
            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.spatialBlend = 0;
        }
        if (restingMusic == null && generatedMusic == null) generatedMusic = CreateWarmLoop();
        music.clip = restingMusic != null ? restingMusic : generatedMusic;
        music.volume = musicVolume;
        music.Play();
    }

    static AudioClip CreateWarmLoop()
    {
        const int rate = 22050, chordSeconds = 4, totalSeconds = 16;
        var samples = new float[rate * totalSeconds];
        // Cmaj7, Am7, Fmaj7, G6: soft local sine chords, no network or asset writes.
        int[,] notes = { { 48, 60, 64, 67, 71 }, { 45, 57, 60, 64, 67 },
                         { 41, 53, 57, 60, 64 }, { 43, 55, 59, 62, 64 } };
        for (int i = 0; i < samples.Length; i++)
        {
            float t = i / (float)rate;
            int chord = Mathf.Min(3, (int)(t / chordSeconds));
            float local = t - chord * chordSeconds;
            float envelope = Mathf.Sin(Mathf.PI * local / chordSeconds);
            envelope *= envelope;
            float sum = 0;
            for (int note = 0; note < 5; note++)
            {
                float hz = 440f * Mathf.Pow(2f, (notes[chord, note] - 69) / 12f);
                float wave = Mathf.Sin(2f * Mathf.PI * hz * t)
                    + .12f * Mathf.Sin(4f * Mathf.PI * hz * t);
                sum += wave * (note == 0 ? .18f : .10f);
            }
            samples[i] = sum * envelope * .32f;
        }
        var clip = AudioClip.Create("SchoolReading_LocalWarmLoop", samples.Length, 1, rate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    void OnDisable()
    {
        Close();
        if (music != null) { music.Stop(); music.clip = null; Destroy(music); music = null; }
        if (generatedMusic != null) { Destroy(generatedMusic); generatedMusic = null; }
    }
    void OnDestroy()
    {
        if (exitButton != null) exitButton.onClick.RemoveListener(Close);
        // OnDisable owns audio destruction; never destroy the Inspector-assigned clip.
    }
    void Warn(string message) { if (!warned) { warned = true; Debug.LogWarning(message, this); } }
}
