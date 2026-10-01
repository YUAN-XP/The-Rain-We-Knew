using System;
using System.Collections;
using TMPro;
using UnityEngine;

// A future stage may implement this boundary; no chase implementation belongs to Stage 5B.
public interface ISchoolOfficeExitHandler { bool TryExit(SchoolYouthController flow); }

[DisallowMultipleComponent]
public sealed class SchoolYouthController : MonoBehaviour
{
    public SchoolSceneController school;
    public SchoolYouthSecurity security;
    public SchoolReadingChair readingChair;
    public ItemData usbItem;
    public DialogueData zhengFull, zhengRemainder, zhangDialogue, toiletDialogue, toiletAfterSubmission;
    public DialogueData directorAccept, directorGoodbye;
    public DialogueData equipmentRecall, equipmentRecallAfterPassword;
    public MonoBehaviour officeExitHandler;
    public StoryStateManager Story => school != null ? school.Story : null;
    public InventoryManager Inventory => school != null ? school.Inventory : null;
    public GameControlManager Controls => school != null ? school.Controls : null;
    public DialogueManager Dialogue => school != null ? school.Dialogue : null;
    public TMP_FontAsset font => school != null ? school.font : null;
    public bool Ready => school != null && school.Ready && Story != null;
    public bool IsYouthMemory => Ready && school.layout.Actor == SchoolActor.Youth && school.layout.Variant == SchoolWorldVariant.Memory;
    public bool Busy => busy || modal != null || (security != null && security.IsOpen) || (readingChair != null && readingChair.IsOpen);
    public bool ModalOpen => modal != null;
    public bool LastDialogueCompleted { get; private set; }
    public bool HasUnsubmittedUsb => usbItem != null && Inventory != null && Inventory.HasItem(usbItem) && !Story.Has(StoryFlag.SchoolEvidenceSubmitted);
    public bool OfficeCanEnter => IsYouthMemory && Story.Has(StoryFlag.SchoolEvidenceObtained)
        && (HasUnsubmittedUsb || Story.Has(StoryFlag.SchoolEvidenceSubmitted));
    bool busy, ownsInteraction, ownsModal, warned;
    DialogueData ownedDialogue;
    GameObject modal;
    SchoolInteractable securityDoor;

    public IEnumerator ContinueOpening(bool oldPrefixComplete)
    {
        busy = true;
        if (!Story.Has(StoryFlag.SchoolYouthConversationCompleted))
        {
            yield return Say(oldPrefixComplete ? zhengRemainder : zhengFull);
            if (!LastDialogueCompleted) { busy = false; yield break; }
            Story.SetTogether(StoryFlag.Scene05OpeningPlayed, StoryFlag.SchoolYouthConversationCompleted);
            Notice("去一班找张若媛。", 4);
        }
        school.layout.SetNarrative(SchoolActor.Youth, Story.Has(StoryFlag.SchoolOfficeHandoffReady) ? SchoolPhase.OfficeHandoff : SchoolPhase.YouthExplore);
        busy = false;
        RestoreSessionPosition();
    }

    public void Interact(SchoolYouthInteractable source)
    {
        if (!IsYouthMemory || Busy || Controls.IsLocked()) return;
        switch (source.action)
        {
            case SchoolYouthAction.ZhengTao:
                if (!Story.Has(StoryFlag.SchoolYouthConversationCompleted)) Run(ContinueOpening(Story.Has(StoryFlag.Scene05OpeningPlayed)));
                else Notice(Story.Has(StoryFlag.SchoolUsbReceived) ? "郑涛还在座位旁。" : "张若媛在一班等你。", 3);
                break;
            case SchoolYouthAction.ZhangRuoyuan: Run(ReceiveUsb()); break;
            case SchoolYouthAction.Director: Run(SubmitEvidence()); break;
            case SchoolYouthAction.SecurityNote: security?.OpenNote(); break;
            case SchoolYouthAction.SecurityComputer: if (security != null) Run(security.UseComputer()); break;
            case SchoolYouthAction.ReadingChair: readingChair?.Toggle(); break;
            case SchoolYouthAction.Toilet:
                if (!Story.Has(StoryFlag.SchoolToiletHeard)) Run(OnceDialogue(Story.Has(StoryFlag.SchoolEvidenceSubmitted) ? toiletAfterSubmission : toiletDialogue, StoryFlag.SchoolToiletHeard));
                break;
            case SchoolYouthAction.Crowd: OnceNotice(StoryFlag.SchoolCrowdSeen, "太多人把路口堵住了，不知道在说些什么，过不去"); break;
            case SchoolYouthAction.Gate: OnceNotice(StoryFlag.SchoolGateTried, "还没放学，还是别离开了。"); break;
            case SchoolYouthAction.FemaleToilet: OnceNotice(StoryFlag.SchoolFemaleToiletTried, "光天化日进女厕所不太好吧，还是先别进去了"); break;
            case SchoolYouthAction.Roof: OnceNotice(StoryFlag.SchoolRoofTried, "老师们怕同学去天台玩出事，把去天台的楼梯间锁上了"); break;
        }
    }

