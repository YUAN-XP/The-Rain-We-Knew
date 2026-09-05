using UnityEngine;

public class PuzzleDoorInteractable : MonoBehaviour,IInteractable
{
    [SerializeField] private ThreeDoorPuzzleController puzzle;
    [SerializeField,Range(0,2)] private int doorIndex;
    [SerializeField] private string interactionPrompt="[E] 进入";
    public string GetInteractionPrompt()=>puzzle!=null && puzzle.IsActive && puzzle.Progress<3?interactionPrompt:"";
    public void Interact(){if(puzzle!=null)puzzle.Choose(doorIndex);}
}
