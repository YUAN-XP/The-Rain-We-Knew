using System;
using System.IO;
using System.Collections;
using UnityEditor.Media;
using UnityEditor;
using UnityEngine;
/// <summary>Editor-only bounded Game-view recording hosted by the isolated test coroutine.</summary>
public sealed class P07EvidenceRecorder
{
 static P07EvidenceRecorder current;
 static int stillCaptureFrame=-1;
 public static void ReserveStillFrame(){stillCaptureFrame=Time.frameCount+1;}
 MediaEncoder encoder;RenderTexture screen,scaled;Texture2D pixels;double begin,validTime;float duration;bool active;int frames;string path;
 public static void Begin(MonoBehaviour host,string label,float seconds)
 {
  if(current!=null)throw new Exception("Recording still active");
  var recorder=new P07EvidenceRecorder();current=recorder;
  try {recorder.StartCapture(host,label,seconds);} catch {Stop();throw;}
 }
 public static void Stop(){var recorder=current;current=null;recorder?.Finish();}
 void StartCapture(MonoBehaviour host,string label,float seconds)
 {
  Directory.CreateDirectory(Path.GetDirectoryName(label));path=label;duration=seconds;begin=Time.realtimeSinceStartupAsDouble;
  screen=new RenderTexture(Screen.width,Screen.height,0,RenderTextureFormat.ARGB32);screen.Create();
  scaled=new RenderTexture(1440,810,0,RenderTextureFormat.ARGB32);scaled.Create();pixels=new Texture2D(1440,810,TextureFormat.RGBA32,false);
  encoder=new MediaEncoder(path,new VideoTrackAttributes{width=1440,height=810,frameRate=new MediaRational(30),includeAlpha=false,bitRateMode=VideoBitrateMode.Medium});active=true;host.StartCoroutine(Capture());
 }
 IEnumerator Capture()
 {
  double next=0;while(active)
  {
   yield return new WaitForEndOfFrame();if(!active)yield break;
   if(Application.isFocused)validTime+=Time.unscaledDeltaTime;double t=Math.Min(validTime,duration);if(!Application.isFocused||t<next||Time.frameCount<=stillCaptureFrame)continue;
   next=t+1.0/30;var old=RenderTexture.active;
   try {ScreenCapture.CaptureScreenshotIntoRenderTexture(screen);Graphics.Blit(screen,scaled,new Vector2(1,-1),new Vector2(0,1));RenderTexture.active=scaled;pixels.ReadPixels(new Rect(0,0,1440,810),0,0);pixels.Apply(false);}
   finally {RenderTexture.active=old;}
   int expected=(int)Math.Floor(t*30)+1;while(frames<expected){encoder.AddFrame(pixels,new MediaTime(frames,30,1));frames++;}if(t>=duration)Stop();
  }
 }
 void Finish()
 {
  bool hadFrames=active;active=false;encoder?.Dispose();encoder=null;
  if(RenderTexture.active==screen||RenderTexture.active==scaled)RenderTexture.active=null;
  if(screen){screen.Release();UnityEngine.Object.Destroy(screen);}if(scaled){scaled.Release();UnityEngine.Object.Destroy(scaled);}if(pixels)UnityEngine.Object.Destroy(pixels);
  if(hadFrames)File.WriteAllText(path+".txt","Final P07 Game-view overlay capture; 1440x810; "+frames+" frames; duration "+(Time.realtimeSinceStartupAsDouble-begin).ToString("F2")+"; no raw frame archive; audio not encoded.");
 }
}
