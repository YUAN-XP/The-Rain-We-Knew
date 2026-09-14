using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Memory-school close-ups and the single USB copy/password transaction.
// The parent controller owns Interaction; this component never acquires it.
public sealed class SchoolYouthSecurity : MonoBehaviour
{
    public SchoolYouthController controller;

    [Header("DialogueData from the Stage 5B text manifest")]
    public DialogueData computerNoUsbDialogue;
    public DialogueData computerCopyDialogue;
    public DialogueData computerPasswordDialogue;

    [Header("Inspector text populated from the Stage 5B manifest")]
    public string noteTitle;
    [TextArea(3, 6)] public string noteBody;
    public string windowTitle;
    [TextArea(2, 4)] public string windowFacts;
    public string computerTitle;
    public string copyProgressLabel;
    public string passwordProgressLabel;
    public string monitorSuccessNotice;
    public string passwordSuccessNotice;
    public string completedNotice;
    public string usbEvidenceName = "U盘（监控）";
    [TextArea(2, 4)] public string usbEvidenceDescription;

    [Header("Alpha presentation; no additional gameplay prerequisites")]
    public string computerRoomId = "SecurityRoom";
    [Min(.15f)] public float copySeconds = 1.35f;
    [Min(.15f)] public float passwordSeconds = .85f;
    [Min(.2f)] public float successNoticeSeconds = 1.65f;

    public bool IsOpen => ownedModal != null || computerScreen != null;
    public bool IsUsingComputer => usingComputer;
    public float ComputerProgress { get; private set; }

    GameObject ownedModal;
    GameObject computerScreen;
    Image progressFill;
    TMP_Text progressLabel;
    int generation;
    bool usingComputer;
    bool warned;

    bool Ready => isActiveAndEnabled && controller != null && controller.Ready
        && controller.IsYouthMemory && controller.Story != null
        && controller.Inventory != null && controller.Controls != null && controller.font != null;

    public void OpenNote()
    {
        if (!CanOpenCloseup() || string.IsNullOrWhiteSpace(noteBody))
        {
            if (Ready && string.IsNullOrWhiteSpace(noteBody)) Warn("The note body must be populated from S05_SECURITY_NOTE_BODY.");
            return;
        }
        var modal = controller.BeginModal(noteTitle);
        if (modal == null) return;
        ownedModal = modal;
        int token = ++generation;

        var paper = RuntimePanelUI.Panel("SecurityNotePaper", modal.transform,
            new Vector2(.13f, .16f), new Vector2(.87f, .82f), new Color(.88f, .84f, .61f));
        RuntimePanelUI.Panel("FoldedCorner", paper.transform, new Vector2(.935f, .89f), Vector2.one,
            new Color(.70f, .66f, .45f), false);
        var body = RuntimePanelUI.Text(noteBody, paper.transform, controller.font,
            new Vector2(.055f, .18f), new Vector2(.45f, .83f), 29);
        body.color = new Color(.19f, .23f, .22f);
        body.alignment = TextAlignmentOptions.MidlineLeft;
        body.enableAutoSizing = true;
        body.fontSizeMin = 22;
        body.fontSizeMax = 29;

        var grid = RuntimePanelUI.Rect("NoteDigitGrid", paper.transform, new Vector2(.53f, .10f), new Vector2(.95f, .90f));
        for (int i = 0; i < 9; i++)
        {
            int x = i % 3, y = i / 3;
            Vector2 lo = new Vector2(x / 3f + .023f, (2 - y) / 3f + .023f);
            RuntimePanelUI.Panel("NumberCell" + (i + 1), grid, lo, lo + Vector2.one * .287f,
                new Color(.95f, .93f, .78f), false);
        }
        var ink = RuntimePanelUI.Rect("BluePathFourTwoSixEight", grid, Vector2.zero, Vector2.one)
            .gameObject.AddComponent<SchoolSecurityNotePathGraphic>();
        ink.raycastTarget = false;
        for (int i = 0; i < 9; i++)
        {
            int x = i % 3, y = i / 3;
            Vector2 lo = new Vector2(x / 3f, (2 - y) / 3f);
            var number = RuntimePanelUI.Text((i + 1).ToString(), grid, controller.font, lo, lo + Vector2.one / 3, 35);
            number.color = i == 3 ? Color.white : new Color(.14f, .23f, .29f);
        }
        Canvas.ForceUpdateCanvases();
        body.ForceMeshUpdate();
        StartCoroutine(MarkNoteAfterPresentation(modal, body, token));
    }

