using UnityEngine;

/// <summary>Hides art from adjacent stacked rooms without overriding story-enabled state.</summary>
[DefaultExecutionOrder(950)]
public sealed class ArtRoomVisibility : MonoBehaviour
{
    [SerializeField] private Vector2 regionMin,regionMax;
    [SerializeField] private SpriteRenderer[] visuals;
    private GameControlManager controls;
    private void LateUpdate()
    {
        if(!controls&&PersistentRuntime.Instance)controls=PersistentRuntime.Instance.GetComponent<GameControlManager>();
        var actor=controls?controls.ActivePlayer:null;bool show=false;
        if(actor){var p=actor.transform.position;show=p.x>=regionMin.x&&p.x<=regionMax.x&&p.y>=regionMin.y&&p.y<=regionMax.y;}
        if(visuals!=null)foreach(var r in visuals)if(r)r.forceRenderingOff=!show;
    }
    private void OnDisable(){if(visuals!=null)foreach(var r in visuals)if(r)r.forceRenderingOff=false;}
}
