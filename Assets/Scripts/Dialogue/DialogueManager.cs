using System.Collections;
using TMPro;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text dialogueText;

    [Tooltip("Fit legacy QHD speaker/continue offsets to smaller windows; custom CanvasScaler modes are preserved.")]
    [SerializeField] private bool fitLegacyCanvasToScreen = true;

    [Header("Game Control")]
    [SerializeField] private GameControlManager gameControlManager;

    private DialogueData currentDialogue;
    private int currentLineIndex;

    private bool isDialogueActive;
    private bool canAdvanceDialogue;

    private int dialogueEndFrame = -1;
    public event System.Action<DialogueData> NaturallyCompleted;
    readonly System.Collections.Generic.HashSet<Object> inputPausers = new System.Collections.Generic.HashSet<Object>();
    float readableSeconds;
    public void SetInputPaused(Object owner, bool paused) { if (paused) inputPausers.Add(owner); else inputPausers.Remove(owner); }


    private void Awake()
    {
        BindLocalizedText();
        if (fitLegacyCanvasToScreen) RuntimePanelUI.FitLegacyCanvas(dialoguePanel);
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        isDialogueActive = false;
        canAdvanceDialogue = false;
    }

    private void OnDisable()
    {
        if (!isDialogueActive) return;
        StopAllCoroutines();
        EndDialogue(false);
    }

    private void Update()
    {
        if (!isDialogueActive)
        {
            return;
        }

        if (!canAdvanceDialogue)
        {
            return;
        }

        inputPausers.RemoveWhere(o => o == null);
        if (inputPausers.Count > 0) return;
        readableSeconds += GameplayClock.Delta;
        if (currentDialogue != null && readableSeconds < currentDialogue.minimumReadSeconds) return;
        if (GameplayInput.GetKeyDown(KeyCode.E) ||
            GameplayInput.GetKeyDown(KeyCode.Space))
        {
            ShowNextLine();
        }
    }

    public void StartDialogue(DialogueData dialogue)
    {
        if (isDialogueActive)
        {
            return;
        }

        if (dialogue == null ||
            dialogue.lines == null ||
            dialogue.lines.Length == 0)
        {
            Debug.LogWarning(
                "DialogueData is empty."
            );

            return;
        }

        currentDialogue = dialogue;
        currentLineIndex = 0;

        isDialogueActive = true;
        canAdvanceDialogue = false;
        // Populate before showing; prevent one frame of the previous scene's line.
        DisplayCurrentLine();

        if (gameControlManager != null)
        {
            gameControlManager.AcquireLock(
                ControlLockReason.Dialogue, this
            );
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        StartCoroutine(
            InitializeDialogueUI()
        );
    }

    private IEnumerator InitializeDialogueUI()
    {
        yield return null;

        DisplayCurrentLine();

        if (speakerText != null)
        {
            speakerText.ForceMeshUpdate();
        }

        if (dialogueText != null)
        {
            dialogueText.ForceMeshUpdate();
        }

        Canvas.ForceUpdateCanvases();

        yield return new WaitForEndOfFrame();

        canAdvanceDialogue = true;
    }

    private void ShowNextLine()
    {
        currentLineIndex++;

        if (currentLineIndex >=
            currentDialogue.lines.Length)
        {
            EndDialogue();
            return;
        }

        DisplayCurrentLine();
    }

    private void DisplayCurrentLine()
    {
        readableSeconds = 0;
        if (currentDialogue == null)
        {
            return;
        }

        if (currentLineIndex < 0 ||
            currentLineIndex >=
            currentDialogue.lines.Length)
        {
            return;
        }

        DialogueLine line =
            currentDialogue.lines[
                currentLineIndex
            ];

        // Display boundaries also cover late-created/replaced dialogue UI.
        // Keep authored source strings intact for identity and story checks.
        BindLocalizedText();

        if (speakerText != null)
        {
            bool hasSpeaker =
                !string.IsNullOrWhiteSpace(
                    line.speaker
                );

            speakerText.gameObject.SetActive(
                hasSpeaker
            );

            speakerText.text = hasSpeaker
                ? line.speaker
                : string.Empty;

            if (hasSpeaker)
            {
                speakerText.ForceMeshUpdate();
            }
        }

        if (dialogueText != null)
        {
            dialogueText.text =
                line.text;

            dialogueText.ForceMeshUpdate();
        }
    }

    private void BindLocalizedText()
    {
        GameLocalization.Bind(speakerText);
        GameLocalization.Bind(dialogueText);
        if (dialoguePanel != null)
            foreach (var label in dialoguePanel.GetComponentsInChildren<TMP_Text>(true))
                GameLocalization.Bind(label);
    }

    public int CompletedCount { get; private set; }
    public DialogueData LastCompletedDialogue { get; private set; }
    public DialogueData CurrentDialogue => currentDialogue;
    public UnityEngine.UI.Image PresentationBackground => dialoguePanel != null ? dialoguePanel.GetComponent<UnityEngine.UI.Image>() : null;
    public Canvas PresentationCanvas => dialoguePanel != null ? dialoguePanel.GetComponentInParent<Canvas>() : null;
    public int CurrentLineIndex => currentLineIndex;
    // Cancel only the caller's own dialogue; cancellation is not natural completion.
    public bool CancelDialogue(DialogueData owned)
    {
        if (!isDialogueActive || owned == null || currentDialogue != owned) return false;
        StopAllCoroutines();
        EndDialogue(false);
        return true;
    }
    private void EndDialogue(bool completed = true)
    {
        var ended = currentDialogue;
        if (completed) { LastCompletedDialogue = currentDialogue; CompletedCount++; }
        isDialogueActive = false;
        canAdvanceDialogue = false;

        dialogueEndFrame =
            Time.frameCount;

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        if (gameControlManager != null)
        {
            gameControlManager.ReleaseLock(
                ControlLockReason.Dialogue, this
            );
        }

        currentDialogue = null;
        currentLineIndex = 0;
        if (completed) NaturallyCompleted?.Invoke(ended);
    }

    public bool IsDialogueActive()
    {
        return isDialogueActive;
    }

    public bool DidDialogueEndThisFrame()
    {
        return dialogueEndFrame ==
               Time.frameCount;
    }
}
