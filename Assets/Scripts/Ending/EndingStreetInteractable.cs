using UnityEngine;
public sealed class EndingStreetInteractable:MonoBehaviour,IInteractable {
    public EndingStreetController owner;
    public string GetInteractionPrompt()=>owner!=null?owner.Prompt:"";
    public void Interact(){if(owner!=null)owner.Interact();}
}
