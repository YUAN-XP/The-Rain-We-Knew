using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ProjectArtIntegrationAudit
{
    const string Output = "C:/Users/31975/Documents/Codex/2026-09-22/demo-demo-gdd-gdd-demo/work/2026-09-23-integration/audit";
    [Serializable] public class Binding { public string property, value; }
    [Serializable] public class ComponentRow { public string type; public List<Binding> fields = new List<Binding>(); }
    [Serializable] public class Row { public string path, sprite, material; public Vector3 position,scale,boundsCenter,boundsSize; public int order; public bool active,enabled; public List<ComponentRow> components=new List<ComponentRow>(); }
    [Serializable] public class Report { public string scene; public int missingScripts; public List<Row> objects=new List<Row>(); }
    public static void Run()
    {
        Directory.CreateDirectory(Output);
        foreach(var scene in Directory.GetFiles("Assets/Scenes","*.unity"))
        {
            EditorSceneManager.OpenScene(scene,OpenSceneMode.Single);
            var report=new Report{scene=scene};
            foreach(var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {
                report.missingScripts+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
                var behaviours=t.GetComponents<MonoBehaviour>();var sr=t.GetComponent<SpriteRenderer>();
                if(behaviours.Length==0&&!sr)continue;
                var row=new Row{path=PathOf(t),position=t.position,scale=t.lossyScale,active=t.gameObject.activeInHierarchy};
                if(sr){row.sprite=AssetDatabase.GetAssetPath(sr.sprite);row.material=AssetDatabase.GetAssetPath(sr.sharedMaterial);row.boundsCenter=sr.bounds.center;row.boundsSize=sr.bounds.size;row.order=sr.sortingOrder;row.enabled=sr.enabled;}
                foreach(var b in behaviours)
                {
                    if(!b)continue;
                    var cr=new ComponentRow{type=b.GetType().Name};var so=new SerializedObject(b);var prop=so.GetIterator();
                    while(prop.NextVisible(true))
                    {
                        if(prop.propertyPath=="m_Script")continue;
                        string value=null;
                        if(prop.propertyType==SerializedPropertyType.ObjectReference){var obj=prop.objectReferenceValue;value=obj?AssetDatabase.GetAssetPath(obj):"null";if(obj&&string.IsNullOrEmpty(value))value=obj is Component c?PathOf(c.transform):obj.name;}
                        else if(prop.propertyType==SerializedPropertyType.String)value=prop.stringValue;
                        else if(prop.propertyType==SerializedPropertyType.Float)value=prop.floatValue.ToString(System.Globalization.CultureInfo.InvariantCulture);
                        else if(prop.propertyType==SerializedPropertyType.Integer)value=prop.intValue.ToString();
                        else if(prop.propertyType==SerializedPropertyType.Boolean)value=prop.boolValue.ToString();
                        if(value!=null)cr.fields.Add(new Binding{property=prop.propertyPath,value=value});
                    }
                    row.components.Add(cr);
                }
                report.objects.Add(row);
            }
            File.WriteAllText(Output+"/"+Path.GetFileNameWithoutExtension(scene)+".json",JsonUtility.ToJson(report,true));
            Debug.Log("ART_INTEGRATION_AUDIT "+scene+" rows="+report.objects.Count+" missingScripts="+report.missingScripts);
        }
        EditorApplication.Exit(0);
    }
    static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
}
