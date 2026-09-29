using UnityEngine;

/// <summary>Faces an existing visual toward its moving owner without driving movement.</summary>
public sealed class SpriteMotionFacing : MonoBehaviour
{
    [SerializeField] Transform motionSource;
    [SerializeField] SpriteRenderer visual;
    Vector3 previous;
    void OnEnable() { if (motionSource) previous = motionSource.position; }
    void LateUpdate()
    {
        if (!motionSource || !visual) return;
        float dx = motionSource.position.x - previous.x;
        previous = motionSource.position;
        // Ignore teleports between rooms; retain facing until actual movement begins.
        if (Mathf.Abs(dx) > .0001f && Mathf.Abs(dx) < .8f) visual.flipX = dx < 0;
    }
}
