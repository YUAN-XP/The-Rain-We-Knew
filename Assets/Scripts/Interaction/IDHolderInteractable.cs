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

    System.Collections.IEnumerator Start() {
        yield return null;
        Resolve(); var story=PersistentRuntime.Instance!=null?PersistentRuntime.Instance.GetComponent<StoryStateManager>():null;
        if(story!=null && (story.Has(StoryFlag.Scene01IDHolderTaken)||story.Has(StoryFlag.CompleteCredentialArchived)||inventoryManager!=null&&inventoryManager.HasItem(idHolderItem))) {
            story.Set(StoryFlag.Scene01IDHolderTaken); collected=true; HidePickup();
        }
    }
    void Resolve(){var runtime=PersistentRuntime.Instance;if(runtime!=null){inventoryManager=runtime.GetComponent<InventoryManager>();dialogueManager=runtime.GetComponentInChildren<DialogueManager>(true);return;}if(inventoryManager==null)inventoryManager=FindAnyObjectByType<InventoryManager>(FindObjectsInactive.Include);if(dialogueManager==null)dialogueManager=FindAnyObjectByType<DialogueManager>(FindObjectsInactive.Include);}
    void HidePickup(){foreach(var c in GetComponents<Collider2D>())c.enabled=false;foreach(var r in GetComponentsInChildren<SpriteRenderer>())r.enabled=false;}
    public string GetInteractionPrompt()
    {
        return interactionPrompt;
    }

    public void Interact()
    {
        Resolve();
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


        // Add the ID holder to Items.
        inventoryManager.AddItem(idHolderItem);
        if(!inventoryManager.HasItem(idHolderItem))return;
        collected=true;PersistentRuntime.Instance?.GetComponent<StoryStateManager>()?.Set(StoryFlag.Scene01IDHolderTaken);

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