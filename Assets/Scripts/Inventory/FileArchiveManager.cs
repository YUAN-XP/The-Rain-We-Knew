using System.Collections.Generic;
using UnityEngine;

public class FileArchiveManager : MonoBehaviour
{
    [Header("Runtime Files")]
    [SerializeField]
    private List<FileData> files =
        new List<FileData>();

    public IReadOnlyList<FileData> Files => files;

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

        return files.Contains(file);
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