    IEnumerator MarkNoteAfterPresentation(GameObject modal, TMP_Text body, int token)
    {
        // No Seen flag during object/asset construction. A full rendered frame must survive.
        yield return new WaitForEndOfFrame();
        yield return null;
        if (token != generation || !Ready || ownedModal != modal || modal == null
            || !modal.activeInHierarchy || body == null || !body.isActiveAndEnabled
            || body.textInfo.characterCount == 0) yield break;
        controller.Story.Set(StoryFlag.SchoolSecurityClueSeen);
    }

    public void OpenWindow()
    {
        if (!CanOpenCloseup()) return;
        if (controller.Story.Has(StoryFlag.SchoolSecurityWindowSeen))
        { controller.Notice(windowFacts, 3); return; }
        var modal = controller.BeginModal(windowTitle);
        if (modal == null) return;
        ownedModal = modal;
        ++generation;
        var room = RuntimePanelUI.Panel("NormalSecurityRoomView", modal.transform,
            new Vector2(.12f, .25f), new Vector2(.88f, .82f), new Color(.67f, .74f, .65f));
        RuntimePanelUI.Panel("RoomFloor", room.transform, Vector2.zero, new Vector2(1, .19f), new Color(.40f, .43f, .35f), false);
        RuntimePanelUI.Panel("OfficeDesk", room.transform, new Vector2(.10f, .21f), new Vector2(.88f, .28f), new Color(.53f, .37f, .23f), false);
        RuntimePanelUI.Panel("LeftDeskLeg", room.transform, new Vector2(.15f, .04f), new Vector2(.18f, .23f), new Color(.42f, .30f, .20f), false);
        RuntimePanelUI.Panel("RightDeskLeg", room.transform, new Vector2(.80f, .04f), new Vector2(.83f, .23f), new Color(.42f, .30f, .20f), false);
        var monitor = RuntimePanelUI.Panel("NormalMonitorHousing", room.transform,
            new Vector2(.14f, .34f), new Vector2(.49f, .83f), new Color(.13f, .18f, .18f), false);
        BuildNeutralMonitor(monitor.transform);
        var computer = RuntimePanelUI.Panel("PublicComputerHousing", room.transform,
            new Vector2(.59f, .32f), new Vector2(.82f, .66f), new Color(.18f, .23f, .24f), false);
        RuntimePanelUI.Panel("PublicComputerScreen", computer.transform, new Vector2(.06f, .08f), new Vector2(.94f, .92f), new Color(.41f, .65f, .65f), false);
        RuntimePanelUI.Panel("ComputerStand", room.transform, new Vector2(.69f, .25f), new Vector2(.72f, .35f), new Color(.19f, .24f, .23f), false);
        RuntimePanelUI.Panel("Keyboard", room.transform, new Vector2(.58f, .275f), new Vector2(.83f, .30f), new Color(.22f, .28f, .27f), false);
        foreach (float x in new[] { .31f, .64f })
            RuntimePanelUI.Panel("ForegroundWindowBar", room.transform, new Vector2(x, 0), new Vector2(x + .018f, 1), new Color(.24f, .29f, .27f), false);
        var text = RuntimePanelUI.Text(windowFacts, modal.transform, controller.font,
            new Vector2(.12f, .08f), new Vector2(.88f, .22f), 29);
        text.enableAutoSizing = true; text.fontSizeMin = 22; text.fontSizeMax = 29;
        // This close-up has no item, collection button or story progress callback.
        StartCoroutine(MarkWindowAfterPresentation(modal));
    }

