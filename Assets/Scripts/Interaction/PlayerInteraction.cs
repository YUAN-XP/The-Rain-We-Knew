using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [SerializeField] private float interactionRadius = .55f;
    [SerializeField] private LayerMask interactableLayer;

    [Header("UI")]
    [SerializeField] private InteractionUI interactionUI;

    [Header("Dialogue")]
    [SerializeField] private DialogueManager dialogueManager;

    private IInteractable currentInteractable;
    private Collider2D playerCollider;
    private void Awake() => playerCollider = GetComponent<Collider2D>();
    public Vector2 Feet => playerCollider ? new Vector2(playerCollider.bounds.center.x, playerCollider.bounds.min.y + .03f) : (Vector2)transform.position;
    public float InteractionRadius => interactionRadius;

    // Presentation reads the same target as the E prompt; it never performs its own scan.
    public IInteractable CurrentVisualTarget =>
        isActiveAndEnabled &&
        (dialogueManager == null ||
         (!dialogueManager.IsDialogueActive() && !dialogueManager.DidDialogueEndThisFrame()))
            ? currentInteractable : null;

    public void BindRuntime(InteractionUI ui, DialogueManager dialogue)
    {
        interactionUI = ui;
        dialogueManager = dialogue;
    }
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
            GameplayInput.GetKeyDown(KeyCode.E))
        {
            currentInteractable.Interact();
        }
    }

    private void DetectInteractable()
    {
        Vector2 feet=Feet;
        // Search is broad enough for wall-mounted objects. Eligibility is evaluated below,
        // so a large sensor is not an implicit permission to interact from across the room.
        var hits=Physics2D.OverlapBoxAll(feet+Vector2.up*.9f,new Vector2(4,4),0,interactableLayer);
        currentInteractable=null;
        float closestDistance=float.MaxValue;
        int closestId=int.MaxValue;
        foreach(var hit in hits)
        {
            var interactable=hit.GetComponent<IInteractable>();
            if(interactable==null || interactable is Behaviour behaviour&&!behaviour.isActiveAndEnabled)continue;
            if(string.IsNullOrWhiteSpace(interactable.GetInteractionPrompt()))continue;
            var reach=hit.GetComponent<InteractionReach>();
            Bounds surface=hit.bounds;
            var highlight=hit.GetComponent<HandDrawnInteractionHighlight>();
            if(highlight&&highlight.TryGetInteractionBounds(out var painted))surface=painted;
            if(reach&&reach.visualSurface)surface=reach.visualSurface.bounds;
            Vector2 point=reach&&reach.anchor?(Vector2)reach.anchor.position:(Vector2)surface.ClosestPoint(feet);
            float horizontal=reach?reach.horizontalReach:interactionRadius;
            float vertical=reach?reach.verticalReach:1.8f;
            if(!WithinReach(feet,point,horizontal,vertical))continue;
            if(IsBlocked(hit,feet,point))continue;
            // Centre proximity breaks overlaps (camera/lamp on a wide cabinet) without expanding reach.
            float aimX=reach&&reach.anchor?point.x:surface.center.x;
            float distance=Mathf.Abs(aimX-feet.x)+Mathf.Abs(point.y-feet.y)*.15f;
            int id=hit.GetEntityId().GetHashCode();
            if(distance<closestDistance-.0001f || Mathf.Abs(distance-closestDistance)<.0001f&&id<closestId)
            {closestDistance=distance;closestId=id;currentInteractable=interactable;}
        }
    }
    public static bool WithinReach(Vector2 feet,Vector2 point,float horizontal,float vertical) =>
        Mathf.Abs(point.x-feet.x)<=horizontal && Mathf.Abs(point.y-feet.y)<=vertical;

    bool IsBlocked(Collider2D target,Vector2 feet,Vector2 point)
    {
        Vector2 origin=feet+Vector2.up*.65f;
        Vector2 end=new Vector2(point.x,Mathf.Max(feet.y+.15f,point.y));
        foreach(var block in Physics2D.LinecastAll(origin,end))
        {
            var c=block.collider;
            if(!c||c.isTrigger||c==target||c==playerCollider||c.transform.IsChildOf(transform))continue;
            if(c.transform==target.transform||c.transform.IsChildOf(target.transform))continue;
            if(Physics2D.GetIgnoreLayerCollision(gameObject.layer,c.gameObject.layer))continue;
            return true;
        }
        return false;
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
                currentInteractable.GetInteractionPrompt(), transform
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