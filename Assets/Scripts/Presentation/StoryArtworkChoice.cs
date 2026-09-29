using UnityEngine;

/// <summary>Presentation-only alternate pose; reads a story flag without changing it.</summary>
public sealed class StoryArtworkChoice : MonoBehaviour
{
    [SerializeField] private StoryFlag flag;
    [SerializeField] private GameObject before,after;
    private StoryStateManager story;
    private void LateUpdate()
    {
        if(!story&&PersistentRuntime.Instance)story=PersistentRuntime.Instance.GetComponent<StoryStateManager>();
        bool done=story&&story.Has(flag);
        if(before&&before.activeSelf==done)before.SetActive(!done);
        if(after&&after.activeSelf!=done)after.SetActive(done);
    }
}
