using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class ChildhoodDragHandle : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public ChildhoodActivityUI owner;
    public int cardIndex;
    public bool ring;
    Vector2 origin, begin;
    RectTransform rect;
    Vector2 pressScreen;
    bool tracking;
    Vector3 initialScale;
    bool settled, returning;
    void Capture(Vector2 position){if(returning||settled||(!ring&&!owner.CanDragCards))return;StopAllCoroutines();rect=(RectTransform)transform;origin=rect.anchoredPosition;initialScale=Vector3.one; if(!ring){rect.localScale=initialScale*1.12f;rect.SetAsLastSibling();}pressScreen=position;tracking=true;RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rect.parent,position,null,out begin);}
    public void OnPointerDown(PointerEventData e){Capture(e.position);}
    public void OnBeginDrag(PointerEventData e){if(!tracking)Capture(e.position);}
    public void OnPointerUp(PointerEventData e){if(tracking&&Vector2.Distance(pressScreen,e.position)>5)OnEndDrag(e);else {tracking=false;if(rect!=null&&!ring)rect.localScale=initialScale;}}
    public void OnDrag(PointerEventData e)
    {
        if(rect==null||!tracking)return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rect.parent,e.position,null,out var point);
        if(ring)owner.DragRing(point-begin,false);else rect.anchoredPosition=origin+point-begin;
    }
    public void OnEndDrag(PointerEventData e)
    {
        if(rect==null||!tracking)return;
        tracking=false;
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rect.parent,e.position,null,out var point);
        if(ring)owner.DragRing(point-begin,true);
        else if(!owner.DropCard(cardIndex,e.position))StartCoroutine(Return());
        else {settled=true;StartCoroutine(Snap());}
    }
    IEnumerator Snap(){Vector3 from=rect.position,to=owner.StorySlot.position;for(float t=0;t<.2f;t+=Time.unscaledDeltaTime){float f=Mathf.SmoothStep(0,1,t/.2f);rect.position=Vector3.Lerp(from,to,f);rect.localScale=Vector3.Lerp(initialScale*1.12f,initialScale*.8f,f);yield return null;}rect.position=to;}
    IEnumerator Return()
    {
        returning=true;Vector2 drop=rect.anchoredPosition;rect.localScale=initialScale;
        for(float t=0;t<.22f;t+=Time.unscaledDeltaTime){rect.anchoredPosition=drop+Vector2.right*Mathf.Sin(t*70)*9;yield return null;}
        for(float t=0;t<.2f;t+=Time.unscaledDeltaTime){rect.anchoredPosition=Vector2.Lerp(drop,origin,Mathf.SmoothStep(0,1,t/.2f));yield return null;}
        rect.anchoredPosition=origin;returning=false;
    }
}
