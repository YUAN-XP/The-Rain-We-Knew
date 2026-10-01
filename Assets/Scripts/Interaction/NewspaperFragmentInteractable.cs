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
    public bool IsCollected => collected;
    private System.Collections.IEnumerator Start()
    {
        yield return null;
        if(scene01State==null)scene01State=FindAnyObjectByType<Scene01State>();
        if(scene01State!=null && scene01State.newspaperFragmentTaken)
        {
            collected=true;
            if(gateKeyObject!=null){gateKeyObject.SetActive(true);gateKeyObject.GetComponent<GateKeyInteractable>()?.RestoreCollectedState();}
            var collider=GetComponent<Collider2D>();if(collider!=null)collider.enabled=false;
            var visual=GetComponent<SpriteRenderer>();if(visual!=null)visual.enabled=false;
        }
    }

    public string GetInteractionPrompt()
    {
        return interactionPrompt;
    }

    public void Interact()
    {
        if (collected) return;
        var key = gateKeyObject != null ? gateKeyObject.GetComponent<GateKeyInteractable>() : null;
        DemoCloseupPresentation.For(gameObject).OpenMailbox(this, key);
    }

    public void CollectFromCloseup()
    {
        if(dialogueManager==null)dialogueManager=FindAnyObjectByType<DialogueManager>();
        if(inventoryManager==null)inventoryManager=FindAnyObjectByType<InventoryManager>();
        if(scene01State==null)scene01State=FindAnyObjectByType<Scene01State>();

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