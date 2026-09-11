using UnityEngine;

public class DialogueInteractable :
    MonoBehaviour,
    IInteractable
{
    [SerializeField] private string interactionPrompt = "[E] Talk";

    [Header("Dialogue")]
    [SerializeField] private DialogueData dialogueData;
    [SerializeField] private DialogueManager dialogueManager;

    [Header("Optional first / repeat dialogue")]
    [SerializeField] private DialogueData repeatDialogue;
    [SerializeField] private StoryFlag firstPlayedFlag=StoryFlag.GuideFirstDialoguePlayed;

    private bool hasWarnedMissingDialogueManager;

    private void Awake()
    {
        ResolveDialogueManager();
    }

    public string GetInteractionPrompt()
    {
        return interactionPrompt;
    }

    public void Interact()
    {
        if (dialogueData == null)
        {
            return;
        }

        if (dialogueManager == null)
        {
            ResolveDialogueManager();
        }

        if (dialogueManager == null)
        {
            if (!hasWarnedMissingDialogueManager)
            {
                Debug.LogWarning(
                    "DialogueInteractable could not find a DialogueManager. " +
                    "Assign one in the Inspector or ensure one exists at runtime.",
                    this
                );

                hasWarnedMissingDialogueManager = true;
            }

            return;
        }

        var controls=FindAnyObjectByType<GameControlManager>();
        if(controls!=null && controls.IsLocked())return;
        var story=repeatDialogue!=null?FindAnyObjectByType<StoryStateManager>():null;
        dialogueManager.StartDialogue(story!=null && story.Has(firstPlayedFlag)?repeatDialogue:dialogueData);
        if(story!=null && dialogueManager.IsDialogueActive())story.Set(firstPlayedFlag);
    }

    public void ConfigureDialogue(DialogueData data) { dialogueData=data;repeatDialogue=null; }

    private void ResolveDialogueManager()
    {
        if (dialogueManager != null)
        {
            return;
        }

        dialogueManager = FindAnyObjectByType<DialogueManager>(
            FindObjectsInactive.Include
        );
    }
}
