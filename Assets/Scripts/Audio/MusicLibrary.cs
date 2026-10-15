using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "ProjectMystery/Audio/Music Library")]
public sealed class MusicLibrary : ScriptableObject
{
    public MusicCue[] cues;
    public AudioClip themeInstrumentalReserve;
    public AudioMixer mixer;
    public AudioMixerGroup musicGroup, ambienceGroup, sfxGroup, uiGroup;
    [Header("Audio Pass 0 dialogue priority; no source-file processing")]
    [Range(0, 1)] public float dialogueGain = .45f;
    [Min(.01f)] public float duckAttack = .2f;
    [Min(.01f)] public float duckRelease = .8f;
    public MusicCue Cue(MusicState state)
    {
        if (cues != null) foreach (var cue in cues) if (cue && cue.state == state) return cue;
        return null;
    }
}
