using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

[DisallowMultipleComponent]
public sealed class DemoDialogueArtwork : MonoBehaviour
{
    DialogueManager dialogue;Image left,right;GameObject root;string lastSignature;DialogueData lastDialogue;Sprite leftCrop,rightCrop;
    bool leftSpeaking,rightSpeaking;
    float leftScale=1,rightScale=1;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install(){SceneManager.sceneLoaded-=OnScene;SceneManager.sceneLoaded+=OnScene;OnScene(SceneManager.GetActiveScene(),LoadSceneMode.Single);}
    static void OnScene(Scene scene,LoadSceneMode mode){foreach(var d in Object.FindObjectsByType<DialogueManager>(FindObjectsInactive.Include))if(!d.GetComponent<DemoDialogueArtwork>())d.gameObject.AddComponent<DemoDialogueArtwork>();}
    void Awake(){dialogue=GetComponent<DialogueManager>();}
    void Build()
    {
        var canvas=dialogue.PresentationCanvas;if(!canvas)return;
        var r=RuntimePanelUI.Rect("DialoguePortraits",canvas.transform,Vector2.zero,Vector2.one);root=r.gameObject;r.SetAsFirstSibling();
        left=Make(r,"XiaoPengLeft",new Vector2(.035f,.06f),new Vector2(.34f,.84f));right=Make(r,"OtherRight",new Vector2(.66f,.06f),new Vector2(.965f,.84f));
        var bg=dialogue.PresentationBackground;
        if(bg){bg.color=new Color(.035f,.045f,.06f,.60f);var b=bg.rectTransform;b.anchorMin=new Vector2(.02f,.025f);b.anchorMax=new Vector2(.98f,.235f);b.offsetMin=b.offsetMax=Vector2.zero;DemoPaintedFrame.Add(b,new Color(.58f,.61f,.60f,.65f),1.6f);}
        foreach(var t in bg?bg.GetComponentsInChildren<TMP_Text>(true):new TMP_Text[0]){
            var b=t.rectTransform;if(t.name.Contains("Speaker")){Place(b,new Vector2(.04f,.69f),new Vector2(.90f,.94f));t.fontSize=29;t.alignment=TextAlignmentOptions.Left;}
            else if(t.name.Contains("DialogueText")){Place(b,new Vector2(.04f,.13f),new Vector2(.94f,.68f));t.fontSize=30;t.alignment=TextAlignmentOptions.TopLeft;}
            else if(t.name.Contains("Continue")){Place(b,new Vector2(.92f,.015f),new Vector2(.985f,.21f));t.fontSize=21;}
        }
    }
    static void Place(RectTransform r,Vector2 min,Vector2 max){r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;}
    static Image Make(Transform parent,string name,Vector2 min,Vector2 max){var i=RuntimePanelUI.Panel(name,parent,min,max,Color.white,false);i.preserveAspect=true;return i;}
    void LateUpdate()
    {
        if(!dialogue||!DemoArtCatalog.Current)return;if(!root)Build();if(!root)return;
        bool active=dialogue.IsDialogueActive();root.SetActive(active);if(!active){lastSignature=null;return;}
        var data=dialogue.CurrentDialogue;int index=dialogue.CurrentLineIndex;if(data==null||index<0||index>=data.lines.Length)return;
        if(data!=lastDialogue){lastDialogue=data;lastSignature=null;if(rightCrop)Destroy(rightCrop);rightCrop=null;right.sprite=null;}
        string speaker=data.lines[index].speaker??"";
        string scene=SceneManager.GetActiveScene().name;
        var controls=PersistentRuntime.Instance?PersistentRuntime.Instance.GetComponent<GameControlManager>():null;
        string xp=scene=="Scene03_Childhood"?"CHR_XP_CHILD_BASE:0":controls&&!controls.AllowsAdultOutfits&&scene=="Scene04_School"?"CHR_XP_YOUTH_BASE:0":"CHR_XP_ADULT_DEFAULT_CLOSEUP";
        string signature=index+":"+xp;if(signature==lastSignature){AlignPortraits();return;}lastSignature=signature;
        bool protagonist=speaker.Contains("肖鹏")||speaker.Contains("Xiao Peng")||speaker.Contains("小鹏");
        bool narration=string.IsNullOrWhiteSpace(speaker)||speaker.Contains("旁白")||speaker.Contains("内心");
        leftSpeaking=protagonist&&!narration;rightSpeaking=!protagonist&&!narration;
        Set(left,xp,ref leftCrop);left.enabled=!narration&&left.sprite;left.color=protagonist?Color.white:new Color(.52f,.56f,.63f,1);
        string other=Other(speaker,scene);if(!protagonist)Set(right,other,ref rightCrop);
        if(narration){right.enabled=false;return;}
        right.enabled=right.sprite;right.color=protagonist?new Color(.52f,.56f,.63f,1):Color.white;
        AlignPortraits();
    }
    void AlignPortraits()
    {
        float blend=1-Mathf.Exp(-Time.unscaledDeltaTime*14);
        leftScale=Mathf.Lerp(leftScale,leftSpeaking?1.025f:1,blend);
        rightScale=Mathf.Lerp(rightScale,rightSpeaking?1.025f:1,blend);
        var bounds=((RectTransform)root.transform).rect;
        float widestRatio=Mathf.Max(PortraitRatio(left),PortraitRatio(right));
        float height=Mathf.Min(bounds.height*.70f,bounds.width*.305f/Mathf.Max(.1f,widestRatio));
        FitPortrait(left,.1875f,leftScale,height);FitPortrait(right,.8125f,rightScale,height);
    }
    static float PortraitRatio(Image image)=>image.sprite?image.sprite.rect.width/image.sprite.rect.height:0;
    void FitPortrait(Image image,float x,float emphasis,float height)
    {
        if(!image.sprite)return;
        var r=image.rectTransform;
        float ratio=PortraitRatio(image);
        r.anchorMin=r.anchorMax=new Vector2(x,.065f);r.pivot=new Vector2(.5f,0);
        r.anchoredPosition=Vector2.zero;r.sizeDelta=new Vector2(height*ratio,height);
        // All busts share one lower edge. Only the speaking side grows, by 2.5%.
        r.localScale=Vector3.one*emphasis;
    }
    static string Other(string s,string scene)
    {
        if(s.Contains("郑涛"))return "CHR_ZHENGTAO_YOUTH_BASE:0";
        if(s.Contains("邱天")||s.Contains("Qiu Tian")||s.Contains("???")||s.Contains("？？？"))return scene=="Scene03_Childhood"?"CHR_BOY_CHILD_BASE:0":"CHR_GUIDE_CLOSEUP";
        if(s.Contains("男孩"))return "CHR_BOY_CHILD_BASE:0";
        if(s.Contains("张若源"))return "CHR_SCHOOL_SUPPORT_BASE:CH_ZHANGRUOYUAN";
        if(s.Contains("主任"))return "CHR_SCHOOL_SUPPORT_BASE:CH_DIRECTOR";
        if(s.Contains("向焱"))return "CHR_SCHOOL_SUPPORT_BASE:CH_XIANGYAN_normal_reference";
        if(s.Contains("母亲")||s.Contains("妈妈"))return "CHR_FAMILY_NEIGHBOR_BASE:CH_XP_MOTHER";
        if(s.Contains("父亲")||s.Contains("爸爸"))return "CHR_FAMILY_NEIGHBOR_BASE:CH_XP_FATHER";
        if(s.Contains("李梅"))return "CHR_FAMILY_NEIGHBOR_BASE:CH_LIMEI";
        if(s.Contains("说书"))return "CHR_STORY_ZHAO_ZHANG_BASE:CH_STORYTELLER";
        if(s.Contains("赵秉承"))return "CHR_STORY_ZHAO_ZHANG_BASE:CH_ZHAOBINGCHENG";
        if(s.Contains("张辰")||s.Contains("张晨"))return "CHR_STORY_ZHAO_ZHANG_BASE:CH_ZHANGCHEN";
        if(s.Contains("孙福"))return "CHR_CHESS_NEIGHBORS_BASE:CH_SUNFU";
        if(s.Contains("王坤"))return "CHR_CHESS_NEIGHBORS_BASE:CH_WANGKUN";
        if(s.Contains("王萍"))return "CHR_CHESS_NEIGHBORS_BASE:CH_WANGPING";
        if(s.Contains("套圈"))return "CHR_TOWN_TRADERS_BASE:CH_RING_VENDOR";
        return null;
    }
    static void Set(Image output,string key,ref Sprite cropped)
    {
        if(cropped){Object.Destroy(cropped);cropped=null;}
        // Closeup source canvases have different transparent margins. Dialogue-specific
        // sprite regions use visible bounds so the shared height and baseline are real.
        var sprite=DemoArtCatalog.Find(key+":portrait")??DemoArtCatalog.Find(key);output.sprite=sprite;
        if(!sprite||key.Contains("CLOSEUP"))return;
        var r=sprite.rect;r.y+=r.height*.43f;r.height*=.57f;
        cropped=Sprite.Create(sprite.texture,r,new Vector2(.5f,0),sprite.pixelsPerUnit,0,SpriteMeshType.FullRect);output.sprite=cropped;
    }
    void OnDestroy(){if(leftCrop)Destroy(leftCrop);if(rightCrop)Destroy(rightCrop);if(root)Destroy(root);}
}
