using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;
using UnityEngine.Rendering;

/// <summary>
/// Distance-driven semantic skeleton with native editable SpriteSkin meshes on its parts.
/// Does not move the actor, change colliders, or write to environment transforms.
/// </summary>
[DefaultExecutionOrder(-100)]
public sealed class P02CharacterRig : MonoBehaviour
{
    public P02RigDefinition definition;
    public bool sampleMode;
    public float sampleSpeed;
    public bool sampleRunning;
    public bool samplePaused;
    public float targetHeight = 1.8f;
    public Transform motionSource;
    public Vector3 groundOffset;
    [NonSerialized] public float phase;
    [NonSerialized] public float actualSpeed;
    [NonSerialized] public bool running;
    [NonSerialized] public bool leftContact, rightContact;
    [NonSerialized] public Vector3 leftSole, rightSole;
    [NonSerialized] public Vector3 leftGround, rightGround;
    readonly Dictionary<string, Transform> joints = new Dictionary<string, Transform>();
    readonly List<SkinnedPart> parts = new List<SkinnedPart>();
    readonly Dictionary<string, float> bendAngles = new Dictionary<string, float>();
    readonly Dictionary<string, SpriteRenderer> renderers = new Dictionary<string, SpriteRenderer>();
    readonly Dictionary<string, Vector2[]> paintedSoles = new Dictionary<string, Vector2[]>();
    readonly Dictionary<string,float> upperAngles=new Dictionary<string,float>();
    readonly HashSet<string> stabilizedWaists=new HashSet<string>();
    Vector3 previous;
    float movementBlend, runBlend, breathTime, motionGrace;
    float facingSign => definition && definition.viewKind=="profile" && (definition.direction=="left" || definition.direction.EndsWith("_left")) ? -1 : 1;
    bool frontal => definition && definition.viewKind!="profile";
    public float ToolBlend { get; set; }
    public string ToolKind { get; set; }
    public bool CredentialOffer { get; set; }
    // P07 opening-only pose; locomotion and other animations are untouched when negative.
    public float OpeningRise=-1;
    public bool planarMovement;
    Vector2 travelAxis=Vector2.right;
    // Ground depth and body height share the screen Y axis in this 2D view.
    // Project the fore/aft stride consistently in both the pose and the stance
    // anchor; treating it as a full vertical leg extension forces a deep squat.
    // This is visual foreshortening only. Actor positions and travel speed stay
    // entirely under the original gameplay controller.
    float depthStrideProjection => frontal ? .14f : 1f;
    public void AdoptTravelState(P02CharacterRig other)
    {if(!other)return;phase=other.phase;movementBlend=other.movementBlend;runBlend=other.runBlend;breathTime=other.breathTime;wasMoving=other.wasMoving;motionGrace=other.motionGrace;actualSpeed=other.actualSpeed;running=other.running;travelAxis=other.travelAxis;previous=other.previous;hasPrevious=other.hasPrevious;plants.Clear();ApplyPose(0);}
    bool hasPrevious;
    bool wasMoving;
    readonly Dictionary<string,FootPlant> plants=new Dictionary<string,FootPlant>();
    sealed class FootPlant
    {
        public bool contact;
        public Vector3 world;
        public int contourIndex;
        public float angle,lastPhase;
    }
    public Transform HandCamera => Joint("near_hand");
    public Transform HandLamp => Joint("near_hand");
    // Anatomical names: this right-facing skin shows its right side near the viewer.
    public Transform FootLeft => Joint(NearAnatomicalSide=="left"?"near_shoe":"far_shoe");
    public Transform FootRight => Joint(NearAnatomicalSide=="right"?"near_shoe":"far_shoe");
    public Vector3 nearHeel, nearToe, farHeel, farToe;
    public float nearSupportAngle, farSupportAngle;
    public float nearLegEndpointGap, farLegEndpointGap;
    public float nearPaintedSoleY,farPaintedSoleY;
    public int nearSoleSupportIndex,farSoleSupportIndex;
    public float bodyLean, pelvisHeight;
    public string NearAnatomicalSide => definition.nearAnatomicalSide;
    public string FarAnatomicalSide => NearAnatomicalSide=="left"?"right":"left";
    public int MeshPartCount => parts.Count;

    sealed class SkinnedPart
    {
        public Transform root, tip,proximal;
        public Quaternion tipBind,proximalBind;
        public SpriteRenderer renderer;
        public SpriteSkin skin;
        public float bend;
        public string joint;
        public bool articulated;
        public Transform[] bones;
        public P02RigDefinition.P02PartBinding binding;
        public bool usingRun;
        public float upperScale=1,lowerScale=1;
    }

