using System;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class P04Bindings {
 public static void Export(){if(EditorApplication.isPlaying)throw new Exception("Edit mode required");EditorSceneManager.OpenScene("Assets/Scenes/Scene03_Childhood.unity");var rows=new System.Collections.Generic.List<string>();foreach(var t in P04Audit.All()) foreach(var c in t.GetComponents<MonoBehaviour>())if(c&&(c is Scene03Chapter||c is Scene03MazeController||c is Scene03RiverSequence||c is Scene03FamilySequence||c is RoomPresentationFraming||c is Scene03Interactable||c is GuitarRhythmGame)){var so=new SerializedObject(c);var p=so.GetIterator();rows.Add(Path(t)+" ["+c.GetType().Name+"]");while(p.NextVisible(true))if(p.propertyType==SerializedPropertyType.ObjectReference&&p.objectReferenceValue){var obj=p.objectReferenceValue;var tr=obj as Transform;if(!tr&&obj is Component co)tr=co.transform;if(!tr&&obj is GameObject go)tr=go.transform;rows.Add(p.propertyPath+" = "+(tr?Path(tr):AssetDatabase.GetAssetPath(obj)));}}File.WriteAllLines(P04Audit.Work+"baseline/bindings.txt",rows);EditorSceneManager.OpenScene("Assets/Scenes/Scene01_Rain.unity");}
 static string Path(Transform t)=>t.parent?Path(t.parent)+"/"+t.name:t.name;
}
