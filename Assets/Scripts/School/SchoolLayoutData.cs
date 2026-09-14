using System;
using UnityEngine;

public enum SchoolWorldVariant { Otherworld, Memory }
public enum SchoolActor { Adult, Youth }
public enum SchoolPhase { OtherworldExplore, YouthOpening, YouthEntrance, YouthExplore, OfficeHandoff, Chase, SecurityFinale, SchoolRecovery }
public enum SchoolPortalGate { Open, Closed, EquipmentKey, SecurityClue, OfficeEvidence }

[Serializable]
public class SchoolRoomDefinition
{
    public string id, displayName;
    public int floor;
    public Vector2 origin;
    public float width;
    public Vector2 CameraX => new Vector2(origin.x + Mathf.Min(9,width/2), origin.x + width - Mathf.Min(9,width/2));
    public float CameraY => origin.y + 3.5f;
    public Vector2 ToWorld(Vector2 local) => origin + local;
}
[Serializable]
public class SchoolPortalDefinition
{
    public string id, sourceRoom, targetRoom;
    public Vector2 sourceLocal, destinationLocal;
    public int facing = 1;
    public SchoolPortalGate otherworld, memory;
    public string blockedText;
    public bool onceOnlyFeedback;
}
[CreateAssetMenu(menuName="Game Data/School Layout")]
public class SchoolLayoutData : ScriptableObject
{
    public SchoolRoomDefinition[] rooms;
    public SchoolPortalDefinition[] portals;
    public SchoolRoomDefinition Room(string id) => Array.Find(rooms,r=>r.id==id);
    public SchoolPortalDefinition Portal(string id) => Array.Find(portals,p=>p.id==id);
}
