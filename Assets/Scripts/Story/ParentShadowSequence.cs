using System.Collections;
using UnityEngine;

public class ParentShadowSequence : MonoBehaviour
{
    [SerializeField] private ItemData requiredItem;
    [SerializeField] private DialogueData dialogue;
    [SerializeField] private SpriteRenderer[] shadows;
    [SerializeField] private SpriteRenderer lamp, dimOverlay;
    [SerializeField] private Transform approachPoint;
    [SerializeField] private AudioSource sound;
    [SerializeField,Min(.1f)] private float approachDistance=2.4f;
    [SerializeField,Range(1f,1.5f)] private float introDuration=1.25f;
    [SerializeField,Min(.1f)] private float fadeDuration=1f;
    [SerializeField] private bool frameDoorAndLamp=true;
    private StoryStateManager story;private GameControlManager controls;
    private InventoryManager inventory;private DialogueManager dialogueManager;
    private Color lampColor;private Color[] shadowColors;
    private bool running,ownsLock;
    public bool IsWaitingForApproach {get;private set;}
    private void Awake()
    {
        lampColor=lamp!=null?lamp.color:Color.white;shadowColors=new Color[shadows.Length];
        for(int i=0;i<shadows.Length;i++){shadowColors[i]=shadows[i].color;shadows[i].enabled=false;}
        if(dimOverlay!=null)dimOverlay.enabled=false;
    }
    private void Start()
    {
        story=FindAnyObjectByType<StoryStateManager>();controls=FindAnyObjectByType<GameControlManager>();
        inventory=FindAnyObjectByType<InventoryManager>();dialogueManager=FindAnyObjectByType<DialogueManager>();
    }
    private void OnTriggerStay2D(Collider2D other)
    {
        var player=other.GetComponentInParent<PlayerController>();
        if(player==null || running || story==null || controls==null || inventory==null || dialogueManager==null || requiredItem==null || dialogue==null)return;
        if(story.Has(StoryFlag.ParentShadowEventPlayed) || !inventory.HasItem(requiredItem) || controls.IsLocked())return;
        // Approaching the left-hand exit from inside, never on arrival through that door.
        if(player.GetComponent<Rigidbody2D>().linearVelocity.x>=-.05f)return;
        running=true;StartCoroutine(Play(player.transform));
    }
    private IEnumerator Play(Transform player)
    {
        controls.AcquireLock(ControlLockReason.Cutscene);ownsLock=true;
        if(dimOverlay!=null)dimOverlay.enabled=true;
        for(float t=0;t<introDuration-.5f;t+=Time.unscaledDeltaTime)
        {
            if(frameDoorAndLamp && Camera.main!=null && lamp!=null && approachPoint!=null){var pos=Camera.main.transform.position;pos.x=Mathf.Lerp(pos.x,(lamp.transform.position.x+approachPoint.position.x)*.5f,5f*Time.unscaledDeltaTime);Camera.main.transform.position=pos;}
            float strength=t<(introDuration-.5f)*.45f ? .8f+.15f*Mathf.Sin(t*32) : t<(introDuration-.5f)*.65f ? .04f : .45f;
            if(lamp!=null)lamp.color=new Color(lampColor.r*strength,lampColor.g*strength,lampColor.b*strength,lampColor.a);
            yield return null;
        }
        for(int i=0;i<shadows.Length;i++){shadows[i].enabled=true;var c=shadowColors[i];c.a=0;shadows[i].color=c;}
        for(float t=0;t<.5f;t+=Time.unscaledDeltaTime){SetShadowAlpha(t/.5f);yield return null;}
        SetShadowAlpha(1);
        dialogueManager.StartDialogue(dialogue);
        controls.ReleaseLock(ControlLockReason.Cutscene);ownsLock=false;
        while(dialogueManager.IsDialogueActive())yield return null;
        IsWaitingForApproach=true;
        while(player!=null && Mathf.Abs(player.position.x-approachPoint.position.x)>approachDistance)yield return null;
        IsWaitingForApproach=false;
        for(float t=0;t<fadeDuration;t+=Time.unscaledDeltaTime)
        {
            SetShadowAlpha(1-t/fadeDuration);
            if(lamp!=null)lamp.color=Color.Lerp(new Color(lampColor.r*.45f,lampColor.g*.45f,lampColor.b*.45f,lampColor.a),lampColor,t/fadeDuration);
            if(dimOverlay!=null){var c=dimOverlay.color;c.a=.22f*(1-t/fadeDuration);dimOverlay.color=c;}
            yield return null;
        }
        Restore();story.Set(StoryFlag.ParentShadowEventPlayed);running=false;
    }
    private void SetShadowAlpha(float scale){for(int i=0;i<shadows.Length;i++){var c=shadowColors[i];c.a*=scale;shadows[i].color=c;}}
    private void Restore(){foreach(var r in shadows)if(r!=null)r.enabled=false;if(lamp!=null)lamp.color=lampColor;if(dimOverlay!=null)dimOverlay.enabled=false;}
    private void OnDisable(){if(running && story!=null)story.Set(StoryFlag.ParentShadowEventPlayed);StopAllCoroutines();if(ownsLock && controls!=null)controls.ReleaseLock(ControlLockReason.Cutscene);ownsLock=false;running=false;IsWaitingForApproach=false;Restore();}
}
