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

        // Match the provided irregular drawing, without arrows or start/end badges.
        var paper = RuntimePanelUI.Rect("SecurityNotePaper", modal.transform,new Vector2(.30f,.14f),new Vector2(.70f,.83f));
        var aspect=paper.gameObject.AddComponent<AspectRatioFitter>();aspect.aspectMode=AspectRatioFitter.AspectMode.FitInParent;aspect.aspectRatio=719f/874;
        P05ArtworkUI.Paint(paper,"PaintedPaper","P05_NotePaper",Vector2.zero,Vector2.one,false);
        var body=RuntimePanelUI.Text(noteBody,paper,controller.font,new Vector2(.09f,.68f),new Vector2(.88f,.93f),23);
        body.color=new Color(.19f,.23f,.22f);body.alignment=TextAlignmentOptions.MidlineLeft;body.enableAutoSizing=true;body.fontSizeMin=18;body.fontSizeMax=23;
        var ink=RuntimePanelUI.Rect("OriginalNineCellDiagram",paper,Vector2.zero,Vector2.one).gameObject.AddComponent<SchoolSecurityNotePathGraphic>();ink.raycastTarget=false;
        // Small conventional keypad numbers retain the existing 4268 clue's legibility.
        for(int i=0;i<9;i++){var c=P05ArtworkUI.NoteCenters[i];var uv=new Vector2(c.x/719,1-c.y/874);var n=RuntimePanelUI.Text((i+1).ToString(),paper,controller.font,uv-new Vector2(.025f,.025f),uv+new Vector2(.025f,.025f),17);n.color=new Color(.15f,.32f,.48f);n.rectTransform.localEulerAngles=new Vector3(0,0,-18);}
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
        P05ArtworkUI.Paint(modal.transform,"InsideSecurityRoom","P05_Security",new Vector2(.12f,.25f),new Vector2(.88f,.82f));
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
            yield return GameplayClock.Wait(successNoticeSeconds);
            if (!Ready || token != generation) yield break;
            controller.Notice(passwordSuccessNotice, successNoticeSeconds);
            yield return GameplayClock.Wait(successNoticeSeconds);
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
        for (float elapsed = 0; elapsed < duration; elapsed += GameplayClock.Delta)
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

    static void BuildNeutralMonitor(Transform parent){P05ArtworkUI.Paint(parent,"PaintedCampusMonitor","P05_Monitors",new Vector2(.03f,.03f),new Vector2(.97f,.97f));}

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

// The reference's irregular blue boxes and thin four-to-two-to-six-to-eight line.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class SchoolSecurityNotePathGraphic:MaskableGraphic {
 protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();var r=rectTransform.rect;var ink=new Color(.10f,.37f,.64f,.94f);var size=new Vector2(42,42);float angle=-18*Mathf.Deg2Rad;
  var centers=P05ArtworkUI.NoteCenters;
  Vector2 P(Vector2 p)=>r.min+Vector2.Scale(r.size,new Vector2(p.x/719,1-p.y/874));
  for(int i=0;i<9;i++){var c=centers[i];var v=new Vector2[4];for(int j=0;j<4;j++){var d=new Vector2(j==0||j==3?-size.x*.5f:size.x*.5f,j<2?-size.y*.5f:size.y*.5f);v[j]=P(c+new Vector2(d.x*Mathf.Cos(angle)-d.y*Mathf.Sin(angle),d.x*Mathf.Sin(angle)+d.y*Mathf.Cos(angle)));}for(int j=0;j<4;j++)Line(vh,v[j],v[(j+1)%4],r.width/719*1.5f,ink);}
  var path=new[]{3,1,5,7};for(int i=0;i<3;i++)Line(vh,P(centers[path[i]]),P(centers[path[i+1]]),r.width/719*1.3f,ink);
 }
 static void Line(VertexHelper vh,Vector2 a,Vector2 b,float w,Color c){var d=(b-a).normalized;var n=new Vector2(-d.y,d.x)*w*.5f;int s=vh.currentVertCount;vh.AddVert(a-n,c,Vector2.zero);vh.AddVert(a+n,c,Vector2.zero);vh.AddVert(b+n,c,Vector2.zero);vh.AddVert(b-n,c,Vector2.zero);vh.AddTriangle(s,s+1,s+2);vh.AddTriangle(s,s+2,s+3);}
}
