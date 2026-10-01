using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// One tool on the existing PersistentRuntime. Ownership stays in InventoryManager;
// usability is a story flag, and Raised is only the current tool pose.
[DisallowMultipleComponent]
[DefaultExecutionOrder(-40)]
public class OilLampController : MonoBehaviour
{
    public const string ExhaustedMessage = "小油灯燃料耗尽了，暂时亮不起来了";
    [SerializeField] private ItemData requiredItem;
    [SerializeField, Min(.2f)] private float chargeSeconds = 2.2f;
    [SerializeField] private Vector3 raisedOffset = new Vector3(.55f, .3f, 0);
    [SerializeField] private Vector3 stowedOffset = new Vector3(.38f, -.55f, 0);
    [SerializeField] private Color glowColor = new Color(1f, .76f, .32f, .3f);
    [SerializeField, Min(.1f)] private float glowDiameter = 6f;
    private GameControlManager controls;
    private InventoryManager inventory;
    private StoryStateManager story;
    private StoryNoticeUI notice;
    private DialogueManager dialogue;
    private CameraModeController cameraTool;
    private GameObject visual;
    private SpriteRenderer body, glow;
    private Texture2D glowTexture;
    private Sprite glowSprite, bodySprite;
    private float charge;
    private bool charging, chargeSignalled, warned, useHintShown;
    public void ConfigureItem(ItemData item) { if(item!=null)requiredItem=item; Resolve(); }
    public bool Owned => requiredItem != null && inventory != null && inventory.HasItemById(requiredItem.itemId);
    public bool StoryUsable => story != null && !story.Has(StoryFlag.OilLampExhausted);
    public bool CanUseLamp => Owned && StoryUsable && controls != null && controls.AllowsAdultOutfits;
    public bool CanRespondThisFrame { get; private set; }
    public bool IsRaised { get; private set; }
    public bool IsIlluminating => IsRaised && CanUseLamp;
    public bool IsCharging => charging;
    public float Charge01 => Mathf.Clamp01(charge / chargeSeconds);
    public event Action ChargeCompleted;

    private void Awake() { Resolve(); SceneManager.activeSceneChanged += SceneChanged; }
    private void Resolve()
    {
        var runtime = PersistentRuntime.Instance;
        if (runtime == null) return;
        controls = runtime.GetComponent<GameControlManager>();
        inventory = runtime.GetComponent<InventoryManager>();
        story = runtime.GetComponent<StoryStateManager>();
        notice = runtime.GetComponent<StoryNoticeUI>();
        dialogue = runtime.GetComponentInChildren<DialogueManager>(true);
        cameraTool = runtime.GetComponent<CameraModeController>();
    }
    private void Update()
    {
        if (controls == null) Resolve();
        if (IsRaised && !CanUseLamp) LowerLamp();
        CanRespondThisFrame = CanReadToolInput();
        if (!useHintShown && CanRespondThisFrame && Owned && StoryUsable)
        {
            useHintShown = true;
            if (notice != null) notice.Show("[R] 使用小油灯", 3);
        }
        if (!GameplayInput.GetKeyDown(KeyCode.R) || !CanRespondThisFrame) return;
        if (!Owned) return;
        if (!StoryUsable) { ShowExhausted(); return; }
        if (IsRaised) LowerLamp(); else RaiseLamp();
    }
    private bool CanReadToolInput()
    {
        if (controls == null || !controls.AllowsAdultOutfits || cameraTool != null && cameraTool.IsBusy) return false;
        if (dialogue != null && (dialogue.IsDialogueActive() || dialogue.DidDialogueEndThisFrame())) return false;
        return !HasLockOtherThanCamera(controls);
    }
    public static bool HasLockOtherThanCamera(GameControlManager owner)
    {
        if (owner == null) return true;
        foreach (ControlLockReason reason in Enum.GetValues(typeof(ControlLockReason)))
            if (reason != ControlLockReason.Camera && owner.IsLockedBy(reason)) return true;
        return false;
    }
    public bool RaiseLamp()
    {
        Resolve();
        if (!CanReadToolInput() || !CanUseLamp) return false;
        if (cameraTool != null && cameraTool.IsRaised) cameraTool.Lower();
        IsRaised = true; return true;
    }
    public void LowerLamp() { IsRaised = false; CancelCharge(); }
    public void SetStoryUsable(bool usable)
    {
        Resolve();
        if (story != null) story.Set(StoryFlag.OilLampExhausted, !usable);
        if (!usable) LowerLamp();
    }
    // Called only by a local checkpoint after its modal lock has been acquired.
    public void RestoreUseState(bool raised)
    {
        LowerLamp();
        if (!raised || !CanUseLamp) return;
        if (cameraTool != null && cameraTool.IsRaised) cameraTool.Lower();
        IsRaised = true;
    }
    public bool BeginCharge()
    {
        if (!IsIlluminating) return false;
        charge = 0; charging = true; chargeSignalled = false; return true;
    }
    // The cutscene owns Space. It must supply the real held state every frame;
    // releasing resets the trial, so a tap never completes on a background clock.
    public void TickCharge(bool held, float responsiveDeltaTime)
    {
        if (!charging || !IsIlluminating || chargeSignalled) return;
        if (!held) { charge = 0; return; }
        charge = Mathf.Min(chargeSeconds, charge + Mathf.Max(0, responsiveDeltaTime));
        if (charge < chargeSeconds) return;
        chargeSignalled = true; charging = false; ChargeCompleted?.Invoke();
    }
    public void CancelCharge() { charging = false; charge = 0; chargeSignalled = false; }
    public void ShowExhausted() { if (notice != null) notice.Show(ExhaustedMessage); }

