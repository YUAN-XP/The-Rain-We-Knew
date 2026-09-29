using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    public float WalkSpeed => moveSpeed;

    private Rigidbody2D rb;
    private SprintAbility sprintAbility;
    private float horizontalInput;
    private float verticalInput;
    private bool planarMovement;
    private float savedGravityScale;
    public bool IsPlanarMovement => planarMovement;
    public void SetPlanarMovement(bool enabled)
    {
        if (planarMovement == enabled) return;
        if (enabled) { savedGravityScale = rb.gravityScale; rb.gravityScale = 0; }
        else rb.gravityScale = savedGravityScale;
        planarMovement = enabled;
        horizontalInput = verticalInput = 0;
        rb.linearVelocity = Vector2.zero;
    }
    private bool canMove = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprintAbility = GetComponent<SprintAbility>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Update()
    {
        if (!canMove)
        {
            horizontalInput = 0f;
            return;
        }

        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = planarMovement ? Input.GetAxisRaw("Vertical") : 0;
        if (sprintAbility != null) sprintAbility.ObserveMovement(horizontalInput);
    }

    private void FixedUpdate()
    {
        float multiplier = sprintAbility != null
            ? sprintAbility.GetMultiplier(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) : 1f;
        if (planarMovement)
            rb.linearVelocity = canMove ? Vector2.ClampMagnitude(new Vector2(horizontalInput, verticalInput), 1) * moveSpeed * multiplier : Vector2.zero;
        else rb.linearVelocity = new Vector2(horizontalInput * moveSpeed * multiplier, rb.linearVelocity.y);
    }

    public void SetMovementEnabled(bool enabled)
    {
        canMove = enabled;

        if (!enabled)
        {
            horizontalInput = 0f;

            verticalInput = 0;
            rb.linearVelocity = new Vector2(0f, planarMovement ? 0f : rb.linearVelocity.y);
        }
    }
}
