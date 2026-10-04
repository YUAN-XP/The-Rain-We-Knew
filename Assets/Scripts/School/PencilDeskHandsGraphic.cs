using UnityEngine;
using UnityEngine.UI;
// Virtual tabletop coordinates remain identical to the accepted rubbing hit-test.
[RequireComponent(typeof(CanvasRenderer))]
public class PencilDeskHandsGraphic : MaskableGraphic {
 Vector2 tip=new Vector2(520,235);float grab;Image writing,grabbing;
 public Vector2 TipPosition{get=>tip;set{tip=value;Layout();}}
 public float GrabProgress{get=>grab;set{grab=Mathf.Clamp01(value);Layout();}}
 public Vector2 WristScreenPosition=>RectTransformUtility.WorldToScreenPoint(null,rectTransform.TransformPoint(Point(tip+new Vector2(135,-185))));
 Vector2 Point(Vector2 p){var r=rectTransform.rect;return r.min+Vector2.Scale(r.size,new Vector2(p.x/1000,p.y/500));}
 protected override void OnEnable(){base.OnEnable();Ensure();Layout();}
 void Ensure(){if(writing)return;writing=P05ArtworkUI.Paint(transform,"PaintedPencilHand","P05_PencilHand",Vector2.zero,Vector2.zero);grabbing=P05ArtworkUI.Paint(transform,"PaintedGrabbingHand","P05_GrabbingHand",Vector2.zero,Vector2.zero);writing.rectTransform.pivot=new Vector2(.020f,.990f);grabbing.rectTransform.pivot=new Vector2(.12f,.5f);}
 void Layout(){if(!writing)return;var r=rectTransform.rect;float k=r.width/1000;writing.rectTransform.sizeDelta=new Vector2(270,500)*k;writing.rectTransform.anchoredPosition=new Vector2(tip.x/1000*r.width,tip.y/500*r.height);grabbing.gameObject.SetActive(grab>0);var wrist=tip+new Vector2(135,-185);var contact=Vector2.Lerp(new Vector2(1160,wrist.y+20),wrist,grab);grabbing.rectTransform.sizeDelta=new Vector2(310,215)*k;grabbing.rectTransform.anchoredPosition=new Vector2(contact.x/1000*r.width,contact.y/500*r.height);}
 protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();}
}
