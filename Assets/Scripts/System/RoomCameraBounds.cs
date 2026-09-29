using System.Collections.Generic;
using UnityEngine;

// Scene-owned view bounds. This does not add physics or alter any door/actor.
// Existing CameraFollow limits remain camera-centre limits for scripted areas.
public sealed class RoomCameraBounds : MonoBehaviour
{
    [SerializeField] private Rect actorRegion;
    [SerializeField] private Vector2 horizontalEdges;
    [SerializeField] private Collider2D leftWall;
    [SerializeField] private Collider2D rightWall;
    [SerializeField] private bool constrainVertical;
    [SerializeField] private Vector2 verticalEdges;
    private static readonly HashSet<RoomCameraBounds> active = new HashSet<RoomCameraBounds>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() { active.Clear(); }
    private void OnEnable() { active.Add(this); }
    private void OnDisable() { active.Remove(this); }

    public static Vector3 Confine(Camera camera, Transform actor, Vector3 position)
    {
        if (camera == null || !camera.orthographic || actor == null) return position;
        foreach (var room in active)
        {
            if (room == null || !room.isActiveAndEnabled || room.gameObject.scene != camera.gameObject.scene ||
                !room.actorRegion.Contains(actor.position)) continue;
            // Opening the corridor wall for the shadow chase also opens its view.
            if (!WallExists(room.leftWall) || !WallExists(room.rightWall)) continue;
            float left = room.leftWall != null ? room.leftWall.bounds.max.x : room.horizontalEdges.x;
            float right = room.rightWall != null ? room.rightWall.bounds.min.x : room.horizontalEdges.y;
            position.x = ClampAxis(position.x, left, right, camera.orthographicSize * camera.aspect);
            if (room.constrainVertical)
                position.y = ClampAxis(position.y, room.verticalEdges.x, room.verticalEdges.y, camera.orthographicSize);
            return position;
        }
        return position;
    }

    private static bool WallExists(Collider2D wall)
    { return wall == null || (wall.enabled && wall.gameObject.activeInHierarchy); }

    public static float ClampAxis(float centre, float near, float far, float halfView)
    {
        // Small rooms stay centred instead of feeding inverted limits to Clamp.
        if (far - near <= 2f * halfView) return (near + far) * .5f;
        return Mathf.Clamp(centre, near + halfView, far - halfView);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(actorRegion.center, new Vector3(actorRegion.width, actorRegion.height, 0));
    }
}
