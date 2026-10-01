using System.Collections;
using UnityEngine;

public class ThreeDoorPuzzleController : MonoBehaviour
{
    [SerializeField] private Transform resetPoint;
    [SerializeField] private Collider2D leftBoundary;
    [SerializeField] private ScreenFadeController screenFade;
    [SerializeField] private Scene03TransitionBridge bridge;
    [SerializeField] private AudioSource[] doorSounds;
    [SerializeField] private AudioClip guitarClip;
    [SerializeField] private AudioClip errorClip;
    [SerializeField] private AudioSource errorSound;
    [SerializeField] private SpriteRenderer[] abnormalLayers;
    [SerializeField] private SpriteRenderer[] normalWalls;
    [SerializeField,Range(0f,1f)] private float strongestVolume=.65f;
    [SerializeField,Range(0f,1f)] private float weakerVolume=.20f;
    [SerializeField,Range(0f,1f)] private float weakestVolume=.09f;
    [SerializeField] private float strongestCutoff=15000;
    [SerializeField] private float weakerCutoff=1700;
    private readonly int[] sequence={2,0,1};
    private GameControlManager controls;private StoryStateManager story;private PlayerController player;
    private Color[] abnormalColors;
    public int Progress {get;private set;}
    public bool IsActive {get;private set;}
    public bool IsBusy {get;private set;}
    public int CorrectDoor => sequence[Mathf.Clamp(Progress,0,2)];
    private void Start()
    {
        controls=FindAnyObjectByType<GameControlManager>();story=FindAnyObjectByType<StoryStateManager>();player=FindAnyObjectByType<PlayerController>();
        abnormalColors=new Color[abnormalLayers.Length];for(int i=0;i<abnormalLayers.Length;i++)abnormalColors[i]=abnormalLayers[i].color;
        leftBoundary.enabled=false;leftBoundary.GetComponent<SpriteRenderer>().enabled=false;SetRecovery(0);
        double start=AudioSettings.dspTime+.1;
        foreach(var sound in doorSounds){sound.clip=guitarClip;sound.loop=true;sound.spatialBlend=0;sound.volume=0;sound.PlayScheduled(start);}
        if(story!=null && story.Has(StoryFlag.ThreeDoorPuzzleCompleted)){Progress=3;SetRecovery(3);}
    }
    private void OnTriggerStay2D(Collider2D other)
    {
        if(other.GetComponent<PlayerController>()==null || story==null || !story.Has(StoryFlag.ChildShadowSequencePlayed))return;
        IsActive=true;leftBoundary.enabled=true;leftBoundary.GetComponent<SpriteRenderer>().enabled=true;
    }
    private void Update()
    {
        if(player==null)return;
        for(int i=0;i<doorSounds.Length;i++){
            bool correct=i==CorrectDoor;
            float volume=correct?strongestVolume:(i==(CorrectDoor+1)%3?weakerVolume:weakestVolume);
            float distance=Mathf.Abs(player.transform.position.x-doorSounds[i].transform.position.x);
            doorSounds[i].volume=IsActive ? volume/(1+distance*.12f) : 0;
            doorSounds[i].panStereo=Mathf.Clamp((doorSounds[i].transform.position.x-player.transform.position.x)/18f,-.8f,.8f);
            var filter=doorSounds[i].GetComponent<AudioLowPassFilter>();
            if(filter!=null)filter.cutoffFrequency=correct?Mathf.Min(22000,strongestCutoff+Progress*2500):weakerCutoff;
        }
        for(int i=0;i<abnormalLayers.Length;i++)if(abnormalLayers[i].enabled){
            var color=abnormalColors[i];color.a*=.6f+.4f*Mathf.Abs(Mathf.Sin(Time.time*(3+i*.2f)));abnormalLayers[i].color=color;
        }
    }
    public bool Choose(int door)
    {
        if(!IsActive || IsBusy || Progress>=3 || controls==null || controls.IsLocked() || door<0 || door>=3)return false;
        StartCoroutine(Resolve(door));return true;
    }
    private IEnumerator Resolve(int door)
    {
        IsBusy=true;controls.AcquireLock(ControlLockReason.Cutscene);
        bool correct=door==CorrectDoor;
        if(!correct && errorClip!=null)errorSound.PlayOneShot(errorClip,.45f);
        if(!correct){SetRecovery(0);foreach(var wall in normalWalls)wall.color=new Color(.7f,.42f,.4f);yield return GameplayClock.Wait(.12f);}
        if(correct && Progress==2){
            Progress=3;SetRecovery(3);story.Set(StoryFlag.ThreeDoorPuzzleCompleted);
            yield return GameplayClock.Wait(1.2f);
            controls.ReleaseLock(ControlLockReason.Cutscene);IsBusy=false;
            bridge.BeginTransition();yield break;
        }
        yield return screenFade.FadeThroughBlack(()=>{
            Progress=correct?Progress+1:0;SetRecovery(Progress);
            player.transform.position=resetPoint.position;var rb=player.GetComponent<Rigidbody2D>();rb.position=resetPoint.position;rb.linearVelocity=Vector2.zero;
            var follow=FindAnyObjectByType<CameraFollow>();if(follow!=null)follow.SnapToTarget();
        });
        controls.ReleaseLock(ControlLockReason.Cutscene);IsBusy=false;
    }
    public void RestoreSave(int progress){Progress=Mathf.Clamp(progress,0,3);SetRecovery(Progress);}
    private void SetRecovery(int level)
    {
        for(int i=0;i<abnormalLayers.Length;i++)abnormalLayers[i].enabled=level<3 && i%3>=level;
        foreach(var wall in normalWalls)wall.color=Color.Lerp(new Color(.20f,.23f,.23f),new Color(.69f,.64f,.51f),level/3f);
    }
    private void OnDisable(){if(IsBusy && controls!=null)controls.ReleaseLock(ControlLockReason.Cutscene);IsBusy=false;}
}
