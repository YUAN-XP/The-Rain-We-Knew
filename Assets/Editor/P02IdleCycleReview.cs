using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Media;
using UnityEngine;
// Independent idle evidence only; never saves a scene or resumes GamePause.
public static class P02IdleCycleReview {
 const string Dir="D:/GitHub Desktop/ProjectMystery/ProjectMystery/Documentation/FirstLargeTest_20261001/P02Evidence/idle_cycles_PV20r2/";
 static GameObject root; static Camera cam; static RenderTexture target; static Texture2D pixels; static MediaEncoder encoder;
 static P02CharacterRig[] rigs; static float[] initial; static double start,last; static int frames;
 static FieldInfo breath=typeof(P02CharacterRig).GetField("breathTime",BindingFlags.Instance|BindingFlags.NonPublic);
 public static void Begin(){
  if(!EditorApplication.isPlaying||root)throw new InvalidOperationException("Isolated Play review required");
  Directory.CreateDirectory(Dir);
  var lib=Resources.Load<P02MotionLibrary>("P02MotionLibrary");
  root=new GameObject("P02 independent idle grid");root.transform.position=new Vector3(10000,0,0);
  rigs=new P02CharacterRig[lib.entries.Length];initial=new float[rigs.Length];
  for(int i=0;i<rigs.Length;i++){
   var e=lib.entries[i];var g=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Art/P02Motion/Production/P02_"+e.actor+"_"+e.direction+".prefab"),root.transform);
   g.transform.localPosition=new Vector3((i%5-2)*2.9f,3.4f-(i/5)*2.3f,0);
   var r=g.GetComponent<P02CharacterRig>();r.sampleMode=true;r.sampleSpeed=0;r.sampleRunning=false;r.ResetTravel();rigs[i]=r;initial[i]=(float)breath.GetValue(r);
   foreach(var t in g.GetComponentsInChildren<Transform>(true))t.gameObject.layer=29;
   var text=new GameObject(e.actor+"_"+e.direction).AddComponent<TextMesh>();text.transform.SetParent(root.transform,false);text.transform.localPosition=g.transform.localPosition+new Vector3(0,2.03f,0);text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=30;text.characterSize=.026f;text.anchor=TextAnchor.MiddleCenter;text.text=e.actor+"_"+e.direction;text.gameObject.layer=29;text.GetComponent<MeshRenderer>().material=text.font.material;
   var line=new GameObject("Ground reference").AddComponent<LineRenderer>();line.transform.SetParent(root.transform,false);line.gameObject.layer=29;line.useWorldSpace=true;line.positionCount=2;line.SetPositions(new[]{g.transform.position+new Vector3(-1.3f,0,0),g.transform.position+new Vector3(1.3f,0,0)});line.startWidth=line.endWidth=.009f;line.material=new Material(Shader.Find("Sprites/Default"));line.startColor=line.endColor=new Color(.6f,.62f,.63f,.8f);
  }
  cam=new GameObject("P02 idle grid camera").AddComponent<Camera>();cam.transform.SetParent(root.transform,false);cam.transform.localPosition=new Vector3(0,1,-10);cam.orthographic=true;cam.orthographicSize=5.5f;cam.cullingMask=1<<29;cam.backgroundColor=new Color(.26f,.28f,.29f);cam.clearFlags=CameraClearFlags.SolidColor;cam.enabled=false;
  target=new RenderTexture(1920,1080,24);pixels=new Texture2D(1920,1080,TextureFormat.RGBA32,false);cam.targetTexture=target;
  encoder=new MediaEncoder(Dir+"idle_twenty_views.mp4",new VideoTrackAttributes{width=1920,height=1080,frameRate=new MediaRational(30),includeAlpha=false,bitRateMode=VideoBitrateMode.High});
  start=EditorApplication.timeSinceStartup;last=-1;frames=0;EditorApplication.update+=Tick;
 }
 static void Tick(){
  if(!EditorApplication.isPlaying||!root){End();return;}
  double now=EditorApplication.timeSinceStartup-start;if(now-last<1.0/25)return;last=now;
  cam.Render();var old=RenderTexture.active;RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply(false,false);RenderTexture.active=old;
  if(!encoder.AddFrame(pixels,new MediaTime((long)(now*1000000),1000000,1))){End();throw new InvalidOperationException("Idle encoder rejected a frame");}frames++;
  if(frames==1||now>=16)File.WriteAllBytes(Dir+(frames==1?"idle_first.png":"idle_last.png"),pixels.EncodeToPNG());
  if(now>=16){
   var rows=rigs.Select((r,i)=>new Row{skin=r.definition.name,cycles=((float)breath.GetValue(r)-initial[i])*1.6f/(Mathf.PI*2),parts=r.MeshPartCount}).ToArray();
   var report=new Result{scope="Independent native idle skins, original game remains paused. Three breathing cycles; no production input or scene change claim.",seconds=(float)now,frames=frames,rows=rows,passed=rows.All(x=>x.cycles>=3&&x.parts>=10),originalGamePaused=GamePause.IsPaused};
   File.WriteAllText(Dir+"idle_cycle_report.json",JsonUtility.ToJson(report,true));End();
  }
 }
 public static void End(){EditorApplication.update-=Tick;encoder?.Dispose();encoder=null;if(target){target.Release();UnityEngine.Object.DestroyImmediate(target);}if(pixels)UnityEngine.Object.DestroyImmediate(pixels);if(root)UnityEngine.Object.Destroy(root);root=null;}
 [Serializable]class Row{public string skin;public float cycles;public int parts;}
 [Serializable]class Result{public string scope;public float seconds;public int frames;public bool passed,originalGamePaused;public Row[] rows;}
}