    IEnumerator ReceiveUsb()
    {
        if (Story.Has(StoryFlag.SchoolUsbReceived)) { Notice("张若媛点了点头。", 3); yield break; }
        if (!Story.Has(StoryFlag.SchoolYouthConversationCompleted)) yield break;
        if (usbItem == null || usbItem.isPermanent || usbItem.category != ItemCategory.Regular)
        { Warn("School youth: bind one non-permanent Regular USB item."); yield break; }
        yield return Say(zhangDialogue);
        if (!LastDialogueCompleted) yield break;
        Inventory.AddItem(usbItem);
        if (Inventory.HasItem(usbItem)) { Story.Set(StoryFlag.SchoolUsbReceived); Notice("获得了 U 盘。", 3); }
    }

    IEnumerator SubmitEvidence()
    {
        if (Story.Has(StoryFlag.SchoolOfficeHandoffReady)) { Notice("教导主任已经收下了 U 盘。", 3); yield break; }
        if (!Story.Has(StoryFlag.SchoolEvidenceSubmitted))
        {
            if (!OfficeCanEnter || !HasUnsubmittedUsb) { Notice("还没有准备好要交给主任的监控。", 3); yield break; }
            yield return Say(directorAccept);
            if (!LastDialogueCompleted || !HasUnsubmittedUsb) yield break;
            Inventory.RemoveItem(usbItem);
            if (Inventory.HasItem(usbItem)) { Warn("School youth: USB transfer did not remove the item; progress was not committed."); yield break; }
            Story.Set(StoryFlag.SchoolEvidenceSubmitted);
        }
        // Resume here after an interrupted farewell. The handed-in USB is never requested again.
        yield return Say(directorGoodbye);
        if (!LastDialogueCompleted) yield break;
        Story.Set(StoryFlag.SchoolOfficeHandoffReady);
        school.layout.SetNarrative(SchoolActor.Youth, SchoolPhase.OfficeHandoff);
        SaveSessionPosition();
    }

    IEnumerator OnceDialogue(DialogueData data, StoryFlag flag)
    {
        if (Story.Has(flag)) yield break;
        yield return Say(data);
        if (LastDialogueCompleted) Story.Set(flag);
    }

    public bool HandleLegacy(SchoolInteractable source)
    {
        if (!IsYouthMemory) return false;
        switch (source.action)
        {
            case SchoolAction.SecurityDoor:
                if (school.layout.CurrentRoom == "SecurityRoom" || Story.Has(StoryFlag.SchoolSecurityMemoryOpened)) source.transition?.Interact();
                else { securityDoor = source; school.ShowSecurityPanel(); }
                return true;
            case SchoolAction.SecurityWindow: security?.OpenWindow(); return true;
            case SchoolAction.EquipmentDoor: source.transition?.Interact(); return true;
            case SchoolAction.EquipmentEcho:
                Run(OnceDialogue(Story.Has(StoryFlag.SchoolMediaPasswordChanged) ? equipmentRecallAfterPassword : equipmentRecall, StoryFlag.SchoolEquipmentRecalled)); return true;
            default: return true; // Old props and pickups have no normal-campus reward path.
        }
    }

    public bool TrySecurityCode(string code)
    {
        if (!IsYouthMemory || !Story.Has(StoryFlag.SchoolSecurityClueSeen) || code != "4268" || securityDoor == null) return false;
        Story.Set(StoryFlag.SchoolSecurityMemoryOpened);
        var door = securityDoor;
        school.CloseModal();
        door.transition?.Interact();
        return true;
    }

