using UnityEngine;

[CreateAssetMenu(menuName="Game Data/Scene03 Content")]
public class Scene03Content : ScriptableObject
{
    public DialogueData phone, parents, parentsRepeat, takeCamera, exitLocked, stairs;
    public DialogueData fish, fishRepeat, cat, feedCat, liMei, chess, messageDelivered;
    public DialogueData storytellerEnd, card, prelude, endpoint;
    public DialogueData ringRetry;
    public ItemData oldCamera, fishItem, coinItem;
    public FileData cameraGentlemanFile;
    [TextArea(3,8)] public string[] notices;
    [TextArea(3,8)] public string[] storyActs;
    public AudioClip cicadas, neighbours, home, phoneRing, water, throwSound, hitSound, retrySound, successSound;
}
