using UnityEngine;

[CreateAssetMenu(menuName="Game Data/Adult Outfit",fileName="Outfit")]
public class OutfitData : ScriptableObject
{
    public AdultOutfit outfit;
    public string outfitName;
    [TextArea(2,6)] public string description;
    public Sprite preview;
    public bool initiallyOwned;
    public StoryFlag unlockFlag=StoryFlag.OverallsUnlocked;
}
