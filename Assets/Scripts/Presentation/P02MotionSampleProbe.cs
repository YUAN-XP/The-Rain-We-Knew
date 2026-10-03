using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D.Animation;

/// <summary>Isolated diagnostics. Measured synthetic movement is not production input evidence.</summary>
[DefaultExecutionOrder(1500)]
public sealed class P02MotionSampleProbe : MonoBehaviour
{
    const string Work = "D:/游戏设计/Codex工作区/work/2026-10-02-P02/";
    P02CharacterRig rig;
    float speed;
    public string runLabel;
    Vector2 motionAxis;
    bool automatic;
    readonly List<Frame> frames = new List<Frame>();
    readonly List<string> failures = new List<string>();
    readonly List<PendingImage> pendingImages = new List<PendingImage>();
    readonly HashSet<string> selectedBins = new HashSet<string>();
    int captureIndex;
    Camera cameraObject;
    SpriteSkin[] skins;
    readonly Dictionary<SpriteSkin,Vector2[]> opaqueShoeEdges=new Dictionary<SpriteSkin,Vector2[]>();
    int stage;
    float delay, previousPhase, travelledCycles, walkCycles, runCycles;
    double startTime;
    bool originalRunInBackground;
    string runDirectory, moviePath, slowMoviePath;
    RenderTexture renderTarget;
    Texture2D pixels;
    RenderTexture closeupTarget;
    Texture2D closeupPixels;
    Func<Texture2D,double,bool> movieFrame;
    Action closeMovie;
    byte[][] captureBuffers;
    TextMesh caption;
    sealed class PendingImage { public string path; public byte[] data; public int width=1920; }

