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
        selected=data;outfitName.text=data.outfitName;description.text=data.description;
        preview.sprite=data.preview;preview.enabled=data.preview!=null;preview.preserveAspect=true;
        bool equipped=story.Outfit==data.outfit;
        wearLabel.text=equipped?"已穿戴":"穿上";wearButton.interactable=!equipped;
    }
    public void WearSelected()
    {
        if(story!=null && selected!=null && story.Outfit!=selected.outfit)story.EquipOutfit(selected);
    }
    private void OnDestroy(){if(story!=null)story.Changed-=Refresh;}
}
