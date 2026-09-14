using System.Collections.Generic;
using UnityEngine;

// Queries authored inactive geometry without activating its colliders or events.
public static class SchoolChaseGeometry
{
    public static IEnumerable<Bounds> Solids(SchoolLayoutController layout, SchoolWorldVariant variant, string roomId)
    {
        var root=variant==SchoolWorldVariant.Memory?layout.memoryRoot:layout.otherworldRoot;
        var room=root!=null?root.transform.Find(roomId):null;
        if(room==null)yield break;
        foreach(var box in room.GetComponentsInChildren<BoxCollider2D>(true)) {
            if(!box.enabled||box.isTrigger)continue;
            bool active=true;for(var p=box.transform;p!=room&&p!=null;p=p.parent)if(!p.gameObject.activeSelf){active=false;break;}
            if(!active)continue;
            var s=box.transform.lossyScale;var size=new Vector3(Mathf.Abs(s.x)*box.size.x,Mathf.Abs(s.y)*box.size.y,1);
            yield return new Bounds(box.transform.TransformPoint(box.offset),size);
        }
    }
    public static bool Ground(SchoolLayoutController layout, SchoolWorldVariant variant,string roomId,float x,out float y)
    {
        y=0;var room=layout.data.Room(roomId);if(room==null||x<room.origin.x+.5f||x>room.origin.x+room.width-.5f)return false;
        bool found=false;
        foreach(var b in Solids(layout,variant,roomId))
            if(x>b.min.x+.05f&&x<b.max.x-.05f&&Mathf.Abs(b.max.y-room.origin.y)<.6f) {y=b.max.y;found=true;}
        return found;
    }
    public static bool Clear(SchoolLayoutController layout,SchoolWorldVariant variant,string room,Vector2 center,Vector2 half)
    {
        foreach(var b in Solids(layout,variant,room))
            if(center.x+half.x>b.min.x+.025f&&center.x-half.x<b.max.x-.025f&&center.y+half.y>b.min.y+.035f&&center.y-half.y<b.max.y-.035f)return false;
        return true;
    }
    public static bool Segment(SchoolLayoutController layout,SchoolWorldVariant variant,string room,Vector2 a,Vector2 b,float half=.25f)
    {
        int samples=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(a,b)/.16f));
        for(int i=0;i<=samples;i++)if(!Clear(layout,variant,room,Vector2.Lerp(a,b,i/(float)samples),new Vector2(half,.5f)))return false;
        return true;
    }
}
