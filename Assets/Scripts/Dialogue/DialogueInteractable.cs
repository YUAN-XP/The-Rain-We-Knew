using UnityEngine;

public class DialogueInteractable :
    MonoBehaviour,
    IInteractable
{
    [SerializeField] private string interactionPrompt = "[E] Talk";

    [Header("Dialogue")]
    [SerializeField] private DialogueData dialogueData;
    [SerializeField] private DialogueManager dialogueManager;

    private bool hasWarnedMissingDialogueManager;

    private void Awake()
    {
        ResolveDialogueManager();
    }

    public string GetInteractionPrompt()
    {
        return interactionPrompt;
    }

    public void Interact()
    {
        if (dialogueData == null)
        {
            return;
        }

        if (dialogueManager == null)
        {
            ResolveDialogueManager();
        }

        if (dialogueManager == null)
        {
            if (!hasWarnedMissingDialogueManager)
            {
                Debug.LogWarning(
                    "DialogueInteractable could not find a DialogueManager. " +
                    "Assign one in the Inspector or ensure one exists at runtime.",
                    this
                );

                hasWarnedMissingDialogueManager = true;
            }

            return;
        }

        dialogueManager.StartDialogue(dialogueData);
    }

    private void ResolveDialogueManager()
    {
        if (dialogueManager != null)
        {
            return;
        }

        dialogueManager = FindAnyObjectByType<DialogueManager>(
            FindObjectsInactive.Include
        );
    }
}
