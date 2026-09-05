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

    [Header("Transition")]
    [SerializeField] private Transform destination;
    [SerializeField] private ScreenFadeController screenFade;

    [Header("Existing Runtime")]
    [SerializeField] private GameControlManager gameControlManager;

    [SerializeField] private UnityEvent onTransitionCompleted = new UnityEvent();

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
            () => MovePlayerAndCamera(player)
        );

        ReleaseControlLock();
        isTransitioning = false;
        onTransitionCompleted.Invoke();
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
            FindAnyObjectByType<PlayerController>();

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
            cameraFollow.SnapToTarget();
        }
    }

    private void OnDisable()
    {
        ReleaseControlLock();
        isTransitioning = false;
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
