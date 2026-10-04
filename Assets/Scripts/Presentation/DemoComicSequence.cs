using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Four authored comic transitions. Callers retain their original story state and control lock.</summary>
public sealed class DemoComicSequence : MonoBehaviour
{
    RectTransform canvas;
    readonly List<CanvasGroup> panels=new List<CanvasGroup>();
    Canvas dialogueCanvas;int priorDialogueOrder;bool priorOverride;
    public bool IsOpen=>canvas;
    public static DemoComicSequence For(GameObject owner){var c=owner.GetComponent<DemoComicSequence>();return c?c:owner.AddComponent<DemoComicSequence>();}
    void Begin()
    {
        Close();canvas=RuntimePanelUI.Canvas("PagedComic",transform,205);
        RuntimePanelUI.Panel("InkGutters",canvas,Vector2.zero,Vector2.one,new Color(.025f,.035f,.05f));
    }
    RectTransform Frame(string name,Vector2 min,Vector2 max,string background=null,bool nativeAspect=false)
    {
        var frame=RuntimePanelUI.Panel(name,canvas,min,max,new Color(.075f,.095f,.12f));
        var mask=frame.gameObject.AddComponent<RectMask2D>();
        var group=frame.gameObject.AddComponent<CanvasGroup>();group.alpha=0;panels.Add(group);
        if(background!=null)Art(frame.transform,background,Vector2.zero,Vector2.one,nativeAspect);
        DemoPaintedFrame.Add(frame.transform,new Color(.68f,.70f,.66f),2.4f);return frame.rectTransform;
    }
    static Image Art(Transform parent,string key,Vector2 min,Vector2 max,bool fit=true)
    {
        var image=RuntimePanelUI.Panel(key,parent,min,max,Color.white,false);image.sprite=DemoArtCatalog.Find(key);image.preserveAspect=fit;image.enabled=image.sprite;return image;
    }
    IEnumerator Reveal(int index,float duration=.32f)
    {
        if(index<0||index>=panels.Count)yield break;var group=panels[index];
        for(float t=0;t<duration;t+=GameplayClock.Delta){if(!group)yield break;group.alpha=Mathf.Clamp01(t/duration);yield return null;}
        if(group)group.alpha=1;
    }
    static IEnumerator FreshAdvance(float minimum)
    {
        yield return GameplayClock.Wait(minimum);
        while(GameplayInput.GetKey(KeyCode.E)||GameplayInput.GetKey(KeyCode.Space)||GameplayInput.GetMouseButton(0))yield return null;
        while(!GameplayInput.GetKeyDown(KeyCode.E)&&!GameplayInput.GetKeyDown(KeyCode.Space)&&!GameplayInput.GetMouseButtonDown(0))yield return null;
        yield return null;
    }
    public IEnumerator Memory(Action gunshot)
    {
        Begin();
        Frame("Breathing",new Vector2(.08f,.52f),new Vector2(.49f,.94f),"P03_GunComic:breathing",true);
        Frame("VoiceInDarkness",new Vector2(.51f,.52f),new Vector2(.92f,.94f),"P03_GunComic:voice",true);
        Frame("AbruptMemory",new Vector2(.08f,.06f),new Vector2(.92f,.48f),"P03_GunComic:shot",true);
        yield return Reveal(0);yield return GameplayClock.Wait(1.2f);
        yield return Reveal(1);yield return GameplayClock.Wait(1.0f);
        gunshot?.Invoke();panels[2].alpha=1;
        // The authored hand, gun, muzzle flash and bullet remain readable.
        yield return GameplayClock.Wait(1.25f);Close();
    }
    public IEnumerator DeskGrab()
    {
        Begin();
        var first=Frame("NameOnDesk",new Vector2(.62f,.53f),new Vector2(.94f,.91f),"CU_SCHOOL_DESK");
        var second=Frame("WristInterrupted",new Vector2(.62f,.09f),new Vector2(.94f,.49f));
        Art(second,"CHR_ADULT_WRIST_GRAB_CUFF3:0",new Vector2(.01f,.02f),new Vector2(.99f,.98f));
        var third=Frame("SchoolMemory",new Vector2(.06f,.09f),new Vector2(.585f,.91f),"BG_SCHOOL_CLASS3_2_MEMORY");
        Art(third,"CHR_YOUTH_GRAB_PAIR2:0",new Vector2(.02f,.01f),new Vector2(.98f,.93f));
        yield return Reveal(0);yield return GameplayClock.Wait(.6f);yield return Reveal(1);yield return GameplayClock.Wait(.6f);yield return Reveal(2);
        yield return FreshAdvance(.85f);Close();
    }
    public void BeginConfrontation(DialogueManager dialogue)
    {
        Begin();
        var a=Frame("OfficeThreshold",new Vector2(.05f,.57f),new Vector2(.48f,.93f),"MOD_SCHOOL_SHELL_MEMORY");
        Art(a,"CHR_SCHOOL_SUPPORT_BASE:CH_XIANGYAN_normal_reference",new Vector2(.58f,.01f),new Vector2(.96f,.94f));
        var b=Frame("XiaoPengReaction",new Vector2(.505f,.57f),new Vector2(.95f,.93f));Art(b,"CHR_XP_ADULT_DEFAULT_CLOSEUP",new Vector2(.1f,-.5f),new Vector2(.9f,1.15f));
        var c=Frame("ThreatApproaches",new Vector2(.505f,.265f),new Vector2(.95f,.54f));
        var threat=Art(c,"CHR_SCHOOL_SUPPORT_ACTION_V02:CH_XIANGYAN_chase",new Vector2(.22f,-.35f),new Vector2(.82f,1.4f));threat.color=new Color(.62f,.43f,.46f,1);
        var d=Frame("Transformation",new Vector2(.05f,.265f),new Vector2(.48f,.54f));
        Art(d,"CHR_XIANGYAN_TRANSFORM:2",new Vector2(.14f,-.40f),new Vector2(.91f,1.3f));
        dialogueCanvas=dialogue?dialogue.PresentationCanvas:null;
        if(dialogueCanvas){priorDialogueOrder=dialogueCanvas.sortingOrder;priorOverride=dialogueCanvas.overrideSorting;dialogueCanvas.overrideSorting=true;dialogueCanvas.sortingOrder=215;}
        StartCoroutine(RevealThreat());
    }
    IEnumerator RevealThreat(){for(int i=0;i<4;i++){yield return Reveal(i,.5f);yield return GameplayClock.Wait(.5f);}}
    public IEnumerator FinishConfrontation(){while(panels.Count==4&&panels[3]&&panels[3].alpha<1)yield return null;yield return GameplayClock.Wait(.3f);Close();}
    public void BeginBoarding()
    {
        Begin();
        var a=Frame("DriverBoards",new Vector2(.055f,.535f),new Vector2(.40f,.915f));
        Art(a,"SPR_POLICE_CAR_SINGLE_DOOR_STATES:0",new Vector2(.00f,.00f),new Vector2(1,.92f));
        Art(a,"CHR_STORY_ZHAO_ZHANG_ACTION:CH_ZHANGCHEN_driver_door",new Vector2(.52f,.07f),new Vector2(.81f,.9f));
        var b=Frame("GuideBoardsFirst",new Vector2(.055f,.085f),new Vector2(.40f,.50f));
        Art(b,"SPR_POLICE_CAR_SINGLE_DOOR_STATES:1",new Vector2(0,0),new Vector2(1,.9f));
        Art(b,"CHR_GUIDE_BASE_V02:0",new Vector2(.21f,.06f),new Vector2(.47f,.9f));
        var c=Frame("XiaoPengBoardsLast",new Vector2(.435f,.085f),new Vector2(.945f,.915f));
        Art(c,"SPR_POLICE_CAR_SINGLE_DOOR_STATES:1",new Vector2(-.1f,.07f),new Vector2(1.15f,.85f));
        Art(c,"CHR_XP_ADULT_DEFAULT_TURN3:1",new Vector2(.27f,.12f),new Vector2(.61f,.82f));
    }
    public IEnumerator BoardingStep(int step){yield return Reveal(step);yield return GameplayClock.Wait(.65f);}
    public IEnumerator FinishBoarding(){yield return FreshAdvance(.55f);Close();}
    public void Close()
    {
        StopAllCoroutines();if(dialogueCanvas){dialogueCanvas.sortingOrder=priorDialogueOrder;dialogueCanvas.overrideSorting=priorOverride;dialogueCanvas=null;}
        if(canvas){canvas.gameObject.SetActive(false);Destroy(canvas.gameObject);canvas=null;}panels.Clear();
    }
    void OnDisable()=>Close();
}
