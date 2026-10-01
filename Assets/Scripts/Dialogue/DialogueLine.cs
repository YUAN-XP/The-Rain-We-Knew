using System;
using UnityEngine;

[Serializable]
public class DialogueLine
{
    public string speaker;
    [Tooltip("Stable identity; speaker is only the displayed name (including ???).")]
    public DialogueCharacterId characterId;
    public DialogueAge age;
    public DialoguePortraitMode portraitMode;
    [Tooltip("Optional approved catalogue key for an expression or anonymous silhouette.")]
    public string portraitKey;
    [TextArea(2, 5)] public string text;
}
