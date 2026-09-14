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

    private void Awake()
    {
        if (fitLegacyCanvasToScreen) RuntimePanelUI.FitLegacyCanvas(dialoguePanel);
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        isDialogueActive = false;
        canAdvanceDialogue = false;
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

        if (Input.GetKeyDown(KeyCode.E) ||
            Input.GetKeyDown(KeyCode.Space))
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

        if (gameControlManager != null)
        {
            gameControlManager.AcquireLock(
                ControlLockReason.Dialogue
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

    public int CompletedCount { get; private set; }
    public DialogueData LastCompletedDialogue { get; private set; }
    public DialogueData CurrentDialogue => currentDialogue;
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
                ControlLockReason.Dialogue
            );
        }

        currentDialogue = null;
        currentLineIndex = 0;
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
