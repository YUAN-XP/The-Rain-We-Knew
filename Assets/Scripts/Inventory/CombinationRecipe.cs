using System;
using UnityEngine;

[CreateAssetMenu(menuName="Game Data/Combination Recipe")]
public sealed class CombinationRecipe : ScriptableObject
{
    [Serializable] public sealed class Material {
        public ItemData item;
        [Min(1)] public int count=1;
        public bool consume=true;
        [Tooltip("An explicit repair input may retain a permanent tool whose original canCombine is false.")]
        public bool retainedTool;
    }
    public string recipeId;
    public Material[] materials;
    public StoryFlag[] prerequisites;
    public StoryFlag completedFlag;
    public FileData resultFile;
    public ItemData resultItem;
    public ItemData retainedResult;
    public string resultName;
    [TextArea] public string resultDescription;
    public bool restoreLamp;
}