    void Awake() { Build(); }
    public void Build()
    {
        if (!definition || parts.Count > 0) return;
        foreach (var binding in definition.parts)
        {
            if (!binding.sprite) continue;
            var semantic = Joint(binding.joint);
            var existing = semantic.Find("skin_" + binding.joint);
            var g = existing ? existing.gameObject : new GameObject("skin_" + binding.joint);
            g.transform.SetParent(semantic, false);
            float scale = ArtScale(binding.sprite,binding.paintedHeight,binding.artworkAngle)*targetHeight/definition.referenceHeight;
            g.transform.localScale = new Vector3(binding.joint=="torso"?scale*definition.torsoWidth:scale,scale,scale);
            g.transform.localRotation=Quaternion.Euler(0,0,binding.artworkAngle);
            var renderer = g.GetComponent<SpriteRenderer>();
            if (!renderer) renderer = g.AddComponent<SpriteRenderer>();
            renderer.sprite = binding.sprite;
            renderer.sortingOrder = 20 + binding.sortingOffset;
            // In a rear quarter view the far hand stays behind both trouser
            // panels. Its palm must not peek through the waist while the arm
            // is correctly occluded by the torso.
            if(definition.projectedPlacement && definition.viewKind=="back" && binding.joint=="far_hand")
                renderer.sortingOrder=20+Array.Find(definition.parts,p=>p.joint=="far_leg").sortingOffset-1;
            renderers[binding.joint] = renderer;
            if(binding.joint.EndsWith("_shoe"))
            {
                if(binding.paintedSole==null||binding.paintedSole.Length<20)
                    throw new InvalidOperationException("Rebuild measured sole: "+binding.joint);
                paintedSoles[binding.joint]=binding.paintedSole;
            }
            var spriteBones = binding.sprite.GetBones();
            int expectedBones=binding.stabilizedWaist||binding.pelvisGarment?3:2;
            if (spriteBones.Length != expectedBones)
                throw new InvalidOperationException("Unexpected native skin bones: " + binding.sprite.name);
            if(binding.stabilizedWaist)stabilizedWaists.Add(binding.joint);
            var bones = new Transform[spriteBones.Length];
            for (int i = 0; i < spriteBones.Length; i++)
            {
                var found = g.GetComponentsInChildren<Transform>(true);
                Transform b = null;
                foreach (var candidate in found) if (candidate.name == spriteBones[i].name) { b = candidate; break; }
                if (!b) b = new GameObject(spriteBones[i].name).transform;
                b.SetParent(spriteBones[i].parentId < 0 ? g.transform : bones[spriteBones[i].parentId], false);
                b.localPosition = spriteBones[i].position;
                b.localRotation = spriteBones[i].rotation;
                bones[i] = b;
            }
            // Calibrated projected legs use independent world-axis thigh and
            // shin matrices. Keeping the shin under a nonuniform thigh scale
            // introduces shear; keeping both scales uniform narrows the pants.
            // The imported native bind poses remain unchanged.
            if(frontal && binding.calibratedLimb){
                if(binding.stabilizedWaist)bones[2].SetParent(bones[0],true);
                else bones[1].SetParent(g.transform,true);
            }
            if(binding.pelvisGarment){bones[1].SetParent(g.transform,true);bones[2].SetParent(g.transform,true);}
            var skin = g.GetComponent<SpriteSkin>();
            if (!skin) skin = g.AddComponent<SpriteSkin>();
            skin.SetRootBone(bones[0]);
            skin.SetBoneTransforms(bones);
            skin.alwaysUpdate = true;
            skin.forceCpuDeformation = true;
            int tipIndex=binding.stabilizedWaist||binding.pelvisGarment?2:1;
            parts.Add(new SkinnedPart { root = bones[0], tip = bones[tipIndex], tipBind = bones[tipIndex].localRotation,
                proximal=binding.stabilizedWaist?bones[1]:null,proximalBind=bones[1].localRotation,
                renderer = renderer, skin = skin, bend = binding.bendInfluence,
                joint = binding.joint, articulated = binding.articulatedLimb,bones=bones,binding=binding });
        }
        previous = motionSource ? motionSource.position : transform.position;
        hasPrevious = true;
        ApplyPose(0);
        // Native parts retain their internal near/far order. The character
        // participates in world sorting as one actor, including its contacts.
        if(!GetComponent<SortingGroup>())gameObject.AddComponent<SortingGroup>();
        if(!GetComponent<P02RigGroundContact>())gameObject.AddComponent<P02RigGroundContact>();
    }

    Transform Joint(string key)
    {
        if (joints.TryGetValue(key, out var result)) return result;
        result = transform.Find(key);
        if (!result) result = new GameObject(key).transform;
        result.SetParent(transform, false);
        joints.Add(key, result);
        return result;
    }

    public void ResetTravel()
    {
        previous = motionSource ? motionSource.position : transform.position;
        hasPrevious = true;
        phase = 0;
        movementBlend = 0;
        runBlend = 0;
        actualSpeed = 0;
        running = false;
        wasMoving=false;motionGrace=0;plants.Clear();
    }

