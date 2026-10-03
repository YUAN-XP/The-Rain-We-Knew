using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Opt-in review of a candidate in the existing game camera. No input or story injection.</summary>
[DefaultExecutionOrder(1600)]
public sealed class P02GameplayMotionProbe : MonoBehaviour
{
    public PlayerController player;
    public SpriteRenderer originalBody;
    public P02CharacterRig rig;
    public Camera gameCamera;
    public string directory;
    public float originalWalkSpeed, reviewWalkSpeed;
    public Func<Texture2D,double,bool> writeMovie;
    public Action closeMovie,restore;
    readonly List<Frame> frames=new List<Frame>();
    readonly List<string> errors=new List<string>();
    readonly List<PendingImage> images=new List<PendingImage>();
    sealed class PendingImage {public string path;public byte[] pixels;}
    RenderTexture target;
    Texture2D pixels;
    double begin,nextImage;
    bool capturing,runInBackground;
    bool groundChecked;
    int groundContactPixelDifferences=-1;
    int[] perFootContactPixelDifferences;
    string[] groundRendererStates;
    [Serializable] public class Frame
    {
        public double time;
        public string scene,image;
        public Vector3 actor,rigRoot;
        public float actualSpeed,phase,pelvisY,bodyLean,walkSpeed,deltaTime,timeScale,sprintMultiplier;
        public Vector2 bodyVelocity;
        public float paintedLeftSoleY,paintedRightSoleY;
        public bool paused,locked,running,leftContact,rightContact,shift;
        public int visibleRigParts;
    }
    [Serializable] public class Report
    {
        public string timestamp,scope,sourceRevision;
        public float originalWalkSpeed,temporaryReviewWalkSpeed;
        public string[] errors;
        public int groundContactPixelDifferences;
        public int[] perFootContactPixelDifferences;
        public string[] groundRendererStates;
        public Frame[] frames;
    }
    public void Begin()
    {
        Directory.CreateDirectory(directory+"frames/");
        target=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);
        pixels=new Texture2D(1920,1080,TextureFormat.RGBA32,false);
        runInBackground=Application.runInBackground;Application.runInBackground=true;
        begin=Time.realtimeSinceStartupAsDouble;capturing=true;
    }
    void Update()
    {
        if(!rig||!player||!originalBody)return;
        // Keep the original physical/interaction root and its ground anchor.
        rig.transform.position=originalBody.transform.TransformPoint(new Vector3(0,originalBody.sprite.bounds.min.y,0));
    }
    void LateUpdate()
    {
        if(!capturing||!rig||!player||!gameCamera)return;
        double seconds=Time.realtimeSinceStartupAsDouble-begin;
        var controls=PersistentRuntime.Instance?PersistentRuntime.Instance.GetComponent<GameControlManager>():null;
        int visible=0;foreach(var r in rig.GetComponentsInChildren<SpriteRenderer>())if(r.enabled&&!r.forceRenderingOff)visible++;
        var frame=new Frame{time=seconds,scene=SceneManager.GetActiveScene().name,actor=player.transform.position,
            rigRoot=rig.transform.position,actualSpeed=rig.actualSpeed,phase=rig.phase,pelvisY=rig.pelvisHeight,
            bodyLean=rig.bodyLean,walkSpeed=player.WalkSpeed,paused=GamePause.IsPaused,
            locked=controls&&controls.IsLocked(),running=rig.running,leftContact=rig.leftContact,rightContact=rig.rightContact,
            shift=GameplayInput.GetKey(KeyCode.LeftShift)||GameplayInput.GetKey(KeyCode.RightShift),
            paintedLeftSoleY=rig.leftSole.y,paintedRightSoleY=rig.rightSole.y,visibleRigParts=visible};
        frame.deltaTime=GameplayClock.Delta;frame.timeScale=Time.timeScale;
        frame.bodyVelocity=player.GetComponent<Rigidbody2D>().linearVelocity;
        frame.sprintMultiplier=player.GetComponent<SprintAbility>()?.GetMultiplier(frame.shift)??1;
        if(visible!=rig.MeshPartCount&&!errors.Contains("Rig parts unexpectedly hidden"))errors.Add("Rig parts unexpectedly hidden");
        if(frame.running&&player.GetComponent<SprintAbility>()?.GetMultiplier(frame.shift)<=1.01f)
            if(!errors.Contains("Run without sprint eligibility"))errors.Add("Run without sprint eligibility");
        var oldTarget=gameCamera.targetTexture;var active=RenderTexture.active;
        try
        {
            gameCamera.targetTexture=target;gameCamera.Render();RenderTexture.active=target;
            pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply(false);
            if(writeMovie!=null&&!writeMovie(pixels,seconds)&&!errors.Contains("Video frame rejected"))errors.Add("Video frame rejected");
            if(seconds>=nextImage&&images.Count<64){nextImage=seconds+.25;frame.image=directory+"frames/"+frames.Count.ToString("D5")+".png";images.Add(new PendingImage{path=frame.image,pixels=pixels.GetRawTextureData<byte>().ToArray()});}
        }
        finally{gameCamera.targetTexture=oldTarget;RenderTexture.active=active;}
        frames.Add(frame);
        if(seconds>=18)Finish();
    }
    void CheckGroundContactRendering()
    {
        groundChecked=true;
        var contact=rig.GetComponent<P02RigGroundContact>();
        if(!contact){errors.Add("Ground contact presentation missing");return;}
        var marks=contact.GetComponentsInChildren<MeshRenderer>();
        var enabledBefore=new bool[marks.Length];groundRendererStates=new string[marks.Length];
        perFootContactPixelDifferences=new int[marks.Length];
        var on=pixels.GetPixels32();
        File.WriteAllBytes(directory+"contact_on.png",pixels.EncodeToPNG());
        try
        {
            for(int i=0;i<marks.Length;i++)
            {
                var r=marks[i];enabledBefore[i]=r.enabled;
                groundRendererStates[i]=r.name+" enabled="+r.enabled+" layer="+r.sortingLayerID+" order="+r.sortingOrder
                    +" shaderSupported="+(r.sharedMaterial&&r.sharedMaterial.shader.isSupported)+" bounds="+r.bounds;
                r.enabled=false;
            }
            gameCamera.Render();RenderTexture.active=target;
            pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply(false);
            File.WriteAllBytes(directory+"contact_off.png",pixels.EncodeToPNG());
            var off=pixels.GetPixels32();groundContactPixelDifferences=0;
            for(int i=0;i<on.Length;i++)if(Math.Abs(on[i].r-off[i].r)+Math.Abs(on[i].g-off[i].g)+Math.Abs(on[i].b-off[i].b)>3)groundContactPixelDifferences++;
            if(groundContactPixelDifferences<20)errors.Add("Ground contact marks did not render visibly");
            for(int i=0;i<marks.Length;i++)
            {
                for(int j=0;j<marks.Length;j++)marks[j].enabled=enabledBefore[j]&&j!=i;
                gameCamera.Render();RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply(false);
                var individual=pixels.GetPixels32();int changed=0;
                for(int j=0;j<on.Length;j++)if(Math.Abs(on[j].r-individual[j].r)+Math.Abs(on[j].g-individual[j].g)+Math.Abs(on[j].b-individual[j].b)>3)changed++;
                perFootContactPixelDifferences[i]=changed;
            }
        }
        finally
        {
            for(int i=0;i<marks.Length;i++)marks[i].enabled=enabledBefore[i];
            gameCamera.Render();RenderTexture.active=target;
            pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply(false);
        }
    }
    public void Finish()
    {
        if(!capturing)return;
        capturing=false;
        closeMovie?.Invoke();closeMovie=null;writeMovie=null;
        // Re-render/PNG comparisons are deliberately outside the timed motion
        // recording. Their synchronous cost must not look like an animation jump.
        if(!groundChecked&&gameCamera&&rig&&pixels)
        {
            var oldTarget=gameCamera.targetTexture;var active=RenderTexture.active;
            try
            {
                gameCamera.targetTexture=target;gameCamera.Render();RenderTexture.active=target;
                pixels.ReadPixels(new Rect(0,0,1920,1080),0,0);pixels.Apply(false);
                CheckGroundContactRendering();
            }
            finally{gameCamera.targetTexture=oldTarget;RenderTexture.active=active;}
        }
        foreach(var image in images){pixels.LoadRawTextureData(image.pixels);pixels.Apply(false);File.WriteAllBytes(image.path,pixels.EncodeToPNG());}images.Clear();
        File.WriteAllText(directory+"gameplay_motion_probe.json",JsonUtility.ToJson(new Report{
            timestamp=DateTime.Now.ToString("O"),
            scope="Existing Scene01 PlayerController and GameplayInput replay; candidate rig temporarily attached; original world camera rendered at 1920x1080; overlay UI and physical keyboard are not certified by this movie; production settings restored on cleanup",
            sourceRevision="P02 native right-facing candidate",originalWalkSpeed=originalWalkSpeed,
            temporaryReviewWalkSpeed=reviewWalkSpeed,groundContactPixelDifferences=groundContactPixelDifferences,
            perFootContactPixelDifferences=perFootContactPixelDifferences,groundRendererStates=groundRendererStates,errors=errors.ToArray(),frames=frames.ToArray()},true));
        if(pixels)Destroy(pixels);if(target)Destroy(target);pixels=null;target=null;
        Application.runInBackground=runInBackground;
    }
    void OnDestroy(){Finish();restore?.Invoke();restore=null;if(rig)Destroy(rig.gameObject);}
}
