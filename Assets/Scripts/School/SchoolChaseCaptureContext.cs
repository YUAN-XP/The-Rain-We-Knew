using UnityEngine;

public sealed class SchoolChaseCaptureContext : CameraCaptureContext
{
    public SchoolChaseController chase;
    public override bool CanRaise => chase!=null&&chase.CanUseCamera;
    public override void Preview(Camera camera) { if(chase!=null)chase.BeginPreview(); }
    public override void CancelPreview(Camera camera) { if(chase!=null)chase.EndPreview(); }
    public override void CommitCapture(Camera camera) { if(chase!=null)chase.CommitPreview(); }
}