    IEnumerator MarkWindowAfterPresentation(GameObject shown)
    {
        yield return new WaitForEndOfFrame();
        yield return null;
        if (Ready && shown != null && ownedModal == shown && shown.activeInHierarchy)
            controller.Story.Set(StoryFlag.SchoolSecurityWindowSeen);
    }

    public IEnumerator UseComputer()
    {
        // Called by controller.Run: Busy and Interaction are already the parent's responsibility.
        if (!Ready || IsOpen || usingComputer || controller.school == null
            || controller.school.layout.CurrentRoom != computerRoomId) yield break;
        if (controller.Story.Has(StoryFlag.SchoolEvidenceSubmitted))
        { ShowAlreadyDone(); yield break; }
        if (controller.Story.Has(StoryFlag.SchoolEvidenceObtained)
            && controller.Story.Has(StoryFlag.SchoolMediaPasswordChanged))
        { ShowAlreadyDone(); yield break; }
        if (controller.usbItem == null || !controller.Inventory.HasItem(controller.usbItem))
        {
            if (ValidDialogue(computerNoUsbDialogue))
            {
                yield return controller.Say(computerNoUsbDialogue);
                if (!controller.LastDialogueCompleted) yield break;
            }
            else Warn("Missing S05_COMPUTER_NO_USB dialogue.");
            yield break;
        }
        if (!HasSingleUsb() || !ValidDialogue(computerCopyDialogue) || !ValidDialogue(computerPasswordDialogue)
            || string.IsNullOrWhiteSpace(usbEvidenceName) || string.IsNullOrWhiteSpace(usbEvidenceDescription) || string.IsNullOrWhiteSpace(monitorSuccessNotice)
            || string.IsNullOrWhiteSpace(passwordSuccessNotice))
        { Warn("Computer needs one non-permanent USB and the manifest copy/password/success/description assets."); yield break; }

        usingComputer = true;
        int token = ++generation;
        try
        {
            BuildComputerScreen();
            computerScreen.SetActive(false);
            yield return controller.Say(computerCopyDialogue);
            if (!controller.LastDialogueCompleted || !CanContinueComputer(token)) yield break;
            yield return ShowProgress(token, copyProgressLabel, copySeconds);
            if (!CanContinueComputer(token)) yield break;
            computerScreen.SetActive(false);
            yield return controller.Say(computerPasswordDialogue);
            if (!controller.LastDialogueCompleted || !CanContinueComputer(token)) yield break;
            yield return ShowProgress(token, passwordProgressLabel, passwordSeconds);
            if (!CanContinueComputer(token)) yield break;

            // All preconditions are checked before this synchronous, non-yielding commit.
            // The original ItemData remains the sole inventory reference and is never edited.
            controller.Inventory.SetRuntimePresentation(controller.usbItem, usbEvidenceName, usbEvidenceDescription);
            controller.Story.SetTogether(StoryFlag.SchoolEvidenceObtained, StoryFlag.SchoolMediaPasswordChanged);
            DestroyComputerScreen();
            controller.Notice(monitorSuccessNotice, successNoticeSeconds);
            yield return new WaitForSecondsRealtime(successNoticeSeconds);
            if (!Ready || token != generation) yield break;
            controller.Notice(passwordSuccessNotice, successNoticeSeconds);
            yield return new WaitForSecondsRealtime(successNoticeSeconds);
        }
        finally
        {
            DestroyComputerScreen();
            usingComputer = false;
            ComputerProgress = 0;
        }
    }

