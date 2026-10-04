using UnityEngine;
using UnityEngine.UI;
// Shared painted close-up assets; no story, inventory or puzzle callbacks.
public static class P05ArtworkUI {
 public static Image Paint(Transform parent,string name,string key,Vector2 lo,Vector2 hi,bool preserve=true){var i=RuntimePanelUI.Rect(name,parent,lo,hi).gameObject.AddComponent<Image>();i.sprite=DemoArtCatalog.Find(key);i.preserveAspect=preserve;i.raycastTarget=false;return i;}
 public static readonly Vector2[] NoteCenters={new Vector2(434,564),new Vector2(526,581),new Vector2(615,609),new Vector2(410,653),new Vector2(502,677),new Vector2(593,705),new Vector2(383,749),new Vector2(469,772),new Vector2(561,790)};
}
