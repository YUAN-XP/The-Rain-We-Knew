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

    private float lastDirection = 1f;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        float movementDirection = Input.GetAxisRaw("Horizontal");

        if (movementDirection != 0)
        {
            lastDirection = Mathf.Sign(movementDirection);
        }

        float desiredX =
            target.position.x +
            lastDirection * lookAheadDistance;

        desiredX = Mathf.Clamp(desiredX, minX, maxX);

        Vector3 targetPosition = new Vector3(
            desiredX,
            transform.position.y,
            transform.position.z
        );

        transform.position = Vector3.Lerp(
            transform.position,
            targetPosition,
            smoothSpeed * Time.deltaTime
        );
    }
}