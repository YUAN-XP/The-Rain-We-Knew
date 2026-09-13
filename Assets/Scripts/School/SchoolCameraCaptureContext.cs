using UnityEngine;

// Ordinary photograph context. It never changes the school's world variant.
public sealed class SchoolCameraCaptureContext : CameraCaptureContext
{
    public SchoolSceneController school;
    public override bool CanRaise => school != null && school.Ready && !school.Busy;
    public override void Preview(Camera camera) { if (school != null) school.PreviewBasketKey(camera); }
    public override void CancelPreview(Camera camera) { if (school != null) school.CancelBasketKeyPreview(); }
    public override void CommitCapture(Camera camera) { if (school != null) school.TryCaptureBasketKey(camera); }
}
