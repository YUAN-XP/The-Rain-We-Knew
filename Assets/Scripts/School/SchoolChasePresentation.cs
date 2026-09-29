using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class SchoolChaseController
{
    readonly Dictionary<SpriteRenderer,Color> originalColors=new Dictionary<SpriteRenderer,Color>();
    SchoolWorldVariant previewVariant;
    public void BeginPreview()
    {
        if(!CanUseCamera||IsPreviewing)return;
        previewVariant=Layout.Variant==SchoolWorldVariant.Memory?SchoolWorldVariant.Otherworld:SchoolWorldVariant.Memory;
        var current=Layout.Variant==SchoolWorldVariant.Memory?Layout.memoryRoot:Layout.otherworldRoot;
        var target=previewVariant==SchoolWorldVariant.Memory?Layout.memoryRoot:Layout.otherworldRoot;
        previewRoot=new GameObject("SchoolCameraVisualPreview");previewRoot.transform.SetParent(transform,false);
        foreach(var renderer in current.GetComponentsInChildren<SpriteRenderer>(true)) {
            hiddenSprites[renderer]=renderer.forceRenderingOff;renderer.forceRenderingOff=true;
        }
        foreach(var canvas in current.GetComponentsInChildren<Canvas>(true)) {hiddenCanvases[canvas]=canvas.enabled;canvas.enabled=false;}
        foreach(var crop in target.GetComponentsInChildren<SpriteArtRegion>(true)) crop.Rebuild();
        var retired=new HashSet<Renderer>(target.GetComponentsInChildren<RetiredArtVisibility>(true).SelectMany(x=>x.Renderers));
        foreach(var renderer in target.GetComponentsInChildren<SpriteRenderer>(true)) {
            if(!renderer.enabled||renderer.sprite==null||retired.Contains(renderer))continue;
            bool active=true;for(var p=renderer.transform;p!=target.transform&&p!=null;p=p.parent)if(!p.gameObject.activeSelf){active=false;break;}
            if(!active)continue;
            var ghost=new GameObject(renderer.name+"_Preview");ghost.transform.SetParent(previewRoot.transform,false);
            ghost.transform.SetPositionAndRotation(renderer.transform.position,renderer.transform.rotation);ghost.transform.localScale=renderer.transform.lossyScale;
            var copy=ghost.AddComponent<SpriteRenderer>();copy.sprite=renderer.sprite;copy.color=renderer.color;copy.flipX=renderer.flipX;copy.flipY=renderer.flipY;
            copy.sortingLayerID=renderer.sortingLayerID;copy.sortingOrder=renderer.sortingOrder;copy.drawMode=renderer.drawMode;copy.size=renderer.size;
        }
        IsPreviewing=true;
    }
    public void EndPreview()
    {
        foreach(var pair in hiddenSprites)if(pair.Key!=null)pair.Key.forceRenderingOff=pair.Value;
        foreach(var pair in hiddenCanvases)if(pair.Key!=null)pair.Key.enabled=pair.Value;
        hiddenSprites.Clear();hiddenCanvases.Clear();
        if(previewRoot!=null){previewRoot.SetActive(false);Destroy(previewRoot);}previewRoot=null;IsPreviewing=false;
    }
    public bool CanSwitchAtCurrentPosition(SchoolWorldVariant target,out Vector2 safe,out string reason)
    {
        safe=Controls.ActivePlayer.transform.position;reason="";
        if(!IsEscaping||doorPending){reason="A door or sequence is still active";return false;}
        var room=Layout.data.Room(Layout.CurrentRoom);
        bool exit=false;
        foreach(var portal in Layout.data.portals)
            if(portal.sourceRoom==room.id&&(target==SchoolWorldVariant.Memory?portal.memory:portal.otherworld)==SchoolPortalGate.Open
                &&portal.targetRoom!="StaffOffice"&&portal.targetRoom!="MaleToilet"&&portal.targetRoom!="EquipmentRoom")exit=true;
        // Roof has a reversible camera exit. This is deliberately NOT a
        // blanket exception for closed classrooms or the blocked reading area.
        bool reversibleRoof=room.id=="Roof"
            &&cameraTool!=null&&cameraTool.ContextAvailable;
        if(!exit&&!reversibleRoof){reason="Target room has no permitted exit: "+room.id+"/"+target;return false;}
        var collider=Controls.ActivePlayer.GetComponent<Collider2D>();
        if(collider==null){reason="Player collider missing";return false;}
        Vector2 offset=(Vector2)collider.bounds.center-safe,half=(Vector2)collider.bounds.extents;
        Vector2 original=safe;
        foreach(float dx in new[]{0f,-.04f,.04f,-.08f,.08f,-.12f,.12f})
        foreach(float dy in new[]{0f,.03f,.07f}) {
            var candidate=original+new Vector2(dx,dy);var center=candidate+offset;
            if(!SchoolChaseGeometry.Ground(Layout,target,room.id,candidate.x,out float ground)||Mathf.Abs(center.y-half.y-ground)>.22f)continue;
            bool crossed=false;
            foreach(var b in SchoolChaseGeometry.Solids(Layout,target,room.id))
                if(b.max.y>ground+.4f&&center.y+half.y>b.min.y&&center.y-half.y<b.max.y
                    &&(original.x-b.center.x)*(candidate.x-b.center.x)<0)crossed=true;
            if(crossed||!SchoolChaseGeometry.Clear(Layout,target,room.id,center,half))continue;
            safe=candidate;return true;
        }
        reason="No same-side minimal clearance at "+room.id+" / "+original;return false;
    }
    public void CommitPreview()
    {
        if(!IsPreviewing||!CanUseCamera)return;
        var target=previewVariant;
        if(!CanSwitchAtCurrentPosition(target,out Vector2 point,out string reason)) {
            LastSwitchRefusal=reason;Notice("这个位置无法安全切换。",3);EndPreview();return;
        }
        var oldVariant=Layout.Variant;string room=Layout.CurrentRoom;
        var p=Controls.ActivePlayer;Vector3 oldPosition=p.transform.position;
        var body=p.GetComponent<Rigidbody2D>();Vector2 velocity=body!=null?body.linearVelocity:Vector2.zero;
        OwnSequence();EndPreview();
        try {
            Layout.ApplyVariant(target);
            p.transform.position=point;if(body!=null){body.position=point;body.linearVelocity=Vector2.zero;}
            Layout.EnterRoom(room);Physics2D.SyncTransforms();
            var collider=p.GetComponent<Collider2D>();
            if(Layout.Variant!=target||!SchoolChaseGeometry.Ground(Layout,target,room,point.x,out float ground)
                ||collider==null||!SchoolChaseGeometry.Clear(Layout,target,room,collider.bounds.center,collider.bounds.extents))
                throw new System.InvalidOperationException("Target geometry did not commit safely");
            // Only the committed world grants relief. Clear logical capture,
            // doorway pursuit and the single pending spawn clock together.
            ClearEnemy();session.AdvanceGeneration();LastSwitchRefusal="";grace=doorwayGrace;
            captureReliefRemaining=room=="Roof"?roofCaptureReliefSeconds:captureReliefSeconds;
            ResetRandom();
        } catch(System.Exception error) {
            Layout.ApplyVariant(oldVariant);p.transform.position=oldPosition;
            if(body!=null){body.position=oldPosition;body.linearVelocity=velocity;}
            Layout.EnterRoom(room);Physics2D.SyncTransforms();LastSwitchRefusal=error.Message;
            Notice("这个位置无法安全切换。",3);
        } finally {ReleaseSequence();}
    }
    void SetHorrorPresentation(bool active)
    {
        if(!active) {
            foreach(var pair in originalColors)if(pair.Key!=null)pair.Key.color=pair.Value;originalColors.Clear();return;
        }
        foreach(var renderer in Layout.memoryRoot.GetComponentsInChildren<SpriteRenderer>(true)) {
            if(!originalColors.ContainsKey(renderer))originalColors[renderer]=renderer.color;
            var c=originalColors[renderer];renderer.color=new Color(c.r*.72f,c.g*.61f,c.b*.72f,c.a);
        }
    }
}
