using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Scene04 exploration and the existing youth handover; optional Stage 5B flow owns subsequent Memory progression.
public sealed class SchoolSceneController : MonoBehaviour
{
    [Header("Shared school / existing modules")]
    public SchoolLayoutController layout;
    public SchoolYouthController youthFlow;
    public SchoolChaseController chase;
    public BasketballThrowController basketball;
    public PencilDeskCloseup desk;
    public SchoolCameraCaptureContext captureContext;
    public TMP_FontAsset font;
    public CanvasGroup whiteFlash;

    [Header("Items / outfit assets; existing inventory retains all earlier contents")]
    public ItemData equipmentKey, pencil;
    public OutfitData schoolUniform;

    [Header("World objects (Otherworld objects, except explicitly youth)")]
    public SpriteRenderer basketKeyVisual;
    public Collider2D basketKeyPickup;
    public Transform basketKeyOnHoop, basketKeyOnGround;
    public GameObject uniformVisual;
    public Transform equipmentSoundOrigin, basketReactionOrigin;
    public AudioSource effects;
    public AudioClip equipmentMovementSound;
    [Min(.5f)] public float equipmentVoiceRadius = 3.1f;
    [Min(.5f)] public float basketReactionRadius = 6.5f;
    [Min(1)] public float cameraCaptureDistance = 12f;

    [Header("Independent youth actor / matched classroom opening")]
    public PlayerController youth;
    public Transform youthOpeningPosition;
    public GameObject zhengTaoVisual, youthGrabbingArm;
    public string classroomRoomId = "Class3_2";
    [Min(.05f)] public float whiteInSeconds = .12f;
    [Min(.05f)] public float whiteHoldSeconds = .13f;
    [Min(.05f)] public float whiteOutSeconds = .22f;

    [Header("Inspector dialogue assets; original writing belongs to data")]
    public DialogueData fogReturnDialogue;
    public DialogueData securityWindowDialogue;
    public DialogueData equipmentVoiceDialogue, equipmentEchoDialogue, equipmentLockedDialogue;
    public DialogueData basketballInvestigation, keyRevealedDialogue, keyObtainedDialogue;
    public DialogueData uniformObtainedDialogue, pencilObtainedDialogue;
    public DialogueData youthOpeningDialogue;

    [Header("Short UI labels / notices")]
    [TextArea] public string cameraReactionNotice = "相机似乎有些反应...";
    [TextArea] public string securityNoResponse = "无反应";
    [TextArea] public string endpointNotice = "Stage 5A 已结束。可以在教室内活动；退出 Play Mode 可结束本次体验。";

    public StoryStateManager Story { get; private set; }
    public GameControlManager Controls { get; private set; }
    public InventoryManager Inventory { get; private set; }
    public DialogueManager Dialogue { get; private set; }
    public bool Ready { get; private set; }
    public bool Busy => busy || (youthFlow != null && youthFlow.Busy) || (basketball != null && basketball.IsOpen) || (desk != null && desk.IsOpen) || modal != null;
    public bool SecurityPanelOpen => modal != null && securityDigits != null;
    public string SecurityEnteredCode { get { if (securityDigits == null) return ""; string s = ""; foreach (var d in securityDigits) s += d.Value.ToString(); return s; } }

    PersistentRuntime runtime;
    CameraModeController cameraMode;
    StoryNoticeUI notices;
    PlayerController adult;
    bool busy, ownsInteraction, ownsTransition, ownsModal, warned, reactionShown;
    bool previewingKey, handingOver, arrivedFromFogWhite, fogArrivalNoticeShown;
    Color keyOriginalColor;
    GameObject modal;
    FourDigitLock[] securityDigits;
    TMP_Text securityFeedback;

