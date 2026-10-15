using UnityEngine;

[CreateAssetMenu(menuName = "ProjectMystery/Audio/Music Cue")]
public sealed class MusicCue : ScriptableObject
{
    public MusicState state;
    public AudioClip clip;
    public bool loop = true;
    [Range(0, 1)] public float baseVolume = .3f;
    [Min(0)] public float fadeIn = 1.5f;
    [Min(0)] public float fadeOut = 1.5f;
    [Min(0)] public float startOffset;
    [Tooltip("Three doors and the authored guitar performance own their playback.")]
    public bool delegatedPlayback;
    public bool playDuringMenuPause;
}
