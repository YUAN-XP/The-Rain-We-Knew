using UnityEngine;

public class RiverTalkInteractable : MonoBehaviour, IInteractable
{
    public Scene03RiverSequence sequence;
    public string GetInteractionPrompt()=>sequence!=null?sequence.GetInteractionPrompt():"";
    public void Interact(){if(sequence!=null)sequence.Interact();}
}
