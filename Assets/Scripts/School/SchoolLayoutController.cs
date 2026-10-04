using UnityEngine;

// One coordinate contract; variants change geometry/doors independently of actor and chapter.
public class SchoolLayoutController : MonoBehaviour
{
    public SchoolLayoutData data;
    public GameObject otherworldRoot, memoryRoot;
    public SchoolWorldVariant Variant { get; private set; }
    public SchoolActor Actor { get; private set; }
    public SchoolPhase Phase { get; private set; }
    public string CurrentRoom { get; private set; } = "Floor01";
    public bool IsYouthEndpoint => Phase == SchoolPhase.YouthEntrance;
    public void SetNarrative(SchoolActor actor, SchoolPhase phase) { Actor=actor; Phase=phase; }
    public void ApplyVariant(SchoolWorldVariant variant)
    {
        Variant=variant;
        otherworldRoot.SetActive(variant==SchoolWorldVariant.Otherworld);
        memoryRoot.SetActive(variant==SchoolWorldVariant.Memory);
        Physics2D.SyncTransforms();
    }
    public void EnterRoom(string roomId)
    {
        var room=data.Room(roomId); if(room==null)return;
        CurrentRoom=roomId;
        var camera=Camera.main?Camera.main.GetComponent<CameraFollow>():null;
        if(camera==null)return;
        if(roomId.StartsWith("Floor03")||roomId=="ReadingPassage")camera.SetLimits(0,80);else camera.SetLimits(room.CameraX.x,room.CameraX.y);
        camera.SetVerticalLimits(true,0,room.CameraY,room.CameraY);
        camera.SnapToTarget();
        RoomPresentationFraming.PrepareCurrentView();
    }
    void Start() { ApplyVariant(Variant); EnterRoom(CurrentRoom); }
}
