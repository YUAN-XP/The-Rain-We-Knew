using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// Isolated rendering diagnostics. Restores the user's Game View resolution and camera.
public sealed class P01PromptChecks {
    const BindingFlags Hidden=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
    static string Folder=>Path.GetFullPath(Path.Combine(Application.dataPath,"../Temp/P01PromptChecks"));
    readonly List<string> rows=new List<string>();
    void Check(bool ok,string text){rows.Add((ok?"PASS ":"FAIL ")+text);File.WriteAllLines(Path.Combine(Folder,"results.txt"),rows);}
    public static void Run(){if(!EditorApplication.isPlaying)throw new Exception("Play Mode required");PersistentRuntime.Instance.StartCoroutine(new P01PromptChecks().Test());}
    IEnumerator Test(){
        Directory.CreateDirectory(Folder);
        var assembly=typeof(Editor).Assembly;var viewType=assembly.GetType("UnityEditor.GameView");var view=EditorWindow.GetWindow(viewType);
        var indexProperty=viewType.GetProperty("selectedSizeIndex",Hidden);int originalIndex=(int)indexProperty.GetValue(view);
        var sizesType=assembly.GetType("UnityEditor.GameViewSizes");var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
        var sizes=singleton.GetProperty("instance",BindingFlags.Static|BindingFlags.Public).GetValue(null);
        var groupType=assembly.GetType("UnityEditor.GameViewSizeGroupType");
        var group=sizesType.GetMethod("GetGroup",Hidden).Invoke(sizes,new[]{Enum.Parse(groupType,"Standalone")});var gt=group.GetType();
        var sizeType=assembly.GetType("UnityEditor.GameViewSize");var mode=assembly.GetType("UnityEditor.GameViewSizeType");
        var ctor=sizeType.GetConstructor(Hidden,null,new[]{mode,typeof(int),typeof(int),typeof(string)},null);
        int builtin=(int)gt.GetMethod("GetBuiltinCount",Hidden).Invoke(group,null);int added=-1;float referenceHeight=-1;
        var actor=PersistentRuntime.Instance.GetComponent<GameControlManager>().ActivePlayer;var rb=actor.GetComponent<Rigidbody2D>();bool simulated=rb.simulated;
        var ui=UnityEngine.Object.FindAnyObjectByType<InteractionUI>();var root=(GameObject)typeof(InteractionUI).GetField("promptRoot",Hidden).GetValue(ui);var rect=root.GetComponent<RectTransform>();var canvas=root.GetComponentInParent<Canvas>();
        RoomPresentationFraming.PrepareCurrentView();var cam=Camera.main;var follow=cam.GetComponent<CameraFollow>();var framing=cam.GetComponent<RoomPresentationFraming>();bool followEnabled=follow&&follow.enabled,frameEnabled=framing&&framing.enabled;var originalPosition=cam.transform.position;
        var interaction=actor.GetComponent<PlayerInteraction>();bool interactionEnabled=interaction.enabled;
        try{
            rb.simulated=false;interaction.enabled=false;if(follow)follow.enabled=false;
            foreach(var resolution in new[]{new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(2560,1440),new Vector2Int(3840,2160),new Vector2Int(1920,1200),new Vector2Int(2560,1080)}){
                added=(int)gt.GetMethod("GetCustomCount",Hidden).Invoke(group,null);
                var size=ctor.Invoke(new object[]{Enum.Parse(mode,"FixedResolution"),resolution.x,resolution.y,"P01 temporary check"});gt.GetMethod("AddCustomSize",Hidden).Invoke(group,new[]{size});indexProperty.SetValue(view,builtin+added);
                for(int i=0;i<8;i++)yield return null;
                Check(Screen.width==resolution.x&&Screen.height==resolution.y,"actual render size "+resolution+" got "+Screen.width+"x"+Screen.height);
                foreach(string side in new[]{"center","left","right","top","bottom"}){
                    cam.transform.position=originalPosition;
                    var p=cam.transform.position;
                    if(side=="left")p.x=actor.transform.position.x+cam.orthographicSize*cam.aspect*.99f;
                    if(side=="right")p.x=actor.transform.position.x-cam.orthographicSize*cam.aspect*.99f;
                    if(side=="top")p.y=actor.transform.position.y-cam.orthographicSize;
                    if(side=="bottom")p.y=actor.transform.position.y+cam.orthographicSize*1.8f;
                    cam.transform.position=p;ui.ShowPrompt("[E] 调查",actor.transform);
                    for(int i=0;i<3;i++)yield return null;
                    Canvas.ForceUpdateCanvases();typeof(InteractionUI).GetMethod("LateUpdate",Hidden).Invoke(ui,null);Canvas.ForceUpdateCanvases();
                    var corners=new Vector3[4];rect.GetWorldCorners(corners);var camera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
                    var pixels=corners.Select(x=>RectTransformUtility.WorldToScreenPoint(camera,x)).ToArray();var safe=cam.pixelRect;
                    if(framing&&framing.FramesPosition(actor.transform.position)){float bar=safe.height*framing.letterbox;safe.yMin+=bar;safe.yMax-=bar;}
                    Check(ui.IsVisible&&pixels.All(x=>x.x>=safe.xMin-1&&x.x<=safe.xMax+1&&x.y>=safe.yMin-1&&x.y<=safe.yMax+1),resolution+" "+side+" prompt fully inside rendered viewport");
                    if(side=="center"){float normalizedHeight=(pixels.Max(v=>v.y)-pixels.Min(v=>v.y))/Mathf.Min(resolution.x/1920f,resolution.y/1080f);if(referenceHeight<0)referenceHeight=normalizedHeight;Check(Mathf.Abs(normalizedHeight-referenceHeight)<1.5f,resolution+" prompt keeps readable proportional size");ScreenCapture.CaptureScreenshot(Path.Combine(Folder,resolution.x+"x"+resolution.y+".png"));yield return new WaitForEndOfFrame();yield return null;}
                }
                indexProperty.SetValue(view,originalIndex);gt.GetMethod("RemoveCustomSize",Hidden).Invoke(group,new object[]{added});added=-1;
            }
        } finally {
            indexProperty.SetValue(view,originalIndex);if(added>=0)gt.GetMethod("RemoveCustomSize",Hidden).Invoke(group,new object[]{added});
            ui.HidePrompt();cam.transform.position=originalPosition;if(follow)follow.enabled=followEnabled;if(framing)framing.enabled=frameEnabled;rb.simulated=simulated;interaction.enabled=interactionEnabled;
        }
        Check(rows.All(x=>!x.StartsWith("FAIL")),"SUMMARY actual resolution rendering; camera offsets are diagnostic, not player input");
    }
}
