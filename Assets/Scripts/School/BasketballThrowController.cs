using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Scene-local physical throw. Story flags and the E key pickup belong to its caller.</summary>
[DisallowMultipleComponent]
public sealed class BasketballThrowController : MonoBehaviour
{
    public enum ThrowState { Closed, Aiming, Flying, ResolvingSuccess, ResolvingFailure, Completed }
    public enum MissReason { StableLowSpeed, OutOfBounds, Timeout }

    [Header("Existing runtime services (optional automatic lookup)")]
    [SerializeField] GameControlManager controls;
    [SerializeField] DialogueManager dialogue;
    [SerializeField] TMP_FontAsset font;
    [SerializeField] Camera worldCamera;

    [Header("World objects; board must be a narrow solid collider")]
    [SerializeField] Rigidbody2D ball;
    [SerializeField] CircleCollider2D ballCollider;
    [SerializeField] Collider2D backboard;
    [SerializeField] Transform launchPoint;
    [SerializeField] Transform hoopCenter;
    [SerializeField] Transform ballRestPoint;
    [SerializeField] Transform keyVisual;
    [SerializeField] Transform keyGroundPoint;

    [Header("Optional throw camera framing")]
    [SerializeField] Transform cameraFrame;
    [Min(1)] [SerializeField] float framingOrthographicSize = 5.8f;

    [Header("Alpha tuning: drag, physics and preview (not locked design values)")]
    [Min(.001f)] [SerializeField] float speedPerReferencePixel = .052f;
    [Min(1)] [SerializeField] float maximumLaunchSpeed = 18f;
    [Min(0)] [SerializeField] float minimumLaunchSpeed = 1.0f;
    [Min(2)] [SerializeField] float minimumDragReferencePixels = 18f;
    [Min(.1f)] [SerializeField] float gravityMagnitude = 12.5f;
    [Range(0, 1)] [SerializeField] float ballBounce = .48f;
    [Min(0)] [SerializeField] float ballFriction = .5f;
    [Min(.1f)] [SerializeField] float previewDuration = 2.3f;
    [Range(12, 80)] [SerializeField] int previewPointCount = 40;
    [Min(.01f)] [SerializeField] float previewWidth = .035f;

    [Header("Miss recovery: bounds are relative to launch point")]
    [SerializeField] Vector2 flightMinimumOffset = new Vector2(-13, -5);
    [SerializeField] Vector2 flightMaximumOffset = new Vector2(22, 18);
    [Min(.1f)] [SerializeField] float stableSpeed = .22f;
    [Min(.1f)] [SerializeField] float stableSeconds = .5f;
    [Min(1)] [SerializeField] float flightTimeout = 10f;
    [Min(.05f)] [SerializeField] float retryFadeSeconds = .22f;
    [Min(.05f)] [SerializeField] float retryBlackHoldSeconds = .18f;

    [Header("Confirmed board hit: authored path through hoop")]
    [Min(.1f)] [SerializeField] float boardToHoopSeconds = .45f;
    [Min(.1f)] [SerializeField] float hoopToFloorSeconds = .65f;
    [Min(.1f)] [SerializeField] float keyFallSeconds = .8f;
    [Range(1, 6)] [SerializeField] int bounceCount = 3;
    [Min(.05f)] [SerializeField] float firstBounceHeight = 1.2f;
    [Min(.05f)] [SerializeField] float firstBounceSeconds = .45f;
    [SerializeField] DialogueData missDialogue;
    [SerializeField] DialogueData successDialogue;

    [Header("Optional project / synthetic placeholder audio")]
    [SerializeField] AudioSource effects;
    [SerializeField] AudioClip throwSound, boardHitSound, bounceSound, missSound;

    public ThrowState State { get; private set; } = ThrowState.Closed;
    public bool IsOpen => State != ThrowState.Closed && State != ThrowState.Completed;
    public bool IsFlying => State == ThrowState.Flying;
    public bool IsDragging => dragging;
    public bool CanCancel => State == ThrowState.Aiming || State == ThrowState.Flying;
    public Vector2 AimedVelocity => aimedVelocity;
    public MissReason? LastMiss { get; private set; }
    public int Attempts { get; private set; }
    public event Action Completed;
    public event Action Cancelled;
    public event Action<MissReason> AttemptFailed;