    void Awake()
    {
        // Capture the incoming transition, not merely a permanent completion
        // flag. Reloading the school later must not repeat the arrival notice.
        var incomingStory = PersistentRuntime.Instance != null
            ? PersistentRuntime.Instance.GetComponent<StoryStateManager>() : null;
        arrivedFromFogWhite = WhiteSceneTransition.IsActive && incomingStory != null
            && incomingStory.Has(StoryFlag.FogIntroCompleted) && incomingStory.Has(StoryFlag.OilLampExhausted)
            && !incomingStory.Has(StoryFlag.SchoolNameRevealed) && !incomingStory.Has(StoryFlag.Scene05OpeningPlayed);
        // K0 must not expose the key for a frame while waiting for persistent services.
        if (basketKeyVisual != null) basketKeyVisual.enabled = false;
        if (basketKeyPickup != null) basketKeyPickup.enabled = false;
    }

    IEnumerator Start()
    {
        // PersistentRuntime and SchoolLayoutController complete their normal Start first.
        yield return null;
        runtime = PersistentRuntime.Instance;
        if (runtime == null || layout == null || font == null || basketball == null || desk == null
            || captureContext == null || basketKeyVisual == null || basketKeyPickup == null
            || basketKeyOnHoop == null || basketKeyOnGround == null || youth == null || youthOpeningPosition == null)
        { Warn("SchoolSceneController needs its existing runtime, school, modules, key markers and separate youth actor."); yield break; }
        Story = runtime.GetComponent<StoryStateManager>();
        Controls = runtime.GetComponent<GameControlManager>();
        Inventory = runtime.GetComponent<InventoryManager>();
        Dialogue = runtime.GetComponentInChildren<DialogueManager>(true);
        cameraMode = runtime.GetComponent<CameraModeController>();
        notices = runtime.GetComponent<StoryNoticeUI>();
        if (Story == null || Controls == null || Inventory == null || Dialogue == null || cameraMode == null)
        { Warn("SchoolSceneController: persistent service binding is incomplete."); yield break; }
        adult = Controls.ActivePlayer;
        keyOriginalColor = basketKeyVisual.color;
        captureContext.school = this;
        cameraMode.ContextAvailable = true;
        cameraMode.SetCaptureContext(captureContext);
        desk.Initialize(font, Controls);
        desk.Completed += OnDeskCompleted;
        basketball.Completed += OnBasketballCompleted;
        Story.Changed += SyncProps;
        if (whiteFlash == null)
        {
            var canvas = RuntimePanelUI.Canvas("SchoolOpeningWhiteFlash", transform, 220);
            RuntimePanelUI.Panel("White", canvas, Vector2.zero, Vector2.one, Color.white);
            whiteFlash = canvas.gameObject.AddComponent<CanvasGroup>();
        }
        whiteFlash.alpha = 0; whiteFlash.blocksRaycasts = false;
        if (youthGrabbingArm != null) youthGrabbingArm.SetActive(false);
        if (zhengTaoVisual != null) zhengTaoVisual.SetActive(false);
        Ready = true;
        SyncProps();
        if(chase!=null) {
            chase.Initialize();
            if(chase.HasSessionToRestore){yield return chase.RestoreCheckpoint();yield break;}
        }
        if (Story.Has(StoryFlag.Scene05OpeningPlayed))
        {
            SwitchToYouth();
            if (youthFlow != null)
            {
                Controls.AcquireLock(ControlLockReason.SceneTransition); ownsTransition = true;
                yield return youthFlow.ContinueOpening(true);
                UnlockTransition();
            }
            else
            {
                layout.SetNarrative(SchoolActor.Youth, SchoolPhase.YouthEntrance);
                notices?.Show(endpointNotice, 6);
            }
        }
        else if (Story.Has(StoryFlag.SchoolNameRevealed))
            StartCoroutine(YouthOpening());
        else
        {
            if (youth != adult) youth.gameObject.SetActive(false);
            layout.SetNarrative(SchoolActor.Adult, SchoolPhase.OtherworldExplore);
            if (arrivedFromFogWhite) StartCoroutine(ShowFogArrivalNotice());
        }
    }

