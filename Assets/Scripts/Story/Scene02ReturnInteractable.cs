using UnityEngine;
public enum Scene02ReturnAction { Fragment, CardEcho, Fog }
public class Scene02ReturnInteractable : MonoBehaviour,IInteractable
{
    public Scene02ReturnController chapter;
    public Scene02ReturnAction action;
    public string GetInteractionPrompt()=>chapter!=null?chapter.Prompt(action):"";
    public void Interact(){if(chapter!=null)chapter.Interact(action);}
}
