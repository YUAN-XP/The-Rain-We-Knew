using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRadius = 1.5f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("UI")]
    [SerializeField] private InteractionUI interactionUI;

    [Header("Dialogue")]
    [SerializeField] private DialogueManager dialogueManager;

    private IInteractable currentInteractable;

    private void OnDisable()
    {
        currentInteractable = null;
        if (interactionUI != null) interactionUI.HidePrompt();
    }

    private void Update()
    {
        // 对话进行中时，不允许再次触发场景交互
        if (dialogueManager != null &&
            dialogueManager.IsDialogueActive())
        {
            if (interactionUI != null)
            {
                interactionUI.HidePrompt();
            }

            return;
        }

        // 对话刚刚在这一帧结束时，
        // 忽略结束对白所使用的那一次 E
        if (dialogueManager != null &&
            dialogueManager.DidDialogueEndThisFrame())
        {
            if (interactionUI != null)
            {
                interactionUI.HidePrompt();
            }

            return;
        }

        DetectInteractable();
        UpdateInteractionUI();

        if (currentInteractable != null &&
            Input.GetKeyDown(KeyCode.E))
        {
            currentInteractable.Interact();
        }
    }

    private void DetectInteractable()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            interactionRadius,
            interactableLayer
        );

        currentInteractable = null;

        float closestDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            IInteractable interactable =
                hit.GetComponent<IInteractable>();

            if (interactable == null)
            {
                continue;
            }

            float distance = Vector2.Distance(
                transform.position,
                hit.transform.position
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                currentInteractable = interactable;
            }
        }
    }

    private void UpdateInteractionUI()
    {
        if (interactionUI == null)
        {
            return;
        }

        if (currentInteractable != null)
        {
            interactionUI.ShowPrompt(
                currentInteractable.GetInteractionPrompt()
            );
        }
        else
        {
            interactionUI.HidePrompt();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            interactionRadius
        );
    }
}