    RectTransform overlay;
    TMP_Text status;
    Image powerFill;
    CanvasGroup curtain;
    LineRenderer trajectory, pullLine;
    Material lineMaterial;
    PhysicsMaterial2D throwMaterial, originalMaterial;
    BasketballBallImpact impact;
    RigidbodyType2D savedBodyType;
    bool savedSimulated, savedColliderEnabled, savedSnapshot;
    float savedGravity, savedDamping, savedAngularDamping;
    CollisionDetectionMode2D savedDetection;
    RigidbodyInterpolation2D savedInterpolation;
    Vector3 savedBallPosition, savedKeyPosition, savedCameraPosition;
    Quaternion savedBallRotation;
    float savedOrtho;
    bool ownsLock, dragging, warned, hasCameraSnapshot, successPending;
    bool restoreKeyOnAbort;
    Vector2 dragOrigin, aimedVelocity;
    float flightTime, lowSpeedTime;
    struct ThrowerCollisionPair
    {
        public Collider2D collider;
        public bool wasIgnored;
    }
    readonly List<ThrowerCollisionPair> throwerCollisionPairs = new List<ThrowerCollisionPair>();

    // Caller invokes only after K1 and once its own interaction/dialogue lock is released.
    public bool Begin()
    {
        if (State != ThrowState.Closed || !isActiveAndEnabled) return false;
        if (!ResolveAndValidate() || controls.IsLocked() || dialogue.IsDialogueActive()) return false;
        controls.AcquireLock(ControlLockReason.CloseUp);
        ownsLock = true;
        SaveBall();
        restoreKeyOnAbort = true;
        savedKeyPosition = keyVisual.position;
        impact = ball.GetComponent<BasketballBallImpact>();
        if (impact == null) impact = ball.gameObject.AddComponent<BasketballBallImpact>();
        impact.Owner = this;
        BuildPresentation();
        if (cameraFrame != null)
        {
            hasCameraSnapshot = true;
            savedCameraPosition = worldCamera.transform.position;
            savedOrtho = worldCamera.orthographicSize;
            Vector3 p = cameraFrame.position; p.z = savedCameraPosition.z;
            worldCamera.transform.position = p;
            worldCamera.orthographicSize = framingOrthographicSize;
        }
        LastMiss = null;
        Attempts = 0;
        ResetForAim();
        return true;
    }

    bool ResolveAndValidate()
    {
        if (controls == null) controls = FindAnyObjectByType<GameControlManager>();
        if (dialogue == null) dialogue = FindAnyObjectByType<DialogueManager>();
        if (worldCamera == null) worldCamera = Camera.main;
        if (ballCollider == null && ball != null) ballCollider = ball.GetComponent<CircleCollider2D>();
        bool valid = controls != null && dialogue != null && worldCamera != null && font != null
            && ball != null && ballCollider != null && !ballCollider.isTrigger && backboard != null && !backboard.isTrigger
            && backboard.enabled && launchPoint != null && hoopCenter != null && ballRestPoint != null
            && keyVisual != null && keyGroundPoint != null && keyGroundPoint.position.x < ballRestPoint.position.x
            && missDialogue != null && successDialogue != null && Physics2D.gravity.y < -.001f;
        if (!valid && !warned)
        {
            Debug.LogWarning("BasketballThrowController: bind ball, solid board, path/key markers, font and dialogues; key ground must be left of resting ball.", this);
            warned = true;
        }
        return valid;
    }

    void SaveBall()
    {
        savedSnapshot = true;
        savedBallPosition = ball.transform.position; savedBallRotation = ball.transform.rotation;
        savedBodyType = ball.bodyType; savedSimulated = ball.simulated;
        savedColliderEnabled = ballCollider.enabled; savedGravity = ball.gravityScale;
        savedDamping = ball.linearDamping; savedAngularDamping = ball.angularDamping;
        savedDetection = ball.collisionDetectionMode; savedInterpolation = ball.interpolation;
        originalMaterial = ballCollider.sharedMaterial;
        throwMaterial = new PhysicsMaterial2D("Basketball Alpha response") { bounciness = ballBounce, friction = ballFriction };
        throwerCollisionPairs.Clear();
        var thrower = controls != null ? controls.ActivePlayer : null;
        if (thrower != null)
            foreach (var other in thrower.GetComponentsInChildren<Collider2D>(true))
                if (other != null && other != ballCollider)
                    throwerCollisionPairs.Add(new ThrowerCollisionPair
                    { collider = other, wasIgnored = Physics2D.GetIgnoreCollision(ballCollider, other) });
    }

