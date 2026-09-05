using UnityEngine;

[DisallowMultipleComponent]
public class ItemPickupInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private string interactionPrompt = "[E] Pick Up";

    [Header("Item")]
    [SerializeField] private ItemData item;

    [Header("Optional Dialogue")]
    [SerializeField] private DialogueData dialogueData;

    [Header("Existing Runtime")]
    [SerializeField] private InventoryManager inventoryManager;
    [SerializeField] private DialogueManager dialogueManager;

    private bool collected;

    private void Start()
    {
        ResolveManagers();

        // A previously acquired unique item must not reappear on scene reload.
        if (item != null && inventoryManager != null && inventoryManager.HasItem(item))
        {
            HideCollectedItem();
        }
    }

    public string GetInteractionPrompt()
    {
        return collected ? string.Empty : interactionPrompt;
    }

    public void Interact()
    {
        if (collected)
        {
            return;
        }

        if (item == null)
        {
            Debug.LogWarning($"{name}: Pickup ItemData is not assigned.", this);
            return;
        }

        ResolveManagers();

        if (inventoryManager == null || !inventoryManager.isActiveAndEnabled)
        {
            Debug.LogWarning($"{name}: No active InventoryManager is available.", this);
            return;
        }

        if (inventoryManager.HasItem(item))
        {
            HideCollectedItem();
            return;
        }

        // Keep the item available if a configured pickup dialogue cannot be played.
        if (dialogueData != null)
        {
            if (dialogueManager == null || !dialogueManager.isActiveAndEnabled ||
                dialogueData.lines == null || dialogueData.lines.Length == 0)
            {
                Debug.LogWarning($"{name}: Pickup dialogue is not ready.", this);
                return;
            }

            if (dialogueManager.IsDialogueActive())
            {
                return;
            }
        }

        inventoryManager.AddItem(item);

        // AddItem currently returns void; confirm ownership before hiding anything.
        if (!inventoryManager.HasItem(item))
        {
            return;
        }

        collected = true;

        if (dialogueData != null)
        {
            dialogueManager.StartDialogue(dialogueData);
        }

        HideCollectedItem();
    }

    private void ResolveManagers()
    {
        if (inventoryManager != null && inventoryManager.isActiveAndEnabled &&
            dialogueManager != null && dialogueManager.isActiveAndEnabled)
        {
            return;
        }

        PersistentRuntime runtime = FindAnyObjectByType<PersistentRuntime>();
        if (runtime == null)
        {
            return;
        }

        if (inventoryManager == null || !inventoryManager.isActiveAndEnabled)
        {
            inventoryManager = runtime.GetComponentInChildren<InventoryManager>();
        }

        if (dialogueManager == null || !dialogueManager.isActiveAndEnabled)
        {
            dialogueManager = runtime.GetComponentInChildren<DialogueManager>();
        }
    }

    private void HideCollectedItem()
    {
        collected = true;

        foreach (Collider2D itemCollider in GetComponents<Collider2D>())
        {
            itemCollider.enabled = false;
        }

        foreach (SpriteRenderer itemRenderer in GetComponents<SpriteRenderer>())
        {
            itemRenderer.enabled = false;
        }
    }
}
