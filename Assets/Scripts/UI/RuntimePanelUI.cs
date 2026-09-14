using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

// Shared lightweight placeholder UI construction. Font is explicitly supplied by the scene.
public static class RuntimePanelUI
{
    // Existing authored UI uses QHD pixel offsets. Preserve that appearance at
    // QHD, but keep its off-centre tabs/labels inside smaller game windows.
    // An Inspector-configured non-constant scaler is always left untouched.
    public static void FitLegacyCanvas(GameObject panel)
    {
        var scaler = panel != null ? panel.GetComponentInParent<CanvasScaler>(true) : null;
        if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ConstantPixelSize) return;
        scaler.referenceResolution = new Vector2(2560, 1440);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = .5f;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    }

    public static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var g = new GameObject(name, typeof(RectTransform));
        var r = g.GetComponent<RectTransform>(); r.SetParent(parent, false);
        r.anchorMin=min; r.anchorMax=max; r.offsetMin=Vector2.zero; r.offsetMax=Vector2.zero;
        return r;
    }
    public static RectTransform Canvas(string name, Transform parent, int order)
    {
        var r=Rect(name,parent,Vector2.zero,Vector2.one);
        var c=r.gameObject.AddComponent<Canvas>(); c.renderMode=RenderMode.ScreenSpaceOverlay; c.sortingOrder=order;
        var scaler=r.gameObject.AddComponent<CanvasScaler>(); scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1600,900);
        r.gameObject.AddComponent<GraphicRaycaster>(); return r;
    }
    public static Image Panel(string name,Transform parent,Vector2 min,Vector2 max,Color color,bool raycast=true)
    {
        var i=Rect(name,parent,min,max).gameObject.AddComponent<Image>(); i.color=color; i.raycastTarget=raycast; return i;
    }
    public static TMP_Text Text(string text,Transform parent,TMP_FontAsset font,Vector2 min,Vector2 max,float size=30)
    {
        var t=Rect("Text",parent,min,max).gameObject.AddComponent<TextMeshProUGUI>();
        t.font=font; t.text=text; t.fontSize=size; t.alignment=TextAlignmentOptions.Center; t.raycastTarget=false;
        t.color=new Color(.91f,.89f,.81f); return t;
    }
    public static Button Button(string text,Transform parent,TMP_FontAsset font,Vector2 min,Vector2 max,UnityAction action)
    {
        var i=Panel(text,parent,min,max,new Color(.19f,.23f,.23f));
        var b=i.gameObject.AddComponent<Button>(); b.targetGraphic=i;
        var colors=b.colors; colors.highlightedColor=new Color(.7f,.8f,.7f); colors.selectedColor=colors.highlightedColor; b.colors=colors;
        Text(text,i.transform,font,new Vector2(.03f,.04f),new Vector2(.97f,.96f));
        b.onClick.AddListener(action); return b;
    }
}
