using UnityEngine;

public enum ChildhoodAction { Phone, Parents, Camera, LeaveHome, Stairs, Fish, Cat, LiMei, Chess, Storyteller, Bulletin, Rings, Endpoint }
public class Scene03Interactable : MonoBehaviour, IInteractable
{
    public Scene03Chapter chapter;
    public ChildhoodAction action;
    public string interactionPrompt="[E] 调查";
    public string GetInteractionPrompt()
    {
        if(chapter==null)return "";
        if(action==ChildhoodAction.Camera && (chapter.Story==null || !chapter.Story.Has(StoryFlag.Scene03IntroCompleted) || chapter.Story.Has(StoryFlag.Scene03CameraTaken)))return "";
        if(action==ChildhoodAction.Fish&&chapter.Story!=null&&chapter.Story.Has(StoryFlag.Scene03RiverSequenceCompleted))return "";
        if(action==ChildhoodAction.Cat && chapter.Story!=null && chapter.Story.Has(StoryFlag.FishObtained) && !chapter.Story.Has(StoryFlag.CatFed))return "[E] 喂小猫";
        return interactionPrompt;
    }
    public void Interact(){if(chapter!=null)chapter.Interact(action);}
}