    void LateUpdate()
    {
        if (!definition || parts.Count == 0) return;
        float dt = sampleMode ? (samplePaused ? 0 : Time.unscaledDeltaTime) : GameplayClock.Delta;
        if (dt <= 0) return;
        var current = motionSource ? motionSource.position : transform.position;
        var delta = hasPrevious ? current - previous : Vector3.zero;
        previous = current;
        hasPrevious = true;
        // Teleports are not a stride. No visible animation state writes to the source actor.
        if (delta.magnitude > Mathf.Max(1, dt * 12)) { ResetTravel(); delta = Vector3.zero; }
        if(planarMovement && frontal && delta.sqrMagnitude>.00000001f)
            foreach(var plant in plants.Values)plant.world.y+=delta.y*(1-depthStrideProjection);
        if(!sampleMode && delta.sqrMagnitude>.00000001f)travelAxis=new Vector2(delta.x,planarMovement?delta.y:0).normalized;
        float distance = sampleMode ? Mathf.Abs(sampleSpeed) * dt : new Vector2(delta.x, planarMovement?delta.y:0).magnitude;
        float displacementSpeed = distance / dt;
        var player = motionSource ? motionSource.GetComponent<PlayerController>() : null;
        var body = player ? player.GetComponent<Rigidbody2D>() : null;
        // Rigidbody movement arrives on fixed ticks. A render frame with no
        // new transform displacement must not restart the cycle or drop run.
        // Authored sequences also move a locked player's Transform while the
        // Rigidbody reports zero. Observe both sources, retaining fixed-tick
        // velocity across render frames. Actual travel alone advances phase.
        float bodySpeed = !sampleMode && body ? new Vector2(body.linearVelocity.x,planarMovement?body.linearVelocity.y:0).magnitude : 0;
        actualSpeed = Mathf.Max(bodySpeed, displacementSpeed);
        if(distance>.00001f)motionGrace=.065f;else motionGrace=Mathf.Max(0,motionGrace-dt);
        var sprint = player ? player.GetComponent<SprintAbility>() : null;
        bool requestedSprint = GameplayInput.GetKey(KeyCode.LeftShift) || GameplayInput.GetKey(KeyCode.RightShift);
        bool sprintQualified = sprint && sprint.GetMultiplier(requestedSprint) > 1.01f;
        running = sampleMode ? sampleRunning : player ? sprintQualified && actualSpeed > player.WalkSpeed * 1.08f : sampleRunning;
        float h = targetHeight / definition.referenceHeight;
        float stride = (running ? definition.runningStride : definition.walkingStride) * h;
        bool moving=sampleMode ? actualSpeed>.025f : actualSpeed>.025f || motionGrace>0;
        if(moving&&!wasMoving)phase=(running?.30f:.54f)*.5f;
        wasMoving=moving;
        if (distance > .00001f) phase = Mathf.Repeat(phase + distance / stride, 1);
        float previousMovementBlend=movementBlend;
        movementBlend = Mathf.MoveTowards(movementBlend, moving ? 1 : 0, dt * 7);
        runBlend = Mathf.MoveTowards(runBlend, running && moving ? 1 : 0, dt * 6);
        // Stance anchors belong to locomotion. Once actual movement stops,
        // release them during the short return to the canonical standing pose.
        // Every stop therefore ends in the same pose for this skin/direction,
        // regardless of which gait frame the player released the controls on.
        if(!moving && movementBlend==0){phase=0;running=false;if(previousMovementBlend>0)plants.Clear();}
        breathTime += dt;
        ApplyPose(h);
    }

    public void ApplyPose(float ignored)
    {
        if (!definition) return;
        float h = targetHeight / definition.referenceHeight;
        float breath = Mathf.Sin(breathTime * 1.6f);
        float lean = Mathf.Lerp(definition.walkingLean, definition.runningLean, runBlend) * movementBlend;
        if(frontal)lean*=travelAxis.x*(definition.projectedPlacement?.7f:.35f);
        var near = FootTarget("near", 0);
        var far = FootTarget("far", .5f);
        float reach = definition.thighLength + definition.shinLength - .005f;
        // The pelvis follows the support legs. It never stretches them to an unreachable ankle.
        float hipX = frontal?0:-.014f + (.064f+.035f*runBlend) * movementBlend;
        float hipY = definition.movingPelvisHeight + .022f*runBlend*Mathf.Sin(phase*Mathf.PI*4);
        float nearHipX=frontal?(definition.viewKind=="front"?1:-1)*definition.hipSeparation*.5f:hipX;
        float farHipX=frontal?-nearHipX:hipX-definition.hipSeparation;
        if(definition.projectedPlacement){nearHipX=definition.nearHipOffset.x;farHipX=definition.farHipOffset.x;}
        float reachable=Mathf.Min(ReachHeight(near.ankle,nearHipX,reach),ReachHeight(far.ankle,farHipX,reach))+definition.waistToHip;
        hipY = Mathf.Min(hipY,reachable);
        // Starting/stopping and idle obey the same reach limit as the loop.
        float pelvisY = Mathf.Min(Mathf.Lerp(definition.idlePelvisHeight, hipY, movementBlend),reachable);
        bodyLean = lean; pelvisHeight = pelvisY;
        var pelvis = new Vector2(hipX, pelvisY);
        Set("pelvis", pelvis * h, 0);
        Set("hem", (pelvis + Rotate(new Vector2(.004f,.07f), -lean)) * h, -lean);
        var torsoBase = pelvis + new Vector2(0,.035f+.0016f*breath*(1-movementBlend));
        Set("torso", torsoBase * h, -lean);
        // Neck and shoulder are coordinates in the torso, not separately world-offset objects.
        // A small painted overlap sits behind the collar; the face is never scaled or replaced.
        var shoulder = torsoBase + Rotate(new Vector2(definition.shoulderOffset.x*definition.torsoWidth,definition.shoulderOffset.y), -lean);
        var neck = torsoBase + Rotate(definition.neckOffset, -lean);
        Set("head", neck * h, -lean * .72f);
        Leg("near", near, new Vector2(nearHipX,pelvis.y+(definition.projectedPlacement?definition.nearHipOffset.y:0)), h);
        Leg("far", far, new Vector2(farHipX,pelvis.y+(definition.projectedPlacement?definition.farHipOffset.y:0)), h);
        if(definition.projectedPlacement){
            Arm("near",0,shoulder,h,lean);
            Arm("far",.5f,torsoBase+Rotate(new Vector2(definition.farShoulderOffset.x*definition.torsoWidth,definition.farShoulderOffset.y),-lean),h,lean);
        }else if(frontal){
            float sign=definition.viewKind=="front"?1:-1;
            Arm("near",0,torsoBase+new Vector2(sign*Mathf.Abs(definition.shoulderOffset.x)*definition.torsoWidth,definition.shoulderOffset.y),h,lean);
            Arm("far",.5f,torsoBase+new Vector2(-sign*Mathf.Abs(definition.shoulderOffset.x)*definition.torsoWidth,definition.shoulderOffset.y),h,lean);
        }else{Arm("near", 0, shoulder, h, lean);Arm("far", .5f, shoulder + Rotate(new Vector2(-.02f,0), -lean), h, lean);}
        if(OpeningRise>=0)ApplyOpeningRise(h);
        foreach (var part in parts)
        {
            if(part.binding.runSprite)
            {
                bool useRun=part.usingRun?runBlend>.25f||ToolBlend>.25f:runBlend>.55f||ToolBlend>.55f;
                if(useRun!=part.usingRun)
                {
                    // One renderer, one hand. Alternate painted pose is rigid;
                    // adopt its own pivot/bind coordinates, never stretch the old hand mesh.
                    var sprite=useRun?part.binding.runSprite:part.binding.sprite;
                    part.renderer.sprite=sprite;
                    float height=useRun?part.binding.runPaintedHeight:part.binding.paintedHeight;
                    float artAngle=useRun?part.binding.runArtworkAngle:part.binding.artworkAngle;
                    part.renderer.transform.localScale=Vector3.one*ArtScale(sprite,height,artAngle)*h;
                    part.renderer.transform.localRotation=Quaternion.Euler(0,0,artAngle);
                    var bones=sprite.GetBones();
                    for(int i=0;i<part.bones.Length;i++){part.bones[i].localPosition=bones[i].position;part.bones[i].localRotation=bones[i].rotation;}
                    part.usingRun=useRun;
                }
            }
            if(part.binding.pelvisGarment){ApplyGarment(part);continue;}
            if(part.binding.calibratedLimb){ApplyMeasuredLimb(part);continue;}
            if(part.proximal&&upperAngles.TryGetValue(part.joint,out var upper))
                part.proximal.localRotation=part.proximalBind*Quaternion.Euler(0,0,upper*facingSign);
            if(part.proximal)part.proximal.localScale=new Vector3(1,part.upperScale,1);
            else if(part.articulated)part.root.localScale=new Vector3(1,part.upperScale,1);
            part.tip.localScale=new Vector3(1,part.lowerScale/part.upperScale,1);
            float bend = part.articulated && bendAngles.TryGetValue(part.joint, out var angle) ? angle :
                part.bend * (breath * .55f + Mathf.Sin(phase * Mathf.PI * 2) * movementBlend);
            part.tip.localRotation = part.tipBind * Quaternion.Euler(0, 0, bend*facingSign);
        }
    }


