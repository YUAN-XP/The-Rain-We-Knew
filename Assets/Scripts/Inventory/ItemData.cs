using UnityEngine;

public enum ItemCategory
{
    Regular,
    KeyItem
}

[CreateAssetMenu(
    fileName = "NewItem",
    menuName = "Game Data/Item Data"
)]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemId;
    public string itemName;

    [TextArea(3, 6)]
    public string description;

    [Header("Display")]
    public Sprite icon;

    [Header("Category")]
    public ItemCategory category =
        ItemCategory.Regular;

    [Header("Properties")]
    public bool isPermanent = false;
    public bool canCombine = false;
}