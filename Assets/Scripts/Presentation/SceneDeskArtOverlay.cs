using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Scene-owned decoration of the existing desk panels. Does not own any input or game state.</summary>
public sealed class SceneDeskArtOverlay : MonoBehaviour
{
    [SerializeField] private Sprite deskSprite;
    [SerializeField] private Texture2D lockTexture;
    private readonly List<GameObject> added = new List<GameObject>();
    private readonly List<Image> tinted = new List<Image>();
    private readonly List<Color> oldColors = new List<Color>();
    private bool installed;
    private void LateUpdate()
    {
        if (installed || PersistentRuntime.Instance == null) return;
        var root = PersistentRuntime.Instance.transform.Find("DeskAndOutfitCanvas");
        if (root == null) return;
        var desk = root.Find("Desktop/WoodDesktop");
        var drawer = root.Find("Drawer");
        if (desk == null || drawer == null || drawer.Find("Digit3") == null) return;
        installed = true;
        var art = Node("AP1B_DeskArt", desk, Vector2.zero, Vector2.one);
        var image = art.AddComponent<Image>(); image.sprite = deskSprite; image.preserveAspect = true; image.raycastTarget = false;
        art.transform.SetAsFirstSibling();
        var wood = desk.GetComponent<Image>(); Tint(wood, new Color(.06f,.065f,.07f,.2f));
        foreach(var button in desk.GetComponentsInChildren<Button>(true))
            Tint(button.GetComponent<Image>(),new Color(.12f,.15f,.16f,.18f));
        // Hide all baked digits, including their upper/lower neighbours. Only source frame strips are rendered.
        // Original four wheel RectTransforms, raycast surfaces and callbacks remain unchanged.
        Strip(drawer,"Top",new Vector2(.21f,.62f),new Vector2(.79f,.69f),new Rect(0,1f-308f/1086f,1,100f/1086f));
        Strip(drawer,"Bottom",new Vector2(.21f,.32f),new Vector2(.79f,.39f),new Rect(0,1f-867f/1086f,1,117f/1086f));
        Strip(drawer,"Left",new Vector2(.21f,.39f),new Vector2(.28f,.62f),new Rect(42f/1448f,1f-750f/1086f,224f/1448f,442f/1086f));
        Strip(drawer,"Right",new Vector2(.715f,.39f),new Vector2(.79f,.62f),new Rect(1201f/1448f,1f-750f/1086f,208f/1448f,442f/1086f));
        for (int i=0;i<4;i++) Tint(drawer.Find("Digit"+i).GetComponent<Image>(),new Color(.25f,.215f,.155f,1));
    }
    private GameObject Node(string name,Transform parent,Vector2 min,Vector2 max)
    {
        var go = new GameObject(name,typeof(RectTransform)); go.transform.SetParent(parent,false);
        var rect=(RectTransform)go.transform; rect.anchorMin=min;rect.anchorMax=max;rect.offsetMin=rect.offsetMax=Vector2.zero;
        added.Add(go);return go;
    }
    private void Strip(Transform parent,string name,Vector2 min,Vector2 max,Rect uv)
    {
        var go=Node("AP1B_LockFrame_"+name,parent,min,max);go.transform.SetAsFirstSibling();
        var image=go.AddComponent<RawImage>();image.texture=lockTexture;image.uvRect=uv;image.raycastTarget=false;
    }
    private void Tint(Image target,Color color)
    {
        if(target==null)return;tinted.Add(target);oldColors.Add(target.color);target.color=color;
    }
    private void OnDisable()
    {
        for(int i=0;i<tinted.Count;i++)if(tinted[i]!=null)tinted[i].color=oldColors[i];
        foreach(var go in added)if(go!=null)Destroy(go);
        added.Clear();tinted.Clear();oldColors.Clear();installed=false;
    }
}
