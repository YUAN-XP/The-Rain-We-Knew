using UnityEngine;

// A local, in-memory checkpoint: it retains references to the real inventory
// and actor and never clones, clears or rewinds the earlier chapter's state.
public sealed class FogCheckpoint
{
    public string Id => "CP_FOG_OUT";
    public bool IsValid { get; private set; }
    public InventoryManager InventoryReference { get; private set; }
    public Vector3 Position { get; private set; }
    public Vector2 CameraLimits { get; private set; }
    private PlayerController player;
    private Camera camera;
    private CameraFollow follow;
    private Vector3 scale, cameraPosition;
    private Vector2 verticalLimits;
    private bool followY, usable, raised;
    private float verticalOffset, size, facing;
    public void Capture(GameControlManager controls, OilLampController lamp, InventoryManager inventory)
    {
        player = controls.ActivePlayer; camera = Camera.main;
        IsValid = player != null && camera != null && lamp != null;
        if (!IsValid) return;
        follow = camera.GetComponent<CameraFollow>();
        IsValid = follow != null; if (!IsValid) return;
        Position = player.transform.position; scale = player.transform.localScale;
        cameraPosition = camera.transform.position; size = camera.orthographicSize;
        facing = follow.Facing; CameraLimits = follow.HorizontalLimits; verticalLimits = follow.VerticalLimits;
        followY = follow.FollowsY; verticalOffset = follow.VerticalOffset;
        usable = lamp.StoryUsable; raised = lamp.IsRaised;
        InventoryReference = inventory;
    }
    public void Restore(GameControlManager controls, OilLampController lamp)
    {
        if (!IsValid || player == null || camera == null || follow == null) return;
        controls.BindPlayer(player, true);
        player.transform.localScale = scale; player.transform.position = Position;
        var body = player.GetComponent<Rigidbody2D>();
        if (body != null) { body.position = Position; body.linearVelocity = Vector2.zero; body.angularVelocity = 0; }
        follow.SetTarget(player.transform); follow.SetLimits(CameraLimits.x, CameraLimits.y);
        follow.SetVerticalLimits(followY, verticalOffset, verticalLimits.x, verticalLimits.y);
        camera.orthographicSize = size; camera.transform.position = cameraPosition; follow.SetFacing(facing); follow.SnapToTarget();
        lamp.SetStoryUsable(usable); lamp.RestoreUseState(raised);
        Physics2D.SyncTransforms();
    }
}

