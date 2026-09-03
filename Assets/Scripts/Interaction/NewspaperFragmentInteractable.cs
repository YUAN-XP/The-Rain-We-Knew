using UnityEngine;

public class NewspaperFragmentInteractable :
    MonoBehaviour,
    IInteractable
{
    [SerializeField] private string interactionPrompt = "[E] Collect";

    [Header("Item")]
    [SerializeField] private ItemData newspaperFragmentItem;

    [Header("Dialogue")]
    [SerializeField] private DialogueData dialogueData;
    [SerializeField] private DialogueManager dialogueManager;

    [Header("Inventory")]
    [SerializeField] private InventoryManager inventoryManager;

    [Header("Scene State")]
    [SerializeField] private Scene01State scene01State;

    [Header("Reveal")]
    [SerializeField] private GameObject gateKeyObject;

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

        if (inventoryManager == null ||
            newspaperFragmentItem == null ||
            scene01State == null)
        {
            Debug.LogWarning(
                "NewspaperFragmentInteractable: Missing reference."
            );

            return;
        }

        collected = true;

        // Add newspaper fragment to Items.
        inventoryManager.AddItem(
            newspaperFragmentItem
        );

        // Record scene state.
        scene01State.TakeNewspaperFragment();

        // Show the fragment dialogue.
        if (dialogueManager != null &&
            dialogueData != null)
        {
            dialogueManager.StartDialogue(
                dialogueData
            );
        }

        // Reveal the key underneath.
        if (gateKeyObject != null)
        {
            gateKeyObject.SetActive(true);
        }

        // Hide this newspaper fragment.
        Collider2D fragmentCollider =
            GetComponent<Collider2D>();

        if (fragmentCollider != null)
        {
            fragmentCollider.enabled = false;
        }

        SpriteRenderer spriteRenderer =
            GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }

        Debug.Log(
            "Scene01: Newspaper fragment added to inventory."
        );
    }
}