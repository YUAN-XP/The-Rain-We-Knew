using UnityEngine;

// Presentation-only suppression. Original renderers remain available to story
// scripts for visibility, bounds and color; no gameplay object is disabled.
[ExecuteAlways, DefaultExecutionOrder(1100)]
public sealed class RetiredArtVisibility : MonoBehaviour
{
    [SerializeField] private Renderer[] renderers;
    public Renderer[] Renderers => renderers ?? System.Array.Empty<Renderer>();
    private bool[] previous;
    private Renderer[] owned;
    public void SetRenderers(Renderer[] values) { Restore(); renderers=values; if(isActiveAndEnabled)LateUpdate(); }
    private void OnEnable() => LateUpdate();
    private void LateUpdate()
    {
        if (!isActiveAndEnabled || renderers == null) return;
        if(owned!=renderers){
            Restore();owned=renderers;previous=new bool[owned.Length];
            for(int i=0;i<owned.Length;i++)if(owned[i])previous[i]=owned[i].forceRenderingOff;
        }
        foreach (var renderer in owned)
            if (renderer != null) renderer.forceRenderingOff = true;
    }
    private void Restore()
    {
        if(owned!=null&&previous!=null)
            for(int i=0;i<owned.Length;i++)if(owned[i])owned[i].forceRenderingOff=previous[i];
        owned=null;previous=null;
    }
    private void OnDisable()=>Restore();
    private void OnDestroy()=>Restore();
}
