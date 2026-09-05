using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class FourDigitLock : MonoBehaviour, IScrollHandler, IBeginDragHandler, IDragHandler
{
    public int Value { get; private set; }
    private TMP_Text label; private float dragRemainder;
    public void Bind(TMP_Text text) { label=text; SetDigit(0); }
    public void SetDigit(int value) { Value=(value%10+10)%10; if(label!=null)label.text=Value.ToString(); }
    public void OnScroll(PointerEventData data) { if(Mathf.Abs(data.scrollDelta.y)>0)SetDigit(Value+(data.scrollDelta.y>0?1:-1)); }
    public void OnBeginDrag(PointerEventData data) { dragRemainder=0; }
    public void OnDrag(PointerEventData data) {
        dragRemainder+=data.delta.y;
        while(Mathf.Abs(dragRemainder)>=24f) { int step=dragRemainder>0?1:-1; SetDigit(Value+step); dragRemainder-=step*24f; }
    }
}