    IEnumerator ShowFogArrivalNotice()
    {
        while (WhiteSceneTransition.IsActive) yield return null;
        if (fogArrivalNoticeShown || !Ready || layout.Actor != SchoolActor.Adult
            || !Story.Has(StoryFlag.OilLampExhausted)) yield break;
        fogArrivalNoticeShown = true;
        notices?.Show(OilLampController.ExhaustedMessage, 4);
    }

    void Update()
    {
        if (!Ready) return;
        if (modal != null && Input.GetKeyDown(KeyCode.Escape)) CloseModal();
        if ((chase!=null&&chase.IsActive) || Busy || Controls.IsLocked() || layout.Actor != SchoolActor.Adult
            || layout.Variant != SchoolWorldVariant.Otherworld || layout.CurrentRoom != "Floor01") return;
        var active = Controls.ActivePlayer;
        if (active == null) return;
        if (!Story.Has(StoryFlag.EquipmentVoicePlayed) && equipmentSoundOrigin != null
            && Vector2.Distance(active.transform.position, equipmentSoundOrigin.position) <= equipmentVoiceRadius)
        {
            StartCoroutine(EquipmentVoice());
            return;
        }
        if (!reactionShown && !Story.Has(StoryFlag.BasketKeyRevealed) && basketReactionOrigin != null
            && Vector2.Distance(active.transform.position, basketReactionOrigin.position) <= basketReactionRadius)
        { reactionShown = true; notices?.Show(cameraReactionNotice, 3); }
    }

    public void Interact(SchoolInteractable source)
    {
        if (source == null || !Ready || Busy || Controls.IsLocked()) return;
        if(chase!=null&&chase.HandleLegacy(source))return;
        if (youthFlow != null && youthFlow.HandleLegacy(source)) return;
        if (layout.IsYouthEndpoint) { notices?.Show(endpointNotice, 5); return; }
        switch (source.action)
        {
            case SchoolAction.SecurityDoor: OpenSecurityPanel(); return;
            case SchoolAction.SecurityWindow: StartCoroutine(WindowView()); return;
            case SchoolAction.OwnDesk: desk.Open(true, Inventory.HasItem(pencil)); return;
            case SchoolAction.ScratchedDesk: desk.Open(false, Inventory.HasItem(pencil)); return;
            case SchoolAction.EquipmentDoor:
                if (source.transition == null) { Warn("School equipment door needs its original RoomTransitionInteractable."); return; }
                if (Story.Has(StoryFlag.EquipmentDoorOpened)) { source.transition?.Interact(); return; }
                if (equipmentKey != null && Inventory.HasItem(equipmentKey))
                { Story.Set(StoryFlag.EquipmentDoorOpened); source.transition?.Interact(); return; }
                StartCoroutine(Talk(equipmentLockedDialogue)); return;
            case SchoolAction.EquipmentEcho:
                if (!Story.Has(StoryFlag.EquipmentVoicePlayed)) StartCoroutine(EquipmentVoice());
                else StartCoroutine(Talk(equipmentEchoDialogue));
                return;
            case SchoolAction.Basketball:
                if (!Story.Has(StoryFlag.BasketKeyRevealed)) StartCoroutine(Talk(basketballInvestigation));
                else if (!Story.Has(StoryFlag.BasketKeyDropped) && !Story.Has(StoryFlag.BasketKeyCollected)) basketball.Begin();
                return;
            case SchoolAction.BasketKey: StartCoroutine(CollectKey()); return;
            case SchoolAction.UniformAndPencil: StartCoroutine(CollectUniformAndPencil()); return;
            case SchoolAction.FogReturn: StartCoroutine(Talk(fogReturnDialogue)); return;
            default: StartCoroutine(Talk(source.investigation)); return;
        }
    }

