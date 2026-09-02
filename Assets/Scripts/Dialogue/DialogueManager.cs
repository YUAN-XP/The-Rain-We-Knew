using System.Collections;
using TMPro;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Player")]
    [SerializeField] private PlayerController playerController;

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

        // 对话刚打开时先不允许推进
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
        // 已经处于对话中时，不允许重复启动
        if (isDialogueActive)
        {
            return;
        }

        if (dialogue == null ||
            dialogue.lines == null ||
            dialogue.lines.Length == 0)
        {
            Debug.LogWarning("DialogueData is empty.");
            return;
        }

        currentDialogue = dialogue;
        currentLineIndex = 0;

        isDialogueActive = true;
        canAdvanceDialogue = false;

        if (playerController != null)
        {
            playerController.SetMovementEnabled(false);
        }

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        // 等UI真正激活后再显示第一句
        StartCoroutine(InitializeDialogueUI());
    }

    private IEnumerator InitializeDialogueUI()
    {
        // 等待一帧，让Canvas和TMP完成首次激活
        yield return null;

        DisplayCurrentLine();

        // 强制刷新TMP
        if (speakerText != null)
        {
            speakerText.ForceMeshUpdate();
        }

        if (dialogueText != null)
        {
            dialogueText.ForceMeshUpdate();
        }

        Canvas.ForceUpdateCanvases();

        // 再等到这一帧结束，防止启动对话的E直接推进
        yield return new WaitForEndOfFrame();

        canAdvanceDialogue = true;
    }

    private void ShowNextLine()
    {
        currentLineIndex++;

        if (currentLineIndex >= currentDialogue.lines.Length)
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
            currentLineIndex >= currentDialogue.lines.Length)
        {
            return;
        }

        DialogueLine line =
            currentDialogue.lines[currentLineIndex];

        if (speakerText != null)
        {
            speakerText.text = line.speaker;
            speakerText.ForceMeshUpdate();
        }

        if (dialogueText != null)
        {
            dialogueText.text = line.text;
            dialogueText.ForceMeshUpdate();
        }
    }

    private void EndDialogue()
    {
        isDialogueActive = false;
        canAdvanceDialogue = false;

        dialogueEndFrame = Time.frameCount;

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        if (playerController != null)
        {
            playerController.SetMovementEnabled(true);
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
        return dialogueEndFrame == Time.frameCount;
    }
}