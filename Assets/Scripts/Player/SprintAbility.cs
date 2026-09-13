using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SprintAbility : MonoBehaviour
{
    [SerializeField,Range(1.1f,2.5f)] private float speedMultiplier=1.7f;
    [SerializeField] private bool allowStoryUnlock = true;
    private StoryStateManager story;
    private StoryNoticeUI notice;
    private readonly HashSet<Object> temporarySuppressors = new HashSet<Object>();
    public bool IsTemporarilySuppressed => temporarySuppressors.Count > 0;
    // A local event owns its own suppression; it never removes the permanent
    // unlock and cannot release another event's suppression.
    public void SetTemporarilySuppressed(Object owner, bool suppressed)
    {
        if (owner == null) return;
        if (suppressed) temporarySuppressors.Add(owner); else temporarySuppressors.Remove(owner);
    }
    private void Start(){ResolveRuntime();}
    private void ResolveRuntime()
    {
        // A scene bootstrap may still exist until the end of its first frame.
        // Always prefer the surviving runtime over an arbitrary duplicate.
        var runtime=PersistentRuntime.Instance;
        if(runtime!=null){story=runtime.GetComponent<StoryStateManager>();notice=runtime.GetComponent<StoryNoticeUI>();}
        else if(story==null){story=FindAnyObjectByType<StoryStateManager>();notice=FindAnyObjectByType<StoryNoticeUI>();}
    }
    public void ObserveMovement(float horizontal)
    {
        ResolveRuntime();
        if(!allowStoryUnlock || story==null || horizontal<=0 || !story.Has(StoryFlag.ChildShadowFollowupPlayed) || story.Has(StoryFlag.SprintUnlocked))return;
        story.Set(StoryFlag.SprintUnlocked);if(notice!=null)notice.Show("[Shift] 奔跑",4f);
    }
    public float GetMultiplier(bool shiftHeld)
    {
        ResolveRuntime();
        temporarySuppressors.RemoveWhere(owner => owner == null);
        return shiftHeld && !IsTemporarilySuppressed && story!=null && story.Has(StoryFlag.SprintUnlocked) ? speedMultiplier : 1f;
    }
}
