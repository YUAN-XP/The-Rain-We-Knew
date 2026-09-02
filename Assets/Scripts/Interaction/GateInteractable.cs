using UnityEngine;

public class GateInteractable :
    MonoBehaviour,
    IInteractable
{
    [SerializeField] private string interactionPrompt = "[E] Inspect";

    [Header("Dialogue")]
    [SerializeField] private DialogueData lockedDialogueData;
    [SerializeField] private DialogueData unlockDialogueData;
    [SerializeField] private DialogueManager dialogueManager;

    [Header("Scene State")]
    [SerializeField] private Scene01State scene01State;

    public string GetInteractionPrompt()
    {
        if (scene01State == null)
        {
            return interactionPrompt;
        }

        if (scene01State.gateOpened)
        {
            return "[E] Enter";
        }

        if (scene01State.hasGateKey)
        {
            return "[E] Open";
        }

        return interactionPrompt;
    }

    public void Interact()
    {
        if (scene01State == null ||
            dialogueManager == null)
        {
            return;
        }

        // The gate is already open.
        if (scene01State.gateOpened)
        {
            EnterGate();
            return;
        }

        // The gate is still locked.
        if (!scene01State.hasGateKey)
        {
            if (lockedDialogueData != null)
            {
                dialogueManager.StartDialogue(
                    lockedDialogueData
                );
            }

            return;
        }

        // The player has the key.
        scene01State.OpenGate();

        if (unlockDialogueData != null)
        {
            dialogueManager.StartDialogue(
                unlockDialogueData
            );
        }

        Debug.Log("Scene01: Gate opened.");
    }

    private void EnterGate()
    {
        Debug.Log(
            "Scene01: Player entered the gate."
        );
    }
}