using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Approved production sources and explicit sprite regions; never flips asymmetric faces.</summary>
[CreateAssetMenu(menuName="Presentation/Demo Art Catalog")]
public sealed class DemoArtCatalog : ScriptableObject
{
    [Serializable] public class Entry { public string key; public Sprite sprite; }
    public Entry[] entries=Array.Empty<Entry>();
    Dictionary<string,Sprite> lookup;
    static DemoArtCatalog loaded;
    public static DemoArtCatalog Current => loaded ? loaded : (loaded=Resources.Load<DemoArtCatalog>("DemoArtCatalog"));
    public Sprite Get(string key)
    {
        if(lookup==null){lookup=new Dictionary<string,Sprite>();foreach(var entry in entries)if(entry!=null&&!string.IsNullOrEmpty(entry.key))lookup[entry.key]=entry.sprite;}
        return key!=null&&lookup.TryGetValue(key,out var sprite)?sprite:null;
    }
    public static Sprite Find(string key)=>Current?Current.Get(key):null;
    void OnEnable(){lookup=null;}
}
