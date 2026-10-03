using System;
using UnityEngine;

/// <summary>Explicit identity and independently painted direction mapping.</summary>
[CreateAssetMenu(menuName="ProjectMystery/P02/Motion library")]
public sealed class P02MotionLibrary : ScriptableObject
{
    [Serializable] public sealed class Entry
    {
        public string actor, direction;
        public P02RigDefinition rig;
    }
    public Entry[] entries;
    public P02RigDefinition Find(string actor,string direction)
    {
        if(entries!=null)foreach(var e in entries)if(e.actor==actor&&e.direction==direction)return e.rig;
        return null;
    }
    public static P02MotionLibrary Current => Resources.Load<P02MotionLibrary>("P02MotionLibrary");
}
