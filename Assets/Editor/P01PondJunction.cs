using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class P01PondJunction {
    // Keep the entire authored pond at one scale. Align both panel baselines and
    // cap heights together; do not widen a single edge or move the covering piers.
    public static void Apply(){
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode first");
        var scene=EditorSceneManager.OpenScene("Assets/Scenes/Scene03_Childhood.unity");
        var root=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Single(t=>t.name=="NewArt_Street"&&t.parent.name=="ChildhoodStreet").gameObject;if(!root)throw new Exception("Missing street art root");
        foreach(var name in new[]{"CloseScale_PondJunction","Seam_PondApproach"}){
            var t=root.transform.Find(name);if(!t)throw new Exception("Missing "+name);
            var p=t.localPosition;p.y=.68f;t.localPosition=p;
        }
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
}
