using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Game Data/Rhythm Phrase")]
public class RhythmPhraseData : ScriptableObject
{
    [Serializable] public class Note
    {
        public KeyCode key = KeyCode.W;
        [Min(0)] public float time;
        public AudioClip tone;
    }
    public KeyCode heldChord = KeyCode.LeftArrow;
    public Note[] notes;
    [Min(.05f)] public float perfectWindow = .25f;
    [Min(.1f)] public float goodWindow = .55f;
    [Min(.1f)] public float strumWindow = .85f;
    [Min(.2f)] public float tail = .8f;
    public float Window(KeyCode key) => key == KeyCode.Space ? Mathf.Max(goodWindow, strumWindow) : goodWindow;
    public bool IsValid()
    {
        if (heldChord != KeyCode.LeftArrow && heldChord != KeyCode.RightArrow || notes == null || notes.Length < 2 || perfectWindow > goodWindow) return false;
        for (int i = 0; i < notes.Length; i++)
        {
            if (notes[i] == null || notes[i].time < 0 || i > 0 && notes[i].time <= notes[i - 1].time) return false;
            var k = notes[i].key;
            if (i == notes.Length - 1 ? k != KeyCode.Space : k != KeyCode.W && k != KeyCode.S && k != KeyCode.A && k != KeyCode.D) return false;
        }
        return notes[notes.Length - 1].time - notes[notes.Length - 2].time > goodWindow * 2;
    }
}
