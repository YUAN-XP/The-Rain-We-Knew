using UnityEngine;

[CreateAssetMenu(
    fileName = "NewDialogue",
    menuName = "Dialogue/Dialogue Data"
)]
public class DialogueData : ScriptableObject
{
    public DialogueLine[] lines;
    [Tooltip("Scene-authored exception: opening monologue / memory flashes / off-screen doctor.")]
    public bool hideAllPortraits;
    [Min(0)] public float minimumReadSeconds;
}