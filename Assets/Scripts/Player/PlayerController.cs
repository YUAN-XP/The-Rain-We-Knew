using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private float horizontalInput;
    private bool canMove = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        if (!canMove)
        {
            horizontalInput = 0f;
            return;
        }

        horizontalInput = Input.GetAxisRaw("Horizontal");
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = new Vector2(
            horizontalInput * moveSpeed,
            rb.linearVelocity.y
        );
    }

    public void SetMovementEnabled(bool enabled)
    {
        canMove = enabled;

        if (!enabled)
        {
            horizontalInput = 0f;

            rb.linearVelocity = new Vector2(
                0f,
                rb.linearVelocity.y
            );
        }
    }
}