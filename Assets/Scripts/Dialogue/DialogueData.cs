using UnityEngine;

[CreateAssetMenu(
    fileName = "NewDialogue",
    menuName = "Dialogue/Dialogue Data"
)]
public class DialogueData : ScriptableObject
{
    public DialogueLine[] lines;
    [Min(0)] public float minimumReadSeconds;
}