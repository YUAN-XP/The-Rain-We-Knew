using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class RoomTransitionInteractable :
    MonoBehaviour,
    IInteractable
{
    [Header("Interaction")]
    [SerializeField] private string interactionPrompt = "[E] Enter";

    [Header("Optional story lock")]
    [SerializeField] private bool requireStoryFlag;
    [SerializeField] private StoryFlag requiredStoryFlag;
    [SerializeField] private DialogueData lockedDialogue;

    [Header("Transition")]
    [SerializeField] private Transform destination;
    [SerializeField] private ScreenFadeController screenFade;

    [Header("Existing Runtime")]
    [SerializeField] private GameControlManager gameControlManager;

    [SerializeField] private UnityEvent onTransitionCompleted = new UnityEvent();

    [Header("Optional destination camera bounds")]
    [SerializeField] private bool overrideCameraLimits;
    [SerializeField] private Vector2 destinationCameraLimits;

    [SerializeField] private bool overrideVerticalCameraLimits;
    [SerializeField] private Vector2 destinationVerticalCameraLimits;
    [SerializeField] private bool overrideFacing;
    [SerializeField] private bool destinationFacesLeft;

    public event System.Action<RoomTransitionInteractable> Arrived;
    public event System.Action<RoomTransitionInteractable> MovedUnderFade;
    private bool isTransitioning;
    private bool ownsInteractionLock;

    public string GetInteractionPrompt()
    {
        return interactionPrompt;
    }

    public void Interact()
    {
        if (isTransitioning ||
            (screenFade != null && screenFade.IsFading))
        {
            return;
        }

        if (!TryResolveReferences(out Transform player))
        {
            return;
        }

        if (gameControlManager.IsLocked()) return;
        if(requireStoryFlag)
        {
            var story=FindAnyObjectByType<StoryStateManager>();
            if(story==null||!story.Has(requiredStoryFlag))
            {
                if(lockedDialogue!=null)FindAnyObjectByType<DialogueManager>(FindObjectsInactive.Include)?.StartDialogue(lockedDialogue);
                return;
            }
        }

        StartCoroutine(
            TransitionRoutine(player)
        );
    }

    private IEnumerator TransitionRoutine(
        Transform player)
    {
        isTransitioning = true;

        if (gameControlManager != null)
        {
            gameControlManager.AcquireLock(
                ControlLockReason.Interaction
            );

            ownsInteractionLock = true;
        }

        yield return screenFade.FadeThroughBlack(
            () => MovePlayerAndCamera(player), this
        );

        ReleaseControlLock();
        isTransitioning = false;
        onTransitionCompleted.Invoke();
        Arrived?.Invoke(this);
    }

    private bool TryResolveReferences(
        out Transform player)
    {
        player = null;

        if (destination == null)
        {
            Debug.LogWarning(
                $"{name}: Room transition destination is not assigned.",
                this
            );

            return false;
        }

        if (screenFade == null)
        {
            screenFade =
                FindAnyObjectByType<ScreenFadeController>();
        }

        if (screenFade == null)
        {
            Debug.LogWarning(
                $"{name}: No ScreenFadeController is available.",
                this
            );

            return false;
        }

        if (gameControlManager == null)
        {
            gameControlManager =
                FindAnyObjectByType<GameControlManager>();
        }

        if (gameControlManager == null)
        {
            Debug.LogWarning(
                $"{name}: No GameControlManager is available.",
                this
            );

            return false;
        }

        PlayerController playerController =
            gameControlManager.ActivePlayer != null
                ? gameControlManager.ActivePlayer
                : FindAnyObjectByType<PlayerController>();

        if (playerController == null)
        {
            Debug.LogWarning(
                $"{name}: No persistent PlayerController is available.",
                this
            );

            return false;
        }

        player = playerController.transform;
        return true;
    }

    private void MovePlayerAndCamera(
        Transform player)
    {
        Rigidbody2D playerRigidbody =
            player.GetComponent<Rigidbody2D>();

        player.position = destination.position;

        if (playerRigidbody != null)
        {
            playerRigidbody.position = destination.position;
            playerRigidbody.linearVelocity = Vector2.zero;
        }

        CameraFollow cameraFollow =
            FindAnyObjectByType<CameraFollow>();

        if (cameraFollow != null)
        {
            if (overrideCameraLimits)
                cameraFollow.SetLimits(destinationCameraLimits.x, destinationCameraLimits.y);
            if (overrideVerticalCameraLimits)
                cameraFollow.SetVerticalLimits(true, 0, destinationVerticalCameraLimits.x, destinationVerticalCameraLimits.y);
            if (overrideFacing) cameraFollow.SetFacing(destinationFacesLeft ? -1 : 1);
            cameraFollow.SnapToTarget();
        }
        MovedUnderFade?.Invoke(this);
        GroundedPlacement.Settle(player.GetComponent<PlayerController>());
        RoomPresentationFraming.PrepareCurrentView();
    }

    private void OnDisable()
    {
        CancelTransition();
        ReleaseControlLock();
        isTransitioning = false;
    }
    public void CancelTransition()
    {
        if(!isTransitioning)return;
        StopAllCoroutines();if(screenFade!=null)screenFade.CancelOwnedFade(this);
        ReleaseControlLock();isTransitioning=false;
    }

    private void ReleaseControlLock()
    {
        if (!ownsInteractionLock ||
            gameControlManager == null)
        {
            return;
        }

        gameControlManager.ReleaseLock(
            ControlLockReason.Interaction
        );

        ownsInteractionLock = false;
    }
}
