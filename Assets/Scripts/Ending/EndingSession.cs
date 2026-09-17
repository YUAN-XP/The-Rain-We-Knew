using UnityEngine;
using UnityEngine.SceneManagement;

// Memory for this running PersistentRuntime only. No authored SO is mutated.
public sealed class EndingSession : MonoBehaviour
{
    public EndingData Data { get; private set; }
    public bool HandoverStarted { get; set; }
    public int HandoverNext { get; set; }
    public bool SirenPlayed { get; set; }
    public Vector3 HandoverAnchor { get; set; }
    public bool PendingStreetArrival { get; private set; }
    public Vector3 StreetArrival { get; private set; }
    public bool SequenceStarted { get; set; }
    StoryStateManager story; DialogueManager dialogue;
    public void Configure(EndingData data) {
        Data=data; story=GetComponent<StoryStateManager>();
        if(dialogue==null){dialogue=GetComponentInChildren<DialogueManager>(true);dialogue.NaturallyCompleted+=Completed;}
        (GetComponent<CombinationManager>()??gameObject.AddComponent<CombinationManager>()).Register(data.credentialRecipe);
    }
    void Completed(DialogueData completed) {
        if(Data!=null&&completed==Data.emailReaction&&story.Has(StoryFlag.SchoolEmailRead))story.Set(StoryFlag.EndingEmailReactionCompleted);
    }
    void OnDestroy(){if(dialogue!=null)dialogue.NaturallyCompleted-=Completed;}
    public bool IsEndingVisit => story!=null&&story.Has(StoryFlag.EndingStreetVisit);
    public void PrepareStreetReturn(string scene,Vector3 position) {
        if(Data==null||scene!=Data.streetScene||!story.Has(StoryFlag.EndingHandoverCompleted))return;
        StreetArrival=position; PendingStreetArrival=true; story.Set(StoryFlag.EndingStreetVisit);
    }
    public bool TryApplyArrival(Scene scene,PlayerController player) {
        if(!PendingStreetArrival||Data==null||scene.name!=Data.streetScene||player==null)return false;
        Place(player,StreetArrival);PendingStreetArrival=false;
        var follow=FindAnyObjectByType<CameraFollow>();if(follow!=null){follow.SetTarget(player.transform);follow.SetFacing(1);follow.SnapToTarget();}
        return true;
    }
    public static Vector3 CompanionAnchor(Transform guide,PlayerController player,float xOffset,float visualGroundOffset=0) {
        var view=guide.GetComponent<SpriteRenderer>();var collider=player.GetComponent<Collider2D>();
        float y=player.transform.position.y;
        var playerView=player.GetComponent<SpriteRenderer>();if(playerView==null)playerView=player.GetComponentInChildren<SpriteRenderer>();
        if(view!=null&&playerView!=null)y=playerView.bounds.min.y+(guide.position.y-view.bounds.min.y);
        else if(view!=null&&collider!=null)y=collider.bounds.min.y+(guide.position.y-view.bounds.min.y);
        return new Vector3(player.transform.position.x+xOffset,y+visualGroundOffset,player.transform.position.z);
    }
    public static void Place(PlayerController player,Vector3 position) {
        player.transform.position=position;var body=player.GetComponent<Rigidbody2D>();
        if(body!=null){body.position=position;body.linearVelocity=Vector2.zero;} Physics2D.SyncTransforms();
    }
}
