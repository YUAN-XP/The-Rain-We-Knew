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
    static Image Make(Transform parent,string name,Vector2 min,Vector2 max)
    {
        var frame=RuntimePanelUI.Rect(name+"Frame",parent,min,max);
        var shape=frame.gameObject.AddComponent<DialoguePortraitMask>();shape.raycastTarget=false;
        var mask=frame.gameObject.AddComponent<Mask>();mask.showMaskGraphic=false;
        var i=RuntimePanelUI.Panel(name,frame,Vector2.zero,Vector2.one,Color.white,false);
        i.preserveAspect=true;return i;
    }
    void LateUpdate()
    {
        if(!dialogue||!DemoArtCatalog.Current)return;if(!root)Build();if(!root)return;
        bool active=dialogue.IsDialogueActive() && !(dialogue.CurrentDialogue&&dialogue.CurrentDialogue.hideAllPortraits);root.SetActive(active);if(!active){lastSignature=null;return;}
        var data=dialogue.CurrentDialogue;int index=dialogue.CurrentLineIndex;if(data==null||index<0||index>=data.lines.Length)return;
        if(data!=lastDialogue){lastDialogue=data;lastSignature=null;if(rightCrop)Destroy(rightCrop);rightCrop=null;right.sprite=null;}
        var line=data.lines[index];
        var identity=DialogueIdentity.Resolve(line);
        var age=ContextAge();
        if(identity==DialogueCharacterId.XiaoPeng && line.age!=DialogueAge.Context)age=line.age;
        string xp=DialogueIdentity.PortraitKey(DialogueCharacterId.XiaoPeng,age);
        string signature=index+":"+xp;
        if(signature==lastSignature){AlignPortraits();return;}lastSignature=signature;
        bool protagonist=identity==DialogueCharacterId.XiaoPeng;
        bool narration=identity==DialogueCharacterId.Narrator;
        leftSpeaking=protagonist&&!narration;rightSpeaking=!protagonist&&!narration;
        Set(left,protagonist ? LineKey(line,age) : xp,ref leftCrop);
        left.enabled=!narration&&left.sprite;
        left.color=protagonist?Color.white:new Color(.52f,.56f,.63f,1);
        var partner=protagonist?PartnerLine(data,index):line;
        Set(right,LineKey(partner,age),ref rightCrop);
        right.enabled=!narration&&right.sprite;
        right.color=protagonist?new Color(.52f,.56f,.63f,1):Color.white;
        AlignPortraits();
    }
    void AlignPortraits()
    {
        float blend=1-Mathf.Exp(-GameplayClock.Delta*14);
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
        var r=(RectTransform)image.transform.parent;
        float ratio=PortraitRatio(image);
        r.anchorMin=r.anchorMax=new Vector2(x,.065f);r.pivot=new Vector2(.5f,0);
        r.anchoredPosition=Vector2.zero;r.sizeDelta=new Vector2(height*ratio,height);
        // All busts share one lower edge. Only the speaking side grows, by 2.5%.
        r.localScale=Vector3.one*emphasis;
    }
    static DialogueAge ContextAge()
    {
        string scene=SceneManager.GetActiveScene().name;
        var controls=PersistentRuntime.Instance?PersistentRuntime.Instance.GetComponent<GameControlManager>():null;
        return scene=="Scene03_Childhood"?DialogueAge.Child:
            controls&&!controls.AllowsAdultOutfits&&scene=="Scene04_School"?DialogueAge.Youth:DialogueAge.Adult;
    }
    static DialogueLine PartnerLine(DialogueData data,int index)
    {
        // Keep the last actual counterpart within THIS conversation; never carry a stranger
        // from a previous DialogueData. A first protagonist line may preview this conversation's partner.
        for(int i=index-1;i>=0;i--)if(IsPartner(data.lines[i]))return data.lines[i];
        for(int i=index+1;i<data.lines.Length;i++)if(IsPartner(data.lines[i]))return data.lines[i];
        return null;
    }
    static bool IsPartner(DialogueLine line)
    {
        var id=DialogueIdentity.Resolve(line);
        return id!=DialogueCharacterId.Narrator&&id!=DialogueCharacterId.XiaoPeng;
    }
    static string LineKey(DialogueLine line,DialogueAge context)
    {
        if(line==null||line.portraitMode==DialoguePortraitMode.None)return null;
        if(!string.IsNullOrEmpty(line.portraitKey))return line.portraitKey;
        // A silhouette must have its own approved resource; it cannot reuse a named person's face.
        if(line.portraitMode==DialoguePortraitMode.Silhouette)return null;
        return DialogueIdentity.PortraitKey(DialogueIdentity.Resolve(line),line.age==DialogueAge.Context?context:line.age);
    }
    static void Set(Image output,string key,ref Sprite cropped)
    {
        if(cropped){Object.Destroy(cropped);cropped=null;}
        // Closeup source canvases have different transparent margins. Dialogue-specific
        // sprite regions use visible bounds so the shared height and baseline are real.
        var sprite=string.IsNullOrEmpty(key)?null:DemoArtCatalog.Find(key+":portrait")??DemoArtCatalog.Find(key);output.sprite=sprite;
        if(!sprite||key.Contains("CLOSEUP"))return;
        var r=sprite.rect;r.y+=r.height*.43f;r.height*=.57f;
        cropped=Sprite.Create(sprite.texture,r,new Vector2(.5f,0),sprite.pixelsPerUnit,0,SpriteMeshType.FullRect);output.sprite=cropped;
    }
    void OnDestroy(){if(leftCrop)Destroy(leftCrop);if(rightCrop)Destroy(rightCrop);if(root)Destroy(root);}
}
