using UnityEngine;

public class FileArchiveInteractable : MonoBehaviour,IInteractable
{
    [SerializeField] private string interactionPrompt="[E] 调查";
    [SerializeField] private FileData file;
    [SerializeField] private DialogueData dialogue;
    [SerializeField] private FileArchiveManager archive;
    [SerializeField] private DialogueManager dialogueManager;
    private bool warned;
    private void Start()
    {
        if(archive==null)archive=FindAnyObjectByType<FileArchiveManager>();
        if(archive!=null && archive.HasFile(file))DisableCollection();
    }
    private void DisableCollection(){var collider=GetComponent<Collider2D>();if(collider!=null)collider.enabled=false;}
    public string GetInteractionPrompt()=>interactionPrompt;
    public void Interact()
    {
        if(archive==null)archive=FindAnyObjectByType<FileArchiveManager>();
        if(dialogueManager==null)dialogueManager=FindAnyObjectByType<DialogueManager>();
        var controls=FindAnyObjectByType<GameControlManager>();
        if(file==null || archive==null || dialogue!=null && dialogueManager==null){if(!warned){Debug.LogWarning("FileArchiveInteractable needs its file and runtime managers.",this);warned=true;}return;}
        if(controls!=null && controls.IsLocked() || archive.HasFile(file))return;
        archive.AddFile(file);
        DisableCollection();
        if(dialogue!=null)dialogueManager.StartDialogue(dialogue);
    }
}
