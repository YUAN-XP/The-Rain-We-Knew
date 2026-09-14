using UnityEngine;

public enum SchoolYouthAction
{
    ZhengTao, ZhangRuoyuan, Crowd, SecurityNote, SecurityComputer,
    Toilet, Gate, FemaleToilet, Roof, ReadingChair, Director
}

[DisallowMultipleComponent]
public sealed class SchoolYouthInteractable : MonoBehaviour, IInteractable
{
    public SchoolYouthController flow;
    public SchoolYouthAction action;
    public string interactionPrompt = "[E] 交谈";
    public DialogueData dialogue;
    public StoryFlag onceFlag;
    public string GetInteractionPrompt() => interactionPrompt;
    public void Interact() { if (flow != null) flow.Interact(this); }
}
