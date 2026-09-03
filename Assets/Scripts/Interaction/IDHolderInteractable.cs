using UnityEngine;

public class IDHolderInteractable :
    MonoBehaviour,
    IInteractable
{
    [SerializeField] private string interactionPrompt = "[E] Collect";

    [Header("Item")]
    [SerializeField] private ItemData idHolderItem;

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
                "IDHolderInteractable: InventoryManager is missing."
            );

            return;
        }

        if (idHolderItem == null)
        {
            Debug.LogWarning(
                "IDHolderInteractable: ID Holder ItemData is missing."
            );

            return;
        }

        collected = true;

        // Add the ID holder to Items.
        inventoryManager.AddItem(idHolderItem);

        // Play inspection dialogue.
        if (dialogueManager != null &&
            dialogueData != null)
        {
            dialogueManager.StartDialogue(
                dialogueData
            );
        }

        // Disable further interaction.
        Collider2D itemCollider =
            GetComponent<Collider2D>();

        if (itemCollider != null)
        {
            itemCollider.enabled = false;
        }

        SpriteRenderer spriteRenderer =
            GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        Debug.Log(
            "Scene01: ID Holder added to inventory."
        );
    }
}