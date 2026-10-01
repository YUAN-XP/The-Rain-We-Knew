using System;
using UnityEngine;

[Serializable] public sealed class StorySnapshot {
    public string[] flags=new string[0]; public int outfit; public Vector2Int mazeCell;
    public bool hasSchoolPosition; public string schoolRoom; public Vector2 schoolLocal; public float schoolFacing=1;
}
[Serializable] public sealed class SavedItem { public string key, id, displayName, description; }
[Serializable] public sealed class SavedFile { public string key,id; }
[Serializable] public sealed class SavedSchool { public bool present; public string room; public int variant,actor,phase; }
[Serializable] public sealed class SavedCompanion { public string role,parent; public Vector3 position,scale=Vector3.one; public bool active; public float direction=1; }
[Serializable] public sealed class GameSaveData {
    public int version=1,slot; public long savedUtcTicks; public double playSeconds;
    public string scene,actor; public Vector3 position,scale=Vector3.one,cameraPosition; public bool planar,followY,lampRaised,cameraAvailable;
    public float facing=1,cameraSize,cameraOffset; public Vector2 cameraX,cameraY;
    public StorySnapshot story; public QuestSnapshot quests; public SavedItem[] items=new SavedItem[0]; public SavedFile[] files=new SavedFile[0];
    public string[] firstObtained=new string[0]; public SavedSchool school=new SavedSchool();
    public SavedCompanion[] companions=new SavedCompanion[0];
    public int threeDoorProgress; public bool distortedJoined;
}
[Serializable] public sealed class SaveEnvelope { public string format="ProjectMystery.Save.1"; public string payload,sha256; }
