using UnityEngine;
using UnityEngine.SceneManagement;

public class GateInteractable :
    MonoBehaviour,
    IInteractable
{
    [SerializeField] private string interactionPrompt = "[E] Inspect";

    [Header("Item")]
    [SerializeField] private ItemData gateKeyItem;

    [Header("Dialogue")]
    [SerializeField] private DialogueData lockedDialogueData;
    [SerializeField] private DialogueData unlockDialogueData;
    [SerializeField] private DialogueManager dialogueManager;

    [Header("Inventory")]
    [SerializeField] private InventoryManager inventoryManager;

    [Header("Scene State")]
    [SerializeField] private Scene01State scene01State;

    public string GetInteractionPrompt()
    {
        if(dialogueManager==null)dialogueManager=FindAnyObjectByType<DialogueManager>();
        if(inventoryManager==null)inventoryManager=FindAnyObjectByType<InventoryManager>();
        if(scene01State==null)scene01State=FindAnyObjectByType<Scene01State>();

        if (scene01State == null)
        {
            return interactionPrompt;
        }

        if (scene01State.gateOpened)
        {
            return "[E] Enter";
        }

        if (inventoryManager != null &&
            gateKeyItem != null &&
            inventoryManager.HasItem(gateKeyItem))
        {
            return "[E] Open";
        }

        return interactionPrompt;
    }

    public void Interact()
    {
        if(dialogueManager==null)dialogueManager=FindAnyObjectByType<DialogueManager>();
        if(inventoryManager==null)inventoryManager=FindAnyObjectByType<InventoryManager>();
        if(scene01State==null)scene01State=FindAnyObjectByType<Scene01State>();

        if (scene01State == null ||
            dialogueManager == null ||
            inventoryManager == null ||
            gateKeyItem == null)
        {
            return;
        }

        // Gate is already open.
        if (scene01State.gateOpened)
        {
            EnterGate();
            return;
        }

        // Player does not have the key.
        if (!inventoryManager.HasItem(gateKeyItem))
        {
            FindAnyObjectByType<QuestProgress>()?.Publish(QuestEvent.GateKeyMissing);
            if (lockedDialogueData != null)
            {
                dialogueManager.StartDialogue(
                    lockedDialogueData
                );
            }

            return;
        }

        // Use and remove the key.
        inventoryManager.RemoveItem(gateKeyItem);

        // Open the gate.
        scene01State.OpenGate();

        if (unlockDialogueData != null)
        {
            dialogueManager.StartDialogue(
                unlockDialogueData
            );
        }

        Debug.Log(
            "Scene01: Gate key consumed and gate opened."
        );
    }

    private void EnterGate()
    {
        Debug.Log(
            "Scene01: Loading Scene02_Courtyard."
        );

        BlackSceneTransition.Begin("Scene02_Courtyard", FindAnyObjectByType<GameControlManager>());
    }
}