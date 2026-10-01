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

    public float Facing => lastDirection;
    // Non-serialized, opt-in compensation when a room uses a tighter visual framing.
    public float PresentationLimitPadding { get; set; }
    public Vector2 HorizontalLimits => new Vector2(minX, maxX);
    public Vector2 VerticalLimits => new Vector2(minY, maxY);
    public bool FollowsY => followY;
    public float VerticalOffset => verticalOffset;
    public void SetVerticalLimits(bool enabled, float offset, float bottom, float top)
    { followY = enabled; verticalOffset = offset; minY = bottom; maxY = top; }
    private float lastDirection = 1f;
    private Camera viewCamera;

    private Vector3 ConfineToRoom(Vector3 position)
    {
        if (viewCamera == null) viewCamera = GetComponent<Camera>();
        return RoomCameraBounds.Confine(viewCamera, target, position);
    }

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

        float movementDirection = GameplayInput.GetAxisRaw("Horizontal");

        if (movementDirection != 0)
        {
            lastDirection = Mathf.Sign(movementDirection);
        }

        Vector3 targetPosition =
            GetTargetPosition();

        transform.position = ConfineToRoom(Vector3.Lerp(
            transform.position,
            targetPosition,
            1f - Mathf.Exp(-smoothSpeed * Time.deltaTime)
        ));
    }

    public void SetTarget(Transform value) { target = value; lastDirection = 1f; }
    public void SetFacing(float direction) { lastDirection = direction < 0 ? -1 : 1; }
    public void SetLimits(float left, float right) { minX = left; maxX = right; }
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
            minX - PresentationLimitPadding,
            maxX + PresentationLimitPadding
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

        return ConfineToRoom(new Vector3(
            desiredX,
            desiredY,
            transform.position.z
        ));
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
