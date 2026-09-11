using UnityEngine;

// Optional scene-owned behavior using the existing camera input, lens and control lock.
public abstract class CameraCaptureContext : MonoBehaviour
{
    public abstract bool CanRaise { get; }
    public abstract void Preview(Camera camera);
    public abstract void CancelPreview(Camera camera);
    public abstract void CommitCapture(Camera camera);
}