    IEnumerator EquipmentVoice()
    {
        if (Story.Has(StoryFlag.EquipmentVoicePlayed)) yield break;
        busy = true; LockInteraction();
        Story.Set(StoryFlag.EquipmentVoicePlayed);
        if (effects != null && equipmentMovementSound != null)
        { effects.PlayOneShot(equipmentMovementSound, .4f); yield return new WaitForSecondsRealtime(.65f); }
        yield return Say(equipmentVoiceDialogue);
        UnlockInteraction(); busy = false;
    }

    IEnumerator CollectKey()
    {
        if (!Story.Has(StoryFlag.BasketKeyDropped) || Story.Has(StoryFlag.BasketKeyCollected)) yield break;
        if (equipmentKey == null) { Warn("SchoolSceneController: equipment key asset missing."); yield break; }
        busy = true; LockInteraction();
        if (!Inventory.HasItem(equipmentKey)) Inventory.AddItem(equipmentKey);
        if (Inventory.HasItem(equipmentKey)) Story.Set(StoryFlag.BasketKeyCollected);
        SyncProps();
        yield return Say(keyObtainedDialogue);
        UnlockInteraction(); busy = false;
    }

    IEnumerator CollectUniformAndPencil()
    {
        if (schoolUniform == null || pencil == null || schoolUniform.unlockFlag != StoryFlag.SchoolUniformOwned)
        { Warn("SchoolSceneController: bind the adult SchoolUniform outfit and pencil item before pickup."); yield break; }
        busy = true; LockInteraction();
        if (!Story.Has(StoryFlag.SchoolUniformOwned))
        {
            Story.Set(StoryFlag.SchoolUniformOwned);
            yield return Say(uniformObtainedDialogue);
        }
        // Ownership is checked separately from the flag, so interruption between two rewards is recoverable.
        if (!Inventory.HasItem(pencil))
        {
            Inventory.AddItem(pencil);
            if (Inventory.HasItem(pencil)) Story.Set(StoryFlag.PencilOwned);
            yield return Say(pencilObtainedDialogue);
        }
        else if (!Story.Has(StoryFlag.PencilOwned)) Story.Set(StoryFlag.PencilOwned);
        SyncProps(); UnlockInteraction(); busy = false;
    }

    public void PreviewBasketKey(Camera camera)
    {
        previewingKey = CanSeeUnrevealedKey(camera);
        SyncProps();
    }
    public void CancelBasketKeyPreview() { previewingKey = false; SyncProps(); }
    bool CanSeeUnrevealedKey(Camera camera)
    {
        if (!Ready || camera == null || layout.Variant != SchoolWorldVariant.Otherworld
            || layout.CurrentRoom != "Floor01" || Story.Has(StoryFlag.BasketKeyRevealed)) return false;
        var actor = Controls.ActivePlayer;
        if (actor == null || Vector2.Distance(actor.transform.position, basketKeyOnHoop.position) > cameraCaptureDistance) return false;
        Vector3 viewport = camera.WorldToViewportPoint(basketKeyOnHoop.position);
        return viewport.z > 0 && viewport.x > .04f && viewport.x < .96f && viewport.y > .04f && viewport.y < .96f;
    }
    public void TryCaptureBasketKey(Camera camera)
    {
        if (!previewingKey || !CanSeeUnrevealedKey(camera)) return;
        previewingKey = false;
        // Own the next interaction before the shutter releases its Camera lock,
        // so no free-input frame can slip between K1 capture and its dialogue.
        busy = true;
        LockInteraction();
        Story.Set(StoryFlag.BasketKeyRevealed);
        SyncProps();
        StartCoroutine(AfterKeyCapture());
    }
    IEnumerator AfterKeyCapture()
    {
        while (cameraMode != null && (cameraMode.IsBusy || cameraMode.IsRaised)) yield return null;
        // Interaction is already ours: waiting for global IsLocked here would
        // wait forever on that very lock.
        yield return Say(keyRevealedDialogue);
        UnlockInteraction(); busy = false;
    }
    void OnBasketballCompleted()
    {
        if (!Ready || Story.Has(StoryFlag.BasketKeyDropped)) return;
        Story.Set(StoryFlag.BasketKeyDropped);
        SyncProps();
    }
    void SyncProps()
    {
        if (Story == null || Inventory == null || basketKeyVisual == null) return;
        bool collected = Story.Has(StoryFlag.BasketKeyCollected);
        bool dropped = Story.Has(StoryFlag.BasketKeyDropped);
        bool revealed = Story.Has(StoryFlag.BasketKeyRevealed);
        // Basketball owns transforms throughout its success animation; do not pull its moving key back onto the hoop.
        if (basketball == null || basketball.State != BasketballThrowController.ThrowState.ResolvingSuccess)
        {
            if (basketKeyOnHoop != null && basketKeyOnGround != null)
                basketKeyVisual.transform.position = (dropped ? basketKeyOnGround : basketKeyOnHoop).position;
        }
        basketKeyVisual.enabled = !collected && (revealed || previewingKey);
        basketKeyVisual.color = revealed ? keyOriginalColor : new Color(.73f, .9f, .81f, .75f);
        basketKeyPickup.enabled = dropped && !collected;
        if (uniformVisual != null)
            uniformVisual.SetActive(!Story.Has(StoryFlag.SchoolUniformOwned) || pencil == null || !Inventory.HasItem(pencil));
    }

