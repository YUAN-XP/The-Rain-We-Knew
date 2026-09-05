using UnityEngine;

// Attach only to the adult player. Child/teen actors deliberately have no dependency on this.
public class AdultOutfitController : MonoBehaviour
{
    [SerializeField] private GameObject overallsVisual;
    [System.Serializable] private struct OutfitVisual { public AdultOutfit outfit; public GameObject visual; }
    [SerializeField] private OutfitVisual[] additionalVisuals;
    private StoryStateManager story;
    private void Start() { story=FindAnyObjectByType<StoryStateManager>(); if(story!=null){ story.Changed+=Apply; Apply(); } }
    private void Apply()
    {
        if(overallsVisual!=null)overallsVisual.SetActive(story.Outfit==AdultOutfit.Overalls);
        if(additionalVisuals!=null)foreach(var entry in additionalVisuals)
            if(entry.visual!=null)entry.visual.SetActive(story.Outfit==entry.outfit);
    }
    private void OnDestroy() { if(story!=null)story.Changed-=Apply; }
}
