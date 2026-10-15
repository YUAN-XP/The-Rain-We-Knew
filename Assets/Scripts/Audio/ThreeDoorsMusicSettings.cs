using System;
using UnityEngine;

[CreateAssetMenu(menuName = "ProjectMystery/Audio/Three Doors Settings")]
public sealed class ThreeDoorsMusicSettings : ScriptableObject
{
    [Serializable] public class ClarityProfile
    {
        [Range(0, 1)] public float volume;
        [Range(10, 22000)] public float lowPass;
        [Range(0, 1)] public float spatialBlend;
        public bool reverb;
        [Range(-10000, 2000)] public float reverbLevel = -2500;
    }
    [Serializable] public class RecoveryProfile
    {
        [Range(0, 1)] public float volumeMultiplier = 1;
        [Range(.1f, 1)] public float cutoffMultiplier = 1;
    }
    public AudioClip clip;
    public AudioClip WrongDoorSFX;
    public ClarityProfile clear = new ClarityProfile { volume = 1, lowPass = 12000 };
    public ClarityProfile medium = new ClarityProfile { volume = .44f, lowPass = 5200 };
    public ClarityProfile distant = new ClarityProfile { volume = .19f, lowPass = 2100 };
    public RecoveryProfile[] stages = {
        new RecoveryProfile { volumeMultiplier = .72f, cutoffMultiplier = .5f },
        new RecoveryProfile { volumeMultiplier = .82f, cutoffMultiplier = .65f },
        new RecoveryProfile { volumeMultiplier = .92f, cutoffMultiplier = .82f },
        new RecoveryProfile { volumeMultiplier = 1, cutoffMultiplier = 1 }
    };
    [Min(.1f)] public float scheduleLead = .5f;
    [Min(.01f)] public float smoothingSeconds = .25f;
    [Min(0)] public float distanceAttenuation = .12f;
    [Min(.01f)] public float panDistance = 18;
    [Range(0, .2f)] public float corridorStartVolume = .025f;
    [Range(0, .2f)] public float corridorLampIncrease = .045f;
    [Min(0)] public float completedHoldSeconds = .2f;
    [Min(.01f)] public float completedFadeSeconds = .8f;
}
