using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
public static class P02PV17Build {
    public static void Run(){
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop before build");
        string folder="D:/游戏设计/Codex工作区/work/2026-10-03-P02/build_PV20";Directory.CreateDirectory(folder);
        var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
        File.WriteAllLines(Path.Combine(folder,"build-scenes.txt"),scenes);
        var r=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=Path.Combine(folder,"ProjectMystery.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
        File.WriteAllText(Path.Combine(folder,"build-result.txt"),DateTime.Now.ToString("O")+" "+r.summary.result+" errors="+r.summary.totalErrors+" warnings="+r.summary.totalWarnings);
        File.WriteAllLines(Path.Combine(folder,"build-messages.txt"),r.steps.SelectMany(s=>s.messages.Select(m=>s.name+" | "+m.type+" | "+m.content)));
        if(r.summary.result!=BuildResult.Succeeded)throw new Exception("PV20 validation build failed");
    }
}
