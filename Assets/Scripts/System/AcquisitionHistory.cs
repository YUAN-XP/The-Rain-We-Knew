using System;
using System.Collections.Generic;
using UnityEngine;
[DisallowMultipleComponent] public sealed class AcquisitionHistory : MonoBehaviour {
    readonly HashSet<string> seen=new HashSet<string>();
    public event Action<string,bool> Obtained;
    public bool Record(string id){if(string.IsNullOrEmpty(id))return false;bool first=seen.Add(id);if(GetComponent<GameSaveService>()?.IsRestoring!=true)Obtained?.Invoke(id,first);return first;}
    public string[] Capture(){var values=new List<string>(seen);values.Sort(StringComparer.Ordinal);return values.ToArray();}
    public void Restore(string[] values){seen.Clear();foreach(var v in values)if(!string.IsNullOrEmpty(v))seen.Add(v);}
}
