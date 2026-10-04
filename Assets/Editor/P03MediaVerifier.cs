using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;
public static class P03MediaVerifier
{
 public static IEnumerator Run(Action<bool,string> check)
 {
  string folder=P03Regression.Work+"runtime_final/";var rows=new List<string>();
  foreach(string name in new[]{"Rain_memory","Courtyard_windows_and_tabletop","Continuous_corridor"})
  {
   string path=folder+name+".mp4";if(!File.Exists(path)){check(false,"bounded video exists "+name);continue;}
   var go=new GameObject("P03 isolated decoder");go.hideFlags=HideFlags.DontSave;var player=go.AddComponent<VideoPlayer>();
   var target=new RenderTexture(720,405,0,RenderTextureFormat.ARGB32);target.Create();
   player.playOnAwake=false;player.source=VideoSource.Url;player.url=path;player.renderMode=VideoRenderMode.RenderTexture;player.targetTexture=target;player.audioOutputMode=VideoAudioOutputMode.None;player.waitForFirstFrame=true;player.sendFrameReadyEvents=true;
   string error=null;long frame=-1;player.errorReceived+=(v,e)=>error=e;player.frameReady+=(v,f)=>frame=f;player.Prepare();
   float end=Time.realtimeSinceStartup+12;while(!player.isPrepared&&error==null&&Time.realtimeSinceStartup<end)yield return null;
   bool prepared=player.isPrepared&&error==null;check(prepared,"native decoder prepares final video "+name+" "+error);
   if(prepared)
   {
    rows.Add(name+" | "+player.width+"x"+player.height+" | "+player.frameRate+"fps | "+player.frameCount+"frames | "+player.length.ToString("F2")+"s | bytes="+new FileInfo(path).Length);
    for(int i=0;i<3;i++)
    {
     frame=-1;player.time=player.length*(i+.5)/3;player.Play();end=Time.realtimeSinceStartup+8;while(frame<0&&error==null&&Time.realtimeSinceStartup<end)yield return null;
     yield return new WaitForEndOfFrame();bool decoded=frame>=0&&error==null;check(decoded,"video sample "+name+" "+i+" decodes "+error);
     player.Pause();if(decoded){var old=RenderTexture.active;RenderTexture.active=target;var image=new Texture2D(720,405,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,720,405),0,0);image.Apply();RenderTexture.active=old;File.WriteAllBytes(folder+name+"_decode_"+i+".jpg",image.EncodeToJPG(85));UnityEngine.Object.Destroy(image);}
    }
   }
   player.Stop();UnityEngine.Object.Destroy(go);target.Release();UnityEngine.Object.Destroy(target);
  }
  File.WriteAllLines(folder+"video_verification.txt",rows);
 }
}