    public void ShowSecurityPanel() { if (!SecurityPanelOpen) OpenSecurityPanel(); }
    void OpenSecurityPanel()
    {
        OpenModal("安保室密码锁");
        RuntimePanelUI.Text("转动四位密码盘", modal.transform, font, new Vector2(.15f, .70f), new Vector2(.85f, .85f), 31);
        securityDigits = new FourDigitLock[4];
        for (int i = 0; i < 4; i++)
        {
            float x = .19f + i * .16f;
            var wheel = RuntimePanelUI.Panel("密码位 " + (i + 1), modal.transform, new Vector2(x, .39f), new Vector2(x + .13f, .64f), new Color(.20f, .23f, .24f));
            var text = RuntimePanelUI.Text("0", wheel.transform, font, Vector2.zero, Vector2.one, 55);
            var digit = wheel.gameObject.AddComponent<FourDigitLock>(); digit.Bind(text); securityDigits[i] = digit;
            RuntimePanelUI.Button("+", modal.transform, font, new Vector2(x, .65f), new Vector2(x + .13f, .71f), () => digit.SetDigit(digit.Value + 1));
            RuntimePanelUI.Button("−", modal.transform, font, new Vector2(x, .31f), new Vector2(x + .13f, .38f), () => digit.SetDigit(digit.Value - 1));
        }
        securityFeedback = RuntimePanelUI.Text("", modal.transform, font, new Vector2(.1f, .19f), new Vector2(.9f, .28f), 27);
        RuntimePanelUI.Button("确认", modal.transform, font, new Vector2(.33f, .055f), new Vector2(.67f, .16f), SubmitSecurityCode);
    }
    public void SubmitSecurityCode()
    {
        if (!SecurityPanelOpen) return;
        // Explicit clue permission boundary. Stage 5A supplies no clue and never opens this door.
        bool hasClue = Story.Has(StoryFlag.SchoolSecurityClueSeen);
        if (!hasClue) { securityFeedback.text = securityNoResponse; return; }
        if(chase!=null&&chase.TrySecurityCode(SecurityEnteredCode))return;
        if (youthFlow != null && youthFlow.TrySecurityCode(SecurityEnteredCode)) return;
        // Otherworld and missing permissions preserve the original no-response behavior.
        securityFeedback.text = securityNoResponse;
    }
    IEnumerator WindowView()
    {
        OpenModal("安保室窗内");
        // Literal window framing and visible oil jar; no item object/collider or pickup button in this UI.
        var room = RuntimePanelUI.Panel("窗内房间", modal.transform, new Vector2(.12f, .24f), new Vector2(.88f, .82f), new Color(.15f, .19f, .18f));
        RuntimePanelUI.Panel("桌面", room.transform, new Vector2(.13f, .20f), new Vector2(.87f, .29f), new Color(.37f, .28f, .19f));
        RuntimePanelUI.Panel("异常监控外框", room.transform, new Vector2(.16f, .30f), new Vector2(.52f, .74f), new Color(.09f, .10f, .10f));
        for (int i = 0; i < 6; i++) RuntimePanelUI.Panel("监控杂讯", room.transform, new Vector2(.18f, .35f + i * .052f), new Vector2(.50f, .36f + i * .052f), new Color(.30f, .34f, .29f), false);
        RuntimePanelUI.Panel("灯油壶", room.transform, new Vector2(.63f, .30f), new Vector2(.76f, .56f), new Color(.65f, .48f, .20f));
        RuntimePanelUI.Panel("壶口", room.transform, new Vector2(.66f, .55f), new Vector2(.73f, .62f), new Color(.37f, .32f, .22f));
        RuntimePanelUI.Text("灯油", room.transform, font, new Vector2(.62f, .32f), new Vector2(.78f, .53f), 22);
        foreach (float x in new[] { .32f, .64f }) RuntimePanelUI.Panel("窗栅", room.transform, new Vector2(x, 0), new Vector2(x + .025f, 1), new Color(.055f, .06f, .06f));
        if (securityWindowDialogue != null && securityWindowDialogue.lines != null && securityWindowDialogue.lines.Length > 0)
            RuntimePanelUI.Text(securityWindowDialogue.lines[0].text, modal.transform, font, new Vector2(.08f, .055f), new Vector2(.92f, .21f), 27);
        yield return null;
    }
    void OpenModal(string title)
    {
        Controls.AcquireLock(ControlLockReason.CloseUp); ownsModal = true;
        var canvas = RuntimePanelUI.Canvas(title, transform, 135);
        modal = canvas.gameObject;
        RuntimePanelUI.Panel("遮罩", canvas, Vector2.zero, Vector2.one, new Color(.025f, .035f, .035f, .97f));
        RuntimePanelUI.Text(title, canvas, font, new Vector2(.12f, .86f), new Vector2(.88f, .96f), 34);
        RuntimePanelUI.Button("退出 [Esc]", canvas, font, new Vector2(.84f, .91f), new Vector2(.97f, .98f), CloseModal);
    }
    public void CloseModal()
    {
        if (modal != null) { modal.SetActive(false); Destroy(modal); }
        modal = null; securityDigits = null; securityFeedback = null;
        if (ownsModal && Controls != null) Controls.ReleaseLock(ControlLockReason.CloseUp);
        ownsModal = false;
    }

