using UnityEngine;

// The same constant-gravity arc used by ChildhoodActivityUI, expressed in world units.
// It does not decide hits: BasketballThrowController uses the ball's real collision callback.
public static class ThrowTrajectory2D
{
    public static Vector2 Position(Vector2 origin, Vector2 velocity, Vector2 gravity, float seconds)
        => origin + velocity * seconds + .5f * gravity * seconds * seconds;

    // Screen-height normalization preserves drag power across window sizes/resolutions.
    public static Vector2 VelocityFromScreenPull(Vector2 pull, float screenHeight,
        float speedPerReferencePixel, float maximumSpeed)
    {
        Vector2 referencePull = pull * (900f / Mathf.Max(1f, screenHeight));
        return Vector2.ClampMagnitude(referencePull * speedPerReferencePixel, maximumSpeed);
    }
}
