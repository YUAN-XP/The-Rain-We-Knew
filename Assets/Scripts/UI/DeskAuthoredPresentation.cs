using UnityEngine;
using UnityEngine.UI;
public partial class DeskCloseupController
{
 void BuildAuthoredDesktop()
 {
  var frame=RuntimePanelUI.Rect("AuthoredDeskFrame",desktop.transform,new Vector2(.15f,.14f),new Vector2(.85f,.94f));
  var area=RuntimePanelUI.Rect("AuthoredDesk",frame,Vector2.zero,Vector2.one);
  var art=area.gameObject.AddComponent<Image>();art.sprite=DemoArtCatalog.Find("P03_PasswordDesk");art.color=Color.white;art.raycastTarget=false;
  var fit=area.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=1150f/907f;
  Hotspot(area,"ComputerHotspot",NativeContour(new[]{new Vector2(330,65),new Vector2(699,65),new Vector2(715,70),new Vector2(719,82),new Vector2(719,291),new Vector2(715,301),new Vector2(548,304),new Vector2(548,320),new Vector2(601,320),new Vector2(618,338),new Vector2(617,347),new Vector2(742,348),new Vector2(753,355),new Vector2(766,391),new Vector2(766,399),new Vector2(759,402),new Vector2(322,402),new Vector2(315,398),new Vector2(316,388),new Vector2(339,355),new Vector2(347,348),new Vector2(407,348),new Vector2(407,338),new Vector2(423,320),new Vector2(475,320),new Vector2(475,304),new Vector2(320,304),new Vector2(311,296),new Vector2(311,79),new Vector2(316,68)}),()=>ShowPage("Computer"));
  Hotspot(area,"DrawerHotspot",NativeContour(new[]{new Vector2(830,449),new Vector2(1278,449),new Vector2(1278,578),new Vector2(830,578)}),()=>ShowPage("Drawer"));
  RuntimePanelUI.Text("点击电脑或密码抽屉",desktop.transform,font,new Vector2(.15f,.035f),new Vector2(.69f,.11f),25);
 }
 // Pixel outline follows the approved 1536x1024 painting, cropped to the native sprite rect.
 Vector2[] NativeContour(Vector2[] pixels)
 {
  for(int i=0;i<pixels.Length;i++)pixels[i]=new Vector2((pixels[i].x-193f)/1150f,(971f-pixels[i].y)/907f);
  return pixels;
 }
 void Hotspot(Transform parent,string label,Vector2[] contour,UnityEngine.Events.UnityAction action)
 {
  var r=RuntimePanelUI.Rect(label,parent,Vector2.zero,Vector2.one);
  var ink=r.gameObject.AddComponent<DrawnHotspotGraphic>();ink.Contour=contour;ink.SetVerticesDirty();
  var button=r.gameObject.AddComponent<Button>();button.targetGraphic=ink;button.transition=Selectable.Transition.None;button.onClick.AddListener(action);
 }
 void BuildAuthoredDrawer()
 {
  var face=RuntimePanelUI.Panel("MatchingDrawer",lockPanel.transform,new Vector2(.41f,.79f),new Vector2(.59f,.97f),Color.white,false);
  face.sprite=DemoArtCatalog.Find("P03_PasswordDesk:drawer");face.preserveAspect=true;face.transform.SetAsFirstSibling();
  var frame=RuntimePanelUI.Rect("AuthoredLockFrame",lockPanel.transform,new Vector2(.24f,.36f),new Vector2(.76f,.66f));
  var zoom=RuntimePanelUI.Rect("AuthoredLock",frame,Vector2.zero,Vector2.one);
  var art=zoom.gameObject.AddComponent<Image>();art.sprite=DemoArtCatalog.Find("P03_LockCloseup");art.raycastTarget=false;
  var fit=zoom.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=1296f/620;
  zoom.transform.SetAsFirstSibling();
  for(int i=0;i<wheels.Length;i++)
  {
   var digit=wheels[i].GetComponent<RectTransform>();digit.SetParent(zoom,false);
   digit.anchorMin=new Vector2(.1775f+i*.1788f,.190f);digit.anchorMax=new Vector2(.2894f+i*.1788f,.810f);
   digit.offsetMin=digit.offsetMax=Vector2.zero;
   var plate=digit.GetComponent<Image>();plate.color=Color.clear;
   var label=digit.GetComponentInChildren<TMPro.TMP_Text>();label.color=new Color(.035f,.035f,.030f);label.fontSize=45;
   var readout=label.rectTransform;readout.anchorMin=new Vector2(0,.30f);readout.anchorMax=new Vector2(1,.70f);readout.offsetMin=readout.offsetMax=Vector2.zero;
  }
 }
}
