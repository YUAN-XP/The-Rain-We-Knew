using UnityEngine;

public class GateKeyInteractable :
    MonoBehaviour,
    IInteractable
{
    [SerializeField] private string interactionPrompt = "[E] Pick Up";

    [Header("Item")]
    [SerializeField] private ItemData gateKeyItem;

    [Header("Dialogue")]
    [SerializeField] private DialogueData dialogueData;
    [SerializeField] private DialogueManager dialogueManager;

    [Header("Inventory")]
    [SerializeField] private InventoryManager inventoryManager;

    private bool collected = false;

    public string GetInteractionPrompt()
    {
        return interactionPrompt;
    }

    public void Interact()
    {
        if (collected)
        {
            return;
        }

        if (inventoryManager == null)
        {
            Debug.LogWarning(
                "GateKeyInteractable: InventoryManager is missing."
            );

            return;
        }

        if (gateKeyItem == null)
        {
            Debug.LogWarning(
                "GateKeyInteractable: GateKey ItemData is missing."
            );

            return;
        }

        collected = true;

        // Add the key to the inventory.
        inventoryManager.AddItem(gateKeyItem);

        // Play pickup dialogue.
        if (dialogueManager != null &&
            dialogueData != null)
        {
            dialogueManager.StartDialogue(dialogueData);
        }

        // Disable further interaction.
        Collider2D keyCollider =
            GetComponent<Collider2D>();

        if (keyCollider != null)
        {
            keyCollider.enabled = false;
        }

        SpriteRenderer spriteRenderer =
            GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        Debug.Log(
            "Scene01: Gate key added to inventory."
        );
    }
}