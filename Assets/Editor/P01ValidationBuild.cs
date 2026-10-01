using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
public static class P01ValidationBuild {
    public static void Run(){
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before building");
        string folder=Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath,"../Temp/P01Standalone"));Directory.CreateDirectory(folder);
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray(),locationPathName=Path.Combine(folder,"ProjectMystery.exe"),target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
        File.WriteAllText(Path.Combine(folder,"build-result.txt"),report.summary.result+" errors="+report.summary.totalErrors+" warnings="+report.summary.totalWarnings+" bytes="+report.summary.totalSize);
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("P01 validation build failed");
    }
}
