using UnityEngine;

// Permission wrapper only. Movement/fade remains the established generic transition.
public class SchoolPortal : MonoBehaviour, IInteractable
{
    public SchoolLayoutController school;
    public string portalId;
    public RoomTransitionInteractable transition;
    public string prompt="[E] 进入";
    bool feedbackShown;
    public SchoolPortalDefinition Definition => school.data.Portal(portalId);
    public SchoolPortalGate Gate => school.Variant==SchoolWorldVariant.Otherworld ? Definition.otherworld : Definition.memory;
    void OnEnable(){ var c=GetComponent<Collider2D>(); if(c!=null && school!=null && school.data!=null) c.enabled=Gate!=SchoolPortalGate.Closed || !string.IsNullOrWhiteSpace(Definition.blockedText); }
    public string GetInteractionPrompt()=>prompt;
    public bool CanPass()
    {
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
        if(school.IsYouthEndpoint){runtime.GetComponent<StoryNoticeUI>()?.Show("Stage 5A 到此结束，可在教室内活动；退出 Play Mode 可结束体验。",4);return;}
        if(CanPass()){transition.Interact();return;}
        if(Definition.onceOnlyFeedback&&feedbackShown)return;
        feedbackShown=true;
        if(!string.IsNullOrWhiteSpace(Definition.blockedText))runtime.GetComponent<StoryNoticeUI>()?.Show(Definition.blockedText,3);
    }
    public void Arrived(){school.EnterRoom(Definition.targetRoom);}
}

