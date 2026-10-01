using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Scene-owned visual skins; the existing activity UI retains its hit areas and rules.</summary>
public sealed class ChildhoodActivityArtwork : MonoBehaviour
{
    [SerializeField] PresentationArtLibrary art;
    ChildhoodActivityUI activities;
    readonly List<Graphic> originals = new List<Graphic>();
    readonly List<GameObject> additions = new List<GameObject>();
    float next;
    void LateUpdate()
    {
        if (!art || GameplayClock.Now < next) return;
        next = GameplayClock.Now + .1f;
        if (!activities) { var chapter=GetComponent<Scene03Chapter>(); if(chapter)activities=chapter.activities; }
        if (!activities || !activities.IsOpen) return;
        foreach (var source in activities.GetComponentsInChildren<ChildhoodIllustration>(true))
        {
            if (source.transform.Find("NewArt_Illustration")) continue;
            string key;
            switch (source.picture)
            {
                case ChildhoodIllustration.Picture.Cat: key="cat_sleep"; break;
                case ChildhoodIllustration.Picture.Princess: key="puppet_princess"; break;
                case ChildhoodIllustration.Picture.Knight: key="puppet_knight"; break;
                case ChildhoodIllustration.Picture.Rebel: key="puppet_rebel"; break;
                case ChildhoodIllustration.Picture.Castle: key="puppet_castle"; break;
                case ChildhoodIllustration.Picture.Ring: key="puppet_ring"; break;
                case ChildhoodIllustration.Picture.Coin: key="puppet_coin"; break;
                default: key="puppet_prize"; break;
            }
            if (Add(source.transform,key,true)) Retire(source);
        }
        foreach(var t in activities.GetComponentsInChildren<RectTransform>(true))
        {
            if(t.name!="木偶舞台" || t.Find("NewArt_Illustration")) continue;
            if(!Add(t,"puppet_stage",false)) continue;
            foreach(Transform child in t)
                if(child.name=="舞台地板"||child.name=="木纹"||child.name=="舞台红帘"||child.name=="上帷幕")
                    Retire(child.GetComponent<Image>());
        }
    }
    bool Add(Transform parent,string key,bool preserveAspect)
    {
        var sprite=DemoArtCatalog.Find(DemoKey(key));if(!sprite)sprite=art.Get(key); if(!sprite)return false;
        var g=new GameObject("NewArt_Illustration",typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
        var r=(RectTransform)g.transform;r.SetParent(parent,false);r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;
        r.offsetMin=r.offsetMax=Vector2.zero;r.SetAsFirstSibling();
        var image=g.GetComponent<Image>();image.sprite=sprite;image.preserveAspect=preserveAspect;image.raycastTarget=false;
        additions.Add(g);return true;
    }
    static string DemoKey(string key)
    {
        switch(key){
            case "cat_sleep":return "AN_CAT_NORMAL_V02:0";
            case "puppet_princess":return "GAME_PUPPET_CARDS:0";
            case "puppet_knight":return "GAME_PUPPET_CARDS:1";
            case "puppet_rebel":return "GAME_PUPPET_CARDS:2";
            case "puppet_castle":return "GAME_PUPPET_STAGE_LAYERS:castle";
            case "puppet_stage":return "BG_PUPPET_STAGE_BASE";
            case "puppet_coin":return "SPR_PICKUPS_KEYS_FISH_COIN:s03_game_coin";
            case "puppet_prize":return "GAME_RING_TARGETS_V02:0";
            default:return null;
        }
    }
    void Retire(Graphic graphic){if(graphic&&graphic.enabled){originals.Add(graphic);graphic.enabled=false;}}
    void OnDisable(){foreach(var g in originals)if(g)g.enabled=true;foreach(var g in additions)if(g)Destroy(g);originals.Clear();additions.Clear();}
}
