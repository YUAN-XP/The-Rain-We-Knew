using System;
using System.Collections.Generic;
using UnityEngine;

// Append new keys; their numeric values are serialized in scene components.
public enum StoryFlag { CameraDoorRevealed, CameraTutorialTriggered, CameraFirstRaised,
    DeskEventTriggered, ChildShadowSequencePlayed, SprintUnlocked, ThreeDoorPuzzleCompleted,
    Scene03BirthdayKnown, OverallsUnlocked, CorridorRemarkShown, DrawerOpened }
public enum AdultOutfit { Default, Overalls }

[DisallowMultipleComponent]
public class StoryStateManager : MonoBehaviour
{
    private readonly HashSet<StoryFlag> flags = new HashSet<StoryFlag>();
    public AdultOutfit Outfit { get; private set; }
    public event Action Changed;
    public bool Has(StoryFlag flag) => flags.Contains(flag);
    public bool Set(StoryFlag flag, bool value = true)
    {
        bool changed = value ? flags.Add(flag) : flags.Remove(flag);
        if (changed) Changed?.Invoke();
        return changed;
    }
    public void SetOutfit(AdultOutfit outfit)
    {
        if (outfit == AdultOutfit.Overalls && !Has(StoryFlag.OverallsUnlocked)) return;
        Outfit = outfit;
        Changed?.Invoke();
    }
}
