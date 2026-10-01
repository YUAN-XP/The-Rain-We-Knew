using UnityEngine;

// Resolve authored side-view entrance anchors against the actual solid floor before
// revealing a transition. Planar mazes deliberately keep their authored coordinates.
public static class GroundedPlacement {
    public static bool IsSupported(PlayerController player){
        if(!player)return false;if(player.IsPlanarMovement)return true;
        Physics2D.SyncTransforms();var shape=player.GetComponent<Collider2D>();if(!shape)return false;
        var bounds=shape.bounds;var origin=new Vector2(bounds.center.x,bounds.min.y+.05f);
        foreach(var hit in Physics2D.RaycastAll(origin,Vector2.down,.12f)){
            var c=hit.collider;if(!c||c==shape||c.isTrigger||c.attachedRigidbody||hit.normal.y<.65f)continue;
            if(c.GetComponentInParent<PlayerController>()||c.GetComponentInParent<IInteractable>()!=null)continue;
            return true;
        }
        return false;
    }
    public static bool Settle(PlayerController player,float maximumDrop=1.5f){
        if(!player||player.IsPlanarMovement)return false;
        Physics2D.SyncTransforms();var shape=player.GetComponent<Collider2D>();if(!shape)return false;
        var body=player.GetComponent<Rigidbody2D>();var bounds=shape.bounds;
        Vector2 origin=new Vector2(bounds.center.x,bounds.min.y+.4f);
        float top=float.NegativeInfinity;
        foreach(var hit in Physics2D.RaycastAll(origin,Vector2.down,maximumDrop+.4f)){
            var c=hit.collider;if(!c||c==shape||c.isTrigger||c.attachedRigidbody||hit.normal.y<.65f)continue;
            if(c.GetComponentInParent<PlayerController>()||c.GetComponentInParent<IInteractable>()!=null)continue;
            if(hit.point.y>top)top=hit.point.y;
        }
        if(float.IsNegativeInfinity(top))return false;
        var position=player.transform.position;position.y+=top+.005f-bounds.min.y;
        player.transform.position=position;if(body){body.position=position;body.linearVelocity=Vector2.zero;}
        Physics2D.SyncTransforms();return true;
    }
}