    public bool CanUsePortal(SchoolPortal portal, out bool allowed)
    {
        allowed = false;
        if (!IsYouthMemory || portal.Definition == null) return false;
        var d = portal.Definition;
        if (d.memory == SchoolPortalGate.OfficeEvidence)
        { allowed = d.sourceRoom == "StaffOffice" ? !Story.Has(StoryFlag.SchoolEvidenceSubmitted) : OfficeCanEnter; return true; }
        if (d.memory == SchoolPortalGate.SecurityClue)
        { allowed = d.sourceRoom == "SecurityRoom" || Story.Has(StoryFlag.SchoolSecurityMemoryOpened); return true; }
        return false;
    }

    public bool HandlePortal(SchoolPortal portal)
    {
        if (!IsYouthMemory || portal.Definition == null) return false;
        var d = portal.Definition;
        if (d.sourceRoom == "StaffOffice" && Story.Has(StoryFlag.SchoolEvidenceSubmitted))
        {
            if (!Story.Has(StoryFlag.SchoolOfficeHandoffReady)) { Notice("先向主任告别吧。", 3); return true; }
            if (officeExitHandler is ISchoolOfficeExitHandler handler && handler.TryExit(this)) return true;
            var canvas = BeginModal("开发检查点");
            RuntimePanelUI.Text("Stage5B已完成，后续追逐待接入", canvas.transform, font, new Vector2(.1f,.32f), new Vector2(.9f,.7f), 34);
            return true;
        }
        if (d.targetRoom == "StaffOffice" && !OfficeCanEnter)
        { Notice("里面都是老师和领导，没有事情还是最好不要进去了", 4); return true; }
        if (d.targetRoom == "Roof")
        { OnceNotice(StoryFlag.SchoolRoofTried, "老师们怕同学去天台玩出事，把去天台的楼梯间锁上了"); return true; }
        return false;
    }

    void Update()
    {
        if (!IsYouthMemory) return;
        if (school.youthGrabbingArm != null && school.youthGrabbingArm.activeSelf
            && Dialogue.CurrentDialogue == zhengFull && Dialogue.CurrentLineIndex >= 2)
            school.youthGrabbingArm.SetActive(false);
        if (modal != null && GameplayInput.GetKeyDown(KeyCode.Escape)) EndModal();
        if (Busy || school.SecurityPanelOpen || Controls.IsLocked() || Dialogue.DidDialogueEndThisFrame()) return;
        SaveSessionPosition();
        if (school.layout.CurrentRoom == "Floor02" && Controls.ActivePlayer.transform.position.x > 41
            && Controls.ActivePlayer.transform.position.x < 51)
            OnceNotice(StoryFlag.SchoolCrowdSeen, "太多人把路口堵住了，不知道在说些什么，过不去");
        if (school.layout.CurrentRoom == "EquipmentRoom" && !Story.Has(StoryFlag.SchoolEquipmentRecalled))
            Run(OnceDialogue(Story.Has(StoryFlag.SchoolMediaPasswordChanged) ? equipmentRecallAfterPassword : equipmentRecall, StoryFlag.SchoolEquipmentRecalled));
        if (school.layout.CurrentRoom == "Floor01" && Controls.ActivePlayer.transform.position.x >= 77
            && Story.Has(StoryFlag.SchoolToiletHeard) && !Story.Has(StoryFlag.SchoolCourtHintSeen))
        {
            Story.Set(StoryFlag.SchoolCourtHintSeen);
            if (!Story.Has(StoryFlag.SchoolEvidenceSubmitted)) Notice("下午的体育课很快就要到了，我得抓紧时间。", 4);
        }
    }

    public void SaveSessionPosition()
    {
        if (!IsYouthMemory || Controls.ActivePlayer == null) return;
        var room = school.layout.data.Room(school.layout.CurrentRoom);
        if (room == null) return;
        var point = (Vector2)Controls.ActivePlayer.transform.position - room.origin;
        // Save only an actually occupied, safe room point. No transient teleport/fade frame is sampled.
        if (point.x < .6f || point.x > room.width - .6f || point.y < .4f || point.y > 3f) return;
        var camera = FindAnyObjectByType<CameraFollow>();
        Story.RememberSchoolPosition(room.id, point, camera != null ? camera.Facing : 1);
    }

