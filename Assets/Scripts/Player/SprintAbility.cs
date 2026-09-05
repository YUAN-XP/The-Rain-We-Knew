using UnityEngine;

[DisallowMultipleComponent]
public class SprintAbility : MonoBehaviour
{
    [SerializeField,Range(1.1f,2.5f)] private float speedMultiplier=1.7f;
    private StoryStateManager story;
    private StoryNoticeUI notice;
    private void Start(){story=FindAnyObjectByType<StoryStateManager>();notice=FindAnyObjectByType<StoryNoticeUI>();}
    public void ObserveMovement(float horizontal)
    {
        if(story==null || horizontal<=0 || !story.Has(StoryFlag.ChildShadowSequencePlayed) || story.Has(StoryFlag.SprintUnlocked))return;
        story.Set(StoryFlag.SprintUnlocked);if(notice!=null)notice.Show("[Shift] 奔跑",4f);
    }
    public float GetMultiplier(bool shiftHeld)=>shiftHeld && story!=null && story.Has(StoryFlag.SprintUnlocked) ? speedMultiplier : 1f;
}
