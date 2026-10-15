using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Text inventory only: opens existing scenes for reading and never saves them.
public static class LocalizationProjectAudit
{
    public const string Work = "D:/游戏设计/Codex工作区/work/2026-10-15-Localization/";
    [Serializable] public sealed class Entry { public string asset, owner, property, value; }
    [Serializable] public sealed class Inventory { public string[] scenes; public List<Entry> entries = new List<Entry>(); }
    static readonly HashSet<string> DisplayFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    { "m_text", "text", "speaker", "itemName", "description", "fileName", "summary", "content", "outfitName", "credits", "gameTitle", "prompt", "promptText", "displayName", "title", "label", "hint", "notice", "tutorial", "name" };

    static void Read(UnityEngine.Object obj, string asset, Inventory inventory)
    {
        if (!obj) return;
        var so = new SerializedObject(obj);
        var p = so.GetIterator();
        while (p.Next(true))
        {
            if (p.propertyType != SerializedPropertyType.String || string.IsNullOrWhiteSpace(p.stringValue)) continue;
            string value = p.stringValue;
            if (p.name.StartsWith("m_") && p.name != "m_text") continue;
            if (!DisplayFields.Contains(p.name) && !Regex.IsMatch(value, "[\\u3400-\\u9fff]")) continue;
            if (value.Contains("Assets/") || value.Contains("D:/") || value.Contains("D:\\")) continue;
            inventory.entries.Add(new Entry { asset = asset, owner = obj.GetType().Name + ": " + obj.name, property = p.propertyPath, value = value });
        }
    }

    public static void Export()
    {
        Directory.CreateDirectory(Work);
        var inventory = new Inventory { scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray() };
        foreach (string path in AssetDatabase.GetAllAssetPaths().Where(p => p.EndsWith(".asset") &&
                     (p.StartsWith("Assets/GameData/") || p.StartsWith("Assets/ScriptableObjects/") || p == "Assets/Resources/P07Presentation.asset")))
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path)) if (asset is ScriptableObject) Read(asset, path, inventory);
        foreach (string path in inventory.scenes)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            foreach (var root in scene.GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<Component>(true))
                    if (component) Read(component, path, inventory);
        }
        File.WriteAllText(Work + "serialized_strings.json", JsonUtility.ToJson(inventory, true));
        File.WriteAllText(Work + "inventory_result.txt", "Exported " + inventory.entries.Count + " display strings from " + inventory.scenes.Length + " build scenes and authored data. No scenes saved.");
        Debug.Log("Localization display inventory complete: " + inventory.entries.Count);
    }
}
