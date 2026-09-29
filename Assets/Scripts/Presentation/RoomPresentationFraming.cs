using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Opt-in room composition; does not change actors, physics, controls or transitions.</summary>
[RequireComponent(typeof(Camera)), DefaultExecutionOrder(500)]
public sealed class RoomPresentationFraming : MonoBehaviour
{
    [Serializable] public struct Room { public Rect area; public float floorY; public float size; }
    public Room[] rooms;
    [Range(0,.2f)] public float letterbox=.10f;
    public float centreAboveFloor=1.55f;
    public bool FramesPosition(Vector2 position)=>isActiveAndEnabled&&rooms!=null&&Array.Exists(rooms,room=>room.area.Contains(position));
    Camera view;CameraFollow follow;GameControlManager controls;
    GameObject bars;bool applied;float previousSize,lastSize;
    void Awake(){view=GetComponent<Camera>();follow=GetComponent<CameraFollow>();}
    void Update(){if(applied&&follow)follow.PresentationLimitPadding=Mathf.Max(0,previousSize-lastSize)*view.aspect;}
    // Explicitly called while a transition mask is opaque. Locks intentionally keep
    // normal following off, but must not postpone destination composition.
    public static void PrepareCurrentView()
    {
        Physics2D.SyncTransforms();
        var camera=Camera.main;
        if(!camera)return;
        var framing=camera.GetComponent<RoomPresentationFraming>();
        if(framing&&framing.isActiveAndEnabled)framing.ApplyFrame(true);
        else camera.GetComponent<CameraFollow>()?.SnapToTarget();
    }
    void LateUpdate(){ApplyFrame(false);}
    void ApplyFrame(bool snap)
    {
        if(!view)view=GetComponent<Camera>();
        if(!follow)follow=GetComponent<CameraFollow>();
        if(!controls&&PersistentRuntime.Instance)controls=PersistentRuntime.Instance.GetComponent<GameControlManager>();
        var actor=controls?controls.ActivePlayer:null;
        int room=-1;
        if(actor&&follow&&rooms!=null)
            for(int i=0;i<rooms.Length;i++)if(rooms[i].area.Contains(actor.transform.position)){room=i;break;}
        if(room<0){Restore();return;}
        // Dialogue/transition locks disable following. Never apply a new zoom without
        // its matching centre: that would crop the actor during a scene entrance.
        if(!snap&&!follow.enabled){if(bars)bars.SetActive(applied);return;}
        if(!applied){previousSize=view.orthographicSize;applied=true;}
        view.orthographicSize=lastSize=rooms[room].size;
        follow.PresentationLimitPadding=Mathf.Max(0,previousSize-lastSize)*view.aspect;
        if(snap)follow.SnapToTarget();
        if(snap||follow.enabled){var p=view.transform.position;p.y=rooms[room].floorY+centreAboveFloor;
        view.transform.position=RoomCameraBounds.Confine(view,actor.transform,p);}
        if(!bars)CreateBars();bars.SetActive(true);
    }
    void CreateBars()
    {
        bars=new GameObject("RoomLetterbox_Runtime",typeof(Canvas));bars.hideFlags=HideFlags.DontSave;
        bars.transform.SetParent(transform,false);
        var canvas=bars.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=-1000;
        for(int i=0;i<2;i++)
        {
            var g=new GameObject(i==0?"Top":"Bottom",typeof(RectTransform),typeof(Image));g.transform.SetParent(bars.transform,false);
            var r=(RectTransform)g.transform;
            r.anchorMin=new Vector2(0,i==0?1-letterbox:0);r.anchorMax=new Vector2(1,i==0?1:letterbox);
            r.offsetMin=r.offsetMax=Vector2.zero;var image=g.GetComponent<Image>();image.color=Color.black;image.raycastTarget=false;
        }
    }
    void Restore()
    {
        if(applied&&view&&Mathf.Approximately(view.orthographicSize,lastSize))view.orthographicSize=previousSize;
        applied=false;if(bars)bars.SetActive(false);
        if(follow)follow.PresentationLimitPadding=0;
    }
    void OnDisable(){Restore();}
    void OnDestroy(){if(bars)Destroy(bars);}
}
