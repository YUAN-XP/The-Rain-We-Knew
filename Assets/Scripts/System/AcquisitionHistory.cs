using System;
using System.Collections.Generic;
using UnityEngine;
[DisallowMultipleComponent] public sealed class AcquisitionHistory:MonoBehaviour {
 readonly HashSet<string> seen=new HashSet<string>();readonly List<(string,bool)> pending=new List<(string,bool)>();string[] before;
 public event Action<string,bool> Obtained;
 public bool Record(string id){if(string.IsNullOrEmpty(id))return false;bool first=seen.Add(id);if(before!=null)pending.Add((id,first));else if(GetComponent<GameSaveService>()?.IsRestoring!=true)Obtained?.Invoke(id,first);return first;}
 public void BeginTransaction(){if(before!=null)throw new InvalidOperationException("Acquisition transaction already active");before=Capture();pending.Clear();}
 public void CommitTransaction(){before=null;var events=pending.ToArray();pending.Clear();if(GetComponent<GameSaveService>()?.IsRestoring==true)return;foreach(var e in events)Obtained?.Invoke(e.Item1,e.Item2);}
 public void RollbackTransaction(){var saved=before;before=null;pending.Clear();if(saved!=null)Restore(saved);}
 public string[] Capture(){var values=new List<string>(seen);values.Sort(StringComparer.Ordinal);return values.ToArray();}
 public void Restore(string[] values){seen.Clear();foreach(var v in values)if(!string.IsNullOrEmpty(v))seen.Add(v);}
}
