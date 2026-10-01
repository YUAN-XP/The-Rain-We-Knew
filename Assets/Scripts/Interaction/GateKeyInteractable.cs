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
    public bool IsCollected => collected;
    private void OnEnable(){RestoreCollectedState();}
    private void Start(){RestoreCollectedState();}
    public void RestoreCollectedState(){
        var story=FindAnyObjectByType<StoryStateManager>();
        if(inventoryManager==null)inventoryManager=FindAnyObjectByType<InventoryManager>();
        collected=(story!=null&&(story.Has(StoryFlag.Scene01GateKeyCollected)||story.Has(StoryFlag.Scene01GateOpened)))
            ||(inventoryManager!=null&&gateKeyItem!=null&&inventoryManager.HasItem(gateKeyItem));
        var shape=GetComponent<Collider2D>();if(shape)shape.enabled=!collected;
        var visual=GetComponent<SpriteRenderer>();if(visual)visual.enabled=!collected;
    }

    public string GetInteractionPrompt()
    {
        return interactionPrompt;
    }

    public void Interact()
    {
        if (!collected) DemoCloseupPresentation.For(gameObject).OpenMailbox(null, this);
    }

    public void CollectFromCloseup()
    {
        if(dialogueManager==null)dialogueManager=FindAnyObjectByType<DialogueManager>();
        if(inventoryManager==null)inventoryManager=FindAnyObjectByType<InventoryManager>();

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
        FindAnyObjectByType<StoryStateManager>()?.Set(StoryFlag.Scene01GateKeyCollected);
        FindAnyObjectByType<QuestProgress>()?.Publish(QuestEvent.GateKeyObtained);

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