    private void LateUpdate()
    {
        if (controls == null || controls.ActivePlayer == null || !Owned || !controls.AllowsAdultOutfits)
        { if (visual != null) visual.SetActive(false); return; }
        if (visual == null) BuildVisual();
        if (visual == null) return;
        visual.SetActive(true);
        var actor = controls.ActivePlayer.transform;
        var offset = IsRaised ? raisedOffset : stowedOffset;
        offset.x *= Mathf.Sign(actor.localScale.x);
        visual.transform.position = actor.position + offset;
        body.color = IsIlluminating ? new Color(1, .77f, .26f) : new Color(.36f, .27f, .16f);
        glow.enabled = IsIlluminating;
        glow.transform.localScale = Vector3.one * glowDiameter * Mathf.Lerp(1, 2.4f, Charge01);
        glow.color = new Color(glowColor.r, glowColor.g, glowColor.b, glowColor.a * Mathf.Lerp(1, 2.2f, Charge01));
    }
    private void BuildVisual()
    {
        if (warned) return;
        if (requiredItem == null) { Debug.LogWarning("OilLampController needs the existing lamp ItemData.", this); warned = true; return; }
        visual = new GameObject("AdultOilLampVisual"); visual.transform.SetParent(transform, false);
        body = new GameObject("LampBody").AddComponent<SpriteRenderer>(); body.transform.SetParent(visual.transform, false);
        bodySprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), Vector2.one * .5f, Texture2D.whiteTexture.width);
        body.sprite = bodySprite; body.sortingOrder = 45; body.transform.localScale = new Vector3(.24f, .4f, 1);
        glowTexture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        var colors = new Color[4096];
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
        { float a = Mathf.Pow(Mathf.Clamp01(1 - Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) / 31.5f), 2); colors[y * 64 + x] = new Color(1, 1, 1, a); }
        glowTexture.SetPixels(colors); glowTexture.Apply();
        glowSprite = Sprite.Create(glowTexture, new Rect(0, 0, 64, 64), Vector2.one * .5f, 64);
        glow = new GameObject("LampGlow").AddComponent<SpriteRenderer>(); glow.transform.SetParent(visual.transform, false);
        glow.sprite = glowSprite; glow.sortingOrder = 40;
    }
    private void SceneChanged(Scene oldScene, Scene newScene) { LowerLamp(); }
    private void OnDisable() { LowerLamp(); if (visual != null) visual.SetActive(false); }
    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= SceneChanged;
        if (glowSprite != null) Destroy(glowSprite); if (glowTexture != null) Destroy(glowTexture);
        if (bodySprite != null) Destroy(bodySprite);
    }
}