    [Serializable] public sealed class Frame
    {
        public string action, image;
        public string upperJoints,lowerJoints;
        public float phase, speed, deltaTime, leftX, leftY, rightX, rightY;
        public float paintedLeftSoleY, paintedRightSoleY;
        public float rootX,rootY,bodyHeight, bodyLean, pelvisY, leftAnkleGap, rightAnkleGap;
        public Vector3 leftGround,rightGround;
        public Vector3 nearHeel, nearToe, farHeel, farToe;
        public float nearSupportAngle,farSupportAngle;
        public int nearSoleSupportIndex,farSoleSupportIndex;
        public double time;
        public bool leftContact, rightContact;
        public int deformedMeshes;
    }
    [Serializable] public sealed class Report
    {
        public string timestamp, scope, movie, slowMovie, nearAnatomicalSide,farAnatomicalSide,soleMeasurement,direction,legLengthMeasurement,depthContactMeasurement;
        public int meshes, duplicateMeshes;
        public float measuredWalkCycles, measuredRunCycles;
        public string[] failures;
        public Frame[] frames;
    }
    void Awake()
    {
        rig = GetComponent<P02CharacterRig>();rig.sampleMode = false;
        string direction=rig.definition.direction;
        motionAxis=new Vector2(direction=="left"||direction.EndsWith("_left")?-1:direction=="right"||direction.EndsWith("_right")?1:0,
            direction.StartsWith("up")?1:direction.StartsWith("down")?-1:0).normalized;
        rig.planarMovement=motionAxis.y!=0;
        cameraObject = Camera.main;skins=GetComponentsInChildren<SpriteSkin>();
        foreach(var skin in skins)if(skin.name.EndsWith("_shoe"))
        {
            // Independent diagnostic read of source alpha. No transparent grid
            // vertex is allowed to masquerade as the visible painted shoe sole.
            var sprite=skin.GetComponent<SpriteRenderer>().sprite;
            var source=sprite.texture.GetPixels32();var rect=sprite.rect;
            var edges=new List<Vector2>();
            for(int x=0;x<(int)rect.width;x++)for(int y=0;y<(int)rect.height;y++)
            {
                int index=((int)rect.y+y)*sprite.texture.width+(int)rect.x+x;
                if(source[index].a<128)continue;
                edges.Add(new Vector2(x+.5f-sprite.pivot.x,y-sprite.pivot.y)/sprite.pixelsPerUnit);break;
            }
            opaqueShoeEdges[skin]=edges.ToArray();
        }
        originalRunInBackground=Application.runInBackground;Application.runInBackground=true;
        float h=rig.targetHeight/rig.definition.referenceHeight;
        cameraObject.orthographicSize=1.55f*h;
        cameraObject.transform.position = new Vector3(transform.position.x+.1f,transform.position.y+.9f*h,-10);
        AddGroundReference();
        caption=new GameObject("P02 diagnostic caption").AddComponent<TextMesh>();
        caption.transform.SetParent(transform,false);caption.transform.localPosition=new Vector3(0,2.18f*h,0);
        caption.anchor=TextAnchor.MiddleCenter;caption.fontSize=28;caption.characterSize=.028f*h;
        caption.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        caption.GetComponent<MeshRenderer>().material=caption.font.material;caption.GetComponent<MeshRenderer>().sortingOrder=100;
    }
    void Update()
    {
        if (!automatic){rig.sampleRunning=Input.GetKey(KeyCode.LeftShift);speed=Input.GetKey(KeyCode.D)?
            (rig.sampleRunning?rig.definition.sampleRunSpeed:rig.definition.sampleWalkSpeed):0;}
        else rig.sampleRunning=stage==3||stage==4;
        caption.text=(speed==0?"IDLE":rig.sampleRunning?"RUN":"WALK")+"  |  "+speed.ToString("F2")+" units/s  |  "+
            (speed>0?((rig.sampleRunning?rig.definition.runningStride:rig.definition.walkingStride)*rig.targetHeight/rig.definition.referenceHeight/speed).ToString("F2")+" s/cycle":"ground reference");
        transform.position += new Vector3(motionAxis.x,motionAxis.y,0)*speed*Time.unscaledDeltaTime;
    }
    void LateUpdate()
    {
        if (cameraObject)cameraObject.transform.position = new Vector3(transform.position.x+.1f,transform.position.y+.9f*rig.targetHeight/rig.definition.referenceHeight,-10);
        if(!automatic)return;
        string action=stage==0?"idle":stage<=2?"walk":stage<=4?"run":"settled_idle";
        Capture(action);
        if(delay>0){delay-=Time.unscaledDeltaTime;return;}
        if(stage==0){speed=rig.definition.sampleWalkSpeed;stage=1;delay=.45f;return;}
        if(stage==1||stage==3){travelledCycles=0;previousPhase=rig.phase;stage++;return;}
        if(stage==2||stage==4)
        {
            travelledCycles+=Mathf.Repeat(rig.phase-previousPhase,1);previousPhase=rig.phase;
            if(travelledCycles>=3)
            {
                if(stage==2){walkCycles=travelledCycles;speed=rig.definition.sampleRunSpeed;stage=3;delay=.45f;}
                else{runCycles=travelledCycles;speed=0;stage=5;delay=.8f;}
            }
            return;
        }
        Finish();
    }
    public string PrepareRun()
    {
        if(automatic)throw new InvalidOperationException("Sample check already running");
        string evidence="D:/GitHub Desktop/ProjectMystery/ProjectMystery/Documentation/FirstLargeTest_20261001/P02Evidence/";
        runDirectory=evidence+(string.IsNullOrEmpty(runLabel)?"sample_runs/":"production_runs/"+runLabel+"/")+DateTime.Now.ToString("yyyyMMdd_HHmmss")+"/";
        Directory.CreateDirectory(runDirectory+"frames/");Directory.CreateDirectory(runDirectory+"joints/");return runDirectory;
    }
    public void StartChecks(Func<Texture2D,double,bool> recordFrame, Action finishRecording, string movie,string slowMovie)
    {
        if(automatic)throw new InvalidOperationException("Sample check already running");
        if(string.IsNullOrEmpty(runDirectory))PrepareRun();
        movieFrame=recordFrame;closeMovie=finishRecording;moviePath=movie;slowMoviePath=slowMovie;
        frames.Clear();failures.Clear();pendingImages.Clear();selectedBins.Clear();captureIndex=0;
        renderTarget=new RenderTexture(1920,1080,24,RenderTextureFormat.ARGB32);
        pixels=new Texture2D(1920,1080,TextureFormat.RGBA32,false);
        closeupTarget=new RenderTexture(960,540,24,RenderTextureFormat.ARGB32);
        closeupPixels=new Texture2D(960,540,TextureFormat.RGBA32,false);
        captureBuffers=new byte[52][];
        // Capture only selected phase bins; PNGs are flushed immediately on D.
        speed=0;stage=0;delay=.4f;startTime=Time.realtimeSinceStartupAsDouble;automatic=true;
    }
    void Finish()
    {
        automatic=false;CloseRecording();
        foreach(string action in new[]{"walk","run"}){
            var loop=frames.Where(f=>f.action==action && f.speed>.025f).ToArray();
            if(loop.Length>0 && loop.Max(f=>f.pelvisY)-loop.Min(f=>f.pelvisY)>.12f)
                failures.Add(action+": excessive pelvis excursion relative to the 1.8-unit bind reference");
        }
        // PNG compression happens after motion, preventing diagnostic capture from destroying frame rate.
        foreach(var image in pendingImages)
        {
            var imagePixels=image.width==1920?pixels:closeupPixels;
            imagePixels.LoadRawTextureData(image.data);imagePixels.Apply();File.WriteAllBytes(image.path,imagePixels.EncodeToPNG());
        }
        pendingImages.Clear();captureBuffers=null;
        var report = new Report {
            timestamp=DateTime.Now.ToString("O"),scope="isolated native rig; measured synthetic Transform movement; visual QA required; no production gameplay input replay",
            movie=moviePath,slowMovie=slowMoviePath,nearAnatomicalSide=rig.NearAnatomicalSide,farAnatomicalSide=rig.FarAnatomicalSide,
            soleMeasurement="independent imported opaque alpha lower contour, threshold 128; shoes are rigid native skins; transparent rectangle bounds are excluded",
            depthContactMeasurement=rig.definition.viewKind=="profile"?"world stance anchor; painted contour and heel-to-toe transfer":"2D ground-depth foreshortening: 0.14 projected stride; stance anchor compensates the same screen-depth displacement; not a 3D fixed world-Y contact",
            direction=rig.definition.direction,legLengthMeasurement=rig.definition.viewKind=="profile"?"fixed length native limb endpoints":"not applicable: frontal limbs project depth travel with foreshortening; visual review required",
            meshes=rig.MeshPartCount,duplicateMeshes=skins.Length-rig.MeshPartCount,
            measuredWalkCycles=walkCycles,measuredRunCycles=runCycles,failures=failures.ToArray(),frames=frames.ToArray()
        };
        string json=JsonUtility.ToJson(report,true);
        File.WriteAllText(runDirectory+"sample_motion_probe.json",json);
        if(string.IsNullOrEmpty(runLabel))File.WriteAllText(Work+"sample_motion_probe.json",json);
        ReleaseCapture();
    }
    void CloseRecording()
    {
        if(closeMovie==null)return;
        try{closeMovie();}catch(Exception e){failures.Add("Recording close: "+e.Message);}
        closeMovie=null;movieFrame=null;
    }
    void ReleaseCapture()
    {
        if(pixels)Destroy(pixels);if(renderTarget){renderTarget.Release();Destroy(renderTarget);}pixels=null;renderTarget=null;
        if(closeupPixels)Destroy(closeupPixels);if(closeupTarget){closeupTarget.Release();Destroy(closeupTarget);}closeupPixels=null;closeupTarget=null;
    }
    void OnDestroy(){CloseRecording();ReleaseCapture();if(referenceRoot)Destroy(referenceRoot);if(referenceMaterial)Destroy(referenceMaterial);Application.runInBackground=originalRunInBackground;}
    void Capture(string action)
    {
        int deformed=0;float leftPaint=float.PositiveInfinity,rightPaint=float.PositiveInfinity;
        foreach (var skin in skins)
        {
            try
            {
                var vertices=skin.GetDeformedVertexPositionData();if(vertices.Count()==297)deformed++;
                if(skin.name=="skin_near_shoe"||skin.name=="skin_far_shoe")
                {
                    float lowest=float.PositiveInfinity;
                    foreach(var v in opaqueShoeEdges[skin])lowest=Mathf.Min(lowest,skin.transform.TransformPoint(v).y);
                    if((skin.name=="skin_near_shoe")==(rig.NearAnatomicalSide=="left"))leftPaint=lowest;else rightPaint=lowest;
                }
            }
            catch(Exception e){string failure=skin.name+": "+e.Message;if(!failures.Contains(failure))failures.Add(failure);}
        }
        double seconds=Time.realtimeSinceStartupAsDouble-startTime;ReadCamera(cameraObject,renderTarget,pixels);
        if(movieFrame!=null&&!movieFrame(pixels,seconds)&&!failures.Contains("Video frame rejected"))failures.Add("Video frame rejected");
        string bin=action+"_"+(stage==1||stage==3?"warm":Mathf.FloorToInt(rig.phase*24).ToString());
        string image="";
        string upper="",lower="";
        bool keep=action=="idle" ? selectedBins.Add(action) : action=="settled_idle" ?
            delay<=0&&selectedBins.Add(action) : (stage==2||stage==4)&&selectedBins.Add(bin);
        if(keep)
        {
            image=runDirectory+"frames/"+captureIndex.ToString("D4")+"_"+bin+".png";
            File.WriteAllBytes(image,pixels.EncodeToPNG());
            int phaseBin=Mathf.FloorToInt(rig.phase*24);
            if((action=="walk"||action=="run")&&(phaseBin==0||phaseBin==6||phaseBin==12||phaseBin==18))
            {
                upper=CaptureJointImage(bin,"neck_shoulder_wrist",1.34f,.53f);
                lower=CaptureJointImage(bin,"hip_knee_ankle",.47f,.55f);
            }
        }
        frames.Add(new Frame {action=action,image=image,phase=rig.phase,speed=rig.actualSpeed,deltaTime=Time.unscaledDeltaTime,
            time=seconds,leftX=rig.leftSole.x,leftY=rig.leftSole.y,rightX=rig.rightSole.x,rightY=rig.rightSole.y,
            paintedLeftSoleY=leftPaint,paintedRightSoleY=rightPaint,leftContact=rig.leftContact,rightContact=rig.rightContact,deformedMeshes=deformed,
            upperJoints=upper,lowerJoints=lower,rootX=transform.position.x,rootY=transform.position.y,bodyHeight=rig.targetHeight,leftGround=rig.leftGround,rightGround=rig.rightGround,bodyLean=rig.bodyLean,pelvisY=rig.pelvisHeight,
            leftAnkleGap=rig.farLegEndpointGap,rightAnkleGap=rig.nearLegEndpointGap,
            nearHeel=rig.nearHeel,nearToe=rig.nearToe,farHeel=rig.farHeel,farToe=rig.farToe,
            nearSupportAngle=rig.nearSupportAngle,farSupportAngle=rig.farSupportAngle,
            nearSoleSupportIndex=rig.nearSoleSupportIndex,farSoleSupportIndex=rig.farSoleSupportIndex});
        captureIndex++;
    }
    string CaptureJointImage(string phaseBin,string group,float height,float halfHeight)
    {
        var position=cameraObject.transform.position;float size=cameraObject.orthographicSize;
        string path=runDirectory+"joints/"+captureIndex.ToString("D4")+"_"+phaseBin+"_"+group+".png";
        try
        {
            float h=rig.targetHeight/rig.definition.referenceHeight;
            cameraObject.transform.position=new Vector3(transform.position.x+.08f,transform.position.y+height*h,-10);
            cameraObject.orthographicSize=halfHeight*h;ReadCamera(cameraObject,closeupTarget,closeupPixels);
            File.WriteAllBytes(path,closeupPixels.EncodeToPNG());
        }
        finally{cameraObject.transform.position=position;cameraObject.orthographicSize=size;}
        return path;
    }
    void AddGroundReference()
    {
        var root=new GameObject("P02 static ground reference");root.hideFlags=HideFlags.DontSave;
        var material=new Material(Shader.Find("Sprites/Default"));
        int count=rig.planarMovement?81:1;
        for(int i=0;i<count;i++){
            float y=rig.planarMovement?(i-40)*.5f:0;
            var line=new GameObject("ground "+i).AddComponent<LineRenderer>();line.transform.SetParent(root.transform,false);
            line.sharedMaterial=material;line.useWorldSpace=true;line.positionCount=2;
            line.SetPosition(0,new Vector3(-60,y,1));line.SetPosition(1,new Vector3(60,y,1));
            line.startWidth=line.endWidth=.007f;line.startColor=line.endColor=new Color(.45f,.46f,.47f,.5f);line.sortingOrder=-100;
        }
        referenceRoot=root;referenceMaterial=material;
    }
    GameObject referenceRoot;Material referenceMaterial;
    static void ReadCamera(Camera camera,RenderTexture texture,Texture2D data)
    {
        var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;
        try
        {
            camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;
            data.ReadPixels(new Rect(0,0,texture.width,texture.height),0,0);data.Apply();
        }
        finally{camera.targetTexture=previousTarget;RenderTexture.active=previousActive;}
    }
    public static void Render(string path,int width,int height)
    {
        var texture=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);var png=new Texture2D(width,height,TextureFormat.RGBA32,false);
        try{ReadCamera(Camera.main,texture,png);File.WriteAllBytes(path,png.EncodeToPNG());}finally{Destroy(png);Destroy(texture);}
    }
}
