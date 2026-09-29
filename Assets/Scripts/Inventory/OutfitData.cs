using UnityEngine;

[CreateAssetMenu(menuName="Game Data/Adult Outfit",fileName="Outfit")]
public class OutfitData : ScriptableObject
{
    public AdultOutfit outfit;
    public string outfitName;
    [TextArea(2,6)] public string description;
    public Sprite preview;
    [Tooltip("Clothing by itself, separate from the full-character wearing preview.")]
    public Sprite clothingImage;
    public bool initiallyOwned;
    public StoryFlag unlockFlag=StoryFlag.OverallsUnlocked;
}
