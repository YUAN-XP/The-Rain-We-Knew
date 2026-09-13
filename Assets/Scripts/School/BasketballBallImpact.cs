using UnityEngine;

// Attach to the real Rigidbody2D ball. Only the configured board can confirm a throw.
[DisallowMultipleComponent]
public sealed class BasketballBallImpact : MonoBehaviour
{
    public BasketballThrowController Owner { private get; set; }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (Owner != null) Owner.ObserveBallCollision(collision);
    }
}