    // Opening-only supported push-up -> half kneel -> stand. Ground contacts
    // are calibrated from the painted sole, exactly as in normal locomotion.
    // No actor/collider or production gait parameters are changed by this pose.
    void ApplyOpeningRise(float h)
    {
        float u=Mathf.Clamp01(OpeningRise);
        var normalPelvis=(Vector2)(Joint("pelvis").localPosition/h);normalPelvis.x*=facingSign;
        Vector2 pelvis=OpeningPoint(u,new Vector2(-.33f,.195f),new Vector2(-.33f,.28f),new Vector2(-.28f,.52f),new Vector2(-.18f,.52f),new Vector2(-.06f,.67f),normalPelvis);
        float lean=OpeningScalar(u,88,67,43,24,13,0);
        for(int n=0;n<6;n++){
            float minimum=1;foreach(string side in new[]{"near","far"}){
                bool close=side=="near";float x=close?Mathf.Lerp(-1.018f,.075f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.34f,u))):OpeningScalar(u,-.995f,-.86f,-.49f,-.43f,-.12f,-.028f);
                float angle=close?Mathf.Lerp(-82,0,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.34f,u))):OpeningScalar(u,-82,-68,-44,-23,0,0);float low=1;foreach(var v in paintedSoles[side+"_shoe"])low=Mathf.Min(low,Rotate(Canonical(v),angle).y);
                var knee=SolveJoint(pelvis+Vector2.down*definition.waistToHip,new Vector2(x,-low),definition.thighLength,definition.shinLength,true);minimum=Mathf.Min(minimum,knee.y);
            }if(minimum>=.065f||u>.82f)break;pelvis.y+=.065f-minimum;
        }
        bodyLean=lean;pelvisHeight=pelvis.y;Set("pelvis",pelvis*h,0);Set("hem",(pelvis+Rotate(new Vector2(.004f,.07f),-lean))*h,-lean);
        var torso=pelvis+new Vector2(0,.035f);Set("torso",torso*h,-lean);Set("head",(torso+Rotate(definition.neckOffset,-lean))*h,-lean);
        foreach(string side in new[]{"near","far"}){
            bool close=side=="near";var normal=(Vector2)(Joint(side+"_shoe").localPosition/h);normal.x*=facingSign;
            float x=close?Mathf.Lerp(-1.018f,.075f,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.34f,u))):OpeningScalar(u,-.995f,-.86f,-.49f,-.43f,-.12f,normal.x);if(close&&u>.82f)x=Mathf.Lerp(.075f,normal.x,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.82f,1,u)));
            float angle=close?Mathf.Lerp(-82,0,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.12f,.34f,u))):OpeningScalar(u,-82,-68,-44,-23,0,0);
            float lift=close&&u>.12f&&u<.34f?.13f*Mathf.Sin((u-.12f)/.22f*Mathf.PI):!close&&u>.62f&&u<.82f?.065f*Mathf.Sin((u-.62f)/.20f*Mathf.PI):0;
            var sole=paintedSoles[side+"_shoe"];var low=Rotate(Canonical(sole[0]),angle);int selected=0;
            for(int i=1;i<sole.Length;i++){var v=Rotate(Canonical(sole[i]),angle);if(v.y<low.y){low=v;selected=i;}}
            var ankle=new Vector2(x,-low.y+lift);
            var foot=new FootPose{ankle=ankle,angle=angle,lift=lift,heel=ankle+Rotate(Canonical(sole[0]),angle),toe=ankle+Rotate(Canonical(sole[sole.Length-1]),angle),support=ankle+low,supportIndex=selected,contact=lift<.002f};
            Leg(side,foot,pelvis,h);
            var shoulder=torso+Rotate(new Vector2(definition.shoulderOffset.x*definition.torsoWidth+(close?0:-.02f),definition.shoulderOffset.y),-lean);
            var rest=(Vector2)(Joint(side+"_hand").localPosition/h);rest.x*=facingSign;
            // Hands press the pavement while the chest is low, then release
            // before their measured reach is exceeded. Arms fold independently.
            var press=new Vector2(.27f+(close?0:.025f),.055f);
            var wrist=OpeningPoint(u,press,press,new Vector2(.31f,.32f),new Vector2(.25f,.57f),new Vector2(.15f,.76f),rest);
            float reach=definition.upperArmLength+definition.forearmLength-.004f;
            var axis=wrist-shoulder;if(axis.magnitude>reach)wrist=shoulder+axis.normalized*reach;
            var elbow=SolveJoint(shoulder,wrist,definition.upperArmLength,definition.forearmLength,false);
            SetSegment(side+"_upper_arm",shoulder*h,elbow*h);SetSegment(side+"_forearm",elbow*h,wrist*h);SetLimb(side+"_arm",shoulder*h,elbow*h,wrist*h);
            Set(side+"_hand",wrist*h,Mathf.Atan2(wrist.y-elbow.y,wrist.x-elbow.x)*Mathf.Rad2Deg+90);
        }
    }
    static Vector2 OpeningPoint(float u,Vector2 a,Vector2 b,Vector2 c,Vector2 d,Vector2 e,Vector2 f)=>new Vector2(OpeningScalar(u,a.x,b.x,c.x,d.x,e.x,f.x),OpeningScalar(u,a.y,b.y,c.y,d.y,e.y,f.y));
    static float OpeningScalar(float u,float a,float b,float c,float d,float e,float f){
        float[] times={0,.22f,.42f,.62f,.82f,1};float[] values={a,b,c,d,e,f};
        for(int i=0;i<5;i++)if(u<=times[i+1])return Mathf.Lerp(values[i],values[i+1],Mathf.SmoothStep(0,1,Mathf.InverseLerp(times[i],times[i+1],u)));return f;
    }

    void ApplyGarment(SkinnedPart part)
    {
        // Calibrated waistband matches the approved wider bib waist. Thighs
        // have independent matrices so this correction cannot shear a leg.
        part.root.localScale=new Vector3(1.2f*definition.torsoWidth,1,1);
        part.root.position=Joint("pelvis").position+transform.TransformVector(Vector3.up*.105f*targetHeight/definition.referenceHeight);
        part.root.rotation=transform.rotation*Quaternion.Euler(0,0,-bodyLean);
        float actorScale=Mathf.Abs(transform.lossyScale.y)*targetHeight/definition.referenceHeight;
        for(int i=1;i<3;i++){
            string side=i==1?"near":"far";
            var hip=Joint(side+"_thigh").position;
            var knee=Joint(side+"_shin").position;
            part.bones[i].position=hip;
            part.bones[i].rotation=Quaternion.Euler(0,0,WorldDownAngle(knee-hip));
            part.bones[i].localScale=new Vector3(1,Vector3.Distance(hip,knee)/(part.binding.bindUpperLength*actorScale),1);
        }
    }

    void ApplyMeasuredLimb(SkinnedPart part)
    {
        // Each source has independently measured shoulder/elbow/wrist or
        // waist/hip/knee/ankle points. The native bind axes follow that painted
        // rest pose; a straight-down generic axis is never assumed.
        string side=part.joint.StartsWith("near_")?"near":"far";
        bool leg=part.binding.stabilizedWaist;
        Vector3 a=Joint(side+(leg?"_thigh":"_upper_arm")).position;
        Vector3 b=Joint(side+(leg?"_shin":"_forearm")).position;
        Vector3 c=Joint(side+(leg?"_shoe":"_hand")).position;
        var upper=leg?part.proximal:part.root;
        part.root.localScale=Vector3.one;upper.localScale=Vector3.one;part.tip.localScale=Vector3.one;
        bool projectedLeg=leg && frontal;
        bool depthProjected=frontal;
        if(leg){
            part.root.position=Joint(part.joint).position;
            part.root.rotation=transform.rotation;
            if(projectedLeg||OpeningRise>=0){
                var waistTurn=Quaternion.Euler(0,0,-bodyLean);
                var center=Joint("pelvis").position;
                part.root.position=center+waistTurn*(part.root.position-center);
                part.root.rotation=transform.rotation*waistTurn;
            }
        }
        float actorScale=Mathf.Abs(transform.lossyScale.y)*targetHeight/definition.referenceHeight;
        float upperScale=Vector3.Distance(a,b)/(part.binding.bindUpperLength*actorScale);
        float lowerScale=Vector3.Distance(b,c)/(part.binding.bindLowerLength*actorScale);
        upper.position=a;upper.rotation=Quaternion.Euler(0,0,WorldDownAngle(b-a));
        // Uniform length correction avoids shear of a child rotated under a
        // nonuniformly scaled parent. Source calibration keeps this correction
        // close to one for profile views; frontal depth projection is explicit.
        upper.localScale=depthProjected ? new Vector3(1,upperScale,1) : Vector3.one*upperScale;
        part.tip.position=b;part.tip.rotation=Quaternion.Euler(0,0,WorldDownAngle(c-b));
        part.tip.localScale=depthProjected ? new Vector3(1,lowerScale,1) : Vector3.one*(lowerScale/upperScale);
    }
    static float WorldDownAngle(Vector3 axis)=>Mathf.Atan2(axis.y,axis.x)*Mathf.Rad2Deg+90;

    public void SetDiagnosticPose(float cyclePhase,bool moving,bool sprinting)
    {
        if(!sampleMode)throw new InvalidOperationException("Diagnostic pose requires isolated sample mode");
        string di=definition.direction;
        planarMovement=di.StartsWith("up")||di.StartsWith("down");
        travelAxis=new Vector2(di=="left"||di.EndsWith("_left")?-1:di=="right"||di.EndsWith("_right")?1:0,
            di.StartsWith("up")?1:di.StartsWith("down")?-1:0).normalized;
        samplePaused=true;phase=cyclePhase;movementBlend=moving?1:0;runBlend=sprinting?1:0;
        plants.Clear();ApplyPose(0);
    }

    struct FootPose { public Vector2 ankle, heel, toe, support; public float angle,lift; public bool contact; public int supportIndex; }
    static float ReachHeight(Vector2 ankle, float hipX, float reach)
    {
        float dx = Mathf.Min(Mathf.Abs(ankle.x-hipX),reach-.002f);
        return ankle.y + Mathf.Sqrt(Mathf.Max(0,reach*reach-dx*dx));
    }
    FootPose FootTarget(string side, float offset)
    {
        float t = Mathf.Repeat(phase + offset, 1);
        float stance = Mathf.Lerp(.54f, .30f, runBlend);
        float stride = Mathf.Lerp(definition.walkingStride, definition.runningStride, runBlend);
        float halfReach = stride * stance * .5f;
        float x, lift, angle;
        bool contact = t < stance;
        if (contact)
        {
            x = halfReach - stride * t; lift = 0;
            float s = t/stance;
            float touchdown=Mathf.Lerp(12,-8,runBlend),toeOff=Mathf.Lerp(-22,-30,runBlend);
            angle = s < .24f ? Mathf.Lerp(touchdown,0,Mathf.SmoothStep(0,1,s/.24f)) :
                s > .65f ? Mathf.Lerp(0,toeOff,Mathf.SmoothStep(0,1,(s-.65f)/.35f)) : 0;
        }
        else
        {
            float swing = (t - stance) / (1 - stance);
            // Hermite endpoint slopes match the -stride stance velocity on BOTH sides of the loop.
            // The foot begins by leaving backward, then swings forward and settles before contact.
            float u=swing, u2=u*u, u3=u2*u, m=-stride*(1-stance);
            x = (2*u3-3*u2+1)*(-halfReach)+(u3-2*u2+u)*m+
                (-2*u3+3*u2)*halfReach+(u3-u2)*m;
            lift = Mathf.Lerp(definition.walkLift.Evaluate(swing)*.085f, definition.runLift.Evaluate(swing)*.22f, runBlend);
            angle = Mathf.Lerp(Mathf.Lerp(-22,-30,runBlend),Mathf.Lerp(12,-8,runBlend),Mathf.SmoothStep(0,1,swing));
        }
        float idleX = side == "near" ? definition.idleFootSeparation : -definition.idleFootSeparation;
        if(frontal&&definition.viewKind=="back")idleX=-idleX;
        var projectedFoot=side=="near"?definition.nearFootOffset:definition.farFootOffset;
        if(definition.projectedPlacement)idleX=projectedFoot.x;
        float depthStep=0;
        if(planarMovement){depthStep=x*travelAxis.y*depthStrideProjection;x*=travelAxis.x*facingSign;}
        if(frontal){x+=idleX;angle*=Mathf.Abs(travelAxis.x)*(definition.projectedPlacement?.9f:.18f);}
        angle *= movementBlend;
        var outline=paintedSoles[side+"_shoe"];
        int supportIndex=0;
        var rotatedSupport=Rotate(Canonical(outline[0]),angle);
        for(int i=1;i<outline.Length;i++)
        {
            var point=Rotate(Canonical(outline[i]),angle);
            if(point.y<rotatedSupport.y){rotatedSupport=point;supportIndex=i;}
        }
        var support=Canonical(outline[supportIndex]);
        var ankle=new Vector2(Mathf.Lerp(idleX,x,movementBlend)+support.x-rotatedSupport.x,
            -rotatedSupport.y+(lift+depthStep)*movementBlend+(definition.projectedPlacement?projectedFoot.y:0));
        bool planted=contact||movementBlend<.01f;
        float h=targetHeight/definition.referenceHeight;
        if(!plants.TryGetValue(side,out var plant)){plant=new FootPlant();plants.Add(side,plant);}
        bool wrapped=t<plant.lastPhase-.5f;
        if(wasMoving&&planted&&plant.contact&&!wrapped)
        {
            // Preserve the world position of the SAME painted support point.
            // When the rolling sole selects another point, transfer the anchor
            // through the previous shoe pose; otherwise heel-to-toe would jump.
            if(plant.contourIndex!=supportIndex)
            {
                var transfer=Rotate(Canonical(outline[supportIndex]-outline[plant.contourIndex]),plant.angle)*h;
                plant.world+=transform.TransformVector(new Vector3(transfer.x*facingSign,transfer.y,0));
            }
            var local=transform.InverseTransformPoint(plant.world)/h;
            ankle.x=local.x*facingSign-rotatedSupport.x;
            if(planarMovement)ankle.y=local.y-rotatedSupport.y;
        }
        if(planted)plant.world=World((ankle+rotatedSupport)*h);
        plant.contact=planted;plant.contourIndex=supportIndex;plant.angle=angle;plant.lastPhase=t;
        return new FootPose {ankle=ankle,heel=Rotate(Canonical(outline[0]),angle)+ankle,
            toe=Rotate(Canonical(outline[outline.Length-1]),angle)+ankle,support=rotatedSupport+ankle,
            supportIndex=supportIndex,angle=angle,lift=lift*movementBlend,contact=planted};
    }
    void Leg(string side, FootPose foot, Vector2 pelvis, float h)
    {
        var ankle = foot.ankle;
        var hip=pelvis+Vector2.down*definition.waistToHip;
        var knee = frontal?Vector2.Lerp(hip,ankle,definition.thighLength/(definition.thighLength+definition.shinLength)):
            SolveJoint(hip, ankle, definition.thighLength, definition.shinLength, true);
        if(definition.projectedPlacement)knee=Vector2.Lerp(knee,SolveJoint(hip,ankle,definition.thighLength,definition.shinLength,!definition.direction.Contains("left")),.5f);
        if(frontal){var p=parts.Find(v=>v.joint==side+"_leg");
            p.upperScale=Vector2.Distance(hip,knee)/definition.thighLength;p.lowerScale=Vector2.Distance(knee,ankle)/definition.shinLength;}
        Set(side + "_shoe", ankle * h, foot.angle);
        SetSegment(side + "_thigh", hip * h, knee * h);
        SetSegment(side + "_shin", knee * h, ankle * h);
        SetLimb(side + "_leg", pelvis * h, knee * h, ankle * h,hip*h);
        Vector3 heel=World(foot.heel*h),toe=World(foot.toe*h);
        Vector3 sole=World(foot.support*h);
        Vector3 ground=World((foot.support-Vector2.up*foot.lift)*h);
        // Endpoint error is measured against the lengths actually used by the painted limb bones.
        float gap=frontal?0:Mathf.Abs(Vector2.Distance(knee,ankle)-definition.shinLength)*h;
        if(side=="near"){nearHeel=heel;nearToe=toe;nearSupportAngle=foot.angle;nearLegEndpointGap=gap;
            nearPaintedSoleY=sole.y;nearSoleSupportIndex=foot.supportIndex;}
        else{farHeel=heel;farToe=toe;farSupportAngle=foot.angle;farLegEndpointGap=gap;
            farPaintedSoleY=sole.y;farSoleSupportIndex=foot.supportIndex;}
        bool anatomicalLeft = side=="near" ? NearAnatomicalSide=="left" : FarAnatomicalSide=="left";
        if (anatomicalLeft) { leftContact = foot.contact; leftSole = sole; leftGround=ground; }
        else { rightContact = foot.contact; rightSole = sole; rightGround=ground; }
    }

    void Arm(string side, float offset, Vector2 shoulder, float h, float lean)
    {
        float swing = -Mathf.Cos((phase + offset) * Mathf.PI * 2);
        // Shoulder leads, elbow follows with a small phase lag. Rear travel
        // comes from the upper arm; the forearm folds instead of pushing both
        // straight hands forward. Tool IK below keeps the approved held pose.
        float runShoulder=swing>=0?38*swing:48*swing;
        float angle = Mathf.Lerp(15*swing,runShoulder,runBlend)*movementBlend-lean*.45f;
        var elbow = shoulder + Rotate(Vector2.down * definition.upperArmLength, angle);
        float runElbow=78+18*(swing+1)*.5f+6*Mathf.Sin((phase+offset)*Mathf.PI*2-.35f);
        float elbowBend = Mathf.Lerp(7+Mathf.Max(0,swing)*5*movementBlend,runElbow,runBlend);
        var wrist = elbow + Rotate(Vector2.down * definition.forearmLength, angle + elbowBend);
        var profileElbow=elbow;var profileWrist=wrist;
        if(definition.projectedPlacement&&definition.direction.Contains("left")){
            profileElbow.x=2*shoulder.x-profileElbow.x;profileWrist.x=2*shoulder.x-profileWrist.x;}
        if(frontal){
            float projection=1-Mathf.Max(0,swing)*Mathf.Lerp(.10f,.26f,runBlend)*movementBlend;
            elbow=shoulder+new Vector2(swing*.018f*movementBlend,-definition.upperArmLength*projection);
            wrist=elbow+new Vector2(swing*.014f*movementBlend,-definition.forearmLength*projection);
            // Frontal running projects the forward forearm in depth. The elbow
            // stays beside the torso and the wrist follows an alternating arc;
            // merely shrinking two straight hanging arms reads as idle fists.
            float runAmount=runBlend*movementBlend;
            float outward=shoulder.x>=0?1:-1;
            elbow.x+=outward*.035f*runAmount;
            wrist=Vector2.Lerp(wrist,elbow+new Vector2(-outward*.065f+swing*.025f,
                definition.forearmLength*(.14f+.10f*swing)),runAmount);
            if(definition.projectedPlacement){elbow=Vector2.Lerp(elbow,profileElbow,.78f);wrist=Vector2.Lerp(wrist,profileWrist,.78f);}
            angle=Mathf.Atan2(elbow.y-shoulder.y,elbow.x-shoulder.x)*Mathf.Rad2Deg+90;
            elbowBend=Mathf.DeltaAngle(angle,Mathf.Atan2(wrist.y-elbow.y,wrist.x-elbow.x)*Mathf.Rad2Deg+90);
            var part=parts.Find(v=>v.joint==side+"_arm");part.upperScale=projection;part.lowerScale=projection;
        }
        float hold=side=="near"?ToolBlend:ToolKind=="camera"?ToolBlend*.92f:0;
        if(CredentialOffer&&side=="near")hold=1;
        if(hold>0){
            var pelvis=Joint("pelvis").localPosition;pelvis.x*=facingSign;pelvis/=h;
            float height=CredentialOffer?1.1f:ToolKind=="camera"?definition.cameraHandHeight:definition.lampHandHeight;
            var desired=new Vector2(pelvis.x+(ToolKind=="camera"?(side=="near"?.32f:.20f):.34f),height);
            var resting=wrist;
            if(CredentialOffer)wrist=Vector2.Lerp(resting,desired,hold);
            else if(side=="near"){
                // Draw and stow traverse the same wrist path in opposite order:
                // neutral -> back/waist reach -> held pose. No second prop/arm.
                var behind=new Vector2(pelvis.x-.105f,pelvis.y+.18f);
                wrist=hold<.32f?Vector2.Lerp(resting,behind,Mathf.SmoothStep(0,1,hold/.32f)):
                    Vector2.Lerp(behind,desired,Mathf.SmoothStep(0,1,(hold-.32f)/.68f));
            }else wrist=Vector2.Lerp(resting,desired,Mathf.SmoothStep(0,1,Mathf.InverseLerp(.42f,.92f,hold)));
            var bendA=SolveJoint(shoulder,wrist,definition.upperArmLength,definition.forearmLength,true);
            var bendB=SolveJoint(shoulder,wrist,definition.upperArmLength,definition.forearmLength,false);
            elbow=bendA.y<bendB.y?bendA:bendB;
            angle=Mathf.Atan2(elbow.y-shoulder.y,elbow.x-shoulder.x)*Mathf.Rad2Deg+90;
            elbowBend=Mathf.DeltaAngle(angle,Mathf.Atan2(wrist.y-elbow.y,wrist.x-elbow.x)*Mathf.Rad2Deg+90);
        }
        SetSegment(side + "_upper_arm", shoulder * h, elbow * h);
        SetSegment(side + "_forearm", elbow * h, wrist * h);
        SetLimb(side + "_arm", shoulder * h, elbow * h, wrist * h);
        Set(side + "_hand", wrist * h, angle + elbowBend);
    }

    static float ArtScale(Sprite sprite,float height,float angle)
    {float a=angle*Mathf.Deg2Rad;return height/(Mathf.Abs(Mathf.Sin(a))*sprite.bounds.size.x+Mathf.Abs(Mathf.Cos(a))*sprite.bounds.size.y);}
    Vector2 Canonical(Vector2 p)=>new Vector2(p.x*facingSign,p.y);
    Vector3 World(Vector2 p)=>transform.TransformPoint(new Vector3(p.x*facingSign,p.y,0));
    static Vector2 Rotate(Vector2 p, float angle)
    {
        float a = angle * Mathf.Deg2Rad;
        return new Vector2(p.x * Mathf.Cos(a) - p.y * Mathf.Sin(a), p.x * Mathf.Sin(a) + p.y * Mathf.Cos(a));
    }
    public static Vector2 SolveJoint(Vector2 start, Vector2 end, float a, float b, bool bendForward)
    {
        var vector = end - start;
        float d = Mathf.Clamp(vector.magnitude, .001f, a + b - .0001f);
        var axis = vector.sqrMagnitude > .000001f ? vector.normalized : Vector2.down;
        float along = (a * a - b * b + d * d) / (2 * d);
        float perpendicular = Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
        var normal = new Vector2(-axis.y, axis.x) * (bendForward ? 1 : -1);
        return start + axis * along + normal * perpendicular;
    }
    void SetSegment(string key, Vector2 root, Vector2 end)
    {
        float angle = Mathf.Atan2(end.y - root.y, end.x - root.x) * Mathf.Rad2Deg + 90;
        Set(key, root, angle);
    }
    void SetLimb(string key, Vector2 root, Vector2 bend, Vector2 end,Vector2? articulationRoot=null)
    {
        var axis=articulationRoot??root;
        float upper = Mathf.Atan2(bend.y-axis.y, bend.x-axis.x)*Mathf.Rad2Deg+90;
        float lower = Mathf.Atan2(end.y-bend.y, end.x-bend.x)*Mathf.Rad2Deg+90;
        // The trouser waist remains on the pelvis, while thigh/shin bones move
        // under its continuous weighted cloth. No rotating waist cap or cover patch.
        Set(key, root, stabilizedWaists.Contains(key)?0:upper);
        upperAngles[key]=upper;
        bendAngles[key] = Mathf.DeltaAngle(upper, lower);
    }
    void Set(string key, Vector2 position, float angle)
    {
        var t = Joint(key);
        t.localPosition = new Vector3(position.x*facingSign,position.y,0);
        t.localRotation = Quaternion.Euler(0, 0, angle*facingSign);
    }
}

