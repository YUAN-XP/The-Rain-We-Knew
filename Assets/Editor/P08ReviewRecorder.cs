using System;
using System.IO;
using System.Collections;
using UnityEditor.Media;
using UnityEditor;
using UnityEngine;
// Bounded editor-only normal-speed review recording; no archive of raw frames.
public sealed class P08ReviewRecorder {
 static P08ReviewRecorder current;MediaEncoder encoder;RenderTexture source,scaled;Texture2D pixels;bool active;int frames;double validTime,next;float duration;string path;
 public static void Begin(MonoBehaviour host,string path,float seconds){if(current!=null)Stop();var r=new P08ReviewRecorder();current=r;r.path=path;r.duration=Mathf.Clamp(seconds,1,2400);Directory.CreateDirectory(Path.GetDirectoryName(path));r.source=new RenderTexture(Screen.width,Screen.height,0,RenderTextureFormat.ARGB32);r.source.Create();r.scaled=new RenderTexture(1080,608,0,RenderTextureFormat.ARGB32);r.scaled.Create();r.pixels=new Texture2D(1080,608,TextureFormat.RGBA32,false);r.encoder=new MediaEncoder(path,new VideoTrackAttributes{width=1080,height=608,frameRate=new MediaRational(12),includeAlpha=false,bitRateMode=VideoBitrateMode.Medium});r.active=true;host.StartCoroutine(r.Capture());}
 public static void Stop(){var r=current;current=null;if(r==null)return;r.active=false;r.encoder?.Dispose();if(RenderTexture.active==r.source||RenderTexture.active==r.scaled)RenderTexture.active=null;if(r.source){r.source.Release();UnityEngine.Object.Destroy(r.source);}if(r.scaled){r.scaled.Release();UnityEngine.Object.Destroy(r.scaled);}if(r.pixels)UnityEngine.Object.Destroy(r.pixels);File.WriteAllText(r.path+".txt","P08 final candidate normal-speed input review. 1080x608, 12 fps; "+r.frames+" frames, focused duration "+r.validTime.ToString("F2")+"s. Original focus pause retained; no audio encoded; no raw-frame archive.");}
 IEnumerator Capture(){while(active){yield return new WaitForEndOfFrame();if(!active)yield break;if(!Application.isFocused)continue;validTime+=Time.unscaledDeltaTime;if(validTime<next)continue;next=validTime+1.0/12;var old=RenderTexture.active;try{ScreenCapture.CaptureScreenshotIntoRenderTexture(source);Graphics.Blit(source,scaled,new Vector2(1,-1),new Vector2(0,1));RenderTexture.active=scaled;pixels.ReadPixels(new Rect(0,0,1080,608),0,0);pixels.Apply(false);}finally{RenderTexture.active=old;}int expected=(int)Math.Floor(validTime*12)+1;while(frames<expected){encoder.AddFrame(pixels,new MediaTime(frames,12,1));frames++;}if(validTime>=duration)Stop();}}
}


