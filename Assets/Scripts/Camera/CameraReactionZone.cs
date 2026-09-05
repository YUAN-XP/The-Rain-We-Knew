using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CameraReactionZone : MonoBehaviour
{
    [SerializeField] private ItemData requiredItem;
    [SerializeField] private Collider2D tutorialContext;
    private StoryStateManager story; private StoryNoticeUI notice; private GameControlManager controls;
    public bool PlayerInside { get; private set; }
    public bool PlayerInContext {
        get { var player=FindAnyObjectByType<PlayerController>();return player!=null && tutorialContext!=null ? tutorialContext.bounds.Contains(player.transform.position) : PlayerInside; }
    }
    private bool ownsLock;
    private void Start() { story=FindAnyObjectByType<StoryStateManager>(); notice=FindAnyObjectByType<StoryNoticeUI>(); controls=FindAnyObjectByType<GameControlManager>(); }
    private void OnTriggerStay2D(Collider2D other)
    {
        if(other.GetComponent<PlayerController>()==null) return;
        PlayerInside=true;
        var inv=FindAnyObjectByType<InventoryManager>();
        if(story==null || controls==null || controls.IsLocked() || inv==null || !inv.HasItem(requiredItem))return;
        if(!story.Has(StoryFlag.CameraTutorialTriggered)) StartCoroutine(Reaction());
    }
    private IEnumerator Reaction()
    {
        story.Set(StoryFlag.CameraTutorialTriggered); controls.AcquireLock(ControlLockReason.Cutscene); ownsLock=true;
        notice.Show("相机似乎有些反应……",2.5f);
        yield return new WaitForSecondsRealtime(1.1f);
        controls.ReleaseLock(ControlLockReason.Cutscene); ownsLock=false;
        if(!story.Has(StoryFlag.CameraFirstRaised))notice.Tutorial("[Q] 举起 / 放下相机");
    }
    private void OnTriggerExit2D(Collider2D other) { if(other.GetComponent<PlayerController>()!=null)PlayerInside=false; }
    private void OnDisable(){ if(ownsLock && controls!=null)controls.ReleaseLock(ControlLockReason.Cutscene); }
}
