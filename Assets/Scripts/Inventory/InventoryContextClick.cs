using UnityEngine;
using UnityEngine.EventSystems;

public sealed class InventoryContextClick : MonoBehaviour, IPointerClickHandler
{
    public InventoryUI owner;
    public ItemData item;
    public void OnPointerClick(PointerEventData e)
    {
        if(e.button==PointerEventData.InputButton.Right&&owner!=null)owner.ShowCombinationMenu(item,e.position);
    }
}
