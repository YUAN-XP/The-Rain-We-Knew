using UnityEngine;

[CreateAssetMenu(
    fileName = "NewFile",
    menuName = "Game Data/File Data"
)]
public class FileData : ScriptableObject
{
    [Header("Basic Info")]
    public string fileId;
    public string fileName;

    [TextArea(3, 6)]
    public string summary;

    [TextArea(8, 20)]
    public string content;

    [Header("Display")]
    public Sprite icon;
}