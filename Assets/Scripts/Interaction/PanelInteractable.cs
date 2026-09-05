using UnityEngine;

public class PanelInteractable : MonoBehaviour,IInteractable
{
    public enum PanelKind { Desk, AdultWardrobe }
    [SerializeField] private PanelKind panel;
    [SerializeField] private string interactionPrompt="[E] 调查";
    public string GetInteractionPrompt()=>interactionPrompt;
    public void Interact() {
        var ui=FindAnyObjectByType<DeskCloseupController>(); if(ui==null)return;
        if(panel==PanelKind.Desk)ui.OpenDesk();else ui.OpenWardrobe();
    }
}
