using System;
using UnityEngine;

/// <summary>Editable native skin configuration. Source pixels and semantic joints stay separate.</summary>
[CreateAssetMenu(menuName = "ProjectMystery/P02/Character rig definition")]
public sealed class P02RigDefinition : ScriptableObject
{
    public string identity;
    public string direction = "right";
    public string actorKey = "adult_default";
    public string viewKind = "profile";
    public string nearAnatomicalSide = "right";
    public float targetHeight = 1.8f;
    [Range(.8f,1.1f)] public float torsoWidth = .93f;
    public Vector2 shoulderOffset = new Vector2(-.013f,.455f);
    public Vector2 neckOffset = new Vector2(.001f,.521f);
    public bool projectedPlacement;
    public Vector2 farShoulderOffset,nearHipOffset,farHipOffset,nearFootOffset,farFootOffset;
    public float idlePelvisHeight = .914f;
    public float movingPelvisHeight = .902f;
    public float hipSeparation = .028f;
    public float idleFootSeparation = .028f;
    public float cameraHandHeight = 1.56f;
    public float lampHandHeight = 1.05f;
    public float referenceHeight = 1.8f;
    public float walkingStride = 1.2f;
    public float runningStride = 1.35f;
    public float sampleWalkSpeed = 1.35f;
    public float sampleRunSpeed = 2.0925f;
    public float walkingLean = 1.8f;
    public float runningLean = 13f;
    public float waistToHip = .115f;
    public float thighLength = .29f;
    public float shinLength = .405f;
    public float upperArmLength = .28f;
    public float forearmLength = .25f;
    public AnimationCurve walkLift = new AnimationCurve(new Keyframe(0,0),new Keyframe(.5f,1),new Keyframe(1,0));
    public AnimationCurve runLift = new AnimationCurve(new Keyframe(0,0),new Keyframe(.36f,1),new Keyframe(.72f,.56f),new Keyframe(1,0));
    public AnimationCurve walkSwing = AnimationCurve.EaseInOut(0,0,1,1);
    public AnimationCurve runSwing = new AnimationCurve(new Keyframe(0,0),new Keyframe(.35f,.25f),new Keyframe(.75f,.91f),new Keyframe(1,1));
    public P02PartBinding[] parts;

    [Serializable]
    public sealed class P02PartBinding
    {
        public string joint;
        public Sprite sprite;
        public Sprite runSprite;
        public float runPaintedHeight;
        public float paintedHeight;
        public float artworkAngle;
        public float runArtworkAngle;
        public int sortingOffset;
        public float bendInfluence;
        public bool articulatedLimb;
        public bool stabilizedWaist;
        public bool calibratedLimb;
        public bool pelvisGarment;
        public float bindUpperLength, bindLowerLength;
        // Opaque lower contour, measured from the imported sprite. Values are in
        // reference actor units, relative to the actual shoe pivot, not texture bounds.
        public Vector2[] paintedSole;
    }
}
