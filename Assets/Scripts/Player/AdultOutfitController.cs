using UnityEngine;

// Attach only to the adult player. Child/teen actors deliberately have no dependency on this.
public class AdultOutfitController : MonoBehaviour
{
    [SerializeField] private GameObject overallsVisual;
    private StoryStateManager story;
    private void Start() { story=FindAnyObjectByType<StoryStateManager>(); if(story!=null){ story.Changed+=Apply; Apply(); } }
    private void Apply() { if(overallsVisual!=null)overallsVisual.SetActive(story.Outfit==AdultOutfit.Overalls); }
    private void OnDestroy() { if(story!=null)story.Changed-=Apply; }
}
