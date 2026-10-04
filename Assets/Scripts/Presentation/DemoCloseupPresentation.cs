using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Modal view only. Collection is committed by the original interactables.</summary>
public sealed class DemoCloseupPresentation : MonoBehaviour
{
    RectTransform canvas,page;
    GameControlManager controls;
    bool ownsLock,busy;
    public bool IsOpen=>canvas&&canvas.gameObject.activeSelf;
    TMP_FontAsset Font=>PersistentRuntime.Instance?PersistentRuntime.Instance.GetComponentInChildren<TMP_Text>(true)?.font:null;
    public static DemoCloseupPresentation For(GameObject owner)
    {var view=owner.GetComponent<DemoCloseupPresentation>();return view?view:owner.AddComponent<DemoCloseupPresentation>();}
    void Build(string title,bool lockInput)
    {
        if(canvas)Destroy(canvas.gameObject);
        controls=PersistentRuntime.Instance?PersistentRuntime.Instance.GetComponent<GameControlManager>():null;
        if(lockInput&&controls&&!controls.IsLockedBy(ControlLockReason.CloseUp)){controls.AcquireLock(ControlLockReason.CloseUp);ownsLock=true;}
        // A screen-space modal must not inherit a puddle's flattened world scale.
        // The owner still closes/destroys it on disable, including scene unload.
        canvas=RuntimePanelUI.Canvas("DemoCloseup",null,175);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(canvas.gameObject,gameObject.scene);
        RuntimePanelUI.Panel("Backdrop",canvas,Vector2.zero,Vector2.one,new Color(.035f,.05f,.07f,.96f));
        page=RuntimePanelUI.Panel(title,canvas,new Vector2(.10f,.12f),new Vector2(.90f,.90f),new Color(.11f,.15f,.18f)).rectTransform;
        DemoPaintedFrame.Add(page,new Color(.65f,.66f,.59f));
        RuntimePanelUI.Text(title,canvas,Font,new Vector2(.10f,.91f),new Vector2(.75f,.98f),27).alignment=TextAlignmentOptions.Left;
    }
    Image Art(string name,string key,Transform parent,Vector2 min,Vector2 max)
    {
        var view=RuntimePanelUI.Panel(name,parent,min,max,Color.white,false);view.sprite=DemoArtCatalog.Find(key);view.preserveAspect=true;view.enabled=view.sprite;return view;
    }
    public void OpenMailbox(NewspaperFragmentInteractable fragment,GateKeyInteractable key)
    {
        if(IsOpen)return;
        Build("邮箱",true);busy=false;
        RuntimePanelUI.Panel("BoxInterior",page,new Vector2(.16f,.12f),new Vector2(.84f,.91f),new Color(.065f,.08f,.09f));
        var keyView=Art("KeyUnderPaper","SPR_PICKUPS_KEYS_FISH_COIN:gate_key",page,new Vector2(.42f,.22f),new Vector2(.64f,.49f));
        var keyButton=keyView.gameObject.AddComponent<Button>();keyButton.targetGraphic=keyView;keyView.raycastTarget=true;
        var paper=Art("FragmentAboveKey","INVENTORY:newspaper_fragment_01:default",page,new Vector2(.23f,.24f),new Vector2(.76f,.84f));
        if(!paper.sprite)paper.sprite=DemoArtCatalog.Find("FILE_NEWSPAPER_FRAGMENTS:0");paper.enabled=paper.sprite;
        paper.raycastTarget=true;var paperButton=paper.gameObject.AddComponent<Button>();paperButton.targetGraphic=paper;
        bool taken=fragment==null||fragment.IsCollected;paper.gameObject.SetActive(!taken);keyView.gameObject.SetActive(taken&&key&&!key.IsCollected);
        var hint=RuntimePanelUI.Text(taken?"纸张下露出一把钥匙，点击拾取。":"点击邮箱内的文件碎片。",canvas,Font,new Vector2(.15f,.025f),new Vector2(.82f,.105f),27);
        paperButton.onClick.AddListener(()=>{
            if(busy||!fragment||fragment.IsCollected)return;
            fragment.CollectFromCloseup();if(!fragment.IsCollected)return;
            paper.gameObject.SetActive(false);StartCoroutine(AfterFragment(keyView,key,hint));
        });
        keyButton.onClick.AddListener(()=>{
            if(busy||!key||key.IsCollected||fragment&&!fragment.IsCollected)return;
            key.CollectFromCloseup();if(key.IsCollected)Close();
        });
        RuntimePanelUI.Button("返回 [Esc]",canvas,Font,new Vector2(.78f,.91f),new Vector2(.92f,.98f),()=>{if(!busy)Close();});
    }
    IEnumerator AfterFragment(Image keyView,GateKeyInteractable key,TMP_Text hint)
    {
        busy=true;var dialogue=PersistentRuntime.Instance.GetComponentInChildren<DialogueManager>(true);
        if(dialogue&&dialogue.IsDialogueActive()){
            canvas.gameObject.SetActive(false);
            while(dialogue&&dialogue.IsDialogueActive())yield return null;
            yield return null;if(canvas)canvas.gameObject.SetActive(true);
        }
        keyView.gameObject.SetActive(key&&!key.IsCollected);hint.text="纸张下露出一把钥匙，点击拾取。";busy=false;
    }
    public IEnumerator Puddle()
    {
        Build("雨水中的倒影",false);busy=true;
        page.gameObject.AddComponent<RectMask2D>();
        var face=Art("IrregularWaterReflection","P03_Puddle",page,Vector2.zero,Vector2.one);
        // One authored water/reflection image, never the regular dialogue portrait.
        if(face.sprite){var fit=face.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;fit.aspectRatio=face.sprite.rect.width/face.sprite.rect.height;}
        face.color=new Color(.30f,.34f,.38f,1);
        bool advance=false;
        var next=RuntimePanelUI.Button("继续 [E / 空格]",canvas,Font,new Vector2(.68f,.025f),new Vector2(.90f,.105f),()=>advance=true);
        next.name="PuddleContinue";next.gameObject.SetActive(false);
        yield return GameplayClock.Wait(.75f);
        var flash=RuntimePanelUI.Panel("ReflectionFlash",canvas,Vector2.zero,Vector2.one,new Color(.86f,.88f,.84f,.7f),false);
        yield return GameplayClock.Wait(.065f);flash.color=new Color(.03f,.02f,.035f,.9f);
        face.color=Color.white;
        yield return GameplayClock.Wait(.075f);Destroy(flash.gameObject);
        yield return GameplayClock.Wait(.75f);
        // Require a new press after the reveal; a held interaction must not skip it.
        while(IsOpen&&(GameplayInput.GetKey(KeyCode.E)||GameplayInput.GetKey(KeyCode.Space)||GameplayInput.GetMouseButton(0)))yield return null;
        if(IsOpen)next.gameObject.SetActive(true);
        while(IsOpen&&!advance&&!GameplayInput.GetKeyDown(KeyCode.E)&&!GameplayInput.GetKeyDown(KeyCode.Space))yield return null;
        Close();yield return null;
    }
    public void Close()
    {
        StopAllCoroutines();busy=false;if(canvas){canvas.gameObject.SetActive(false);Destroy(canvas.gameObject);canvas=null;}
        if(ownsLock&&controls)controls.ReleaseLock(ControlLockReason.CloseUp);ownsLock=false;
    }
    void Update(){if(IsOpen&&!busy&&GameplayInput.GetKeyDown(KeyCode.Escape))Close();}
    void OnDisable()=>Close();
}
