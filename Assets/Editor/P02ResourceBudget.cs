using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class P02ResourceBudget {
    public static void Restore() {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Edit mode only");
        AssetDatabase.DesiredWorkerCount=2;
        AssetDatabase.ForceToDesiredWorkerCount();
        File.WriteAllText("D:/游戏设计/Codex工作区/维护记录/unity-resource-restored.txt",DateTime.Now.ToString("O")+" import_workers="+AssetDatabase.DesiredWorkerCount);
    }
    public static void Trim() {
        if(EditorApplication.isPlaying) throw new InvalidOperationException("Edit mode only");
        AssetDatabase.DesiredWorkerCount=0;
        AssetDatabase.ForceToDesiredWorkerCount();
        EditorUtility.UnloadUnusedAssetsImmediate();
        GC.Collect(); GC.WaitForPendingFinalizers();
        Directory.CreateDirectory("D:/游戏设计/Codex工作区/维护记录");
        File.WriteAllText("D:/游戏设计/Codex工作区/维护记录/unity-resource-trim.txt",DateTime.Now.ToString("O")+" managed="+GC.GetTotalMemory(false)+" import_workers="+AssetDatabase.DesiredWorkerCount);
    }
}