    void ResetForAim()
    {
        FreezeBall();
        ball.transform.SetPositionAndRotation(launchPoint.position, Quaternion.identity);
        ball.position = launchPoint.position;
        State = ThrowState.Aiming;
        dragging = false; aimedVelocity = Vector2.zero;
        if (overlay != null) overlay.gameObject.SetActive(true);
        if (powerFill != null) powerFill.rectTransform.anchorMax = new Vector2(0, 1);
        SetStatus("按住篮球，向投掷方向的反方向拖动，再松手。  [Esc] 放下篮球");
        HideGuides();
    }

    void Update()
    {
        if (!IsOpen) return;
        if (Input.GetKeyDown(KeyCode.Escape) && CanCancel) { Cancel(); return; }
        if (successPending)
        {
            successPending = false;
            StartCoroutine(ResolveSuccess());
            return;
        }
        if (State == ThrowState.Aiming) UpdateAimInput();
    }

    void UpdateAimInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 point = worldCamera.WorldToScreenPoint(ball.position);
            float radiusPixels = Mathf.Max(30f * Screen.height / 900f,
                ballCollider.radius * Mathf.Abs(ball.transform.lossyScale.x) * Screen.height / (worldCamera.orthographicSize * 2f) * 1.5f);
            if (Vector2.Distance(Input.mousePosition, point) <= radiusPixels)
            {
                dragging = true;
                dragOrigin = point;
            }
        }
        if (!dragging) return;
        Vector2 pull = dragOrigin - (Vector2)Input.mousePosition;
        aimedVelocity = ThrowTrajectory2D.VelocityFromScreenPull(pull, Screen.height, speedPerReferencePixel, maximumLaunchSpeed);
        float power = Mathf.Clamp01(aimedVelocity.magnitude / maximumLaunchSpeed);
        powerFill.rectTransform.anchorMax = new Vector2(power, 1);
        SetStatus("力度 " + Mathf.RoundToInt(power * 100) + "% · 轨迹线是篮球的飞行方向 · 松手投掷  [Esc] 取消");
        UpdateGuides();
        if (!Input.GetMouseButtonUp(0)) return;
        dragging = false;
        if (pull.magnitude * 900f / Mathf.Max(1, Screen.height) < minimumDragReferencePixels || aimedVelocity.magnitude < minimumLaunchSpeed)
        {
            HideGuides();
            SetStatus("再拉开一点距离，然后松手投掷。  [Esc] 放下篮球");
            return;
        }
        Launch(aimedVelocity);
    }

    // Deliberately private: normal gameplay cannot award success by invoking a button/API.
    void Launch(Vector2 velocity)
    {
        if (State != ThrowState.Aiming) return;
        State = ThrowState.Flying;
        Attempts++;
        flightTime = lowSpeedTime = 0;
        ballCollider.enabled = true;
        ballCollider.sharedMaterial = throwMaterial;
        ball.bodyType = RigidbodyType2D.Dynamic;
        ball.gravityScale = gravityMagnitude / Mathf.Max(.001f, Mathf.Abs(Physics2D.gravity.y));
        ball.linearDamping = 0;
        ball.angularDamping = .1f;
        ball.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        ball.interpolation = RigidbodyInterpolation2D.Interpolate;
        ball.simulated = true;
        // Unity can discard pair ignores when a collider/body is unsimulated.
        // Reapply only after the ball becomes simulated, before the next physics step.
        ApplyThrowerCollisionIgnores();
        ball.linearVelocity = velocity;
        ball.angularVelocity = -velocity.x * 45;
        ball.WakeUp();
        HideGuides();
        SetStatus("篮球飞行中……  [Esc] 放下篮球");
        Play(throwSound, .35f);
    }

    void FixedUpdate()
    {
        if (State != ThrowState.Flying) return;
        flightTime += Time.fixedDeltaTime;
        Vector2 offset = ball.position - (Vector2)launchPoint.position;
        if (offset.x < flightMinimumOffset.x || offset.x > flightMaximumOffset.x
            || offset.y < flightMinimumOffset.y || offset.y > flightMaximumOffset.y)
        { StartMiss(MissReason.OutOfBounds); return; }
        if (flightTime >= flightTimeout) { StartMiss(MissReason.Timeout); return; }
        if (flightTime > .4f && ball.linearVelocity.magnitude <= stableSpeed) lowSpeedTime += Time.fixedDeltaTime;
        else lowSpeedTime = 0;
        if (lowSpeedTime >= stableSeconds) StartMiss(MissReason.StableLowSpeed);
    }

    internal void ObserveBallCollision(Collision2D collision)
    {
        if (State != ThrowState.Flying || collision.collider != backboard) return;
        // Collision2D comes from the real ball on a solid, deliberately narrow backboard.
        // A basket/rim trigger or a broad vicinity check can never succeed here.
        State = ThrowState.ResolvingSuccess;
        successPending = true;
        FreezeBall();
        Play(boardHitSound, .45f);
    }

    void StartMiss(MissReason reason)
    {
        if (State != ThrowState.Flying) return;
        LastMiss = reason;
        State = ThrowState.ResolvingFailure;
        StartCoroutine(ResolveMiss(reason));
    }

    IEnumerator ResolveMiss(MissReason reason)
    {
        FreezeBall();
        SetStatus("这都没中...");
        Play(missSound, .25f);
        if (overlay != null) overlay.gameObject.SetActive(false);
        yield return Say(missDialogue);
        if (overlay != null) overlay.gameObject.SetActive(true);
        yield return Fade(1);
        RestorePhysics(true);
        RestoreCameraFrame();
        yield return new WaitForSecondsRealtime(retryBlackHoldSeconds);
        yield return Fade(0);
        State = ThrowState.Closed;
        restoreKeyOnAbort = false;
        RestoreCameraAndRelease();
        DestroyPresentation();
        AttemptFailed?.Invoke(reason);
    }

    IEnumerator ResolveSuccess()
    {
        SetStatus("击中了挡板！");
        Vector3 start = ball.transform.position;
        Vector3 aboveHoop = hoopCenter.position + Vector3.up * .38f;
        for (float t = 0; t < boardToHoopSeconds; t += Time.unscaledDeltaTime)
        {
            float f = Mathf.Clamp01(t / boardToHoopSeconds);
            MoveBall(Vector3.Lerp(start, aboveHoop, f) + Vector3.up * (.6f * Mathf.Sin(f * Mathf.PI)));
            yield return null;
        }
        MoveBall(aboveHoop);
        Vector3 keyFrom = keyVisual.position;
        float fallDuration = Mathf.Max(hoopToFloorSeconds, keyFallSeconds);
        for (float t = 0; t < fallDuration; t += Time.unscaledDeltaTime)
        {
            float b = Mathf.Clamp01(t / hoopToFloorSeconds);
            // Vertical entry crosses the actual hoop center before moving toward its landing marker.
            Vector3 through = Vector3.Lerp(aboveHoop, hoopCenter.position - Vector3.up * .4f, Mathf.Min(1, b * 3));
            Vector3 below = Vector3.Lerp(hoopCenter.position - Vector3.up * .4f, ballRestPoint.position, Mathf.Clamp01((b - 1f / 3) * 1.5f));
            MoveBall(b < 1f / 3 ? through : below);
            float k = Mathf.Clamp01(t / keyFallSeconds);
            keyVisual.position = Vector3.Lerp(keyFrom, keyGroundPoint.position, k * k);
            yield return null;
        }
        MoveBall(ballRestPoint.position);
        keyVisual.position = keyGroundPoint.position;
        for (int i = 0; i < bounceCount; i++)
        {
            Play(bounceSound, .3f / (i + 1));
            float height = firstBounceHeight * Mathf.Pow(.46f, i);
            float seconds = firstBounceSeconds * Mathf.Pow(.78f, i);
            for (float t = 0; t < seconds; t += Time.unscaledDeltaTime)
            {
                float f = t / seconds;
                MoveBall(ballRestPoint.position + Vector3.up * (4 * height * f * (1 - f)));
                yield return null;
            }
            MoveBall(ballRestPoint.position);
        }
        if (overlay != null) overlay.gameObject.SetActive(false);
        yield return Say(successDialogue);
        restoreKeyOnAbort = false;
        State = ThrowState.Completed;
        // Release before the caller enables the reachable, separate E pickup.
        RestoreCameraAndRelease();
        DestroyPresentation();
        RestorePhysics(false);
        Completed?.Invoke();
    }

    IEnumerator Say(DialogueData data)
    {
        while (dialogue != null && dialogue.IsDialogueActive()) yield return null;
        if (dialogue == null || data == null) yield break;
        dialogue.StartDialogue(data);
        yield return null;
        while (dialogue != null && dialogue.IsDialogueActive()) yield return null;
        yield return null;
    }

    public void Cancel()
    {
        if (!CanCancel) return;
        StopAllCoroutines();
        Abort();
        Cancelled?.Invoke();
    }

    void OnApplicationFocus(bool focus)
    {
        // Releasing the mouse outside Unity never launches an unintended throw on focus return.
        if (!focus && dragging) { dragging = false; aimedVelocity = Vector2.zero; HideGuides(); }
    }

    void OnDisable()
    {
        StopAllCoroutines();
        Abort();
    }

    void Abort()
    {
        successPending = dragging = false;
        if (State == ThrowState.Completed)
        {
            RestoreCameraAndRelease();
            DestroyPresentation();
            return;
        }
        if (restoreKeyOnAbort && keyVisual != null) keyVisual.position = savedKeyPosition;
        RestorePhysics(true);
        State = ThrowState.Closed;
        RestoreCameraAndRelease();
        DestroyPresentation();
    }

    void FreezeBall()
    {
        if (ball == null) return;
        ball.linearVelocity = Vector2.zero;
        ball.angularVelocity = 0;
        ball.simulated = false;
    }

    void MoveBall(Vector3 position)
    {
        ball.transform.position = position;
        ball.position = position;
        ball.transform.Rotate(0, 0, -180 * Time.unscaledDeltaTime);
    }

    void RestorePhysics(bool restorePosition)
    {
        if (!savedSnapshot || ball == null) { RestoreThrowerCollisions(); return; }
        FreezeBall();
        if (restorePosition)
        {
            ball.transform.SetPositionAndRotation(savedBallPosition, savedBallRotation);
            ball.position = savedBallPosition;
        }
        ball.bodyType = savedBodyType; ball.gravityScale = savedGravity;
        ball.linearDamping = savedDamping; ball.angularDamping = savedAngularDamping;
        ball.collisionDetectionMode = savedDetection; ball.interpolation = savedInterpolation;
        ballCollider.sharedMaterial = originalMaterial;
        // Restore pairs while the ball's collider is live; no physics step occurs
        // between this temporary activation and restoration of its original state.
        ballCollider.enabled = true;
        ball.simulated = true;
        RestoreThrowerCollisions();
        ballCollider.enabled = savedColliderEnabled;
        // A successful authored bounce is final. Keep the resting prop still even if the
        // original authoring body was Dynamic; cancellations restore the original setup.
        ball.simulated = restorePosition && savedSimulated;
        if (throwMaterial != null) Destroy(throwMaterial);
        throwMaterial = null;
        if (impact != null) impact.Owner = null;
        savedSnapshot = false;
    }

    void ApplyThrowerCollisionIgnores()
    {
        if (ballCollider == null) return;
        foreach (var pair in throwerCollisionPairs)
            if (pair.collider != null) Physics2D.IgnoreCollision(ballCollider, pair.collider, true);
    }

    void RestoreThrowerCollisions()
    {
        if (ballCollider != null)
            foreach (var pair in throwerCollisionPairs)
                if (pair.collider != null) Physics2D.IgnoreCollision(ballCollider, pair.collider, pair.wasIgnored);
        throwerCollisionPairs.Clear();
    }

    void RestoreCameraAndRelease()
    {
        RestoreCameraFrame();
        if (ownsLock && controls != null) controls.ReleaseLock(ControlLockReason.CloseUp);
        ownsLock = false;
    }

    void RestoreCameraFrame()
    {
        if (hasCameraSnapshot && worldCamera != null)
        {
            worldCamera.transform.position = savedCameraPosition;
            worldCamera.orthographicSize = savedOrtho;
        }
        hasCameraSnapshot = false;
    }

    void BuildPresentation()
    {
        overlay = RuntimePanelUI.Canvas("BasketballThrowOverlay", transform, 130);
        // The middle remains the actual world view; only the framing bars mask the edges.
        RuntimePanelUI.Panel("ThrowHeader", overlay, new Vector2(0, .89f), Vector2.one, new Color(.045f, .055f, .065f, .94f));
        RuntimePanelUI.Text("把架上的钥匙打下来", overlay, font, new Vector2(.12f, .905f), new Vector2(.88f, .982f), 31);
        RuntimePanelUI.Panel("ThrowFooter", overlay, Vector2.zero, new Vector2(1, .14f), new Color(.045f, .055f, .065f, .94f));
        status = RuntimePanelUI.Text("", overlay, font, new Vector2(.035f, .055f), new Vector2(.965f, .13f), 25);
        var meter = RuntimePanelUI.Panel("ThrowPowerTrack", overlay, new Vector2(.32f, .018f), new Vector2(.68f, .037f), new Color(.22f, .25f, .26f));
        powerFill = RuntimePanelUI.Panel("ThrowPower", meter.transform, Vector2.zero, Vector2.one, new Color(.95f, .68f, .24f));
        powerFill.rectTransform.anchorMax = new Vector2(0, 1);
        var black = RuntimePanelUI.Panel("ThrowRetryFade", overlay, Vector2.zero, Vector2.one, Color.black);
        curtain = black.gameObject.AddComponent<CanvasGroup>(); curtain.alpha = 0; curtain.blocksRaycasts = false;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader != null) lineMaterial = new Material(shader);
        trajectory = CreateLine("BasketballTrajectory", new Color(1, .86f, .38f, .88f), previewWidth);
        pullLine = CreateLine("BasketballPullDirection", new Color(.96f, .65f, .22f, .9f), previewWidth * 1.5f);
    }

    LineRenderer CreateLine(string name, Color color, float width)
    {
        var g = new GameObject(name); g.transform.SetParent(transform, false);
        var line = g.AddComponent<LineRenderer>();
        line.sharedMaterial = lineMaterial; line.useWorldSpace = true;
        line.startColor = line.endColor = color; line.startWidth = line.endWidth = width;
        line.numCapVertices = 4; line.sortingOrder = 500; line.enabled = false;
        return line;
    }

    void UpdateGuides()
    {
        if (trajectory == null || pullLine == null) return;
        trajectory.enabled = pullLine.enabled = true;
        trajectory.positionCount = previewPointCount;
        Vector2 gravity = Vector2.down * gravityMagnitude;
        for (int i = 0; i < previewPointCount; i++)
        {
            float t = previewDuration * i / (previewPointCount - 1);
            Vector3 p = ThrowTrajectory2D.Position(launchPoint.position, aimedVelocity, gravity, t);
            p.z = ball.transform.position.z;
            trajectory.SetPosition(i, p);
        }
        Vector3 mouseWorld = worldCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y,
            Mathf.Abs(worldCamera.transform.position.z - ball.transform.position.z)));
        mouseWorld.z = ball.transform.position.z;
        pullLine.positionCount = 2;
        pullLine.SetPosition(0, ball.transform.position); pullLine.SetPosition(1, mouseWorld);
    }

    void HideGuides()
    {
        if (trajectory != null) trajectory.enabled = false;
        if (pullLine != null) pullLine.enabled = false;
    }

    IEnumerator Fade(float alpha)
    {
        if (curtain == null) yield break;
        float start = curtain.alpha;
        for (float t = 0; t < retryFadeSeconds; t += Time.unscaledDeltaTime)
        { curtain.alpha = Mathf.Lerp(start, alpha, t / retryFadeSeconds); yield return null; }
        curtain.alpha = alpha;
    }

    void DestroyPresentation()
    {
        if (overlay != null) { overlay.gameObject.SetActive(false); Destroy(overlay.gameObject); }
        if (trajectory != null) Destroy(trajectory.gameObject);
        if (pullLine != null) Destroy(pullLine.gameObject);
        if (lineMaterial != null) Destroy(lineMaterial);
        overlay = null; trajectory = pullLine = null; lineMaterial = null; status = null; curtain = null;
    }

    void SetStatus(string text) { if (status != null) status.text = text; }
    void Play(AudioClip clip, float volume) { if (effects != null && clip != null) effects.PlayOneShot(clip, volume); }
}
