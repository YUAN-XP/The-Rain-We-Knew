using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
public static class P05ValidationBuild
{
 public static void Run()
 {
  if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before validation build");
  string folder=P05Audit.Work+"build";Directory.CreateDirectory(folder);
  var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();File.WriteAllLines(folder+"/build-scenes.txt",scenes);
  var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=scenes,locationPathName=folder+"/ProjectMystery.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
  File.WriteAllText(folder+"/build-result.txt",DateTime.Now.ToString("O")+" "+report.summary.result+" errors="+report.summary.totalErrors+" warnings="+report.summary.totalWarnings+" bytes="+report.summary.totalSize+" seconds="+report.summary.totalTime.TotalSeconds);
  File.WriteAllLines(folder+"/build-messages.txt",report.steps.SelectMany(s=>s.messages.Select(m=>s.name+" | "+m.type+" | "+m.content)));
  if(report.summary.result!=BuildResult.Succeeded)throw new Exception("P05 validation build failed");
 }
}