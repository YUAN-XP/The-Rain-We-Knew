using System;
using UnityEngine;
using TMPro;

[CreateAssetMenu(menuName="Game Data/Ending/Stage 6")]
public sealed class EndingData : ScriptableObject
{
    [Serializable] public struct Cue { public string id; public DialogueData dialogue; }
    public Cue[] cues;
    public ItemData identityCard, originalHolder;
    public FileData completeCredential;
    public CombinationRecipe credentialRecipe;
    public DialogueData emailReaction;
    public TMP_FontAsset font;
    public Sprite bodySprite, treeSprite, pavilionSprite;
    public AudioClip siren, thunder, carLoop, riverLoop, rainLoop, wheelsLoop, monitorLoop, monitorStop;
    public string streetScene="Scene01_Rain", homeScene="Scene02_Courtyard", sequenceScene="EndingSequence";
    [Header("Alpha timings / replaceable presentation")]
    [Min(.1f)] public float approachSpeed=1.4f, followSpeed=4.8f, followDistance=2.2f;
    [Min(.1f)] public float panSeconds=2.4f, panHold=1.2f, boardingSeconds=1.8f;
    [HideInInspector] public float slowWalkSpeed=1.5f; // Serialized legacy value; Flashback Ratio now drives walking.
    [Min(.1f)] public float actionSeconds=2.6f, focusSeconds=3f, sirenDelay=.65f;
    [Tooltip("Optional visual feet offset when replacing the existing Guide placeholder.")]
    public float guideVisualGroundOffset=0;
    [Header("Ending walk only; ordinary movement and Sprint remain unchanged")]
    [Range(.8f,1f)] public float flashbackRatio=.85f;
    public DialogueData Find(string id) { if(cues!=null)foreach(var c in cues)if(c.id==id)return c.dialogue; return null; }
}
