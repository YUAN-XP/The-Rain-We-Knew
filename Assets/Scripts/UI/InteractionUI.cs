using TMPro;
using UnityEngine;

// Runs after camera framing and painted actor placement.
[DefaultExecutionOrder(1500)]
public class InteractionUI : MonoBehaviour
{
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TMP_Text promptText;
    [SerializeField, Min(0)] private float headGap = .16f;
    private Transform actor;
    private Canvas canvas;
    public bool IsVisible => promptRoot && promptRoot.activeSelf;
    private void Awake()
    {
        canvas=promptRoot?promptRoot.GetComponentInParent<Canvas>():null;
        if(promptText){promptText.raycastTarget=false;promptText.alignment=TextAlignmentOptions.Center;}
        HidePrompt();
    }
    public void ShowPrompt(string text,Transform target)
    {
        if(!promptRoot||!promptText||!target){HidePrompt();return;}
        actor=target;promptText.text=P07Language.Display(text);promptRoot.SetActive(true);
    }
    // Preserve the API for Inspector events; runtime player interaction always supplies its owner.
    public void ShowPrompt(string text)
    {
        var controls=PersistentRuntime.Instance?PersistentRuntime.Instance.GetComponent<GameControlManager>():null;
        ShowPrompt(text,controls&&controls.ActivePlayer?controls.ActivePlayer.transform:null);
    }
    public void HidePrompt(){actor=null;if(promptRoot)promptRoot.SetActive(false);}
    private void LateUpdate()
    {
        if(!IsVisible||!actor)return;
        var cam=Camera.main;
        if(!cam||!canvas){HidePrompt();return;}
        Bounds bounds=new Bounds(actor.position,new Vector3(.4f,1.8f,0));
        bool found=false;
        foreach(var r in actor.GetComponentsInChildren<SpriteRenderer>())
        {
            if(!r.enabled||r.forceRenderingOff||!r.sprite||r.color.a<=.01f)continue;
            if(r.name=="Demo2026_PlayerPainting"){bounds=r.bounds;found=true;break;}
            if(!found){bounds=r.bounds;found=true;}
        }
        Vector3 screen=cam.WorldToScreenPoint(new Vector3(bounds.center.x,bounds.max.y+headGap,actor.position.z));
        if(screen.z<0){HidePrompt();return;}
        var rect=promptRoot.GetComponent<RectTransform>();
        if(!rect)return;
        // Keep the prompt readable at the same relative size on 720p through 4K.
        // Scale only this prompt, preserving existing inventory/dialogue canvas settings.
        float scale=Mathf.Max(.25f,Mathf.Min(Screen.width/1920f,Screen.height/1080f));
        rect.localScale=Vector3.one*(scale/Mathf.Max(.001f,canvas.scaleFactor));
        float width=Mathf.Clamp(promptText.preferredWidth+24,100,420);
        rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.pivot=new Vector2(.5f,0);
        rect.sizeDelta=new Vector2(width,Mathf.Max(40,promptText.preferredHeight+8));
        Rect view=cam.pixelRect;
        var framing=cam.GetComponent<RoomPresentationFraming>();
        if(framing&&framing.FramesPosition(actor.position)){float bar=view.height*framing.letterbox;view.yMin+=bar;view.yMax-=bar;}
        float halfWidth=Mathf.Min(width*scale*.5f,(view.width-16)*.5f);
        screen.x=Mathf.Clamp(screen.x,view.xMin+halfWidth+8,view.xMax-halfWidth-8);
        screen.y=Mathf.Clamp(screen.y,view.yMin+8,Mathf.Max(view.yMin+8,view.yMax-rect.rect.height*scale-8));
        var parent=rect.parent as RectTransform;
        Camera uiCamera=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
        if(parent&&RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,screen,uiCamera,out var local))
            rect.localPosition=new Vector3(local.x,local.y,0);
    }
}
