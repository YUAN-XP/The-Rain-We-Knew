using UnityEngine;

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

    [Header("Properties")]
    public bool canCombine = false;
}