    bool CanOpenCloseup() => Ready && !IsOpen && !usingComputer && !controller.Busy && !controller.Controls.IsLocked();
    bool ValidDialogue(DialogueData data) => data != null && data.lines != null && data.lines.Length > 0;
    bool HasSingleUsb()
    {
        var item = controller.usbItem;
        if (item == null || item.isPermanent || item.category != ItemCategory.Regular || string.IsNullOrWhiteSpace(item.itemId)) return false;
        int count = 0;
        foreach (var entry in controller.Inventory.Items)
            if (entry != null && entry.itemId == item.itemId) count++;
        return count == 1 && controller.Inventory.HasItem(item);
    }
    bool CanContinueComputer(int token) => token == generation && Ready && usingComputer
        && controller.school != null && controller.school.layout.CurrentRoom == computerRoomId
        && !controller.Story.Has(StoryFlag.SchoolEvidenceSubmitted) && HasSingleUsb();

    void ShowAlreadyDone()
    {
        if (!string.IsNullOrWhiteSpace(completedNotice)) controller.Notice(completedNotice, 3);
    }

    void BuildComputerScreen()
    {
        var canvas = RuntimePanelUI.Canvas("SchoolPublicComputer", transform, 130);
        computerScreen = canvas.gameObject;
        RuntimePanelUI.Panel("ComputerBackdrop", canvas, Vector2.zero, Vector2.one, new Color(.055f, .075f, .07f, .99f));
        var frame = RuntimePanelUI.Panel("ComputerMonitorFrame", canvas,
            new Vector2(.14f, .17f), new Vector2(.86f, .83f), new Color(.15f, .20f, .21f));
        var screen = RuntimePanelUI.Panel("ComputerDesktop", frame.transform,
            new Vector2(.025f, .04f), new Vector2(.975f, .96f), new Color(.25f, .37f, .35f));
        RuntimePanelUI.Text(computerTitle, screen.transform, controller.font, new Vector2(.08f, .76f), new Vector2(.92f, .92f), 35);
        var image = RuntimePanelUI.Panel("NeutralLocalMonitorPreview", screen.transform,
            new Vector2(.09f, .30f), new Vector2(.42f, .72f), new Color(.12f, .19f, .19f), false);
        BuildNeutralMonitor(image.transform);
        progressLabel = RuntimePanelUI.Text("", screen.transform, controller.font,
            new Vector2(.47f, .49f), new Vector2(.92f, .67f), 27);
        progressLabel.enableAutoSizing = true; progressLabel.fontSizeMin = 20; progressLabel.fontSizeMax = 27;
        var track = RuntimePanelUI.Panel("LocalOperationTrack", screen.transform,
            new Vector2(.48f, .35f), new Vector2(.91f, .40f), new Color(.12f, .22f, .20f), false);
        progressFill = RuntimePanelUI.Panel("LocalOperationProgress", track.transform,
            Vector2.zero, Vector2.one, new Color(.69f, .82f, .62f), false);
        // No buttons or Esc handler: the original two-step transaction is controlled until finished.
    }

    IEnumerator ShowProgress(int token, string label, float seconds)
    {
        ComputerProgress = 0;
        computerScreen.SetActive(true);
        progressLabel.text = label;
        float duration = Mathf.Max(.15f, seconds);
        for (float elapsed = 0; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            if (!CanContinueComputer(token) || progressFill == null) yield break;
            ComputerProgress = Mathf.Clamp01(elapsed / duration);
            progressFill.rectTransform.anchorMax = new Vector2(ComputerProgress, 1);
            yield return null;
        }
        if (progressFill != null) progressFill.rectTransform.anchorMax = Vector2.one;
        ComputerProgress = 1;
        yield return null;
    }

    static void BuildNeutralMonitor(Transform parent)
    {
        // Ordinary unoccupied campus geometry only: no invented abuse footage or horror noise.
        var view = RuntimePanelUI.Panel("NeutralCorridorView", parent,
            new Vector2(.055f, .075f), new Vector2(.945f, .925f), new Color(.47f, .61f, .55f), false);
        RuntimePanelUI.Panel("CorridorFloor", view.transform, Vector2.zero, new Vector2(1, .23f), new Color(.34f, .45f, .39f), false);
        for (int i = 0; i < 3; i++)
            RuntimePanelUI.Panel("CorridorWindow" + i, view.transform,
                new Vector2(.08f + i * .31f, .39f), new Vector2(.25f + i * .31f, .81f), new Color(.62f, .77f, .74f), false);
    }

