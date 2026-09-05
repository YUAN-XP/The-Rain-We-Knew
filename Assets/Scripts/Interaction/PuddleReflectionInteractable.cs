using System.Collections;
using UnityEngine;

public class PuddleReflectionInteractable :
    MonoBehaviour,
    IInteractable
{
    [SerializeField]
    private string interactionPrompt =
        "[E] Inspect";

    [Header("Dialogue")]
    [SerializeField]
    private DialogueData beforeReflectionDialogue;

    [SerializeField]
    private DialogueData afterReflectionDialogue;

    [SerializeField]
    private DialogueData repeatDialogue;

    [SerializeField]
    private DialogueManager dialogueManager;

    [Header("Reflection")]
    [SerializeField]
    private GameObject reflectionObject;

    [SerializeField]
    private float beforeRevealDelay = 0.7f;

    [SerializeField]
    private float afterRevealDelay = 0.5f;

    [Header("Game Control")]
    [SerializeField]
    private GameControlManager gameControlManager;

    private bool reflectionSeen = false;
    private bool sequenceRunning = false;

    private void Awake()
    {
        if (reflectionObject != null)
        {
            reflectionObject.SetActive(false);
        }
    }

    public string GetInteractionPrompt()
    {
        return interactionPrompt;
    }

    public void Interact()
    {
        if(dialogueManager==null)dialogueManager=FindAnyObjectByType<DialogueManager>();
        if(gameControlManager==null)gameControlManager=FindAnyObjectByType<GameControlManager>();

        if (sequenceRunning)
        {
            return;
        }

        if (dialogueManager == null)
        {
            Debug.LogWarning(
                "PuddleReflectionInteractable: " +
                "DialogueManager is missing."
            );

            return;
        }

        if (reflectionSeen)
        {
            PlayRepeatDialogue();
            return;
        }

        StartCoroutine(
            PlayFirstReflectionSequence()
        );
    }

    private IEnumerator PlayFirstReflectionSequence()
    {
        sequenceRunning = true;

        if (gameControlManager != null)
        {
            gameControlManager.AcquireLock(
                ControlLockReason.Interaction
            );
        }

        if (beforeReflectionDialogue != null)
        {
            dialogueManager.StartDialogue(
                beforeReflectionDialogue
            );

            yield return new WaitUntil(
                () =>
                    !dialogueManager
                        .IsDialogueActive()
            );
        }

        yield return new WaitForSeconds(
            beforeRevealDelay
        );

        if (reflectionObject != null)
        {
            reflectionObject.SetActive(true);
        }

        reflectionSeen = true;

        Debug.Log(
            "Scene01: Puddle reflection revealed."
        );

        yield return new WaitForSeconds(
            afterRevealDelay
        );

        if (afterReflectionDialogue != null)
        {
            dialogueManager.StartDialogue(
                afterReflectionDialogue
            );

            yield return new WaitUntil(
                () =>
                    !dialogueManager
                        .IsDialogueActive()
            );
        }

        if (gameControlManager != null)
        {
            gameControlManager.ReleaseLock(
                ControlLockReason.Interaction
            );
        }

        sequenceRunning = false;
    }

    private void PlayRepeatDialogue()
    {
        if (repeatDialogue == null)
        {
            return;
        }

        dialogueManager.StartDialogue(
            repeatDialogue
        );
    }
}