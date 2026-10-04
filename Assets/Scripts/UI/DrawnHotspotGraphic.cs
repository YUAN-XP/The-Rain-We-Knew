using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
/// <summary>Artist contour defines both the clickable region and its hand-drawn hover ink.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class DrawnHotspotGraphic:MaskableGraphic,IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler
{
 public Vector2[] Contour;
 bool hover,selected;
 protected override void OnPopulateMesh(VertexHelper vh)
 {
  vh.Clear();if(Contour==null||Contour.Length<3)return;
  var r=rectTransform.rect;var shade=new Color(.95f,.88f,.66f,hover||selected?.94f:.70f);
  for(int i=0;i<Contour.Length;i++)
  {
   Vector2 a=r.min+Vector2.Scale(r.size,Contour[i]),b=r.min+Vector2.Scale(r.size,Contour[(i+1)%Contour.Length]);
   var n=new Vector2(-(b-a).y,(b-a).x).normalized*1.45f;
   int k=vh.currentVertCount;vh.AddVert(a-n,shade,Vector2.zero);vh.AddVert(a+n,shade,Vector2.zero);vh.AddVert(b+n,shade,Vector2.zero);vh.AddVert(b-n,shade,Vector2.zero);
   vh.AddTriangle(k,k+1,k+2);vh.AddTriangle(k,k+2,k+3);
  }
 }
 public override bool Raycast(Vector2 screen,Camera camera)
 {
  if(!base.Raycast(screen,camera)||Contour==null||Contour.Length<3)return false;
  if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,screen,camera,out var p))return false;
  var r=rectTransform.rect;p=(p-r.min)/r.size;bool inside=false;
  for(int i=0,j=Contour.Length-1;i<Contour.Length;j=i++)
  {var a=Contour[i];var b=Contour[j];if((a.y>p.y)!=(b.y>p.y)&&p.x<(b.x-a.x)*(p.y-a.y)/(b.y-a.y)+a.x)inside=!inside;}
  return inside;
 }
 public void OnPointerEnter(PointerEventData e){hover=true;SetVerticesDirty();}
 public void OnPointerExit(PointerEventData e){hover=false;SetVerticesDirty();}
 public void OnSelect(BaseEventData e){selected=true;SetVerticesDirty();}
 public void OnDeselect(BaseEventData e){selected=false;SetVerticesDirty();}
}
