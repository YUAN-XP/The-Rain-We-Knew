using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Catalog and visuals are data; StoryStateManager remains the only ownership/equipment truth.
public class OutfitInventoryPage : MonoBehaviour
{
    [SerializeField] private OutfitData[] catalog;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Transform list;
    [SerializeField] private TMP_Text outfitName, description, wearLabel;
    [SerializeField] private Image preview;
    [SerializeField] private Image wearingPreview;
    [SerializeField] private Button wearButton;
    private StoryStateManager story;
    private OutfitData selected;
    // Runtime installation also covers Scene01's serialized PersistentRuntime instance.
    public void RegisterOutfit(OutfitData data)
    {
        if(data==null)return;
        var entries=new System.Collections.Generic.List<OutfitData>(catalog??new OutfitData[0]);
        foreach(var entry in entries)if(entry==data || (entry!=null && entry.outfit==data.outfit))return;
        entries.Add(data);catalog=entries.ToArray();
        if(story!=null)Refresh();
    }
    private void Start()
    {
        ConfigureArt();
        story=FindAnyObjectByType<StoryStateManager>();
        if(story==null){Debug.LogWarning("OutfitInventoryPage needs StoryStateManager.",this);return;}
        story.Changed+=Refresh;wearButton.onClick.AddListener(WearSelected);Refresh();
    }
    private void OnEnable(){if(story!=null)Refresh();}
    private void Refresh()
    {
        if(!gameObject.activeInHierarchy)return;
        foreach(Transform child in list){child.gameObject.SetActive(false);Destroy(child.gameObject);}
        int index=0;
        foreach(var data in catalog)
        {
            if(!story.OwnsOutfit(data))continue;
            var button=RuntimePanelUI.Button(data.outfitName,list,font,new Vector2(.02f,1-(index+1)*.18f),new Vector2(.98f,.98f-index*.18f),()=>Select(data));
            button.GetComponentInChildren<TMP_Text>().fontSize=24;
            if(selected==null || !story.OwnsOutfit(selected))selected=data;
            index++;
        }
        Select(selected);
    }
    public void Select(OutfitData data)
    {
        if(!story.OwnsOutfit(data))return;
        selected=data;outfitName.text="穿戴效果";description.text=data.description;
        preview.sprite=data.clothingImage;preview.enabled=data.clothingImage!=null;preview.preserveAspect=true;
        wearingPreview.sprite=data.preview;wearingPreview.enabled=data.preview!=null;wearingPreview.preserveAspect=true;
        bool equipped=story.Outfit==data.outfit;
        wearLabel.text=equipped?"已穿戴":"穿上";wearButton.interactable=!equipped;
    }
    public void WearSelected()
    {
        if(story!=null && selected!=null && story.Outfit!=selected.outfit)story.EquipOutfit(selected);
    }
    static void SetRect(Transform t,Vector2 min,Vector2 max){if(t is RectTransform r){r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;}}
    void ConfigureArt()
    {
        // These fields live inside separate authored panels. Lay out the panels
        // first, then their children; page coordinates on children shrink them twice.
        SetRect(list,new Vector2(.015f,.035f),new Vector2(.205f,.97f));
        SetRect(preview.transform.parent,new Vector2(.235f,.055f),new Vector2(.595f,.945f));
        SetRect(preview.transform,new Vector2(.06f,.22f),new Vector2(.94f,.94f));
        SetRect(outfitName.transform.parent,new Vector2(.625f,.04f),new Vector2(.985f,.96f));
        SetRect(outfitName.transform,new Vector2(.045f,.9f),new Vector2(.955f,.98f));
        description.transform.SetParent(preview.transform.parent,false);
        SetRect(description.transform,new Vector2(.07f,.045f),new Vector2(.93f,.17f));
        SetRect(wearButton.transform,new Vector2(.16f,.045f),new Vector2(.84f,.15f));
        if(!wearingPreview)wearingPreview=RuntimePanelUI.Panel("WearingPreview",outfitName.transform.parent,new Vector2(.08f,.185f),new Vector2(.92f,.89f),Color.white,false);
        foreach(var panel in new[]{list,preview.transform.parent,outfitName.transform.parent})
            if(panel.GetComponent<Image>() is Image image)image.color=new Color(.94f,.925f,.87f);
        if(wearButton.targetGraphic)wearButton.targetGraphic.color=new Color(.26f,.32f,.33f);
        var colors=wearButton.colors;colors.disabledColor=new Color(.50f,.56f,.54f,1);wearButton.colors=colors;
        wearLabel.color=new Color(.94f,.925f,.87f);wearLabel.fontSize=28;
        description.textWrappingMode=TextWrappingModes.Normal;
        foreach(var text in new[]{outfitName,description}){text.color=new Color(.16f,.20f,.23f);text.alignment=TextAlignmentOptions.TopLeft;text.fontSize=text==outfitName?34:28;}
    }
    private void OnDestroy(){if(story!=null)story.Changed-=Refresh;}
}
