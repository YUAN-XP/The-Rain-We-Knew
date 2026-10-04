using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Scene03FamilySequence : MonoBehaviour
{
    public Scene03Chapter chapter;
    public Scene03RiverSequence river;
    public DialogueData invitation, timerExplanation, callBoy, readyForPhoto;
    public Transform mother, father, homeBoyPoint, roomBoyPoint, motherPose, fatherPose, childPose, boyEdge, boyOutside, viewPoint;
    public GameObject cameraPlacement, placedCamera;
    public AudioClip mechanicalTick, shutter, rain;
    public string returnSceneName;
    public bool Busy {get;private set;}
    public bool PhotoFrozen {get;private set;}
    public bool AwaitingPlacement=>chapter.Story!=null&&chapter.Story.Has(StoryFlag.Scene03FamilySetupComplete)&&!chapter.Story.Has(StoryFlag.Scene03FamilyPhotoTaken);
    bool evening,ownsLock;
    Image flash,finishedPhoto,posedPhoto;
    public bool PortraitReady=>posedPhoto&&posedPhoto.gameObject.activeSelf;
    CanvasGroup frame;
    AudioSource timer;
    RectTransform portraitRect,snapshotRect,photoBackdrop;
    readonly List<RectTransform> mattes=new List<RectTransform>(),borders=new List<RectTransform>();
    TMP_Text photoCaption,photoAdvance;
    Canvas originalDialogueCanvas;
    bool originalDialogueEnabled,ownsDialoguePresentation;
    int layoutWidth,layoutHeight;
    IEnumerator Start()
    {
        while(!chapter.Ready)yield return null;
        chapter.Story.Changed+=ApplyEvening;ApplyEvening();
        var canvas=RuntimePanelUI.Canvas("FamilyCameraPresentation",transform,160);
        flash=RuntimePanelUI.Panel("PhotoWhiteFlash",canvas,Vector2.zero,Vector2.one,new Color(1,1,1,0));
        flash.raycastTarget=false;
        var framing=RuntimePanelUI.Rect("FamilyCameraFrame",canvas,Vector2.zero,Vector2.one);frame=framing.gameObject.AddComponent<CanvasGroup>();frame.alpha=0;
        frame.blocksRaycasts=false;
        for(int i=0;i<4;i++)mattes.Add(RuntimePanelUI.Panel("ViewfinderMatte",framing,Vector2.zero,Vector2.one,Color.black,false).rectTransform);
        photoBackdrop=RuntimePanelUI.Panel("PhotographBacking",framing,Vector2.zero,Vector2.one,Color.black,false).rectTransform;
        photoBackdrop.gameObject.SetActive(false);
        // Fit both stages to the painting's real aspect ratio. The developed photo keeps
        // the same opaque stage, so neither world art nor old portraits leak beside it.
        portraitRect=RuntimePanelUI.Rect("PosedFamilyPortrait",framing,Vector2.zero,Vector2.one);
        posedPhoto=portraitRect.gameObject.AddComponent<Image>();posedPhoto.sprite=DemoArtCatalog.Find("FILE_FAMILY_PHOTO_CLEAN");posedPhoto.preserveAspect=true;posedPhoto.raycastTarget=false;posedPhoto.gameObject.SetActive(false);
        snapshotRect=RuntimePanelUI.Rect("DevelopedFamilyPhoto",framing,Vector2.zero,Vector2.one);
        finishedPhoto=snapshotRect.gameObject.AddComponent<Image>();finishedPhoto.sprite=posedPhoto.sprite;finishedPhoto.preserveAspect=true;finishedPhoto.raycastTarget=false;finishedPhoto.gameObject.SetActive(false);
        for(int i=0;i<4;i++)borders.Add(RuntimePanelUI.Panel("PhotoFrame",framing,Vector2.zero,Vector2.one,new Color(.96f,.93f,.80f),false).rectTransform);
        photoCaption=RuntimePanelUI.Text("",framing,chapter.font,new Vector2(.12f,.04f),new Vector2(.88f,.125f),26);
        photoCaption.enableAutoSizing=true;photoCaption.fontSizeMin=20;photoCaption.fontSizeMax=26;
        photoAdvance=RuntimePanelUI.Text("[E] 继续",framing,chapter.font,new Vector2(.4f,.008f),new Vector2(.6f,.04f),19);
        photoCaption.gameObject.SetActive(false);photoAdvance.gameObject.SetActive(false);
        LayoutPhotoStage();
        flash.transform.SetAsLastSibling();
        timer=gameObject.AddComponent<AudioSource>();timer.playOnAwake=false;
        cameraPlacement.SetActive(AwaitingPlacement);placedCamera.SetActive(false);
    }
    void ApplyEvening()
    {
        if(evening||chapter.Story==null||!chapter.Story.Has(StoryFlag.Scene03ReturnedHome))return;
        evening=true;
        foreach(var root in new[]{chapter.homeRoot,chapter.livingRoomRoot})
            foreach(var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))renderer.color*=new Color(1,.86f,.68f,1);
        river.boy.SetParent(chapter.homeRoot.transform,true);river.boy.position=homeBoyPoint.position;
        river.boy.gameObject.SetActive(true);river.guitar.SetActive(false);
        var neck=river.boy.Find("GuitarNeck");if(neck!=null)neck.gameObject.SetActive(false);
        var talk=river.boy.GetComponent<RiverTalkInteractable>();if(talk!=null)talk.enabled=false;
        var collider=river.boy.GetComponent<Collider2D>();if(collider!=null)collider.enabled=false;
    }
    public void EnterLivingRoom()
    {
        if(!evening||Busy||chapter.Controls.IsLocked()||chapter.Story.Has(StoryFlag.Scene03FamilyPhotoTaken))return;
        river.boy.SetParent(chapter.livingRoomRoot.transform,true);river.boy.position=roomBoyPoint.position;
        if(!chapter.Story.Has(StoryFlag.Scene03FamilySetupComplete))StartCoroutine(Prepare());
        else cameraPlacement.SetActive(true);
    }
    IEnumerator Prepare()
    {
        Busy=true;Lock();
        try {yield return chapter.Say(invitation);yield return chapter.Say(timerExplanation);chapter.Story.Set(StoryFlag.Scene03FamilySetupComplete);cameraPlacement.SetActive(true);}
        finally {Busy=false;Unlock();}
    }
    public void PlaceCamera()
    {
        if(Busy||!AwaitingPlacement||chapter.Controls.IsLocked())return;
        StartCoroutine(Photograph());
    }
    IEnumerator Photograph()
    {
        Busy=true;Lock();
        try
        {
            chapter.carriedCamera.SetActive(false);cameraPlacement.SetActive(false);placedCamera.SetActive(true);
            PersistentRuntime.Instance.GetComponent<QuestProgress>()?.Publish(QuestEvent.CameraPlaced);
            var mode=PersistentRuntime.Instance.GetComponent<CameraModeController>();mode.Lower();mode.ContextAvailable=false;
            mother.position=motherPose.position;father.position=fatherPose.position;
            SetChild(childPose.position);river.boy.position=boyOutside.position;
            Camera.main.transform.position=new Vector3(viewPoint.position.x,viewPoint.position.y,Camera.main.transform.position.z);Camera.main.orthographicSize=1.75f;frame.alpha=1;TakeDialoguePresentation();
            timer.clip=mechanicalTick;timer.loop=true;timer.volume=.5f;timer.Play();
            yield return GameplayClock.Wait(1.5f);yield return chapter.Say(callBoy);
            Vector3 from=river.boy.position;Vector3 childFrom=chapter.child.transform.position;
            for(float t=0;t<1.2f;t+=GameplayClock.Delta)
            {float k=Mathf.Clamp01(t/1.2f);river.boy.position=Vector3.Lerp(from,boyEdge.position,k);SetChild(childFrom+Vector3.left*(.25f*Mathf.SmoothStep(0,1,k)));yield return null;}
            river.boy.position=boyEdge.position;
            chapter.child.GetComponent<DemoActorArtwork>()?.SetPose("CHR_XP_CHILD_TURN3:0",1.35f);
            var boyView=river.boy.GetComponent<P02ActorPresentation>();if(boyView)boyView.SetStaticPose(true);
            var boyPaint=river.boy.Find("NewArt_Actor").GetComponent<SpriteRenderer>();boyPaint.sprite=DemoArtCatalog.Find("CHR_BOY_CHILD_BASE:0");boyPaint.forceRenderingOff=false;
            photoBackdrop.gameObject.SetActive(true);posedPhoto.gameObject.SetActive(true);
            yield return chapter.Say(readyForPhoto);timer.Stop();chapter.effects.PlayOneShot(shutter,.6f);
            yield return White(1,.08f);yield return White(0,.22f);PhotoFrozen=true;posedPhoto.gameObject.SetActive(false);finishedPhoto.gameObject.SetActive(true);
            yield return GameplayClock.Wait(2.6f);
            yield return White(1,.65f);
            if(!Application.CanStreamedLevelBeLoaded(returnSceneName)){Debug.LogWarning("Family photo needs its Scene02 return scene.",this);yield return White(0,.3f);yield break;}
            chapter.Story.Set(StoryFlag.Scene03FamilyPhotoTaken);chapter.Story.Set(StoryFlag.Scene03Completed);chapter.Story.Set(StoryFlag.Scene03BirthdayKnown);
            chapter.ClearChildhoodItems();WhiteSceneTransition.Begin(returnSceneName,chapter.Controls);
        }
        finally{RestoreDialoguePresentation();chapter.child.GetComponent<DemoActorArtwork>()?.SetPose(null);Busy=false;Unlock();}
    }
    IEnumerator White(float alpha,float duration)
    {float before=flash.color.a;for(float t=0;t<duration;t+=GameplayClock.Delta){flash.color=new Color(1,1,1,Mathf.Lerp(before,alpha,t/duration));yield return null;}flash.color=new Color(1,1,1,alpha);}
    void SetChild(Vector3 p){var rb=chapter.child.GetComponent<Rigidbody2D>();rb.linearVelocity=Vector2.zero;rb.position=p;chapter.child.transform.position=p;}
    void Lock(){chapter.Controls.AcquireLock(ControlLockReason.Cutscene,this);ownsLock=true;}
    void Unlock(){if(ownsLock&&chapter!=null&&chapter.Controls!=null)chapter.Controls.ReleaseLock(ControlLockReason.Cutscene,this);ownsLock=false;}
    void LateUpdate()
    {
        if(!frame)return;
        if(layoutWidth!=Screen.width||layoutHeight!=Screen.height)LayoutPhotoStage();
        var dialogue=chapter.Dialogue;
        bool active=Busy&&frame.alpha>.01f&&dialogue&&dialogue.IsDialogueActive();
        var data=active?dialogue.CurrentDialogue:null;
        active=active&&data&&data.lines!=null&&dialogue.CurrentLineIndex>=0&&dialogue.CurrentLineIndex<data.lines.Length;
        photoCaption.gameObject.SetActive(active);photoAdvance.gameObject.SetActive(active);
        if(active){var line=data.lines[dialogue.CurrentLineIndex];photoCaption.text=(string.IsNullOrEmpty(line.speaker)?"":line.speaker+"：")+line.text;}
    }
    void LayoutPhotoStage()
    {
        layoutWidth=Screen.width;layoutHeight=Screen.height;
        float ratio=posedPhoto&&posedPhoto.sprite?posedPhoto.sprite.rect.width/posedPhoto.sprite.rect.height:4f/3;
        float screenRatio=(float)Mathf.Max(1,Screen.width)/Mathf.Max(1,Screen.height);
        float w=.68f,h=.72f;
        if(w*screenRatio/h>ratio)w=h*ratio/screenRatio;else h=w*screenRatio/ratio;
        float left=(1-w)*.5f,right=(1+w)*.5f,bottom=(1-h)*.5f,top=(1+h)*.5f;
        SetRect(portraitRect,left,bottom,right,top);SetRect(snapshotRect,left,bottom,right,top);SetRect(photoBackdrop,left,bottom,right,top);
        SetRect(mattes[0],0,0,1,bottom);SetRect(mattes[1],0,top,1,1);
        SetRect(mattes[2],0,bottom,left,top);SetRect(mattes[3],right,bottom,1,top);
        float bx=2f/Mathf.Max(1,Screen.width),by=2f/Mathf.Max(1,Screen.height);
        SetRect(borders[0],left-bx,bottom-by,right+bx,bottom);SetRect(borders[1],left-bx,top,right+bx,top+by);
        SetRect(borders[2],left-bx,bottom,left,top);SetRect(borders[3],right,bottom,right+bx,top);
    }
    static void SetRect(RectTransform r,float x0,float y0,float x1,float y1){r.anchorMin=new Vector2(x0,y0);r.anchorMax=new Vector2(x1,y1);r.offsetMin=r.offsetMax=Vector2.zero;}
    void TakeDialoguePresentation()
    {
        if(ownsDialoguePresentation)return;
        originalDialogueCanvas=chapter.Dialogue.PresentationCanvas;
        if(originalDialogueCanvas){originalDialogueEnabled=originalDialogueCanvas.enabled;originalDialogueCanvas.enabled=false;ownsDialoguePresentation=true;}
    }
    void RestoreDialoguePresentation(){if(ownsDialoguePresentation&&originalDialogueCanvas)originalDialogueCanvas.enabled=originalDialogueEnabled;ownsDialoguePresentation=false;}
    void OnDisable(){StopAllCoroutines();RestoreDialoguePresentation();Unlock();if(chapter!=null&&chapter.Story!=null)chapter.Story.Changed-=ApplyEvening;}
}
