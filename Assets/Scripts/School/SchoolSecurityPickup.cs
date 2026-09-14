using UnityEngine;

public sealed class SchoolSecurityPickup : MonoBehaviour, IInteractable
{
    public SchoolChaseController chase;
    public bool newspaperFragment;
    public SpriteRenderer[] visuals;
    public string GetInteractionPrompt()=>newspaperFragment?"[E] 拾取":"[E] 拿起灯油";
    public void Interact(){if(chase!=null)chase.TakeSecurityItem(newspaperFragment);}
    public void SetAvailable(bool value)
    {
        var c=GetComponent<Collider2D>();if(c!=null)c.enabled=value;
        if(visuals!=null)foreach(var v in visuals)if(v!=null)v.enabled=value;
    }
}