    void OnDeskCompleted()
    {
        if (!Ready || handingOver || Story.Has(StoryFlag.Scene05OpeningPlayed)) return;
        Story.Set(StoryFlag.SchoolNameRevealed);
        StartCoroutine(YouthOpening());
    }
    IEnumerator YouthOpening()
    {
        if (handingOver) yield break;
        handingOver = busy = true;
        Controls.AcquireLock(ControlLockReason.SceneTransition); ownsTransition = true;
        yield return FadeWhite(1, whiteInSeconds);
        if (desk != null) desk.Close();
        if (cameraMode.IsRaised) cameraMode.Lower();
        SwitchToYouth();
        layout.SetNarrative(SchoolActor.Youth, SchoolPhase.YouthOpening);
        if (zhengTaoVisual != null) zhengTaoVisual.SetActive(true);
        if (youthGrabbingArm != null) youthGrabbingArm.SetActive(true);
        yield return new WaitForSecondsRealtime(whiteHoldSeconds);
        yield return FadeWhite(0, whiteOutSeconds);
        if (youthFlow != null) yield return youthFlow.ContinueOpening(false);
        else
        {
            yield return Say(youthOpeningDialogue);
            Story.Set(StoryFlag.Scene05OpeningPlayed);
            layout.SetNarrative(SchoolActor.Youth, SchoolPhase.YouthEntrance);
        }
        if (youthGrabbingArm != null) youthGrabbingArm.SetActive(false);
        UnlockTransition(); busy = handingOver = false;
        if (youthFlow == null) notices?.Show(endpointNotice, 7);
    }
    void SwitchToYouth()
    {
        if (adult != null && adult != youth)
        {
            adult.SetMovementEnabled(false);
            var adultBody = adult.GetComponent<Rigidbody2D>();
            if (adultBody != null) adultBody.linearVelocity = Vector2.zero;
            adult.gameObject.SetActive(false);
        }
        layout.ApplyVariant(SchoolWorldVariant.Memory);
        layout.SetNarrative(SchoolActor.Youth, SchoolPhase.YouthOpening);
        youth.gameObject.SetActive(true);
        youth.transform.position = youthOpeningPosition.position;
        var body = youth.GetComponent<Rigidbody2D>();
        if (body != null) { body.position = youthOpeningPosition.position; body.linearVelocity = Vector2.zero; }
        youth.GetComponent<PlayerInteraction>()?.BindRuntime(runtime.GetComponentInChildren<InteractionUI>(true), Dialogue);
        Controls.BindPlayer(youth, false);
        layout.EnterRoom(classroomRoomId);
        Physics2D.SyncTransforms();
        if (zhengTaoVisual != null) zhengTaoVisual.SetActive(true);
    }
    IEnumerator FadeWhite(float target, float duration)
    {
        whiteFlash.blocksRaycasts = true;
        float from = whiteFlash.alpha;
        for (float t = 0; t < duration; t += Time.unscaledDeltaTime)
        { whiteFlash.alpha = Mathf.Lerp(from, target, t / duration); yield return null; }
        whiteFlash.alpha = target;
        whiteFlash.blocksRaycasts = target > 0;
    }
    IEnumerator Talk(DialogueData data)
    {
        busy = true; LockInteraction();
        yield return Say(data);
        UnlockInteraction(); busy = false;
    }
    IEnumerator Say(DialogueData data)
    {
        if (data == null) yield break;
        while (Dialogue.IsDialogueActive()) yield return null;
        Dialogue.StartDialogue(data);
        yield return null;
        while (Dialogue.IsDialogueActive()) yield return null;
        yield return null;
    }
    void LockInteraction() { if (!ownsInteraction) { Controls.AcquireLock(ControlLockReason.Interaction); ownsInteraction = true; } }
    void UnlockInteraction() { if (ownsInteraction && Controls != null) Controls.ReleaseLock(ControlLockReason.Interaction); ownsInteraction = false; }
    void UnlockTransition() { if (ownsTransition && Controls != null) Controls.ReleaseLock(ControlLockReason.SceneTransition); ownsTransition = false; }
    void Warn(string message) { if (!warned) { Debug.LogWarning(message, this); warned = true; } }
    void OnDisable()
    {
        StopAllCoroutines();
        if (Story != null) Story.Changed -= SyncProps;
        if (basketball != null) basketball.Completed -= OnBasketballCompleted;
        if (desk != null) desk.Completed -= OnDeskCompleted;
        CloseModal(); UnlockInteraction(); UnlockTransition();
        if (whiteFlash != null) { whiteFlash.alpha = 0; whiteFlash.blocksRaycasts = false; }
        if (effects != null) effects.Stop();
        if (cameraMode != null) cameraMode.SetCaptureContext(null);
        Ready = false; busy = handingOver = false;
    }
}
