using System;
using UnityEngine;

// Authored asset GUIDs are stable keys, never runtime instance IDs. Variants with
// the same semantic fileId (e.g. undeveloped/developed photo) retain distinct keys.
public sealed class SaveAssetRegistry : ScriptableObject {
    [Serializable] public sealed class Entry {public string key;public ScriptableObject asset;}
    public Entry[] entries=new Entry[0];
    public string Key(ScriptableObject asset){foreach(var e in entries)if(e.asset==asset)return e.key;throw new InvalidOperationException("Unregistered save asset: "+asset?.name);}
    public T Resolve<T>(string key) where T:ScriptableObject {foreach(var e in entries)if(e.key==key)return e.asset as T;return null;}
    public static SaveAssetRegistry Load(){var value=Resources.Load<SaveAssetRegistry>("SaveAssetRegistry");if(!value)throw new InvalidOperationException("Missing SaveAssetRegistry");return value;}
}
