using System.Collections.Generic;
using UnityEngine;

// Bounds have a deterministic priority where the P03 corridor overlaps the original second floor.
public sealed class RoomCameraBounds : MonoBehaviour
{
 [SerializeField] Rect actorRegion;
 [SerializeField] Vector2 horizontalEdges;
 [SerializeField] Collider2D leftWall,rightWall;
 [SerializeField] bool constrainVertical;
 [SerializeField] Vector2 verticalEdges;
 [SerializeField] int priority;
 static readonly HashSet<RoomCameraBounds> active=new HashSet<RoomCameraBounds>();
 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]static void ResetRegistry()=>active.Clear();
 void OnEnable()=>active.Add(this);void OnDisable()=>active.Remove(this);
 static RoomCameraBounds Resolve(Camera camera,Transform actor)
 {
  if(!camera||!camera.orthographic||!actor)return null;
  RoomCameraBounds selected=null;
  foreach(var room in active)
   if(room&&room.isActiveAndEnabled&&room.gameObject.scene==camera.gameObject.scene&&room.actorRegion.Contains(actor.position)&&WallExists(room.leftWall)&&WallExists(room.rightWall))
    if(!selected||room.priority>selected.priority)selected=room;
  return selected;
 }
 public static bool TryGetEdges(Camera camera,Transform actor,out Vector2 edges)
 {
  var r=Resolve(camera,actor);edges=default;
  if(!r)return false;
  edges=new Vector2(r.leftWall?r.leftWall.bounds.max.x:r.horizontalEdges.x,r.rightWall?r.rightWall.bounds.min.x:r.horizontalEdges.y);
  return true;
 }
 public static Vector3 Confine(Camera camera,Transform actor,Vector3 position)
 {
  var r=Resolve(camera,actor);if(!r)return position;
  float left=r.leftWall?r.leftWall.bounds.max.x:r.horizontalEdges.x,right=r.rightWall?r.rightWall.bounds.min.x:r.horizontalEdges.y;
  position.x=ClampAxis(position.x,left,right,camera.orthographicSize*camera.aspect);
  if(r.constrainVertical)position.y=ClampAxis(position.y,r.verticalEdges.x,r.verticalEdges.y,camera.orthographicSize);
  return position;
 }
 static bool WallExists(Collider2D wall)=>!wall||(wall.enabled&&wall.gameObject.activeInHierarchy);
 public static float ClampAxis(float centre,float near,float far,float halfView)=>far-near<=2*halfView?(near+far)*.5f:Mathf.Clamp(centre,near+halfView,far-halfView);
 void OnDrawGizmosSelected(){Gizmos.color=Color.cyan;Gizmos.DrawWireCube(actorRegion.center,new Vector3(actorRegion.width,actorRegion.height,0));}
}

