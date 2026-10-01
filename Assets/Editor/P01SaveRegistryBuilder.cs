using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class P01SaveRegistryBuilder {
    [MenuItem("Tools/ProjectMystery/P01/Rebuild Save Asset Registry")]
    public static void Run(){
        if(Application.isPlaying)throw new InvalidOperationException("Edit mode required");
        const string path="Assets/Resources/SaveAssetRegistry.asset";
        var registry=AssetDatabase.LoadAssetAtPath<SaveAssetRegistry>(path);
        if(!registry){registry=ScriptableObject.CreateInstance<SaveAssetRegistry>();AssetDatabase.CreateAsset(registry,path);}
        registry.entries=AssetDatabase.FindAssets("t:ItemData").Concat(AssetDatabase.FindAssets("t:FileData")).Distinct().OrderBy(x=>x).Select(g=>new SaveAssetRegistry.Entry{key=g,asset=AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(g))}).ToArray();
        EditorUtility.SetDirty(registry);AssetDatabase.SaveAssets();Debug.Log("Save registry: "+registry.entries.Length+" stable assets");
    }
}
