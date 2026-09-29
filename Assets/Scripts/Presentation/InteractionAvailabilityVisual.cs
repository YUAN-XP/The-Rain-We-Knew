using UnityEngine;

/// <summary>Read-only presentation for pickups whose existing collider owns availability.</summary>
[DisallowMultipleComponent]
public sealed class InteractionAvailabilityVisual : MonoBehaviour
{
    [SerializeField] private Collider2D availability;
    [SerializeField] private SpriteRenderer visual;
    private void OnEnable()=>Sync();
    private void LateUpdate()=>Sync();
    private void Sync()
    {
        if(visual!=null)visual.enabled=availability!=null&&availability.enabled&&availability.gameObject.activeInHierarchy;
    }
    private void OnDisable(){if(visual!=null)visual.enabled=false;}
}
