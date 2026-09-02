using UnityEngine;

public class GateKeyInteractable :
    MonoBehaviour,
    IInteractable
{
    [SerializeField] private string interactionPrompt = "[E] Pick Up";

    [Header("Dialogue")]
    [SerializeField] private DialogueData dialogueData;
    [SerializeField] private DialogueManager dialogueManager;

    [Header("Scene State")]
    [SerializeField] private Scene01State scene01State;

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

        if (scene01State == null)
        {
            Debug.LogWarning("GateKeyInteractable: Scene01State is missing.");
            return;
        }

        collected = true;

        // Record that the player has obtained the gate key.
        scene01State.GetGateKey();

        // Play pickup dialogue.
        if (dialogueManager != null &&
            dialogueData != null)
        {
            dialogueManager.StartDialogue(dialogueData);
        }

        // Disable further interaction with the key.
        Collider2D keyCollider = GetComponent<Collider2D>();

        if (keyCollider != null)
        {
            keyCollider.enabled = false;
        }

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
    }
}