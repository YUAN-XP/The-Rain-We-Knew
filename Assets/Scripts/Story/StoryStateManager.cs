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
    Scene02Fragment2Collected, Scene03ReturnReactionPlayed, Scene03FileNoticeShown, Scene03DrawerNoticeShown,
    OilLampExhausted, FogIntroCompleted, EquipmentVoicePlayed, EquipmentDoorOpened,
    BasketKeyRevealed, BasketKeyDropped, BasketKeyCollected, SchoolUniformOwned, PencilOwned,
    SchoolNameRevealed, Scene05OpeningPlayed, SchoolSecurityClueSeen, SchoolEvidenceObtained,
    SchoolYouthConversationCompleted, SchoolUsbReceived, SchoolSecurityMemoryOpened,
    SchoolMediaPasswordChanged, SchoolEvidenceSubmitted, SchoolOfficeHandoffReady,
    SchoolToiletHeard, SchoolEquipmentRecalled, SchoolCourtHintSeen, SchoolCrowdSeen,
    SchoolGateTried, SchoolFemaleToiletTried, SchoolRoofTried, SchoolSecurityWindowSeen,
    SchoolChaseStarted, SchoolChaseCompleted, SchoolSecurityEntered, SchoolOilTaken,
    SchoolLampRestored, SchoolFragment3Taken, SchoolNewspaperCombined, SchoolReturnedHome,
    SchoolGuideReturnSpoken, SchoolComputerLoggedIn, SchoolEmailRead,
    Scene01IDHolderTaken, EndingEmailReactionCompleted, EndingHandoverCompleted,
    EndingIdentityReceived, EndingIdentityHintIssued, CompleteCredentialArchived, EndingStreetVisit,
    EndingStreetPanCompleted, EndingZhangCompleted, EndingBoardingCompleted, EndingCarCompleted,
    EndingChildCompleted, EndingYouthCompleted, EndingHospitalCompleted, EndingFocusCompleted,
    EndingNameHeard, EndingFinalLineCompleted, DemoCompleted, Scene01MemoryFlashPlayed, Scene01GateKeyCollected }
public enum AdultOutfit { Default, Overalls, SchoolUniform }

[DisallowMultipleComponent]
public class StoryStateManager : MonoBehaviour
{
    private readonly HashSet<StoryFlag> flags = new HashSet<StoryFlag>();
    public AdultOutfit Outfit { get; private set; }
    // Session memory only; a reload resumes at a safe center in the remembered cell.
    public Vector2Int Scene03MazeCell { get; private set; } = new Vector2Int(14, 0);
    public void SetScene03MazeCell(Vector2Int cell) { Scene03MazeCell = cell; }
    // Same-run school resume only. Never interpreted as a disk save or a chase checkpoint.
    public bool HasSchoolPosition { get; private set; }
    public string SchoolChangedMediaPassword => Has(StoryFlag.SchoolMediaPasswordChanged) ? "xpzt0715" : null;
    public string SchoolRoomId { get; private set; }
    public Vector2 SchoolLocalPosition { get; private set; }
    public float SchoolFacing { get; private set; } = 1;
    public void RememberSchoolPosition(string room, Vector2 localPosition, float facing)
    {
        SchoolRoomId = room; SchoolLocalPosition = localPosition;
        SchoolFacing = facing < 0 ? -1 : 1; HasSchoolPosition = true;
    }
    // Observers see the entire transaction, never a half-copied / half-changed pair.
    public void SetTogether(params StoryFlag[] values)
    {
        bool changed = false;
        foreach (var flag in values) changed |= flags.Add(flag);
        if (changed) Changed?.Invoke();
    }
    public event Action Changed;
    // Checkpoints restore only their chapter-local keys in one notification.
    internal void RestoreSubset(IReadOnlyDictionary<StoryFlag,bool> values)
    {
        bool changed=false;
        foreach(var pair in values)changed |= pair.Value ? flags.Add(pair.Key) : flags.Remove(pair.Key);
        if(changed)Changed?.Invoke();
    }
    public bool Has(StoryFlag flag) => flags.Contains(flag);
    public bool Set(StoryFlag flag, bool value = true)
    {
        bool changed = value ? flags.Add(flag) : flags.Remove(flag);
        if (changed) Changed?.Invoke();
        return changed;
    }
    public void RefreshObservers(){Changed?.Invoke();}
    public StorySnapshot CaptureSave(){var list=new List<string>();foreach(var flag in flags)list.Add(flag.ToString());list.Sort(StringComparer.Ordinal);return new StorySnapshot{flags=list.ToArray(),outfit=(int)Outfit,mazeCell=Scene03MazeCell,hasSchoolPosition=HasSchoolPosition,schoolRoom=SchoolRoomId,schoolLocal=SchoolLocalPosition,schoolFacing=SchoolFacing};}
    public void RestoreSave(StorySnapshot value,bool notify=true){
        flags.Clear();foreach(string name in value.flags)flags.Add((StoryFlag)Enum.Parse(typeof(StoryFlag),name));
        Outfit=(AdultOutfit)value.outfit;Scene03MazeCell=value.mazeCell;HasSchoolPosition=value.hasSchoolPosition;
        SchoolRoomId=value.schoolRoom;SchoolLocalPosition=value.schoolLocal;SchoolFacing=value.schoolFacing;
        if(notify)Changed?.Invoke();
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
        if (outfit == AdultOutfit.SchoolUniform && !Has(StoryFlag.SchoolUniformOwned)) return;
        Outfit = outfit;
        Changed?.Invoke();
    }
}
