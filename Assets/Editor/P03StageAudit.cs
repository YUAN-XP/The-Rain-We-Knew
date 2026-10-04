using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
public static class P03StageAudit
{
    const string Work="D:/游戏设计/Codex工作区/work/2026-10-04-P03/";
    [Serializable] class Entry { public string path,kind,json,asset,guid,spriteName;public Vector3 position,localPosition,scale;public Vector2 size;public Rect rect;public bool active,enabled,hidden;public int order; }
    [Serializable] class Snapshot {public string time,scene;public bool playing,dirty;public List<Entry> entries=new List<Entry>();}
    static string PathOf(Transform t){return t.parent?PathOf(t.parent)+"/"+t.name:t.name;}
    public static void Rain(){Dump("Scene01_Rain");}
    public static void Courtyard(){Dump("Scene02_Courtyard");}
    static void Dump(string name)
    {
        if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Read-only audit requires clean edit mode");
        EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");var scene=SceneManager.GetActiveScene();
        var result=new Snapshot{time=DateTime.Now.ToString("O"),scene=name,playing=EditorApplication.isPlaying,dirty=scene.isDirty};
        foreach(var go in scene.GetRootGameObjects())foreach(var t in go.GetComponentsInChildren<Transform>(true))
        {
            foreach(var c in t.GetComponents<Component>())
            {
                if(c==null)continue;bool record=c is Renderer||c is Collider2D||c is MonoBehaviour||c is Camera;
                if(!record)continue;var e=new Entry{path=PathOf(t),kind=c.GetType().Name,position=t.position,localPosition=t.localPosition,scale=t.lossyScale,active=t.gameObject.activeInHierarchy,json=EditorJsonUtility.ToJson(c)};
                if(c is Behaviour be)e.enabled=be.enabled;
                if(c is SpriteRenderer sr){e.enabled=sr.enabled;e.hidden=sr.forceRenderingOff;e.order=sr.sortingOrder;e.size=sr.bounds.size;if(sr.sprite){e.spriteName=sr.sprite.name;e.asset=AssetDatabase.GetAssetPath(sr.sprite);e.guid=AssetDatabase.AssetPathToGUID(e.asset);e.rect=sr.sprite.rect;}}
                if(c is Collider2D co){e.enabled=co.enabled;e.size=co.bounds.size;}
                result.entries.Add(e);
            }
        }
        Directory.CreateDirectory(Work);File.WriteAllText(Work+name+"_baseline.json",JsonUtility.ToJson(result,true));
        File.WriteAllText(Work+name+"_hierarchy.txt",string.Join("\n",result.entries.Select(e=>$"{e.path} | {e.kind} | {e.position} | active={e.active} enabled={e.enabled} | {e.spriteName} | {e.size} | {e.asset}")));
    }
}
