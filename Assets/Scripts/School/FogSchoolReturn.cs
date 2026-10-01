using System.Collections;
using UnityEngine;

public partial class FogIntroController
{
    [Header("School return: reuse this same fog space, no outgoing encounter")]
    [SerializeField,Min(5)] private float returnWalkLength=25;
    bool schoolReturnMode,returnFinishing;
    SchoolHomeReturnController schoolHome;
    float returnExitX;
    public bool IsSchoolReturn=>schoolReturnMode;
    public bool TryEnterSchoolReturn(SchoolHomeReturnController home)
    {
        if(!Resolve()||home==null||fogSpawn==null||screenFade==null||!story.Has(StoryFlag.SchoolChaseCompleted))return false;
        var session=PersistentRuntime.Instance.GetComponent<SchoolChaseSession>();
        if(session==null||!session.PendingReturnFog||session.Checkpoint==null||session.Checkpoint.Reason!=SchoolCheckpointReason.FogReturn)return false;
        schoolReturnMode=true;schoolHome=home;returnFinishing=false;returnExitX=fogSpawn.position.x;
        if(uiRoot==null)BuildUI();ResetTransient();OwnTransition();Phase=FogIntroPhase.Entering;
        lamp.ChargeCompleted-=ChargeCompleted;cameraTool.AbortCapture();cameraTool.SetCaptureContext(null);cameraTool.ContextAvailable=true;
        StartCoroutine(EnterSchoolReturn());return true;
    }
    IEnumerator EnterSchoolReturn()
    {
        yield return screenFade.FadeThroughBlack(()=>{
            fogVisualRoot.SetActive(true);uiRoot.SetActive(true);ResetFigures();
            var p=controls.ActivePlayer;Vector3 position=fogSpawn.position+Vector3.right*returnWalkLength;p.transform.position=position;
            var b=p.GetComponent<Rigidbody2D>();b.position=position;b.linearVelocity=Vector2.zero;
            var cam=Camera.main;var follow=cam.GetComponent<CameraFollow>();cam.orthographicSize=cameraSize;follow.SetTarget(p.transform);
            follow.SetLimits(fogCameraLimits.x,fogCameraLimits.y);follow.SetVerticalLimits(true,0,cameraY,cameraY);follow.SetFacing(-1);follow.SnapToTarget();
            lamp.RestoreUseState(true);Physics2D.SyncTransforms();
        });
        Phase=FogIntroPhase.FreeWalk;ReleaseTransition();
    }
    void UpdateSchoolReturn()
    {
        if(Phase==FogIntroPhase.Dead){
            if(!GameplayInput.GetKey(KeyCode.E))retryArmed=true;
            if(retryArmed&&GameplayInput.GetKeyDown(KeyCode.E)&&Time.frameCount>completedFrame)RetrySchoolReturn();return;
        }
        if(Phase!=FogIntroPhase.FreeWalk||returnFinishing)return;
        UpdateDarkness();if(Phase==FogIntroPhase.Dead)return;
        if(CanStageAdvance()&&lamp.IsIlluminating&&controls.ActivePlayer.transform.position.x<=returnExitX){returnFinishing=true;StartCoroutine(FinishSchoolReturn());}
    }
    public bool RetrySchoolReturn()
    {
        if(!schoolReturnMode||Phase!=FogIntroPhase.Dead)return false;
        var session=PersistentRuntime.Instance.GetComponent<SchoolChaseSession>();session.PendingReturnFog=false;session.PendingHomeArrival=false;session.AdvanceGeneration();
        if(!WhiteSceneTransition.Begin(schoolHome.data.schoolScene,controls))return false;
        Phase=FogIntroPhase.Completed;ClearDanger();ReleaseCutscene();return true;
    }
    IEnumerator FinishSchoolReturn()
    {
        OwnTransition();ClearDanger();cameraTool.AbortCapture();
        var session=PersistentRuntime.Instance.GetComponent<SchoolChaseSession>();session.PendingHomeArrival=true;
        yield return screenFade.FadeThroughBlack(()=>{fogVisualRoot.SetActive(false);uiRoot.SetActive(false);schoolHome.PlaceHome();});
        Phase=FogIntroPhase.Completed;ReleaseTransition();schoolHome.FinishArrival();
    }
}
