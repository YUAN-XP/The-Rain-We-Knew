using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Scene-local presentation. Persistent rewards live only in StoryState/Inventory.
public class ChildhoodActivityUI : MonoBehaviour
{
    Scene03Chapter chapter;
    RectTransform canvas, panel, stage, slot, launchPad;
    TMP_Text heading, body, status;
    GameObject page;
    bool ownsLock, flying, retrying, reconstructing;
    Button continueStory;
    RectTransform catPortrait, closeupCoin, pullGuide;
    public bool Reconstructing=>reconstructing;
    public bool CanDragCards=>IsOpen && mode=="城门前的小骑士" && !reconstructing && !retrying;
    int act, attempts;
    string mode;
    readonly bool[] targetsHit=new bool[4];
    readonly Vector2[] targets={new Vector2(330,55),new Vector2(535,155),new Vector2(735,90),new Vector2(925,195)};
    RectTransform[] targetViews=new RectTransform[4];
    RectTransform ring;
    Slider angleSlider,powerSlider;
    RectTransform[] trajectoryDots;
    public bool IsOpen=>page!=null && page.activeSelf;
    public int Act=>act;
    public int Attempts=>attempts;
    public bool Flying=>flying || retrying;
    public int HitCount {get {int n=0;foreach(bool h in targetsHit)if(h)n++;return n;}}
    public bool CanPay=>chapter.Inventory.HasItem(chapter.content.coinItem)&&!chapter.Story.Has(StoryFlag.GameCoinSpent);
    public RectTransform StorySlot=>slot;
    public void Initialize(Scene03Chapter owner){chapter=owner;canvas=RuntimePanelUI.Canvas("ChildhoodActivitiesCanvas",transform,130);}
    bool Open(string name)
    {
        if(chapter==null||IsOpen||chapter.Controls.IsLocked())return false;
        chapter.Controls.AcquireLock(ControlLockReason.CloseUp);ownsLock=true;BuildPage(name);return true;
    }
    void BuildPage(string name)
    {
        if(page!=null){page.SetActive(false);Destroy(page);}
        panel=RuntimePanelUI.Panel(name,canvas,new Vector2(.06f,.06f),new Vector2(.94f,.94f),new Color(.16f,.20f,.22f,1)).rectTransform;
        page=panel.gameObject;mode=name;body=null;status=null;stage=null;
        heading=Text(name,panel,.03f,.87f,.85f,.98f,34);
        RuntimePanelUI.Button("离开",panel,chapter.font,new Vector2(.87f,.9f),new Vector2(.97f,.97f),Close);
    }
    TMP_Text Text(string t,Transform p,float x,float y,float xx,float yy,float size=26)=>RuntimePanelUI.Text(t,p,chapter.font,new Vector2(x,y),new Vector2(xx,yy),size);
    public void OpenBoard()
    {
        if(!Open("街坊公告栏"))return;
        for(int i=0;i<chapter.content.notices.Length;i++)
        {
            float x=.035f+(i%3)*.32f,y=i<3?.49f:.07f;
            var paper=RuntimePanelUI.Panel("公告 "+(i+1),panel,new Vector2(x,y),new Vector2(x+.29f,y+.35f),new Color(.88f,.84f,.70f));
            paper.transform.localRotation=Quaternion.Euler(0,0,i%2==0?1.2f:-1.4f);
            var text=Text(chapter.content.notices[i],paper.transform,.05f,.05f,.95f,.95f,24);text.color=new Color(.17f,.19f,.16f);
        }
    }
    RectTransform Art(string name,Transform parent,ChildhoodIllustration.Picture picture,float x,float y,float xx,float yy)
    {
        var r=RuntimePanelUI.Rect(name,parent,new Vector2(x,y),new Vector2(xx,yy));
        var graphic=r.gameObject.AddComponent<ChildhoodIllustration>();graphic.picture=picture;graphic.raycastTarget=false;return r;
    }
    public void OpenCat()
    {
        if(chapter.Story.Has(StoryFlag.CatFed)&&chapter.Story.Has(StoryFlag.GameCoinObtained))
        {PersistentRuntime.Instance.GetComponent<StoryNoticeUI>().Show("小猫吃得正香。");return;}
        if(!Open("墙根的小猫"))return;
        stage=RuntimePanelUI.Panel("斑驳墙根",panel,new Vector2(.09f,.26f),new Vector2(.91f,.85f),new Color(.73f,.77f,.65f)).rectTransform;
        RuntimePanelUI.Panel("阴凉地面",stage,Vector2.zero,new Vector2(1,.32f),new Color(.48f,.55f,.43f),false);
        for(int i=0;i<5;i++)RuntimePanelUI.Panel("砖缝",stage,new Vector2(0,.4f+i*.13f),new Vector2(1,.407f+i*.13f),new Color(.61f,.67f,.56f),false);
        closeupCoin=Art("藏着的游戏币",stage,ChildhoodIllustration.Picture.Coin,.43f,.07f,.49f,.19f);closeupCoin.gameObject.SetActive(chapter.Story.Has(StoryFlag.CatFed));
        catPortrait=Art("小猫近景",stage,ChildhoodIllustration.Picture.Cat,.22f,.09f,.75f,.92f);
        body=Text("一只很可爱的小猫，怀里护着什么东西。",panel,.08f,.13f,.92f,.24f,29);
        if(chapter.Story.Has(StoryFlag.CatFed))body.text="小猫低头吃着鱼仔，身旁露出一枚游戏币。";
        bool canFeed=chapter.Inventory.HasItem(chapter.content.fishItem)&&chapter.Story.Has(StoryFlag.FishObtained);
        if(canFeed||chapter.Story.Has(StoryFlag.CatFed))RuntimePanelUI.Button(chapter.Story.Has(StoryFlag.CatFed)?"拾起游戏币":"把鱼仔放在一旁",panel,chapter.font,new Vector2(.34f,.035f),new Vector2(.66f,.115f),()=>StartCoroutine(FeedCat()));
    }
    IEnumerator FeedCat()
    {
        if(retrying||chapter.Story.Has(StoryFlag.GameCoinObtained))yield break;
        bool fed=chapter.Story.Has(StoryFlag.CatFed);
        if(!fed&&!chapter.Inventory.HasItem(chapter.content.fishItem))yield break;
        retrying=true;
        foreach(var button in page.GetComponentsInChildren<Button>()){button.interactable=false;if(button.name!="离开")button.gameObject.SetActive(false);}
        if(!fed)
        {
            chapter.Inventory.RemoveItem(chapter.content.fishItem);chapter.Story.Set(StoryFlag.CatFed);
            body.text="把小鱼仔放在一旁。";closeupCoin.gameObject.SetActive(true);chapter.coinVisual.SetActive(true);
            Vector3 from=chapter.catVisual.transform.localPosition;Vector2 portraitFrom=catPortrait.anchoredPosition;
            for(float t=0;t<.9f;t+=Time.unscaledDeltaTime)
            {float f=Mathf.SmoothStep(0,1,t/.9f);chapter.catVisual.transform.localPosition=Vector3.Lerp(from,new Vector3(2,0,0),f);catPortrait.anchoredPosition=portraitFrom+Vector2.right*(stage.rect.width*.23f*f);yield return null;}
            chapter.catVisual.transform.localPosition=new Vector3(2,0,0);
            body.text="小猫移开了，游戏币露了出来。";yield return new WaitForSecondsRealtime(1);
        }
        page.SetActive(false);yield return chapter.Say(chapter.content.feedCat);
        if(!chapter.Story.Has(StoryFlag.GameCoinObtained)){chapter.Inventory.AddItem(chapter.content.coinItem);chapter.Story.Set(StoryFlag.GameCoinObtained);}
        chapter.coinVisual.SetActive(false);retrying=false;Close();
    }
    public void OpenStory()
    {
        if(!Open("城门前的小骑士"))return;
        act=0;reconstructing=false;DrawAct();
    }
    ChildhoodIllustration.Picture Character(int index)=>index==0?ChildhoodIllustration.Picture.Princess:index==1?ChildhoodIllustration.Picture.Knight:ChildhoodIllustration.Picture.Rebel;
    void DrawAct()
    {
        if(stage!=null){stage.gameObject.SetActive(false);Destroy(stage.gameObject);}
        if(continueStory!=null)Destroy(continueStory.gameObject);
        heading.text="城门前的小骑士 · 第 "+(act+1)+" 幕";
        if(body==null)body=Text("",panel,.06f,.71f,.94f,.86f,27);
        string introduction=chapter.content.storyActs[act].Split('\n')[0];
        int pause=introduction.IndexOf('，');
        body.text=pause>=0?introduction.Substring(0,pause)+"……":"把角色放到两方之间，让故事继续。";
        stage=RuntimePanelUI.Panel("木偶舞台",panel,new Vector2(.06f,.14f),new Vector2(.94f,.70f),new Color(.77f,.82f,.78f)).rectTransform;
        RuntimePanelUI.Panel("舞台地板",stage,Vector2.zero,new Vector2(1,.36f),new Color(.52f,.36f,.25f),false);
        for(int i=0;i<8;i++)RuntimePanelUI.Panel("木纹",stage,new Vector2(i*.125f,0),new Vector2(i*.125f+.004f,.36f),new Color(.38f,.26f,.23f),false);
        Art("纸景城门",stage,ChildhoodIllustration.Picture.Castle,.09f,.36f,.36f,.93f);
        for(int i=0;i<4;i++)Art("围观木偶",stage,ChildhoodIllustration.Picture.Rebel,.73f+i*.045f,.38f,.81f+i*.045f,.68f);
        foreach(float x in new[]{0f,.94f}){RuntimePanelUI.Panel("舞台红帘",stage,new Vector2(x,.30f),new Vector2(x+.06f,1),new Color(.54f,.19f,.27f),false);}
        RuntimePanelUI.Panel("上帷幕",stage,new Vector2(0,.92f),Vector2.one,new Color(.62f,.23f,.30f),false);
        foreach(var caption in new[]{Text("宫门",stage,.10f,.77f,.33f,.88f,22),Text("城门前",stage,.39f,.79f,.64f,.90f,22),Text("人群",stage,.72f,.77f,.94f,.88f,22)})caption.color=new Color(.29f,.32f,.28f);
        if(act>0)Art("已入场的公主",stage,Character(0),.17f,.37f,.29f,.76f);
        if(act>1)Art("已入场的反叛者",stage,Character(2),.71f,.35f,.83f,.75f);
        float sx=act==0?.15f:act==1?.69f:.43f;
        slot=RuntimePanelUI.Panel("角色位置",stage,new Vector2(sx,.35f),new Vector2(sx+.16f,.76f),new Color(.97f,.86f,.53f,.44f)).rectTransform;
        var outline=slot.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.98f,.83f,.43f);outline.effectDistance=new Vector2(3,-3);
        Text("· · ·",slot,.1f,0,.9f,.16f,25);
        for(int i=0;i<3;i++)
        {
            string label=new[]{"公主","骑士","反叛者"}[i];
            var card=RuntimePanelUI.Panel(label+"卡",stage,new Vector2(.15f+i*.255f,.018f),new Vector2(.34f+i*.255f,.29f),new Color(.96f,.87f,.68f)).rectTransform;
            var shadow=card.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.15f,.12f,.14f,.6f);shadow.effectDistance=new Vector2(5,-6);
            Art(label+"木偶",card,Character(i),.08f,.04f,.49f,.94f);Text(label,card,.45f,.08f,.96f,.91f,28).color=new Color(.28f,.23f,.23f);
            var drag=card.gameObject.AddComponent<ChildhoodDragHandle>();drag.owner=this;drag.cardIndex=i;
        }
        if(status==null)status=Text("",panel,.12f,.02f,.88f,.115f,24);
        status.text="让角色走上舞台 · 拖动卡片，放错了也可以再试";
    }
    public bool DropCard(int index,Vector2 screenPoint)
    {
        if(!CanDragCards)return false;
        int expected=act==0?0:act==1?2:1;
        if(index!=expected||!RectTransformUtility.RectangleContainsScreenPoint(slot,screenPoint,null))
        {status.text="这位角色还没有到出场的时候，再想一想。";return false;}
        reconstructing=true;chapter.effects.PlayOneShot(chapter.content.hitSound,.22f);
        StartCoroutine(Reconstruct(index));return true;
    }
    IEnumerator Reconstruct(int index)
    {
        // Leave the snapped card visible before revealing the completed puppet scene.
        yield return new WaitForSecondsRealtime(.45f);
        foreach(var card in stage.GetComponentsInChildren<ChildhoodDragHandle>())card.gameObject.SetActive(false);
        slot.GetComponent<Image>().color=Color.clear;slot.GetComponent<Outline>().enabled=false;
        foreach(Transform c in slot)c.gameObject.SetActive(false);
        Art("定格角色",slot,Character(index),.08f,0,.92f,1);
        body.text=chapter.content.storyActs[act].Split('\n')[0];
        status.text="";
        yield return new WaitForSecondsRealtime(.6f);
        continueStory=RuntimePanelUI.Button("继续听书",panel,chapter.font,new Vector2(.37f,.025f),new Vector2(.63f,.115f),ContinueStory);
    }
    public void ContinueStory()
    {
        if(!reconstructing||retrying||continueStory==null)return;
        continueStory.interactable=false;Destroy(continueStory.gameObject);continueStory=null;
        reconstructing=false;act++;if(act<3)DrawAct();else StartCoroutine(FinishStory());
    }
    IEnumerator FinishStory()
    {
        retrying=true;page.SetActive(false);
        yield return chapter.Say(chapter.content.storytellerEnd);
        chapter.Story.Set(StoryFlag.StorytellerGameCompleted);retrying=false;Close();
    }
    public void OpenRingOffer()
    {
        if(!Open("街边套圈"))return;
        if(chapter.Story.Has(StoryFlag.CameraGentlemanCardObtained))
        {Text("相机先生卡片已经收好了。",panel,.1f,.3f,.9f,.75f,32);return;}
        Text(chapter.Story.Has(StoryFlag.GameCoinSpent)?"老板：上次的机会还在，要继续吗？":CanPay?"老板：一枚游戏币，套四个目标。要试试吗？":"老板：想试试吗？可以找父母兑换游戏币，或看看周围有没有。",panel,.12f,.47f,.88f,.78f,30);
        var yes=RuntimePanelUI.Button("是",panel,chapter.font,new Vector2(.23f,.2f),new Vector2(.46f,.35f),PayAndPlay);
        yes.interactable=CanPay||chapter.Story.Has(StoryFlag.GameCoinSpent);
        RuntimePanelUI.Button("否",panel,chapter.font,new Vector2(.54f,.2f),new Vector2(.77f,.35f),Close);
    }
    public void PayAndPlay()
    {
        if(!IsOpen||mode!="街边套圈"||chapter.Story.Has(StoryFlag.CameraGentlemanCardObtained))return;
        if(!chapter.Story.Has(StoryFlag.GameCoinSpent))
        {if(!CanPay)return;chapter.Inventory.RemoveItem(chapter.content.coinItem);chapter.Story.Set(StoryFlag.GameCoinSpent);}
        BuildPage("套圈挑战");body=null;status=null;
        Text("拖住左下方的圈，向左下拉开后松手投掷；也可调节角度与力度，再点投掷。",panel,.04f,.79f,.96f,.89f,24);
        stage=RuntimePanelUI.Panel("投掷场",panel,new Vector2(.04f,.26f),new Vector2(.96f,.78f),new Color(.69f,.77f,.65f)).rectTransform;
        RuntimePanelUI.Panel("木质摊面",stage,Vector2.zero,new Vector2(1,.08f),new Color(.48f,.32f,.24f),false);
        for(int stripe=0;stripe<12;stripe++)RuntimePanelUI.Panel("摊棚条纹",stage,new Vector2(stripe/12f,.91f),new Vector2((stripe+1)/12f,1),stripe%2==0?new Color(.83f,.43f,.32f):new Color(.95f,.86f,.64f),false);
        // A fixed logical field keeps the trajectory stable at any display resolution.
        var field=RuntimePanelUI.Rect("投掷坐标",stage,Vector2.zero,Vector2.zero);field.pivot=Vector2.zero;field.sizeDelta=new Vector2(1120,380);field.anchoredPosition=new Vector2(30,25);launchPad=field;
        for(int i=0;i<4;i++)
        {
            var target=RuntimePanelUI.Panel("目标 "+(i+1),field,Vector2.zero,Vector2.zero,new Color(.88f,.64f,.27f)).rectTransform;
            target.pivot=Vector2.one*.5f;target.sizeDelta=new Vector2(62,40);target.anchoredPosition=targets[i];targetViews[i]=target;
            Art("木偶奖品",target,ChildhoodIllustration.Picture.Prize,0,.0f,1,2.1f);
            Text((i+1).ToString(),target,-.05f,-.6f,1.05f,.1f,22).color=new Color(.25f,.24f,.20f);
            var pedestal=RuntimePanelUI.Panel("台座",field,Vector2.zero,Vector2.zero,new Color(.42f,.36f,.28f)).rectTransform;
            pedestal.pivot=new Vector2(.5f,0);pedestal.sizeDelta=new Vector2(40,targets[i].y-20);pedestal.anchoredPosition=new Vector2(targets[i].x,0);pedestal.SetAsFirstSibling();
        }
        ring=RuntimePanelUI.Panel("拖动套圈",field,Vector2.zero,Vector2.zero,new Color(1,1,1,.001f)).rectTransform;
        ring.pivot=Vector2.one*.5f;ring.sizeDelta=new Vector2(55,30);ring.anchoredPosition=new Vector2(60,40);
        Art("竹圈",ring,ChildhoodIllustration.Picture.Ring,0,0,1,1);var drag=ring.gameObject.AddComponent<ChildhoodDragHandle>();drag.owner=this;drag.ring=true;
        pullGuide=RuntimePanelUI.Panel("拉弦力度",field,Vector2.zero,Vector2.zero,new Color(.97f,.77f,.31f,.8f),false).rectTransform;pullGuide.pivot=new Vector2(0,.5f);pullGuide.gameObject.SetActive(false);
        trajectoryDots=new RectTransform[32];
        for(int i=0;i<trajectoryDots.Length;i++){
            var dot=RuntimePanelUI.Panel("抛物线预览",field,Vector2.zero,Vector2.zero,new Color(.22f,.35f,.32f,.8f),false).rectTransform;
            dot.sizeDelta=new Vector2(6,6);trajectoryDots[i]=dot;
        }
        angleSlider=Slider("角度",.09f,15,75,45);powerSlider=Slider("力度",.52f,250,1050,650);
        RuntimePanelUI.Button("投掷",panel,chapter.font,new Vector2(.78f,.02f),new Vector2(.95f,.11f),()=>Launch(angleSlider.value,powerSlider.value));
        status=Text("",panel,.05f,.01f,.72f,.11f,24);ResetRound();
    }
    Slider Slider(string label,float x,float min,float max,float value)
    {
        Text(label,panel,x,.17f,x+.1f,.24f,23);
        var r=RuntimePanelUI.Panel(label,panel,new Vector2(x+.1f,.17f),new Vector2(x+.36f,.23f),new Color(.38f,.43f,.42f)).rectTransform;
        var handle=RuntimePanelUI.Panel("Handle",r,Vector2.zero,Vector2.one,new Color(.95f,.79f,.47f)).rectTransform;handle.sizeDelta=new Vector2(15,0);
        var s=r.gameObject.AddComponent<Slider>();s.handleRect=handle;s.targetGraphic=handle.GetComponent<Image>();s.minValue=min;s.maxValue=max;s.value=value;return s;
    }
    void ResetRound(){attempts=4;flying=false;for(int i=0;i<4;i++){targetsHit[i]=false;targetViews[i].GetComponent<Image>().color=new Color(.88f,.64f,.27f);}UpdateStatus();ring.anchoredPosition=new Vector2(60,40);}
    void UpdateStatus(){status.text="剩余 "+attempts+" 次  ·  已套中 "+HitCount+" / 4";}
    public void DragRing(Vector2 pull,bool release)
    {
        if(flying||retrying)return;
        Vector2 v=new Vector2(Mathf.Max(20,-pull.x),Mathf.Max(20,-pull.y));
        float angle=Mathf.Clamp(Mathf.Atan2(v.y,v.x)*Mathf.Rad2Deg,15,75),power=Mathf.Clamp(v.magnitude*4,250,1050);
        angleSlider.value=angle;powerSlider.value=power;
        status.text="剩余 "+attempts+" 次 · 向右上投出 · 力度 "+Mathf.RoundToInt(Mathf.InverseLerp(250,1050,power)*100)+"% · 松手投掷";
        pullGuide.gameObject.SetActive(!release);pullGuide.anchoredPosition=new Vector2(60,40);pullGuide.sizeDelta=new Vector2(Mathf.Min(pull.magnitude,180),5);pullGuide.localRotation=Quaternion.Euler(0,0,Mathf.Atan2(pull.y,pull.x)*Mathf.Rad2Deg);
        if(release)Launch(angle,power);
    }
    public bool Launch(float angle,float power)
    {
        if(mode!="套圈挑战"||!IsOpen||flying||retrying||attempts<=0)return false;
        StartCoroutine(Flight(Mathf.Clamp(angle,15,75),Mathf.Clamp(power,250,1050)));return true;
    }
    IEnumerator Flight(float angle,float power)
    {
        pullGuide.gameObject.SetActive(false);flying=true;attempts--;chapter.effects.PlayOneShot(chapter.content.throwSound,.25f);UpdateStatus();
        Vector2 start=new Vector2(60,40),v=new Vector2(Mathf.Cos(angle*Mathf.Deg2Rad),Mathf.Sin(angle*Mathf.Deg2Rad))*power;
        float t=0;int hit=-1;
        while(t<5)
        {
            t+=Time.unscaledDeltaTime;
            Vector2 p=start+v*t+Vector2.down*(250*t*t);ring.anchoredPosition=p;ring.localRotation=Quaternion.Euler(0,0,t*220);
            if(v.y-500*t<0)for(int i=0;i<4;i++)if(!targetsHit[i]&&Mathf.Abs(p.x-targets[i].x)<36&&Mathf.Abs(p.y-targets[i].y)<24){hit=i;break;}
            if(hit>=0||p.y<0||p.x>1150)break;yield return null;
        }
        if(hit>=0){targetsHit[hit]=true;targetViews[hit].GetComponent<Image>().color=new Color(.36f,.73f,.46f);chapter.effects.PlayOneShot(chapter.content.hitSound,.3f);status.text="套中了！ · 剩余 "+attempts+" 次";yield return Wobble(targetViews[hit]);}
        else {status.text="差一点…… · 剩余 "+attempts+" 次";yield return new WaitForSecondsRealtime(.5f);}
        yield return new WaitForSecondsRealtime(.3f);flying=false;ring.anchoredPosition=start;ring.localRotation=Quaternion.identity;UpdateStatus();
        if(HitCount==4){yield return Win();}
        else if(attempts==0)
        {
            retrying=true;chapter.effects.PlayOneShot(chapter.content.retrySound,.25f);
            yield return chapter.Fade(1,.22f);page.SetActive(false);
            yield return new WaitForSecondsRealtime(.3f);yield return chapter.Fade(0,.25f);
            yield return chapter.Say(chapter.content.ringRetry);
            ResetRound();page.SetActive(true);retrying=false;UpdateStatus();
        }
    }
    IEnumerator Wobble(RectTransform target)
    {
        for(float t=0;t<.45f;t+=Time.unscaledDeltaTime){target.localRotation=Quaternion.Euler(0,0,Mathf.Sin(t*40)*13*(1-t/.45f));target.localScale=Vector3.one*(1+.13f*Mathf.Sin(t/.45f*Mathf.PI));yield return null;}
        target.localRotation=Quaternion.identity;target.localScale=Vector3.one;
    }
    IEnumerator Win()
    {
        retrying=true;chapter.effects.PlayOneShot(chapter.content.successSound,.3f);
        chapter.ArchiveCameraGentleman();
        chapter.Story.Set(StoryFlag.CameraGentlemanCardObtained);page.SetActive(false);
        yield return chapter.Say(chapter.content.card);retrying=false;Close();
    }
    public void Close()
    {
        if(flying||retrying)return;
        StopAllCoroutines();reconstructing=false;continueStory=null;
        if(page!=null){page.SetActive(false);Destroy(page);}page=null;body=null;status=null;stage=null;
        if(ownsLock&&chapter!=null)chapter.Controls.ReleaseLock(ControlLockReason.CloseUp);ownsLock=false;
    }
    void Update()
    {
        if(IsOpen&&Input.GetKeyDown(KeyCode.Escape))Close();
        if(mode!="套圈挑战"||!IsOpen||trajectoryDots==null)return;
        float scale=Mathf.Min((stage.rect.width-60)/1120f,(stage.rect.height-50)/380f);
        launchPad.localScale=Vector3.one*scale;
        float angle=angleSlider.value*Mathf.Deg2Rad;
        Vector2 v=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*powerSlider.value;
        for(int i=0;i<trajectoryDots.Length;i++){
            float t=(i+1)*.10f;Vector2 p=new Vector2(60,40)+v*t+Vector2.down*(250*t*t);
            var dot=trajectoryDots[i];dot.gameObject.SetActive(!flying&&!retrying&&p.x<1100&&p.y>0&&p.y<360);dot.anchoredPosition=p;
        }
    }
    void OnDisable(){StopAllCoroutines();flying=retrying=false;Close();}
}
