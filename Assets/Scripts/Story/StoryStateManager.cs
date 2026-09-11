using System;
using System.Collections.Generic;
using UnityEngine;

// Append new keys; their numeric values are serialized in scene components.
public enum StoryFlag { CameraDoorRevealed, CameraTutorialTriggered, CameraFirstRaised,
    DeskEventTriggered, ChildShadowSequencePlayed, SprintUnlocked, ThreeDoorPuzzleCompleted,
    Scene03BirthdayKnown, OverallsUnlocked, CorridorRemarkShown, DrawerOpened, ChildShadowFollowupPlayed,
    DistortedCorridorStarted, ParentShadowEventPlayed, GuideFirstDialoguePlayed, Scene01GateOpened, Scene01NewspaperTaken,
    Scene03Started, Scene03IntroCompleted, Scene03PhoneAnswered, Scene03CameraTaken,
    StorytellerGameCompleted, FishObtained, CatFed, GameCoinObtained, GameCoinSpent,
    CameraGentlemanCardObtained, Scene03StreetReached, Scene03MazePreludeReached,
    LiMeiMessageAccepted, LiMeiMessageDelivered, Scene03FishHintShown,
    Scene03MazeEntered, Scene03MazeCurrentLayout, Scene03MazeFirstSwitch, Scene03MazeCompleted,
    Scene03RhythmCompleted, Scene03RiverSequenceCompleted, Scene03ReturnedHome,
    Scene03FamilySetupComplete, Scene03FamilyPhotoTaken, Scene03Completed,
    LivingRoomDoorOpened, Scene03CameraGentlemanEchoCollected, OilLampObtained,
    Scene02Fragment2Collected, Scene03ReturnReactionPlayed, Scene03FileNoticeShown, Scene03DrawerNoticeShown }
public enum AdultOutfit { Default, Overalls }

[DisallowMultipleComponent]
public class StoryStateManager : MonoBehaviour
{
    private readonly HashSet<StoryFlag> flags = new HashSet<StoryFlag>();
    public AdultOutfit Outfit { get; private set; }
    // Session memory only; a reload resumes at a safe center in the remembered cell.
    public Vector2Int Scene03MazeCell { get; private set; } = new Vector2Int(14, 0);
    public void SetScene03MazeCell(Vector2Int cell) { Scene03MazeCell = cell; }
    public event Action Changed;
    public bool Has(StoryFlag flag) => flags.Contains(flag);
    public bool Set(StoryFlag flag, bool value = true)
    {
        bool changed = value ? flags.Add(flag) : flags.Remove(flag);
        if (changed) Changed?.Invoke();
        return changed;
    }
    public bool OwnsOutfit(OutfitData data) => data!=null && (data.initiallyOwned || Has(data.unlockFlag));
    public bool EquipOutfit(OutfitData data)
    {
        if(!OwnsOutfit(data))return false;
        SetOutfit(data.outfit);return Outfit==data.outfit;
    }
    public void SetOutfit(AdultOutfit outfit)
    {
        if (outfit == AdultOutfit.Overalls && !Has(StoryFlag.OverallsUnlocked)) return;
        Outfit = outfit;
        Changed?.Invoke();
    }
}
