using UnityEngine;

public class SimpleInteractable : MonoBehaviour, IInteractable
{
    [Header("Interaction")]
    [SerializeField] private string interactionPrompt = "[E] 调查";

    [TextArea]
    [SerializeField] private string interactionText = "调查";

    public string GetInteractionPrompt()
    {
        return interactionPrompt;
    }

    public void Interact()
    {
        Debug.Log(interactionText);
    }
}