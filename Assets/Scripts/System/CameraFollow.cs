using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;

    [Header("Follow Settings")]
    [SerializeField] private float smoothSpeed = 5f;
    [SerializeField] private float lookAheadDistance = 2f;

    [Header("Camera Limits")]
    [SerializeField] private float minX = -15f;
    [SerializeField] private float maxX = 33f;

    [Header("Vertical Follow")]
    [SerializeField] private bool followY = false;
    [SerializeField] private float verticalOffset = 0f;
    [SerializeField] private float minY = -10f;
    [SerializeField] private float maxY = 10f;

    private float lastDirection = 1f;

    private void OnEnable()
    {
        ResolveTarget();
    }

    private void LateUpdate()
    {
        ResolveTarget();

        if (target == null)
        {
            return;
        }

        float movementDirection = Input.GetAxisRaw("Horizontal");

        if (movementDirection != 0)
        {
            lastDirection = Mathf.Sign(movementDirection);
        }

        Vector3 targetPosition =
            GetTargetPosition();

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothSpeed * Time.deltaTime
        );
    }

    public void SnapToTarget()
    {
        ResolveTarget();

        if (target == null)
        {
            return;
        }

        transform.position = GetTargetPosition();
    }

    private Vector3 GetTargetPosition()
    {
        float desiredX =
            target.position.x +
            lastDirection * lookAheadDistance;

        desiredX = Mathf.Clamp(
            desiredX,
            minX,
            maxX
        );

        float desiredY = transform.position.y;

        if (followY)
        {
            desiredY = Mathf.Clamp(
                target.position.y + verticalOffset,
                minY,
                maxY
            );
        }

        return new Vector3(
            desiredX,
            desiredY,
            transform.position.z
        );
    }

    private void ResolveTarget()
    {
        if (target != null)
        {
            return;
        }

        PlayerController playerController =
            FindAnyObjectByType<PlayerController>();

        if (playerController != null)
        {
            target = playerController.transform;
        }
    }
}
