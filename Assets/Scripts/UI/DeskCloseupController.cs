using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class DeskCloseupController : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private ItemData oilLamp;
    private GameControlManager controls; private StoryStateManager story; private InventoryManager inventory;
    private GameObject canvas,desktop,lockPanel,computerPanel;
    private TMP_Text lockMessage,computerMessage;
    private TMP_InputField password;
    private FourDigitLock[] wheels;
    public bool IsOpen => canvas!=null && canvas.activeSelf;
    public string Page { get; private set; }="";
    public string LockMessage => lockMessage.text;
    public string ComputerMessage => computerMessage.text;
    private void Start()
    {
        controls=GetComponent<GameControlManager>(); story=GetComponent<StoryStateManager>(); inventory=GetComponent<InventoryManager>();
        Build(); canvas.SetActive(false);
        SceneManager.activeSceneChanged+=SceneChanged;
    }
    private void Build()
    {
        var root=RuntimePanelUI.Canvas("DeskAndOutfitCanvas",transform,130); canvas=root.gameObject;
        RuntimePanelUI.Panel("Backdrop",root,Vector2.zero,Vector2.one,new Color(.045f,.055f,.057f,.99f));
        desktop=PageRoot("Desktop",root); lockPanel=PageRoot("Drawer",root); computerPanel=PageRoot("Computer",root);
        var desk=RuntimePanelUI.Panel("WoodDesktop",desktop.transform,new Vector2(.12f,.17f),new Vector2(.88f,.77f),new Color(.26f,.19f,.13f));
        RuntimePanelUI.Button("电脑",desk.transform,font,new Vector2(.5f,.38f),new Vector2(.85f,.9f),()=>ShowPage("Computer"));
        RuntimePanelUI.Button("抽屉",desk.transform,font,new Vector2(.08f,.1f),new Vector2(.45f,.38f),()=>ShowPage("Drawer"));
        RuntimePanelUI.Button("[Esc] 退出",desktop.transform,font,new Vector2(.7f,.06f),new Vector2(.88f,.13f),Close);
        wheels=new FourDigitLock[4];
        for(int i=0;i<4;i++) {
            float x=.28f+i*.115f;
            var plate=RuntimePanelUI.Panel("Digit"+i,lockPanel.transform,new Vector2(x,.39f),new Vector2(x+.09f,.62f),new Color(.22f,.24f,.22f));
            var label=RuntimePanelUI.Text("0",plate.transform,font,Vector2.zero,Vector2.one,62);
            wheels[i]=plate.gameObject.AddComponent<FourDigitLock>(); wheels[i].Bind(label);
        }
        RuntimePanelUI.Text("滚轮 / 上下拖动",lockPanel.transform,font,new Vector2(.28f,.67f),new Vector2(.72f,.75f),24);
        lockMessage=RuntimePanelUI.Text("",lockPanel.transform,font,new Vector2(.25f,.24f),new Vector2(.75f,.34f));
        RuntimePanelUI.Button("确认",lockPanel.transform,font,new Vector2(.52f,.13f),new Vector2(.7f,.21f),ConfirmLock);
        RuntimePanelUI.Button("返回",lockPanel.transform,font,new Vector2(.3f,.13f),new Vector2(.48f,.21f),()=>ShowPage("Desktop"));
        var field=RuntimePanelUI.Panel("Password",computerPanel.transform,new Vector2(.3f,.48f),new Vector2(.7f,.58f),new Color(.15f,.2f,.18f));
        var inputText=RuntimePanelUI.Text("",field.transform,font,new Vector2(.04f,.05f),new Vector2(.96f,.95f));
        password=field.gameObject.AddComponent<TMP_InputField>(); password.textViewport=field.rectTransform;
        password.textComponent=(TextMeshProUGUI)inputText; password.contentType=TMP_InputField.ContentType.Password;
        password.characterLimit=128; password.onSubmit.AddListener(_=>SubmitComputer());
        computerMessage=RuntimePanelUI.Text("",computerPanel.transform,font,new Vector2(.3f,.30f),new Vector2(.7f,.4f));
        RuntimePanelUI.Button("确认",computerPanel.transform,font,new Vector2(.52f,.18f),new Vector2(.7f,.27f),SubmitComputer);
        RuntimePanelUI.Button("返回",computerPanel.transform,font,new Vector2(.3f,.18f),new Vector2(.48f,.27f),()=>ShowPage("Desktop"));
        RuntimePanelUI.Text("电脑",computerPanel.transform,font,new Vector2(.3f,.65f),new Vector2(.7f,.75f),38);

    }
    private GameObject PageRoot(string name,Transform root)=>RuntimePanelUI.Rect(name,root,Vector2.zero,Vector2.one).gameObject;
    public bool OpenDesk() => Open("Desktop");
    public bool OpenWardrobe()
    {
        if(controls==null || controls.IsLocked() || IsOpen)return false;
        bool first=story.Set(StoryFlag.OverallsUnlocked);
        GetComponent<StoryNoticeUI>().Show(first?"获得了服装「背带装」。":"衣柜已经空了。");
        return true;
    }
    private bool Open(string page) { if(controls==null || controls.IsLocked() || IsOpen)return false; controls.AcquireLock(ControlLockReason.CloseUp); canvas.SetActive(true); ShowPage(page);return true; }
    public void ShowPage(string page)
    {
        if(!IsOpen)return; Page=page;
        desktop.SetActive(page=="Desktop"); lockPanel.SetActive(page=="Drawer"); computerPanel.SetActive(page=="Computer");
        if(page=="Computer"){ computerMessage.text=""; password.text=""; password.ActivateInputField(); }
        if(page=="Drawer")lockMessage.text="";
    }

    public void SubmitComputer(){ if(IsOpen && Page=="Computer")computerMessage.text="密码错误"; }
    public void ConfirmLock()
    {
        if(!IsOpen || Page!="Drawer")return;
        string code=""; foreach(var wheel in wheels)code+=wheel.Value;
        if(code!="0715") { lockMessage.text="密码不对。";return; }
        if(!story.Has(StoryFlag.Scene03BirthdayKnown)){ lockMessage.text="锁没有反应。";return; }
        if(story.Has(StoryFlag.DrawerOpened)){lockMessage.text="抽屉已经打开，小油灯已取出。";return;}
        if(oilLamp==null){Debug.LogWarning("Drawer needs its oil lamp ItemData.",this);return;}
        inventory.AddItem(oilLamp); if(inventory.HasItem(oilLamp)){story.Set(StoryFlag.DrawerOpened);story.Set(StoryFlag.OilLampObtained); lockMessage.text="获得了小油灯。";}
    }
    public void Back() { if(Page=="Drawer" || Page=="Computer")ShowPage("Desktop"); else Close(); }
    public void Close()
    {
        if(!IsOpen)return;
        bool desk=Page!="Outfit"; canvas.SetActive(false); Page="";
        if(desk)story.Set(StoryFlag.DeskEventTriggered);
        controls.ReleaseLock(ControlLockReason.CloseUp);
    }
    private void Update() { if(IsOpen && Input.GetKeyDown(KeyCode.Escape))Back(); }
    private void OnDisable() { if(controls!=null && IsOpen)Close(); }
    private void SceneChanged(Scene oldScene,Scene newScene){Close();}
    private void OnDestroy(){SceneManager.activeSceneChanged-=SceneChanged;}
}
