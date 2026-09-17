using UnityEngine;

// Permission wrapper only. Movement/fade remains the established generic transition.
public class SchoolPortal : MonoBehaviour, IInteractable
{
    public SchoolLayoutController school;
    public SchoolYouthController youthFlow;
    public SchoolChaseController chase;
    public string portalId;
    public RoomTransitionInteractable transition;
    public string prompt="[E] 进入";
    bool feedbackShown;
    public SchoolPortalDefinition Definition => school.data.Portal(portalId);
    public SchoolPortalGate Gate => school.Variant==SchoolWorldVariant.Otherworld ? Definition.otherworld : Definition.memory;
    void OnEnable(){ var c=GetComponent<Collider2D>(); if(c!=null && school!=null && school.data!=null) c.enabled=Gate!=SchoolPortalGate.Closed || !string.IsNullOrWhiteSpace(Definition.blockedText); }
    public string GetInteractionPrompt()=>chase!=null&&chase.IsActive&&!chase.CanPass(Definition)?"":prompt;
    public bool CanPass()
    {
        if(chase!=null&&chase.IsActive)return chase.CanPass(Definition);
        if (youthFlow != null && youthFlow.CanUsePortal(this, out bool allowed)) return allowed;
        if(school.IsYouthEndpoint)return false;
        var state=PersistentRuntime.Instance?.GetComponent<StoryStateManager>();
        switch(Gate)
        {
            case SchoolPortalGate.Open:return true;
            case SchoolPortalGate.EquipmentKey:return state!=null&&state.Has(StoryFlag.EquipmentDoorOpened);
            // Future clue/evidence ownership never bypasses the explicit corresponding interaction.
            default:return false;
        }
    }
    public void Interact()
    {
        var runtime=PersistentRuntime.Instance;if(runtime==null)return;
        var controls=runtime.GetComponent<GameControlManager>();if(controls==null||controls.IsLocked())return;
        if(chase!=null&&chase.HandlePortal(this))return;
        if (youthFlow != null && youthFlow.HandlePortal(this)) return;
        if(school.IsYouthEndpoint){runtime.GetComponent<StoryNoticeUI>()?.Show("Stage 5A 到此结束，可在教室内活动；退出 Play Mode 可结束体验。",4);return;}
        if(CanPass()){transition.Interact();return;}
        if(Definition.onceOnlyFeedback&&feedbackShown)return;
        feedbackShown=true;
        if(!string.IsNullOrWhiteSpace(Definition.blockedText))runtime.GetComponent<StoryNoticeUI>()?.Show(Definition.blockedText,3);
    }
    public void Arrived(){school.EnterRoom(Definition.targetRoom);if(chase!=null)chase.PortalArrived(portalId);}
}
