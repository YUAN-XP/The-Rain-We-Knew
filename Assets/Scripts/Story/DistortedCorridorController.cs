using UnityEngine;

public class DistortedCorridorController : MonoBehaviour
{
    [SerializeField] private GameObject corridorArea;
    [SerializeField] private Transform joinPoint;
    [SerializeField] private Vector3 spaceOffset=new Vector3(200,24,0);
    [SerializeField] private SpriteRenderer familiarDoor;
    [SerializeField] private SpriteRenderer[] anomalyLayers;
    [SerializeField] private Collider2D rearBoundary;
    [SerializeField] private GameObject sourceExtension;
    [SerializeField] private GameObject originalRightBoundary;
    [SerializeField] private Collider2D joinedRearBoundary;
    [SerializeField] private AudioSource guitarGuide;
    [SerializeField] private float remarkWorldX=0;
    [SerializeField] private float distortionStartX=216;
    [SerializeField] private float distortionEndX=248;
    private PlayerController player;private StoryStateManager story;private StoryNoticeUI notice;private GameControlManager controls;
    private bool joined;
    private Color[] colors;
    public bool HasJoined=>joined;
    private void Start()
    {
        player=FindAnyObjectByType<PlayerController>();story=FindAnyObjectByType<StoryStateManager>();notice=FindAnyObjectByType<StoryNoticeUI>();controls=FindAnyObjectByType<GameControlManager>();
        colors=new Color[anomalyLayers.Length];for(int i=0;i<colors.Length;i++)colors[i]=anomalyLayers[i].color;
        corridorArea.SetActive(false);rearBoundary.enabled=false;guitarGuide.volume=0;
        if(sourceExtension!=null)sourceExtension.SetActive(false);
        if(joinedRearBoundary!=null)joinedRearBoundary.enabled=false;
    }
    private void LateUpdate()
    {
        if(story==null || player==null || !story.Has(StoryFlag.ChildShadowSequencePlayed))return;
        corridorArea.SetActive(true);
        if(sourceExtension!=null)sourceExtension.SetActive(true);
        if(originalRightBoundary!=null)originalRightBoundary.SetActive(false);
        bool upstairs=Mathf.Abs(player.transform.position.y-joinPoint.position.y)<2;
        if(!joined && upstairs){
            rearBoundary.enabled=true;
            var rb=player.GetComponent<Rigidbody2D>();
            if(player.transform.position.x>remarkWorldX && rb.linearVelocity.x>.1f && !story.Has(StoryFlag.CorridorRemarkShown)){
                story.Set(StoryFlag.CorridorRemarkShown);notice.Show("走廊好像不一样了。",3.5f);
            }
            if(player.transform.position.x>=joinPoint.position.x && !controls.IsLocked()){
                player.transform.position+=spaceOffset;rb.position=player.transform.position;
                // Preserve the exact screen-relative offset at the seam; no black frame or forced motion.
                if(Camera.main!=null)Camera.main.transform.position+=spaceOffset;
                joined=true;
                if(joinedRearBoundary!=null)joinedRearBoundary.enabled=true;
            }
        }
        float amount=0;
        if(joined && Camera.main!=null && Camera.main.WorldToViewportPoint(familiarDoor.bounds.max).x<0)
            amount=Mathf.InverseLerp(distortionStartX,distortionEndX,player.transform.position.x);
        for(int i=0;i<anomalyLayers.Length;i++){
            var c=colors[i];c.a=amount*(.6f+.35f*Mathf.Sin(Time.time*(2f+i*.17f)+i));anomalyLayers[i].color=c;
        }
        guitarGuide.volume=player.transform.position.x<250 ? amount*.22f : 0;
        if(joined && !guitarGuide.isPlaying)guitarGuide.Play();
    }
}
