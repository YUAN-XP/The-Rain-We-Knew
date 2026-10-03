using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>One visible body. Scene physics and the P01 feet contract remain the movement owner.</summary>
[DisallowMultipleComponent, DefaultExecutionOrder(-110)]
public sealed class P02ActorPresentation : MonoBehaviour
{
    PlayerController player;
    SpriteRenderer original;
    StoryStateManager story;
    P02MotionLibrary library;
    Vector3 previous;
    string fixedActor, direction="right";
    float fixedHeight;
    float companionFootOffset;
    bool staticPose,originalForceOff;
    bool facingRestored;
    readonly System.Collections.Generic.Dictionary<SpriteRenderer,bool> companionLegacy=new System.Collections.Generic.Dictionary<SpriteRenderer,bool>();
    Scene03Chapter childChapter;
    readonly System.Collections.Generic.Dictionary<P02RigDefinition,P02CharacterRig> skinCache=new System.Collections.Generic.Dictionary<P02RigDefinition,P02CharacterRig>();
    Coroutine warming;
    string warmActor;
    bool wasExplicitPose;
    public P02CharacterRig Rig { get; private set; }
    public string ActorKey { get; private set; }
    public string Direction => direction;
    public bool OwnsBody => Rig && !staticPose && Rig.gameObject.activeInHierarchy;
    public bool CredentialOffer { get; set; }
    public float ToolBlend { get; set; }
    public string ToolKind { get; set; }
    public bool CompanionRunning { get; set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;AttachPlayers();}
    static void Loaded(Scene scene,LoadSceneMode mode)=>AttachPlayers();
    static void AttachPlayers(){foreach(var p in FindObjectsByType<PlayerController>(FindObjectsInactive.Include))
        if(!p.GetComponent<P02ActorPresentation>())p.gameObject.AddComponent<P02ActorPresentation>();}
    void Awake(){player=GetComponent<PlayerController>();previous=transform.position;library=P02MotionLibrary.Current;}
    public static P02ActorPresentation AttachCompanion(Transform owner,SpriteRenderer body,string key,float height)
    {
        if(!owner)return null;
        var view=owner.GetComponent<P02ActorPresentation>();if(!view)view=owner.gameObject.AddComponent<P02ActorPresentation>();
        view.fixedActor=key;view.fixedHeight=height;view.original=body;
        if(body){view.originalForceOff=body.forceRenderingOff;view.companionFootOffset=body.transform.TransformPoint(new Vector3(0,body.sprite?body.sprite.bounds.min.y:0,0)).y-owner.position.y;}
        foreach(var r in owner.GetComponentsInChildren<SpriteRenderer>(true))
            if(!r.GetComponentInParent<P02CharacterRig>()&&r.name!="HandDrawnHighlight_Runtime"&&!view.companionLegacy.ContainsKey(r))view.companionLegacy.Add(r,r.forceRenderingOff);
        view.ResetTravel();return view;
    }
    public void SetStaticPose(bool enabled){staticPose=enabled;if(Rig)Rig.gameObject.SetActive(!enabled);if(enabled)RestoreCompanionLegacy();}
    void RestoreCompanionLegacy(){foreach(var pair in companionLegacy)if(pair.Key)pair.Key.forceRenderingOff=pair.Value;}
    public void ResetTravel(){previous=transform.position;if(Rig)Rig.ResetTravel();}
    P02CharacterRig CreateSkin(P02RigDefinition definition){
        var root=new GameObject("P02_NativeBody");root.transform.SetParent(transform,false);root.SetActive(false);
        var rig=root.AddComponent<P02CharacterRig>();rig.definition=definition;rig.motionSource=transform;
        rig.targetHeight=fixedHeight>0?fixedHeight:definition.targetHeight;rig.Build();skinCache.Add(definition,rig);return rig;
    }
    IEnumerator WarmSkins(string key){
        // Prebuild inactive direction skins across initialization frames. Turning
        // must not construct ten SpriteSkins and their bones every time.
        yield return null;
        foreach(var e in library.entries.Where(e=>e.actor==key)){
            if(!skinCache.ContainsKey(e.rig)){CreateSkin(e.rig);yield return null;}
        }
        warming=null;
    }
    public void RestoreFacing(bool left)=>RestoreDirection(left?"left":"right");
    public void RestoreDirection(string value){
        if(!library)library=P02MotionLibrary.Current;
        if(library&&library.Find(ResolveActor(),value)){direction=value;facingRestored=true;ResetTravel();}
    }
    string ResolveActor()
    {
        if(!string.IsNullOrEmpty(fixedActor))return fixedActor;
        if(!story&&PersistentRuntime.Instance)story=PersistentRuntime.Instance.GetComponent<StoryStateManager>();
        if(GetComponent<AdultOutfitController>())return story&&story.Outfit==AdultOutfit.Overalls?"adult_overalls":
            story&&story.Outfit==AdultOutfit.SchoolUniform?"adult_school":"adult_default";
        return name.Contains("Youth")?"youth":"child";
    }
    void LateUpdate()
    {
        if(!library)library=P02MotionLibrary.Current;if(!library)return;
        if(!original){
            // Unity's destroyed component wrapper can be CLR-non-null while
            // comparing false as a Unity object. Do not use ?? for this fallback.
            var rootBody=GetComponent<SpriteRenderer>();
            original=rootBody?rootBody:GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r=>r&&r.name=="NewArt_Actor");
            if(original){originalForceOff=original.forceRenderingOff;if(!facingRestored)direction=original.flipX?"left":"right";}}
        if(!original)return;
        var delta=transform.position-previous;previous=transform.position;
        bool teleport=delta.magnitude>.8f;
        bool planar=player&&player.IsPlanarMovement;
        if(!teleport&&delta.sqrMagnitude>.00000001f){
            if(planar){var d=new Vector2(delta.x,delta.y);if(d.magnitude>.0001f)direction=PlanarDirection(d);}
            else if(Mathf.Abs(delta.x)>.0001f)direction=delta.x<0?"left":"right";
        }
        if(!planar&&direction!="left"&&direction!="right")direction=direction.Contains("left")?"left":"right";
        string key=ResolveActor();var definition=library.Find(key,direction);
        if(!definition){if(Rig)Rig.gameObject.SetActive(false);return;}
        var artwork=GetComponent<DemoActorArtwork>();
        var headache=transform.Find("HeadachePose");
        bool explicitPose=staticPose||artwork&&artwork.HasExplicitPose||headache&&headache.gameObject.activeInHierarchy;
        if(!Rig||Rig.definition!=definition){
            var old=Rig;
            if(!skinCache.TryGetValue(definition,out var cached))cached=CreateSkin(definition);
            Rig=cached;Rig.AdoptTravelState(old);
            if(old)old.gameObject.SetActive(false);
            ActorKey=key;
        }
        if(warmActor!=key){if(warming!=null)StopCoroutine(warming);warmActor=key;warming=StartCoroutine(WarmSkins(key));}
        Rig.gameObject.SetActive(!explicitPose);
        if(explicitPose){wasExplicitPose=true;return;}
        // The childhood square camera is an old always-carried decoration.
        // Q/Space are presented by the single tool owner on the actual hand.
        if(player&&key=="child"){
            if(!childChapter)childChapter=FindAnyObjectByType<Scene03Chapter>();
            if(childChapter&&childChapter.child==player&&childChapter.carriedCamera)childChapter.carriedCamera.SetActive(false);
        }
        foreach(var pair in companionLegacy)if(pair.Key)pair.Key.forceRenderingOff=true;
        var foot=!string.IsNullOrEmpty(fixedActor)?transform.position+Vector3.up*companionFootOffset:
            original.sprite?original.transform.TransformPoint(new Vector3(0,original.sprite.bounds.min.y,0)):transform.position;
        Rig.transform.position=foot;
        var scale=transform.lossyScale;Rig.transform.localScale=new Vector3(1/Mathf.Abs(scale.x),1/Mathf.Abs(scale.y),1);
        // Inactive rigs did not observe seated travel. Discard their previous
        // world-space sole anchors when handing back to the moving body.
        if(wasExplicitPose){Rig.ResetTravel();wasExplicitPose=false;}
        Rig.planarMovement=planar;Rig.ToolBlend=ToolBlend;Rig.ToolKind=ToolKind;Rig.CredentialOffer=CredentialOffer;
        var follower=GetComponent<P02CompanionFollow>();
        Rig.sampleRunning=follower&&follower.IsFollowing?CompanionRunning:
            !player&&GameplayClock.Delta>0&&!teleport&&delta.magnitude/GameplayClock.Delta>definition.sampleWalkSpeed*1.4f;
        if(teleport)Rig.ResetTravel();
        var group=Rig.GetComponent<SortingGroup>();group.sortingLayerID=original.sortingLayerID;group.sortingOrder=original.sortingOrder+2;
        foreach(var part in Rig.GetComponentsInChildren<SpriteRenderer>())part.color=new Color(1,1,1,original.color.a);
        original.forceRenderingOff=true;
    }
    public static string PlanarDirection(Vector2 v)
    {
        int octant=Mathf.RoundToInt(Mathf.Atan2(v.y,v.x)*Mathf.Rad2Deg/45f);
        switch((octant+8)%8){case 0:return "right";case 1:return "up_right";case 2:return "up";case 3:return "up_left";
            case 4:return "left";case 5:return "down_left";case 6:return "down";default:return "down_right";}
    }
    void OnDisable(){if(warming!=null)StopCoroutine(warming);warming=null;warmActor=null;if(Rig)Rig.gameObject.SetActive(false);RestoreCompanionLegacy();if(original)original.forceRenderingOff=originalForceOff;}
}
