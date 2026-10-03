using System;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Local integration/verification commands; no scene mutation on startup.</summary>
[InitializeOnLoad]
public static class DemoIntegrationBridge
{
    public const string Work="D:/游戏设计/Codex工作区/work/2026-09-23-integration/";
    static double next;
    static DemoIntegrationBridge(){if(!Application.dataPath.Replace('\\','/').Equals("D:/GitHub Desktop/ProjectMystery/ProjectMystery/Assets",StringComparison.OrdinalIgnoreCase))return;EditorApplication.update+=Tick;File.WriteAllText(Work+"bridge_ready.txt",DateTime.Now.ToString("O"));}
    static void Tick()
    {
        if(EditorApplication.isCompiling||EditorApplication.timeSinceStartup<next)return;next=EditorApplication.timeSinceStartup+.4;
        var file=Work+"editor_command.txt";if(!File.Exists(file))return;
        string command=File.ReadAllText(file).Trim();File.Delete(file);
        try{
            if(command=="status")Status();
            else if(command=="refresh")AssetDatabase.Refresh();
            else if(command=="import"){RequireEdit();DemoArtImporter.Run();}
            else if(command=="play"){RequireEdit();EditorApplication.isPlaying=true;}
            else if(command=="stop")EditorApplication.isPlaying=false;
            else if(command.StartsWith("open:")){RequireEdit();EditorSceneManager.OpenScene("Assets/Scenes/"+command.Substring(5)+".unity");}
            else if(command.StartsWith("invoke:")){var parts=command.Substring(7).Split('|');var type=Type.GetType(parts[0]+",Assembly-CSharp-Editor");if(type==null)throw new Exception("Missing editor type "+parts[0]);type.GetMethod(parts[1],BindingFlags.Public|BindingFlags.Static).Invoke(null,null);}
            else if(command.StartsWith("capture:")){Directory.CreateDirectory(Work+"screenshots");ScreenCapture.CaptureScreenshot(Work+"screenshots/"+command.Substring(8)+".png");}
            else throw new Exception("Unknown command");
            File.AppendAllText(Work+"editor_events.txt",DateTime.Now.ToString("O")+" OK "+command+"\n");
        }catch(Exception e){File.AppendAllText(Work+"editor_errors.txt",DateTime.Now.ToString("O")+" "+command+" "+e+"\n");}
    }
    static void RequireEdit(){if(EditorApplication.isPlaying||SceneManager.GetActiveScene().isDirty)throw new Exception("Current scene is dirty or playing; operation refused.");}
    public static void Status()
    {
        var logs=typeof(Editor).Assembly.GetType("UnityEditor.LogEntries");object[] counts={0,0,0};logs.GetMethod("GetCountsByType",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic).Invoke(null,counts);
        File.WriteAllText(Work+"editor_status.txt",$"{DateTime.Now:O} scene={SceneManager.GetActiveScene().name} dirty={SceneManager.GetActiveScene().isDirty} playing={EditorApplication.isPlaying} counts={string.Join(",",counts)} art={DemoArtCatalog.Current?.entries.Length} newCloseupType={typeof(DemoCloseupPresentation).FullName}");
    }
}
