using System;
using System.Collections.Generic;
using UnityEngine;

public enum SchoolChaseRoute { None, OfficePressure, RoofCrossing, ThirdFloorLeft, LowerEscape, SecurityFinale, Completed }
public enum SchoolChaseStep { Dormant, Opening, Escaping, SecurityArrival, OilTutorial, OilReady, FinalCharge, Dispersing, Exploring, Dead, Restoring, FogReturn, Returned }
public enum SchoolCheckpointReason { Office, Security, FogReturn }

// Session memory on the surviving runtime; never a disk save or a replacement
// for CP_FOG_OUT. Scene-owned actors/cameras are resolved again after reload.
[DisallowMultipleComponent]
public sealed class SchoolChaseSession : MonoBehaviour
{
    public SchoolChaseCheckpoint Checkpoint { get; private set; }
    public bool HasCheckpoint => Checkpoint != null;
    public bool PendingHomeArrival { get; set; }
    public bool PendingReturnFog { get; set; }
    public SchoolChaseCheckpoint CompletedResume { get; set; }
    public int Generation { get; private set; }
    public int AdvanceGeneration() => ++Generation;
    public void Store(SchoolChaseCheckpoint value) { Checkpoint = value; }
}

public sealed class SchoolChaseCheckpoint
{
    public SchoolCheckpointReason Reason;
    public SchoolChaseRoute Route;
    public SchoolActor Actor;
    public SchoolWorldVariant Variant;
    public SchoolPhase Phase;
    public string Room;
    public Vector2 LocalPosition;
    public Vector3 Scale;
    public float Facing, CameraSize;
    public Vector2 CameraX, CameraY;
    public bool FollowY, LampUsable, LampRaised, Simulated;
    public float CameraOffset;
    public AdultOutfit Outfit;
    public InventoryManager.SessionSnapshot Inventory;
    public FileData[] Files;
    public readonly Dictionary<StoryFlag, bool> LocalFlags = new Dictionary<StoryFlag, bool>();

    public static SchoolChaseCheckpoint Capture(SchoolSceneController school, SchoolCheckpointReason reason, SchoolChaseRoute route)
    {
        var player=school.Controls.ActivePlayer; var cam=Camera.main;
        var follow=cam != null ? cam.GetComponent<CameraFollow>() : null;
        var room=school.layout.data.Room(school.layout.CurrentRoom);
        var runtime=PersistentRuntime.Instance;
        if(player==null || follow==null || room==null || runtime==null) return null;
        var lamp=runtime.GetComponent<OilLampController>(); var body=player.GetComponent<Rigidbody2D>();
        var result=new SchoolChaseCheckpoint {
            Reason=reason, Route=route, Actor=school.layout.Actor, Variant=school.layout.Variant, Phase=school.layout.Phase,
            Room=room.id, LocalPosition=(Vector2)player.transform.position-room.origin, Scale=player.transform.localScale,
            Facing=follow.Facing, CameraSize=cam.orthographicSize, CameraX=follow.HorizontalLimits, CameraY=follow.VerticalLimits,
            FollowY=follow.FollowsY, CameraOffset=follow.VerticalOffset, Simulated=body==null||body.simulated,
            LampUsable=lamp!=null&&lamp.StoryUsable, LampRaised=lamp!=null&&lamp.IsRaised, Outfit=school.Story.Outfit,
            Inventory=school.Inventory.CaptureSession(), Files=new List<FileData>(runtime.GetComponent<FileArchiveManager>().Files).ToArray()
        };
        foreach(StoryFlag flag in Enum.GetValues(typeof(StoryFlag)))
            if((int)flag >= (int)StoryFlag.SchoolChaseStarted || flag==StoryFlag.OilLampExhausted)
                result.LocalFlags[flag]=school.Story.Has(flag);
        return result;
    }
}
