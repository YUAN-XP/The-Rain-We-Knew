using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class InventoryUI
{
    static readonly Color Paper=new Color(.89f,.87f,.80f),Ink=new Color(.16f,.20f,.23f);
    static void ArtRect(Transform t,Vector2 min,Vector2 max)
    {if(t is RectTransform r){r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;}}
    static void InkText(TMP_Text t,float size){if(!t)return;t.fontSize=size;t.color=Ink;t.alignment=TextAlignmentOptions.TopLeft;}
    void ConfigureArtLayout()
    {
        if(!inventoryPanel)return;
        var rootCanvas=inventoryPanel.GetComponentInParent<Canvas>(true)?.rootCanvas;
        if(rootCanvas){rootCanvas.renderMode=RenderMode.ScreenSpaceOverlay;rootCanvas.worldCamera=null;rootCanvas.sortingOrder=120;}
        ArtRect(inventoryPanel.transform,new Vector2(.055f,.07f),new Vector2(.945f,.94f));
        var bg=inventoryPanel.GetComponent<Image>();if(bg)bg.color=Paper;
        DemoPaintedFrame.Add(inventoryPanel.transform,Ink,2.7f);
        var tabs=itemsTabButton?itemsTabButton.transform.parent:null;ArtRect(tabs,new Vector2(.025f,.86f),new Vector2(.975f,.975f));
        var buttons=new[]{itemsTabButton,keyItemsTabButton,filesTabButton,outfitsTabButton};var labels=new[]{"物品","重要物品","文件","服装"};
        for(int i=0;i<buttons.Length;i++)if(buttons[i]){
            ArtRect(buttons[i].transform,new Vector2(.012f+i*.246f,.04f),new Vector2(.235f+i*.246f,.96f));
            var label=buttons[i].GetComponentInChildren<TMP_Text>(true);if(label){label.text=labels[i];label.fontSize=32;label.color=Ink;}
            if(buttons[i].targetGraphic)buttons[i].targetGraphic.color=new Color(.78f,.81f,.77f);
            DemoPaintedFrame.Add(buttons[i].transform,new Color(.32f,.39f,.39f),1.5f);
        }
        foreach(var page in new[]{itemsPage,filesPage,outfitsPage})if(page)ArtRect(page.transform,new Vector2(.025f,.025f),new Vector2(.975f,.835f));
        LayoutDetail(itemsListContainer,itemPreviewImage,itemNameText,itemDescriptionText);
        LayoutDetail(filesListContainer,filePreviewImage,fileNameText,fileContentText);
        if(entryButtonTemplate){
            var layout=entryButtonTemplate.GetComponent<LayoutElement>();if(!layout)layout=entryButtonTemplate.gameObject.AddComponent<LayoutElement>();layout.preferredHeight=94;layout.minHeight=78;
        }
    }
    static void LayoutDetail(Transform list,Image preview,TMP_Text title,TMP_Text description)
    {
        if(list){var scroll=list.GetComponentInParent<ScrollRect>(true);if(scroll)ArtRect(scroll.transform,new Vector2(.015f,.035f),new Vector2(.205f,.97f));}
        if(preview){ArtRect(preview.transform.parent,new Vector2(.235f,.055f),new Vector2(.595f,.945f));ArtRect(preview.transform,new Vector2(.04f,.04f),new Vector2(.96f,.96f));preview.preserveAspect=true;}
        if(title){ArtRect(title.transform.parent,new Vector2(.625f,.04f),new Vector2(.985f,.96f));ArtRect(title.transform,new Vector2(.045f,.81f),new Vector2(.955f,.96f));InkText(title,34);}
        if(description){ArtRect(description.transform,new Vector2(.045f,.06f),new Vector2(.955f,.77f));InkText(description,28);description.textWrappingMode=TextWrappingModes.Normal;}
        if(preview&&preview.transform.parent.GetComponent<Image>() is Image image)image.color=new Color(.94f,.925f,.87f);
        if(title&&title.transform.parent.GetComponent<Image>() is Image panel)panel.color=new Color(.94f,.925f,.87f);
    }
    static void DecorateArtEntry(Button button,Sprite sprite)
    {
        var icon=RuntimePanelUI.Panel("EntryIcon",button.transform,new Vector2(.015f,.12f),new Vector2(.27f,.88f),Color.white,false);icon.sprite=sprite;icon.preserveAspect=true;icon.enabled=sprite;
        var label=button.GetComponentInChildren<TMP_Text>(true);if(label){ArtRect(label.transform,new Vector2(.30f,.08f),new Vector2(.96f,.92f));label.fontSize=25;label.color=Ink;label.alignment=TextAlignmentOptions.MidlineLeft;}
    }
}
