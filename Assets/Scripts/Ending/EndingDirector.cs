using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum EndingStep { Car, WalkChild, Child, WalkYouth, Youth, WalkHospital, Hospital, WalkBed, BedIntro, Focus, Declaration, Complete }
public sealed partial class EndingDirector:EndingStageController
{
    public GameObject riverLandmarksTemplate;
    [Header("Original actor walks along one foreground lane")]
    public float childWatchX=5, youthWatchX=19, hospitalWatchX=33, bedsideX=49;
    [Header("Walking shot composition")]
    public float memoryViewSize=3.2f, hospitalViewSize=2.5f;
    public EndingStep Step {get;private set;}
    public EndingFocusProgress Focus {get;private set;}
    public int ActiveStretchers {get;private set;}
    Transform presentation;
    GameObject carSet,childSet,youthSet,hospitalSet,wardSet,painHand;
    EndingActorView child,boy,youth,friend,carGuide;
    Transform[] stretchers;
    PlayerController player;Rigidbody2D playerBody;bool savedSimulated;
    Camera shotCamera;CameraFollow shotFollow;bool savedFollow;
    Vector3 savedPlayerPosition,savedCameraPosition;
    RectTransform endingCanvas;
    Image veil,bedMist,faceCover;
    GameObject focusPage,endPage;
    TMP_Text focusText;
    Button exitButton;
    bool bedArmed,endArmed;
    AudioSource monitor,footsteps;
    protected override bool PauseAvailable=>true;
    protected override IEnumerator Begin(){
        if(!session.SequenceStarted){Debug.LogWarning("Ending sequence requires actual boarding; no default completion or debug start.",this);yield break;}
        player=controls.ActivePlayer;playerBody=player.GetComponent<Rigidbody2D>();savedSimulated=playerBody.simulated;savedPlayerPosition=player.transform.position;
        shotCamera=Camera.main;shotFollow=shotCamera.GetComponent<CameraFollow>();savedFollow=shotFollow.enabled;savedCameraPosition=shotCamera.transform.position;
        Lock();playerBody.simulated=false;shotFollow.enabled=false;
        runtime.GetComponent<CameraModeController>()?.SetCaptureContext(null);
        BuildPresentation();BuildEndingUI();Focus=new EndingFocusProgress();
        story.Set(StoryFlag.EndingBoardingCompleted);
        if(story.Has(StoryFlag.DemoCompleted)){ShowCompletion();yield break;}
        if(story.Has(StoryFlag.EndingFinalLineCompleted)){ShowCompletion();yield break;}
        if(story.Has(StoryFlag.EndingFocusCompleted)){yield return Declaration();yield break;}
        if(!story.Has(StoryFlag.EndingCarCompleted)){yield return Car();if(!story.Has(StoryFlag.EndingCarCompleted))yield break;}
        ResumeWalking();
    }
    IEnumerator Car(){
        Step=EndingStep.Car;SetSets(carSet);PlaceForeground(-2.2f);player.transform.position=new Vector3(-2.2f,.15f,0);
        shotCamera.transform.position=new Vector3(0,1.5f,-10);shotCamera.orthographicSize=4.4f;
        for(int n=1;n<=15;n++){yield return Say("C"+n.ToString("00"));if(!speechCompleted)yield break;}
        painHand.SetActive(true);yield return FadeOwnedAudio(.7f);yield return Cover(Color.black,.55f);
        story.Set(StoryFlag.EndingCarCompleted);
    }
    void ResumeWalking(){
        shotCamera.orthographicSize=4.8f;veil.color=Color.clear;painHand.SetActive(true);Focus.RequireNewPress();
        PrepareWalkLane();PlaceForeground(0);
        if(story.Has(StoryFlag.EndingHospitalCompleted))EnterWalkStage(EndingStep.WalkBed);
        else if(story.Has(StoryFlag.EndingYouthCompleted))EnterWalkStage(EndingStep.WalkHospital);
        else if(story.Has(StoryFlag.EndingChildCompleted))EnterWalkStage(EndingStep.WalkYouth);
        else EnterWalkStage(EndingStep.WalkChild);
        bedArmed=false;
    }
    void ApplyWalkComposition(float x){
        if(!shotCamera)return;
        bool hospital=Step==EndingStep.WalkHospital||Step==EndingStep.Hospital||Step==EndingStep.WalkBed||Step==EndingStep.BedIntro;
        shotCamera.orthographicSize=hospital?hospitalViewSize:memoryViewSize;
        shotCamera.transform.position=new Vector3(x+(hospital?1.5f:3.2f),hospital?.25f:.9f,-10);
    }
    void PlaceForeground(float x){EndingSession.Place(player,new Vector3(x,foregroundY,0));ApplyWalkComposition(x);}
    protected override void Update(){
        base.Update();if(!ready||player==null||paused)return;
        if(IsWalking){
            float axis=GameplayInput.GetAxisRaw("Horizontal");
            TickWalk(axis,Delta);
            if(Step==EndingStep.WalkBed){if(!GameplayInput.GetKey(KeyCode.E)&&!dialogue.IsDialogueActive()&&!dialogue.DidDialogueEndThisFrame())bedArmed=true;bool near=Mathf.Abs(player.transform.position.x-bedAnchorX)<1.5f;
                focusText.text=near?"[E] 调查":"";focusText.gameObject.SetActive(true);
                if(near&&bedArmed&&GameplayInput.GetKeyDown(KeyCode.E))StartCoroutine(BedIntro());}
        }
        else if(Step==EndingStep.Focus){Focus.Tick(GameplayInput.GetKey(KeyCode.Space),GameplayInput.GetKeyDown(KeyCode.Space),Delta,data.focusSeconds,false);RenderFocus();if(Focus.Complete){story.Set(StoryFlag.EndingFocusCompleted);StartCoroutine(Declaration());}}
        else if(Step==EndingStep.Complete&&!endArmed){if(!GameplayInput.GetKey(KeyCode.E)&&!GameplayInput.GetKey(KeyCode.Space)){endArmed=true;exitButton.interactable=true;}}
    }
    bool IsWalking=>Step==EndingStep.WalkChild||Step==EndingStep.Child||Step==EndingStep.WalkYouth||Step==EndingStep.Youth
        ||Step==EndingStep.WalkHospital||Step==EndingStep.Hospital||Step==EndingStep.WalkBed;
    // Shared real input path, also exercised explicitly as API diagnostics (never labelled native).
    internal void TickWalk(float axis,float delta){
        if(!IsWalking||paused||Mathf.Abs(axis)<.001f)return;
        float x=player.transform.position.x+Mathf.Clamp(axis,-1,1)*player.WalkSpeed*data.flashbackRatio*Mathf.Clamp(delta,0,.1f);
        player.transform.position=new Vector3(x,foregroundY,0);
        ApplyWalkComposition(x);
        UpdateWalkStage();
        if(axis<=0||x<segmentTriggerX)return;
        if(Step==EndingStep.WalkChild){Step=EndingStep.Child;StartCoroutine(ChildMemory());}
        else if(Step==EndingStep.WalkYouth){Step=EndingStep.Youth;StartCoroutine(YouthMemory());}
        else if(Step==EndingStep.WalkHospital){Step=EndingStep.Hospital;StartCoroutine(HospitalMemory());}
    }
    protected override void Pause(bool value){base.Pause(value);Focus?.RequireNewPress();bedArmed=false;}
    IEnumerator Cover(Color color,float seconds){Color start=veil.color;float t=0;while(t<seconds){t+=Delta;veil.color=Color.Lerp(start,color,Mathf.Clamp01(t/seconds));yield return null;}veil.color=color;}
    IEnumerator FadeOwnedAudio(float seconds){var all=GetComponents<AudioSource>();var volumes=new float[all.Length];for(int i=0;i<all.Length;i++)volumes[i]=all[i].volume;float t=0;while(t<seconds){t+=Delta;for(int i=0;i<all.Length;i++)if(all[i]!=null)all[i].volume=volumes[i]*(1-Mathf.Clamp01(t/seconds));yield return null;}foreach(var a in all)if(a!=null)a.Stop();}
    void SetSets(GameObject active){foreach(var s in new[]{carSet,childSet,youthSet,hospitalSet,wardSet})if(s!=null)s.SetActive(s==active);}
    protected override void OnDisable(){
        if(dialogueCanvas!=null)dialogueCanvas.sortingOrder=originalDialogueOrder;
        if(dialogueBackground!=null)dialogueBackground.color=originalDialogueBackground;
        base.OnDisable();if(playerBody!=null){playerBody.simulated=savedSimulated;playerBody.linearVelocity=Vector2.zero;}
        if(shotFollow!=null)shotFollow.enabled=savedFollow;
        if(painHand!=null)Destroy(painHand);if(presentation!=null)Destroy(presentation.gameObject);if(endingCanvas!=null)Destroy(endingCanvas.gameObject);
        player=null;
    }
}
