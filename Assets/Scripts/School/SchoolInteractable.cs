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
    public string GetInteractionPrompt() => school!=null&&school.chase!=null&&school.chase.IsActive
        &&school.chase.Step!=SchoolChaseStep.Escaping&&school.chase.Step!=SchoolChaseStep.Exploring?"":interactionPrompt;
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
