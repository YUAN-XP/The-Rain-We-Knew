using System.Collections;
using TMPro;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Game Control")]
    [SerializeField] private GameControlManager gameControlManager;

    private DialogueData currentDialogue;
    private int currentLineIndex;

    private bool isDialogueActive;
    private bool canAdvanceDialogue;

    private int dialogueEndFrame = -1;

    private void Awake()
    {
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
            speakerText.text =
                line.speaker;

            speakerText.ForceMeshUpdate();
        }

        if (dialogueText != null)
        {
            dialogueText.text =
                line.text;

            dialogueText.ForceMeshUpdate();
        }
    }

    private void EndDialogue()
    {
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