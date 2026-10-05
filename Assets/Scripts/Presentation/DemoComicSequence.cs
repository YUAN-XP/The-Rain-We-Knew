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
    public bool Completed {get;private set;}
    public IEnumerator Confrontation(DialogueData source)
    {
        Completed=false;if(source==null||source.lines==null||source.lines.Length==0)yield break;
        Begin();
        Frame("Threshold",new Vector2(.035f,.51f),new Vector2(.49f,.96f),"P06_TransformationComic:0");
        Frame("UnsettlingChange",new Vector2(.51f,.51f),new Vector2(.965f,.96f),"P06_TransformationComic:1");
        Frame("Transformation",new Vector2(.035f,.055f),new Vector2(.49f,.49f),"P06_TransformationComic:2");
        Frame("Pursuit",new Vector2(.51f,.055f),new Vector2(.965f,.49f),"P06_TransformationComic:3");
        var font=PersistentRuntime.Instance.GetComponentInChildren<TMPro.TMP_Text>(true).font;
        var bubble=RuntimePanelUI.Panel("ComicDialogue",canvas,new Vector2(.06f,.045f),new Vector2(.94f,.21f),new Color(.025f,.035f,.045f,.94f),false);
        DemoPaintedFrame.Add(bubble.transform,new Color(.65f,.67f,.60f),1.6f);
        var text=RuntimePanelUI.Text("",bubble.transform,font,new Vector2(.025f,.19f),new Vector2(.975f,.93f),30);text.alignment=TMPro.TextAlignmentOptions.TopLeft;text.textWrappingMode=TMPro.TextWrappingModes.Normal;
        RuntimePanelUI.Text("[E] 继续",bubble.transform,font,new Vector2(.84f,.015f),new Vector2(.975f,.19f),19);
        int visible=-1;
        for(int n=0;n<source.lines.Length;n++){
            int frame=source.lines.Length==1?3:Mathf.Min(3,n*3/(source.lines.Length-1));
            while(visible<frame){visible++;yield return Reveal(visible,.42f);}
            var line=source.lines[n];text.text=(string.IsNullOrWhiteSpace(line.speaker)?"":line.speaker+"：")+line.text;
            yield return FreshAdvance(Mathf.Max(.45f,source.minimumReadSeconds));
        }
        while(visible<3){visible++;yield return Reveal(visible,.42f);}
        yield return GameplayClock.Wait(.45f);Close();Completed=true;
    }
    public void BeginBoarding()
    {
        Begin();
        var story=PersistentRuntime.Instance.GetComponent<StoryStateManager>();
        string variant=story.Outfit==AdultOutfit.Overalls?"Overalls":story.Outfit==AdultOutfit.SchoolUniform?"School":"Default";
        for(int i=0;i<3;i++)Frame(new[]{"DriverEnteringFront","GuideEnteringRear","XiaoPengTakingSeat"}[i],new Vector2(.035f+i*.32f,.12f),new Vector2(.325f+i*.32f,.92f),"P06_Boarding"+(i<2?"Default":variant)+":"+i,true);
    }
    public IEnumerator BoardingStep(int step){yield return Reveal(step,.42f);yield return GameplayClock.Wait(1.15f);}
    public IEnumerator FinishBoarding(){yield return FreshAdvance(.55f);Close();}
    public void Close()
    {
        StopAllCoroutines();if(dialogueCanvas){dialogueCanvas.sortingOrder=priorDialogueOrder;dialogueCanvas.overrideSorting=priorOverride;dialogueCanvas=null;}
        if(canvas){canvas.gameObject.SetActive(false);Destroy(canvas.gameObject);canvas=null;}panels.Clear();
    }
    void OnDisable()=>Close();
}
