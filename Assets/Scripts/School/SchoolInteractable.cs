using UnityEngine;

public enum SchoolAction
{
    FogReturn, SecurityDoor, SecurityWindow, EquipmentDoor, EquipmentEcho,
    Basketball, BasketKey, UniformAndPencil, OwnDesk, ScratchedDesk, Investigation
}

// Place this on the interactable collider. Keep RoomTransition on its existing child.
public sealed class SchoolInteractable : MonoBehaviour, IInteractable
{
    public SchoolSceneController school;
    public SchoolAction action;
    public string interactionPrompt = "[E] 调查";
    public RoomTransitionInteractable transition;
    public string destinationRoomId;
    public DialogueData investigation;
    public string GetInteractionPrompt()
    {
        if(school!=null&&school.chase!=null&&school.chase.IsActive
            &&school.chase.Step!=SchoolChaseStep.Escaping&&school.chase.Step!=SchoolChaseStep.Exploring)return "";
        // The normal-campus flow deliberately ignores these otherworld-only props.
        // Do not leave a dead E target in front of the nearby living character.
        if(school!=null&&school.youthFlow!=null&&school.youthFlow.IsYouthMemory
            &&(action==SchoolAction.OwnDesk||action==SchoolAction.ScratchedDesk
               ||action==SchoolAction.UniformAndPencil||action==SchoolAction.Basketball||action==SchoolAction.BasketKey))return "";
        return interactionPrompt;
    }
    public void Interact() { if (school != null) school.Interact(this); }
    public void Arrived()
    {
        if (school != null && school.layout != null && !string.IsNullOrEmpty(destinationRoomId))
        {
            school.layout.EnterRoom(destinationRoomId);
            if(school.chase!=null&&action==SchoolAction.SecurityDoor)
                school.chase.PortalArrived(destinationRoomId=="SecurityRoom"?"Security_Door_AtoB":"Security_Door_BtoA");
        }
    }
}
