using UnityEngine;

public class DialogueInteractable :
    MonoBehaviour,
    IInteractable
{
    [SerializeField] private string interactionPrompt = "[E] Talk";

    [Header("Dialogue")]
    [SerializeField] private DialogueData dialogueData;
    [SerializeField] private DialogueManager dialogueManager;

    public string GetInteractionPrompt()
    {
        return interactionPrompt;
    }

    public void Interact()
    {
        if (dialogueManager == null ||
            dialogueData == null)
        {
            return;
        }

        dialogueManager.StartDialogue(dialogueData);
    }
}