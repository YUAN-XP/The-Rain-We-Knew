using UnityEngine;
public class FamilyCameraPlacement : MonoBehaviour,IInteractable
{
    public Scene03FamilySequence sequence;
    public string GetInteractionPrompt()=>sequence!=null&&sequence.AwaitingPlacement?"[E] 放置相机":"";
    public void Interact(){if(sequence!=null)sequence.PlaceCamera();}
}