    void DestroyComputerScreen()
    {
        if (computerScreen != null) { computerScreen.SetActive(false); Destroy(computerScreen); }
        computerScreen = null; progressFill = null; progressLabel = null;
    }
    void Warn(string message)
    {
        if (warned) return;
        warned = true;
        Debug.LogWarning("SchoolYouthSecurity: " + message, this);
    }
    public void Close()
    {
        ++generation;
        StopAllCoroutines();
        // The controller owns the modal lock and its dialogue/Interaction cleanup.
        // EndModal is only called while this module's modal still exists.
        if (ownedModal != null && controller != null) controller.EndModal();
        ownedModal = null;
        DestroyComputerScreen();
        usingComputer = false;
        ComputerProgress = 0;
    }
    void OnDisable() { Close(); }
}

// Mesh-native note diagram. Text digits are added above this graphic by the close-up.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class SchoolSecurityNotePathGraphic : MaskableGraphic
{
    static readonly Vector2[] Path = {
        new Vector2(1f / 6, .5f), new Vector2(.5f, 5f / 6),
        new Vector2(5f / 6, .5f), new Vector2(.5f, 1f / 6)
    };

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;
        float unit = Mathf.Min(r.width, r.height);
        Color ink = new Color(.10f, .37f, .64f, .94f);
        for (int i = 0; i < Path.Length - 1; i++)
        {
            Vector2 a = Point(r, Path[i]), b = Point(r, Path[i + 1]);
            Vector2 direction = (b - a).normalized;
            Segment(vh, a + direction * unit * .045f, b - direction * unit * .045f, unit * .009f, ink);
            Vector2 tip = Vector2.Lerp(a, b, .70f);
            Vector2 normal = new Vector2(-direction.y, direction.x);
            Triangle(vh, tip, tip - direction * unit * .050f + normal * unit * .025f,
                tip - direction * unit * .050f - normal * unit * .025f, ink);
        }
        // Filled starting node at 4; a small ring distinguishes the endpoint at 8.
        Disc(vh, Point(r, Path[0]), unit * .052f, ink);
        Vector2 end = Point(r, Path[3]);
        Disc(vh, end, unit * .052f, ink);
        Disc(vh, end, unit * .040f, new Color(.95f, .93f, .78f));
    }
    static Vector2 Point(Rect r, Vector2 uv) => r.min + Vector2.Scale(r.size, uv);
    static void Segment(VertexHelper vh, Vector2 a, Vector2 b, float thickness, Color color)
    {
        Vector2 d = (b - a).normalized;
        Vector2 n = new Vector2(-d.y, d.x) * thickness * .5f;
        int start = vh.currentVertCount;
        vh.AddVert(a - n, color, Vector2.zero); vh.AddVert(a + n, color, Vector2.zero);
        vh.AddVert(b + n, color, Vector2.zero); vh.AddVert(b - n, color, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2); vh.AddTriangle(start, start + 2, start + 3);
    }
    static void Triangle(VertexHelper vh, Vector2 a, Vector2 b, Vector2 c, Color color)
    {
        int start = vh.currentVertCount;
        vh.AddVert(a, color, Vector2.zero); vh.AddVert(b, color, Vector2.zero); vh.AddVert(c, color, Vector2.zero);
        vh.AddTriangle(start, start + 1, start + 2);
    }
    static void Disc(VertexHelper vh, Vector2 center, float radius, Color color)
    {
        const int sides = 24;
        for (int i = 0; i < sides; i++)
        {
            float a = i * Mathf.PI * 2 / sides, b = (i + 1) * Mathf.PI * 2 / sides;
            Triangle(vh, center, center + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius,
                center + new Vector2(Mathf.Cos(b), Mathf.Sin(b)) * radius, color);
        }
    }
}
