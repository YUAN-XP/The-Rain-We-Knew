using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public interface IInventoryAccessPolicy { bool TryCanOpenInventory(out bool allowed); }

public partial class InventoryUI
{
    public bool IsOpen=>inventoryPanel!=null&&inventoryPanel.activeSelf;
    public event Action Opened;
    public event Action Closed;
    IInventoryAccessPolicy accessPolicy;
    CombinationManager combinations;
    readonly HashSet<ItemData> selectedMaterials=new HashSet<ItemData>();
    readonly Dictionary<ItemData,Button> materialButtons=new Dictionary<ItemData,Button>();
    GameObject combinationMenu;
    ScrollRect fileReadingScroll;
    void FitFileReading()
    {
        if(fileContentText==null)return;
        var content=fileContentText.rectTransform;
        if(fileReadingScroll==null){
            Canvas.ForceUpdateCanvases();var parent=content.parent as RectTransform;if(parent==null)return;
            Vector3[] corners=new Vector3[4];content.GetWorldCorners(corners);
            Vector2 low=parent.InverseTransformPoint(corners[0]),high=parent.InverseTransformPoint(corners[2]);
            var bounds=parent.rect;
            low.x=Mathf.Max(low.x,bounds.xMin+12);high.x=Mathf.Min(high.x,bounds.xMax-12);
            low.y=Mathf.Max(low.y,bounds.yMin+20);high.y=Mathf.Min(high.y,bounds.yMax-20);
            if(high.y-low.y<80){low.y=bounds.yMin+20;high.y=bounds.yMax*.5f;}
            var viewport=RuntimePanelUI.Rect("文件正文滚动区",parent,Vector2.zero,Vector2.zero);
            viewport.pivot=Vector2.zero;viewport.anchoredPosition=low-bounds.min;viewport.sizeDelta=high-low;
            viewport.gameObject.AddComponent<RectMask2D>();var touch=viewport.gameObject.AddComponent<Image>();touch.color=Color.clear;
            content.SetParent(viewport,false);content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(.5f,1);
            content.offsetMin=Vector2.zero;content.offsetMax=Vector2.zero;
            fileContentText.alignment=TextAlignmentOptions.TopLeft;fileContentText.overflowMode=TextOverflowModes.Overflow;
            fileReadingScroll=viewport.gameObject.AddComponent<ScrollRect>();fileReadingScroll.viewport=viewport;fileReadingScroll.content=content;
            fileReadingScroll.horizontal=false;fileReadingScroll.vertical=true;fileReadingScroll.movementType=ScrollRect.MovementType.Clamped;fileReadingScroll.scrollSensitivity=45;
        }
        Canvas.ForceUpdateCanvases();float height=fileContentText.GetPreferredValues(fileContentText.text,fileReadingScroll.viewport.rect.width,0).y+8;
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,Mathf.Max(height,fileReadingScroll.viewport.rect.height));
        content.anchoredPosition=Vector2.zero;fileReadingScroll.StopMovement();fileReadingScroll.verticalNormalizedPosition=1;
    }
    public void SetAccessPolicy(IInventoryAccessPolicy policy){accessPolicy=policy;}
    public void PreferKeyItems(){if(!IsOpen)currentTab=InventoryTab.KeyItems;}
    bool MayOpen()
    {
        if(accessPolicy is UnityEngine.Object o&&o==null)accessPolicy=null;
        if(accessPolicy!=null&&accessPolicy.TryCanOpenInventory(out bool allowed))return allowed;
        return gameControlManager==null||!gameControlManager.IsLocked();
    }
    void ResolveCombinations(){if(combinations==null&&inventoryManager!=null)combinations=inventoryManager.GetComponent<CombinationManager>();}
    void BindCombination(ItemData item,Button button)
    {
        ResolveCombinations();materialButtons[item]=button;
        var handler=button.gameObject.AddComponent<InventoryContextClick>();handler.owner=this;handler.item=item;
        RefreshCombinationHighlights();
    }
    void RefreshCombinationHighlights()
    {
        foreach(var pair in materialButtons){if(pair.Value==null)continue;
            bool selected=selectedMaterials.Contains(pair.Key);
            bool compatible=combinations!=null&&combinations.CanSelect(pair.Key,selectedMaterials);
            var label=pair.Value.GetComponentInChildren<TMP_Text>(true);
            if(label!=null){label.text=(selected?"✓ ":"")+inventoryManager.DisplayName(pair.Key);label.color=selected?new Color(.36f,.23f,.025f):selectedMaterials.Count==0||compatible?new Color(.12f,.14f,.14f):new Color(.59f,.60f,.60f);}
        }
    }
    void ClearCombinationSelection(){selectedMaterials.Clear();CloseCombinationMenu();RefreshCombinationHighlights();}
    void CloseCombinationMenu(){if(combinationMenu!=null){combinationMenu.SetActive(false);Destroy(combinationMenu);}combinationMenu=null;}
    public void ShowCombinationMenu(ItemData item,Vector2 screenPoint)
    {
        if(!IsOpen||item==null)return;ResolveCombinations();CloseCombinationMenu();
        var root=RuntimePanelUI.Canvas("物品组合菜单",inventoryPanel.transform,230);combinationMenu=root.gameObject;
        var dismiss=RuntimePanelUI.Panel("关闭菜单",root,Vector2.zero,Vector2.one,Color.clear).gameObject.AddComponent<Button>();dismiss.onClick.AddListener(CloseCombinationMenu);
        Canvas.ForceUpdateCanvases();RectTransformUtility.ScreenPointToLocalPointInRectangle(root,screenPoint,null,out Vector2 local);
        const float width=250,height=168;var box=RuntimePanelUI.Panel("菜单",root,Vector2.one*.5f,Vector2.one*.5f,new Color(.10f,.13f,.14f));
        var rect=box.rectTransform;rect.pivot=new Vector2(0,1);rect.sizeDelta=new Vector2(width,height);
        rect.anchoredPosition=new Vector2(Mathf.Clamp(local.x,root.rect.xMin+8,root.rect.xMax-width-8),Mathf.Clamp(local.y,root.rect.yMin+height+8,root.rect.yMax-8));
        var font=itemNameText!=null?itemNameText.font:null;bool chosen=selectedMaterials.Contains(item);
        var select=RuntimePanelUI.Button(chosen?"取消选择":"选择",rect,font,new Vector2(.04f,.53f),new Vector2(.96f,.95f),()=>{
            if(chosen)selectedMaterials.Remove(item);else if(combinations!=null&&combinations.CanSelect(item,selectedMaterials))selectedMaterials.Add(item);
            CloseCombinationMenu();RefreshCombinationHighlights();
        });select.interactable=chosen||combinations!=null&&combinations.CanSelect(item,selectedMaterials);
        var combine=RuntimePanelUI.Button("组合",rect,font,new Vector2(.04f,.05f),new Vector2(.96f,.47f),()=>{
            string feedback="仍缺少物品/文件";bool success=combinations!=null&&combinations.TryCombine(selectedMaterials,out feedback);
            CloseCombinationMenu();if(success){selectedMaterials.Clear();RefreshItems();RefreshFiles();}
            RefreshCombinationHighlights();if(itemNameText!=null)itemNameText.text=success?"组合完成":"组合";
            if(itemDescriptionText!=null)itemDescriptionText.text=feedback;
        });combine.interactable=chosen;
        // Disabled actions must also read as disabled on the dark Alpha menu.
        if(!select.interactable)select.GetComponentInChildren<TMP_Text>().color=new Color(.43f,.46f,.46f);
        if(!combine.interactable)combine.GetComponentInChildren<TMP_Text>().color=new Color(.43f,.46f,.46f);
    }
}
