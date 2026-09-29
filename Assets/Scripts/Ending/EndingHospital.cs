using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed partial class EndingDirector
{
    Canvas dialogueCanvas;int originalDialogueOrder;
    Image dialogueBackground;Color originalDialogueBackground;
    Image[] bedDetails;
    void BuildEndingUI(){
        dialogueCanvas=dialogue.PresentationCanvas;
        dialogueBackground=dialogue.PresentationBackground;
        if(dialogueBackground!=null){originalDialogueBackground=dialogueBackground.color;dialogueBackground.color=new Color(.055f,.065f,.08f,.96f);}
        if(dialogueCanvas!=null){originalDialogueOrder=dialogueCanvas.sortingOrder;dialogueCanvas.sortingOrder=450;}
        endingCanvas=RuntimePanelUI.Canvas("EndingCloseupAndCompletion",transform,400);
        focusPage=RuntimePanelUI.Rect("BedCloseup",endingCanvas,Vector2.zero,Vector2.one).gameObject;
        RuntimePanelUI.Panel("Room",focusPage.transform,Vector2.zero,Vector2.one,new Color(.19f,.25f,.28f));
        RuntimePanelUI.Panel("Curtain",focusPage.transform,new Vector2(.05f,.16f),new Vector2(.22f,.93f),new Color(.29f,.39f,.4f));
        RuntimePanelUI.Panel("BedFrame",focusPage.transform,new Vector2(.31f,.2f),new Vector2(.85f,.55f),new Color(.47f,.5f,.49f));
        RuntimePanelUI.Panel("Blanket",focusPage.transform,new Vector2(.34f,.39f),new Vector2(.80f,.68f),new Color(.67f,.7f,.65f));
        RuntimePanelUI.Panel("Pillow",focusPage.transform,new Vector2(.67f,.64f),new Vector2(.83f,.75f),new Color(.64f,.66f,.6f));
        // There is NO identity portrait behind this protection, even with every effect disabled.
        faceCover=RuntimePanelUI.Panel("PermanentFeaturelessHeadOcclusion",focusPage.transform,new Vector2(.68f,.61f),new Vector2(.825f,.79f),new Color(.055f,.065f,.075f));
        bedDetails=new[]{RuntimePanelUI.Panel("NearRail",focusPage.transform,new Vector2(.3f,.35f),new Vector2(.86f,.375f),Color.gray),RuntimePanelUI.Panel("BedLegLeft",focusPage.transform,new Vector2(.36f,.14f),new Vector2(.38f,.36f),Color.gray),RuntimePanelUI.Panel("BedLegRight",focusPage.transform,new Vector2(.8f,.14f),new Vector2(.82f,.36f),Color.gray)};
        bedMist=RuntimePanelUI.Panel("FocusHaze",focusPage.transform,Vector2.zero,Vector2.one,new Color(.47f,.53f,.56f,.91f),false);
        focusPage.SetActive(false);
        focusText=RuntimePanelUI.Text("",endingCanvas,data.font,new Vector2(.2f,.79f),new Vector2(.8f,.9f),31);
        endPage=RuntimePanelUI.Rect("DemoCompletion",endingCanvas,Vector2.zero,Vector2.one).gameObject;
        RuntimePanelUI.Panel("CompletionBackground",endPage.transform,Vector2.zero,Vector2.one,new Color(.035f,.045f,.055f));
        RuntimePanelUI.Text("Demo结束",endPage.transform,data.font,new Vector2(.18f,.53f),new Vector2(.82f,.69f),60);
        RuntimePanelUI.Text("感谢游玩",endPage.transform,data.font,new Vector2(.25f,.4f),new Vector2(.75f,.5f),32);
        exitButton=RuntimePanelUI.Button("退出游戏",endPage.transform,data.font,new Vector2(.38f,.23f),new Vector2(.62f,.33f),QuitExplicitly);
        endPage.SetActive(false);
        veil=RuntimePanelUI.Panel("WhiteBlackTransition",endingCanvas,Vector2.zero,Vector2.one,Color.clear,false);
    }
    IEnumerator BedIntro(){
        Step=EndingStep.BedIntro;focusText.text="";focusPage.SetActive(true);
        yield return Say("F06");if(!speechCompleted)yield break;yield return Say("F07");if(!speechCompleted)yield break;
        Focus.RequireNewPress();Step=EndingStep.Focus;focusText.text="长按空格努力看清";
        monitor=Sound(data.monitorLoop,true,.06f);footsteps=Sound(data.wheelsLoop,true,.015f);RenderFocus();
    }
    void RenderFocus(){float p=Focus.Progress;bedMist.color=new Color(.47f,.53f,.56f,Mathf.Lerp(.91f,.06f,p));
        foreach(var detail in bedDetails)detail.color=Color.Lerp(new Color(.4f,.45f,.47f),new Color(.83f,.87f,.83f),p);
        if(monitor!=null)monitor.volume=Mathf.Lerp(.06f,.32f,p);if(footsteps!=null)footsteps.volume=Mathf.Lerp(.015f,.14f,Mathf.Clamp01((p-.22f)/.78f));
    }
    IEnumerator Declaration(){
        Step=EndingStep.Declaration;focusText.text="";
        yield return Cover(Color.white,.4f);focusPage.SetActive(false);yield return FadeOwnedAudio(.45f);
        var stop=Sound(data.monitorStop,false,.4f);yield return Wait(.85f);if(stop!=null)stop.volume=.08f;
        if(!story.Has(StoryFlag.EndingNameHeard)){yield return Say("E01");if(!speechCompleted)yield break;story.Set(StoryFlag.EndingNameHeard);}
        yield return Say("E02");if(!speechCompleted)yield break;
        yield return Cover(Color.black,.6f);yield return Say("E03");if(!speechCompleted)yield break;
        story.Set(StoryFlag.EndingFinalLineCompleted);ShowCompletion();
    }
    void ShowCompletion(){
        if(endPage==null){Debug.LogError("Completion page missing; completion not committed.",this);return;}
        Step=EndingStep.Complete;focusPage.SetActive(false);focusText.text="";endPage.SetActive(true);endPage.transform.SetAsLastSibling();exitButton.interactable=false;endArmed=false;
        foreach(var sound in GetComponents<AudioSource>())sound.Stop();
        if(endPage.activeInHierarchy)story.Set(StoryFlag.DemoCompleted);
    }
    void QuitExplicitly(){
        if(!endArmed||Step!=EndingStep.Complete||paused)return;
        #if UNITY_EDITOR
        Debug.Log("Demo completed. Exit Play Mode using the Unity Play button; the editor remains open.",this);
        #else
        Application.Quit();
        #endif
    }
}
