using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
public static class P01InteractionBindings {
    public static void Audit(){
        if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");var original=SceneManager.GetActiveScene().path;var lines=new List<string>();
        try{foreach(string name in new[]{"Scene03_Childhood","Scene04_School"}){
            var scene=EditorSceneManager.OpenScene("Assets/Scenes/"+name+".unity");
            foreach(var region in scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<SpriteArtRegion>(true)))region.Rebuild();
            foreach(var source in scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<MonoBehaviour>(true)).Where(x=>x is IInteractable)){
                lines.Add("TARGET "+PathOf(source.transform)+" "+source.GetType().Name+" at="+source.transform.position);
                foreach(var v in scene.GetRootGameObjects().SelectMany(x=>x.GetComponentsInChildren<SpriteRenderer>(true)).Where(v=>v.enabled&&v.sprite&&Mathf.Abs(v.bounds.center.x-source.transform.position.x)<2&&Mathf.Abs(v.bounds.center.y-source.transform.position.y)<3))lines.Add(" NEAR "+PathOf(v.transform)+" active="+v.gameObject.activeInHierarchy+" bounds="+v.bounds+" asset="+AssetDatabase.GetAssetPath(v.sprite));
                foreach(var v in source.GetComponentsInChildren<SpriteRenderer>(true))lines.Add(" SPRITE "+PathOf(v.transform)+" enabled="+v.enabled+" forceOff="+v.forceRenderingOff+" active="+v.gameObject.activeInHierarchy+" alpha="+v.color.a+" bounds="+v.bounds+" asset="+AssetDatabase.GetAssetPath(v.sprite));
                var h=source.GetComponent<HandDrawnInteractionHighlight>();if(h){var so=new SerializedObject(h);var a=so.FindProperty("visuals");for(int i=0;i<a.arraySize;i++)lines.Add(" BIND "+a.GetArrayElementAtIndex(i).objectReferenceValue);}
            }
        }}finally{if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);}
        Directory.CreateDirectory("Temp/P01InteractionBindings");File.WriteAllLines("Temp/P01InteractionBindings/audit.txt",lines);
    }

    public static void Apply(){
        if(EditorApplication.isPlaying)throw new Exception("Edit Mode required");
        var original=SceneManager.GetActiveScene().path;var lines=new List<string>();
        try{
            EditorSceneManager.OpenScene("Assets/Scenes/Scene03_Childhood.unity");
            Bind("Environment/ChildhoodStreet/S03_LiMei","Environment/ChildhoodStreet/李梅/NewArt_Actor",lines);
            Bind("Environment/ChildhoodStreet/S03_Fish","Environment/ChildhoodStreet/赵炳程/NewArt_Actor",lines);
            Bind("Environment/ChildhoodStreet/S03_Rings","Environment/ChildhoodStreet/套圈老板/NewArt_Actor",lines);
            Bind("Environment/ChildhoodStreet/S03_Storyteller","Environment/ChildhoodStreet/说书人/NewArt_Actor",lines);
            Bind("Environment/ChildhoodStreet/S03_ZhaoHome","Environment/ChildhoodStreet/NewArt_Street/ZhaoHome",lines);
            Bind("Environment/Home_Floor01_Normal/S03_ExitHome","Environment/Home_Floor01_Normal/家门/NewArt_Visual",lines);
            Bind("Environment/LivingRoomInterior/S03_LivingRoomExit","Environment/LivingRoomInterior/LivingRoomExitVisual/NewArt_Visual",lines);
            Bind("Environment/LivingRoomInterior/FamilyCameraPlacement","Environment/LivingRoomInterior/LivingRoomFurniture/TV_Cabinet/NewArt_Visual",lines);
            Point("Environment/ChildhoodStreet/Stage4B_Entry",lines);
            Point("Environment/ChildhoodStreet/S03_Puddle",lines);
            var source=Find("Environment/ChildhoodStreet/S03_Puddle").GetComponent<DialogueInteractable>();var data=(DialogueData)new SerializedObject(source).FindProperty("dialogueData").objectReferenceValue;
            if(data)lines.Add("PUDDLE_TEXT "+string.Join(" / ",data.lines.Select(x=>x.text)));
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            EditorSceneManager.OpenScene("Assets/Scenes/Scene04_School.unity");
            Bind("School/Otherworld/Floor01/Basketball_Interaction","School/Otherworld/Floor01/Basketball/NewArt_Visual",lines);
            var chairPoint=Find("School/Memory/ReadingPassage/B5_ReadingChair");var chairAt=chairPoint.position;chairAt.x=Find("School/Memory/ReadingPassage/ReadingDesk").position.x;chairPoint.position=chairAt;lines.Add("ALIGNED reading chair hotspot and child seat/stand anchors to "+chairAt);
            Bind("School/Memory/ReadingPassage/B5_ReadingChair","School/Memory/ReadingPassage/ReadingDesk/NewArt_Desk",lines);
            foreach(string path in new[]{"School/Memory/ReadingPassage/B5_ReadingChair","School/Otherworld/Floor01/FogReturnBoundary","School/Otherworld/Floor01/EquipmentEcho","School/Memory/Floor01/B5_SchoolGateNotice","School/Memory/Floor02/B5_FemaleToiletNotice","School/Memory/MaleToilet/B5_ToiletListening"})Point(path,lines);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        }finally{if(!string.IsNullOrEmpty(original))EditorSceneManager.OpenScene(original);}
        Directory.CreateDirectory("Temp/P01InteractionBindings");File.WriteAllLines("Temp/P01InteractionBindings/applied.txt",lines);
    }
    static Transform Find(string path){var segments=path.Split('/');var root=SceneManager.GetActiveScene().GetRootGameObjects().Single(x=>x.name==segments[0]).transform;for(int i=1;i<segments.Length;i++){root=root.Find(segments[i]);if(!root)throw new Exception("Missing "+path);}return root;}
    static void Bind(string path,string visualPath,List<string> lines){
        var target=Find(path);var source=target.GetComponents<MonoBehaviour>().First(x=>x is IInteractable);var visual=Find(visualPath).GetComponent<SpriteRenderer>();if(!visual)throw new Exception("Missing renderer "+visualPath);
        var h=target.GetComponent<HandDrawnInteractionHighlight>();if(!h)h=target.gameObject.AddComponent<HandDrawnInteractionHighlight>();var so=new SerializedObject(h);
        so.FindProperty("interactionSource").objectReferenceValue=source;var list=so.FindProperty("visuals");list.arraySize=1;list.GetArrayElementAtIndex(0).objectReferenceValue=visual;so.FindProperty("outerContour").arraySize=0;
        so.FindProperty("outlineMaterial").objectReferenceValue=AssetDatabase.FindAssets("HandDrawnInteraction t:Material").Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadAssetAtPath<Material>).First();
        so.ApplyModifiedPropertiesWithoutUndo();lines.Add("BOUND "+path+" => "+visualPath);
    }
    static void Point(string path,List<string> lines){var target=Find(path);var reach=target.GetComponent<InteractionReach>();if(!reach)reach=target.gameObject.AddComponent<InteractionReach>();reach.anchor=target;reach.horizontalReach=.55f;reach.verticalReach=1.8f;lines.Add("POINT_ANCHOR "+path);}
    static string PathOf(Transform t){string s=t.name;while(t.parent){t=t.parent;s=t.name+"/"+s;}return s;}
}