    void RestoreSessionPosition()
    {
        if (!Story.HasSchoolPosition) return;
        var room = school.layout.data.Room(Story.SchoolRoomId);
        if (room == null) return;
        Vector2 point = room.ToWorld(Story.SchoolLocalPosition);
        var player = Controls.ActivePlayer;
        player.transform.position = point;
        var body = player.GetComponent<Rigidbody2D>();
        if (body != null) { body.position = point; body.linearVelocity = Vector2.zero; }
        school.layout.EnterRoom(room.id);
        var camera = FindAnyObjectByType<CameraFollow>();
        if (camera != null) { camera.SetFacing(Story.SchoolFacing); camera.SnapToTarget(); }
        if (Story.Has(StoryFlag.SchoolEvidenceObtained) && !Story.Has(StoryFlag.SchoolEvidenceSubmitted) && Inventory.HasItem(usbItem))
            Inventory.SetRuntimePresentation(usbItem, "U盘（监控）", "已拷入用于举报的监控视频");
        Physics2D.SyncTransforms();
    }

    public void Run(IEnumerator routine)
    {
        if (busy || !IsYouthMemory) return;
        StartCoroutine(RunOwned(routine));
    }
    IEnumerator RunOwned(IEnumerator routine)
    {
        busy = true; ownsInteraction = true; Controls.AcquireLock(ControlLockReason.Interaction);
        try { yield return routine; }
        finally { if (ownsInteraction && Controls != null) Controls.ReleaseLock(ControlLockReason.Interaction); ownsInteraction = false; busy = false; }
    }
    public IEnumerator Say(DialogueData data)
    {
        LastDialogueCompleted = false;
        if (data == null || data.lines == null || data.lines.Length == 0)
        { Warn("School youth: required dialogue data is missing; transaction not completed."); yield break; }
        while (Dialogue.IsDialogueActive()) yield return null;
        int before = Dialogue.CompletedCount;
        ownedDialogue = data;
        Dialogue.StartDialogue(data);
        yield return null;
        while (Dialogue.IsDialogueActive()) yield return null;
        LastDialogueCompleted = Dialogue.CompletedCount > before && Dialogue.LastCompletedDialogue == data;
        ownedDialogue = null;
        yield return null;
    }
    public GameObject BeginModal(string title)
    {
        EndModal();
        Controls.AcquireLock(ControlLockReason.CloseUp); ownsModal = true;
        modal = RuntimePanelUI.Canvas(title, transform, 140).gameObject;
        RuntimePanelUI.Panel("Backdrop", modal.transform, Vector2.zero, Vector2.one, new Color(.025f,.035f,.035f,.96f));
        RuntimePanelUI.Text(title, modal.transform, font, new Vector2(.12f,.87f), new Vector2(.88f,.97f), 34);
        RuntimePanelUI.Button("退出 [Esc]", modal.transform, font, new Vector2(.83f,.91f), new Vector2(.98f,.98f), EndModal);
        return modal;
    }
    public void EndModal()
    {
        if (modal != null) { modal.SetActive(false); Destroy(modal); }
        modal = null;
        if (ownsModal && Controls != null) Controls.ReleaseLock(ControlLockReason.CloseUp);
        ownsModal = false;
    }
    public void Notice(string text, float seconds = 4)
    { if (PersistentRuntime.Instance != null) PersistentRuntime.Instance.GetComponent<StoryNoticeUI>()?.Show(text, seconds); }
    void OnceNotice(StoryFlag flag, string text) { if (Story.Has(flag)) return; Notice(text); Story.Set(flag); }
    void Warn(string text) { if (!warned) { Debug.LogWarning(text, this); warned = true; } }
    void OnDisable()
    {
        StopAllCoroutines();
        if (ownedDialogue != null && Dialogue != null) Dialogue.CancelDialogue(ownedDialogue);
        ownedDialogue = null;
        if (security != null) security.Close();
        if (readingChair != null) readingChair.Close();
        EndModal();
        if (ownsInteraction && Controls != null) Controls.ReleaseLock(ControlLockReason.Interaction);
        ownsInteraction = busy = false;
    }
}
