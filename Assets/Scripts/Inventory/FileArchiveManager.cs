using System.Collections.Generic;
using UnityEngine;

public class FileArchiveManager : MonoBehaviour
{
    [Header("Runtime Files")]
    [SerializeField]
    private List<FileData> files =
        new List<FileData>();

    public IReadOnlyList<FileData> Files => files;
    internal void RestoreSession(IReadOnlyList<FileData> snapshot)
    {
        if(snapshot==null)return;
        files.Clear(); foreach(var file in snapshot)if(file!=null&&!HasFile(file))files.Add(file);
    }

    public void AddFile(FileData file)
    {
        if (file == null)
        {
            return;
        }

        if (HasFile(file))
        {
            Debug.Log(
                "Files: File already exists - "
                + file.fileName
            );

            return;
        }

        files.Add(file);

        Debug.Log(
            "Files: Added file - "
            + file.fileName
        );
    }

    public bool HasFile(FileData file)
    {
        if (file == null)
        {
            return false;
        }

        return files.Contains(file) || !string.IsNullOrWhiteSpace(file.fileId) && HasFileById(file.fileId);
    }

    // Replace the runtime list entry, never mutate the source ScriptableObject.
    public bool ReplaceFileById(FileData replacement)
    {
        if(replacement==null||string.IsNullOrWhiteSpace(replacement.fileId))return false;
        for(int i=0;i<files.Count;i++)if(files[i]!=null&&files[i].fileId==replacement.fileId)
        {files[i]=replacement;return true;}
        AddFile(replacement);return true;
    }

    public bool HasFileById(string fileId)
    {
        foreach (FileData file in files)
        {
            if (file != null &&
                file.fileId == fileId)
            {
                return true;
            }
        }

        return false;
    }
}
