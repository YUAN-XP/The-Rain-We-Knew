using System.Collections;
using UnityEngine;

public class CameraRevealTarget : MonoBehaviour
{
    [SerializeField] private StoryFlag revealedFlag=StoryFlag.CameraDoorRevealed;
    [SerializeField] private SpriteRenderer doorRenderer;
    [SerializeField] private Collider2D interactionCollider;
    [SerializeField,Range(1f,1.5f)] private float revealDuration=1.25f;
    private StoryStateManager story;
    private Color original;
    public bool IsRevealed => story!=null && story.Has(revealedFlag);
    private void Awake() { original=doorRenderer.color; doorRenderer.enabled=false; interactionCollider.enabled=false; }
    private void Start() { story=FindAnyObjectByType<StoryStateManager>(); Restore(); }
    public void Restore() { doorRenderer.enabled=IsRevealed; doorRenderer.color=original; interactionCollider.enabled=IsRevealed; }
    public void Preview(bool visible) { if(IsRevealed)return; doorRenderer.enabled=visible; doorRenderer.color=new Color(.62f,.80f,.72f,.65f); }
    public bool CanCapture(Camera camera)
    {
        if(IsRevealed || camera==null || !doorRenderer.enabled)return false;
        var v=camera.WorldToViewportPoint(doorRenderer.bounds.center);
        return v.z>0 && v.x>-.02f && v.x<1.02f && v.y>-.02f && v.y<1.02f;
    }
    public IEnumerator Reveal()
    {
        if(IsRevealed)yield break;
        doorRenderer.enabled=true;
        for(float t=0;t<revealDuration;t+=GameplayClock.Delta) {
            float a=Mathf.Clamp01(t/revealDuration);
            doorRenderer.color=Color.Lerp(new Color(.75f,.9f,.8f,0),original,a);
            doorRenderer.color*=1f+.13f*Mathf.Sin(t*83f)*(1-a);
            yield return null;
        }
        story.Set(revealedFlag); Restore();
    }
}
