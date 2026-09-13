using UnityEngine;

// Attach only to the adult player. Child/teen actors deliberately have no dependency on this.
public class AdultOutfitController : MonoBehaviour
{
    [SerializeField] private GameObject overallsVisual;
    [System.Serializable] private struct OutfitVisual { public AdultOutfit outfit; public GameObject visual; }
    [SerializeField] private OutfitVisual[] additionalVisuals;
    private StoryStateManager story;
    // Append one scene-provided adult visual without replacing the saved outfit selection.
    public void RegisterVisual(AdultOutfit outfit, GameObject visual)
    {
        if(visual==null)return;
        if(additionalVisuals==null)additionalVisuals=new OutfitVisual[0];
        for(int i=0;i<additionalVisuals.Length;i++)if(additionalVisuals[i].outfit==outfit)
        {
            var old=additionalVisuals[i].visual;
            if(old!=null && old!=visual)old.SetActive(false);
            additionalVisuals[i]=new OutfitVisual{outfit=outfit,visual=visual};
            if(story!=null)Apply();else visual.SetActive(false);
            return;
        }
        System.Array.Resize(ref additionalVisuals,additionalVisuals.Length+1);
        additionalVisuals[additionalVisuals.Length-1]=new OutfitVisual{outfit=outfit,visual=visual};
        if(story!=null)Apply();else visual.SetActive(false);
    }
    private void Start() { story=FindAnyObjectByType<StoryStateManager>(); if(story!=null){ story.Changed+=Apply; Apply(); } }
    private void Apply()
    {
        if(overallsVisual!=null)overallsVisual.SetActive(story.Outfit==AdultOutfit.Overalls);
        if(additionalVisuals!=null)foreach(var entry in additionalVisuals)
            if(entry.visual!=null)entry.visual.SetActive(story.Outfit==entry.outfit);
    }
    private void OnDestroy() { if(story!=null)story.Changed-=Apply; }
}
