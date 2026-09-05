using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum ControlLockReason
{
    Dialogue,
    Inventory,
    Interaction,
    Camera,
    CloseUp,
    Cutscene
}

public class GameControlManager : MonoBehaviour
{
    [Header("Persistent Player")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private Rigidbody2D playerRigidbody;

    private CameraFollow cameraFollow;

    private readonly HashSet<ControlLockReason> activeLocks =
        new HashSet<ControlLockReason>();

    private void Awake()
    {
        FindCurrentCamera();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        FindCurrentCamera();

        ApplyControlState();
    }

    private void FindCurrentCamera()
    {
        cameraFollow =
            FindFirstObjectByType<CameraFollow>();

        if (cameraFollow == null)
        {
            Debug.LogWarning(
                "GameControlManager: No CameraFollow found in current scene."
            );
        }
    }

    public void AcquireLock(
        ControlLockReason reason)
    {
        activeLocks.Add(reason);

        ApplyControlState();
    }

    public void ReleaseLock(
        ControlLockReason reason)
    {
        activeLocks.Remove(reason);

        ApplyControlState();
    }

    public bool IsLocked()
    {
        return activeLocks.Count > 0;
    }

    public bool IsLockedBy(
        ControlLockReason reason)
    {
        return activeLocks.Contains(reason);
    }

    private void ApplyControlState()
    {
        bool canControl =
            activeLocks.Count == 0;

        if (playerController != null)
        {
            playerController.SetMovementEnabled(
                canControl
            );
        }

        if (playerInteraction != null)
        {
            playerInteraction.enabled =
                canControl;
        }

        if (!canControl &&
            playerRigidbody != null)
        {
            playerRigidbody.linearVelocity =
                Vector2.zero;
        }

        if (cameraFollow != null)
        {
            cameraFollow.enabled =
                canControl;
        }
    }
}
