using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class SchoolChaseController : IInventoryAccessPolicy
{
    [Header("Security room - original oil display and authored recipes")]
    public ItemData oilItem,thirdFragment;
    public SchoolSecurityPickup oilPickup,fragmentPickup;
    public CombinationRecipe[] recipes;
    public DialogueData finaleDialogue;
    InventoryUI inventoryUI;CombinationManager combination;
    bool spaceArmed,finaleStarted;
    [Tooltip("After the first successful bag close, allow reading/charging before the visible doorway threat starts moving. Bag pause preserves this clock.")]
    [Min(0)] public float finalePromptLead=1.2f;
    float finaleLeadRemaining;
    Image finaleFlash;
    public bool TryCanOpenInventory(out bool allowed)
    {
        allowed=false;if(!IsActive)return false;
        if(Step==SchoolChaseStep.Escaping||Step==SchoolChaseStep.Exploring){allowed=!Controls.IsLocked();return true;}
        if(Step!=SchoolChaseStep.OilTutorial&&Step!=SchoolChaseStep.OilReady&&Step!=SchoolChaseStep.FinalCharge)return true;
        allowed=true;foreach(ControlLockReason reason in Enum.GetValues(typeof(ControlLockReason)))
            if(reason!=ControlLockReason.SchoolSequence&&Controls.IsLockedBy(reason))allowed=false;
        return true;
    }
    void InitializeSecurity()
    {
        var runtime=PersistentRuntime.Instance;
        combination=runtime.GetComponent<CombinationManager>()??runtime.gameObject.AddComponent<CombinationManager>();combination.Configure(recipes);
        inventoryUI=runtime.GetComponentInChildren<InventoryUI>(true);if(inventoryUI!=null){inventoryUI.SetAccessPolicy(this);inventoryUI.Opened+=BagOpened;inventoryUI.Closed+=BagClosed;}
        combination.Completed+=CombinationCompleted;lamp.ChargeCompleted+=ChargeCompleted;
        var canvas=RuntimePanelUI.Canvas("SchoolLampFinalFlash",transform,160);finaleFlash=RuntimePanelUI.Panel("Glow",canvas,Vector2.zero,Vector2.one,Color.clear,false);
        SyncSecurityPickups();
    }
    partial void OnSecurityReady(){finaleStarted=false;spaceArmed=false;Layout.SetNarrative(SchoolActor.Adult,SchoolPhase.SecurityFinale);SyncSecurityPickups();}
    void SyncSecurityPickups()
    {
        if(oilPickup!=null)oilPickup.SetAvailable(Step==SchoolChaseStep.SecurityArrival&&!Story.Has(StoryFlag.SchoolOilTaken));
        if(fragmentPickup!=null)fragmentPickup.SetAvailable(Step==SchoolChaseStep.Exploring&&!Story.Has(StoryFlag.SchoolFragment3Taken));
    }
    public void TakeSecurityItem(bool fragment)
    {
        if(Controls.IsLocked()||Layout.CurrentRoom!="SecurityRoom"||Layout.Variant!=SchoolWorldVariant.Otherworld)return;
        var item=fragment?thirdFragment:oilItem;var pickup=fragment?fragmentPickup:oilPickup;
        if(item==null||pickup==null||Vector2.Distance(Controls.ActivePlayer.transform.position,pickup.transform.position)>2.6f)return;
        if(fragment){
            if(Step!=SchoolChaseStep.Exploring||Story.Has(StoryFlag.SchoolFragment3Taken))return;
            school.Inventory.AddItem(item);if(!school.Inventory.HasItem(item))return;Story.Set(StoryFlag.SchoolFragment3Taken);SyncSecurityPickups();SaveCompletedResume();Notice("获得了报纸碎片3。",3);return;
        }
        if(Step!=SchoolChaseStep.SecurityArrival||Story.Has(StoryFlag.SchoolOilTaken))return;
        school.Inventory.AddItem(item);if(!school.Inventory.HasItem(item))return;
        OwnSequence();Story.Set(StoryFlag.SchoolOilTaken);Step=SchoolChaseStep.OilTutorial;SyncSecurityPickups();inventoryUI?.PreferKeyItems();
        Notice("按【tab】打开背包右键组合关键物品",8);
    }
    void CombinationCompleted(CombinationRecipe recipe)
    {
        if(Step==SchoolChaseStep.OilTutorial&&recipe.restoreLamp&&Story.Has(StoryFlag.SchoolLampRestored)&&lamp.StoryUsable)Step=SchoolChaseStep.OilReady;
        if(Step==SchoolChaseStep.Exploring)SaveCompletedResume();
    }
    void BagOpened(){if(Step==SchoolChaseStep.FinalCharge){spaceArmed=false;lamp.CancelCharge();}}
    void BagClosed()
    {
        if(Step==SchoolChaseStep.OilTutorial){Notice("按【tab】打开背包右键组合关键物品",6);return;}
        if(Step==SchoolChaseStep.OilReady&&!finaleStarted){StartCoroutine(BeginFinale());return;}
        if(Step==SchoolChaseStep.FinalCharge){spaceArmed=false;lamp.CancelCharge();}
    }
    IEnumerator BeginFinale()
    {
        finaleStarted=true;OwnSequence();cameraTool.AbortCapture();lamp.RestoreUseState(true);
        Route=SchoolChaseRoute.SecurityFinale;Step=SchoolChaseStep.FinalCharge;spaceArmed=false;
        var door=Layout.data.Portal("Security_Door_BtoA");float x=Layout.data.Room(door.sourceRoom).ToWorld(door.sourceLocal).x;
        ClearEnemy();Spawn("SecurityRoom",x,false);inputAfterFrame=Time.frameCount+1;finaleLeadRemaining=finalePromptLead;
        Notice("按住【Space】让灯光驱散向焱",8);yield return null;
    }
    bool SecurityPaused {
        get {if(inventoryUI!=null&&inventoryUI.IsOpen||school.SecurityPanelOpen||Time.timeScale<=0)return true;
            foreach(ControlLockReason r in Enum.GetValues(typeof(ControlLockReason)))if(r!=ControlLockReason.SchoolSequence&&Controls.IsLockedBy(r))return true;
            return false;}
    }
    partial void TickSecurity()
    {
        TickFinalCharge(GameplayInput.GetKey(KeyCode.Space),GameplayClock.Delta);
    }
    void TickFinalCharge(bool spaceHeld,float dt)
    {
        if(Step!=SchoolChaseStep.FinalCharge)return;
        if(SecurityPaused){spaceArmed=false;lamp.CancelCharge();return;}
        if(Time.frameCount<=inputAfterFrame)return;
        if(!spaceHeld){spaceArmed=true;lamp.CancelCharge();}
        else if(spaceArmed){if(!lamp.IsCharging)lamp.BeginCharge();lamp.TickCharge(true,dt);}
        if(Step!=SchoolChaseStep.FinalCharge)return;
        // Prompt and original charge input are live immediately. This one-time
        // lead is not refreshed by reopening the bag; its remaining time pauses.
        float approachDt=Mathf.Max(0,dt-finaleLeadRemaining);
        finaleLeadRemaining=Mathf.Max(0,finaleLeadRemaining-dt);
        if(EnemyActive){MoveEnemyTowards(Controls.ActivePlayer.transform.position.x,finalEnemySpeed*approachDt);
            if(Mathf.Abs(Controls.ActivePlayer.transform.position.x-enemy.transform.position.x)<=catchDistance)Die();}
    }
    void ChargeCompleted(){if(Step==SchoolChaseStep.FinalCharge&&!SecurityPaused)StartCoroutine(Disperse());}
    IEnumerator Disperse()
    {
        Step=SchoolChaseStep.Dispersing;Route=SchoolChaseRoute.Completed;session.AdvanceGeneration();ClearEnemy();
        cameraTool.AbortCapture();cameraTool.SetCaptureContext(null);cameraTool.ContextAvailable=false;
        for(float t=0;t<.85f;t+=GameplayClock.Delta){finaleFlash.color=new Color(1,.9f,.64f,Mathf.Sin(t/.85f*Mathf.PI)*.65f);yield return null;}
        finaleFlash.color=Color.clear;Story.Set(StoryFlag.SchoolChaseCompleted);
        yield return Say(finaleDialogue);
        // A cancelled dialogue does not re-enable capture. The threat has
        // already been permanently removed, and retry remains checkpointed.
        Step=SchoolChaseStep.Exploring;Layout.SetNarrative(SchoolActor.Adult,SchoolPhase.SchoolRecovery);
        lamp.RestoreUseState(true);SyncSecurityPickups();cameraTool.ContextAvailable=true;
        while(GameplayInput.GetKey(KeyCode.Space)||GameplayInput.GetKey(KeyCode.E))yield return null;
        ReleaseSequence();SaveCompletedResume();
    }
    partial void RestoreSecurity(SchoolCheckpointReason reason)
    {
        inventoryUI?.CloseInventory();lamp.CancelCharge();finaleStarted=false;spaceArmed=false;finaleLeadRemaining=0;
        if(finaleFlash!=null)finaleFlash.color=Color.clear;
        if(reason==SchoolCheckpointReason.Security)OnSecurityReady();SyncSecurityPickups();
    }
    partial void CleanupSecurity()
    {
        if(inventoryUI!=null){inventoryUI.Opened-=BagOpened;inventoryUI.Closed-=BagClosed;inventoryUI.SetAccessPolicy(null);inventoryUI.CloseInventory();}
        if(combination!=null)combination.Completed-=CombinationCompleted;if(lamp!=null){lamp.ChargeCompleted-=ChargeCompleted;lamp.CancelCharge();}
        if(finaleFlash!=null)finaleFlash.color=Color.clear;
    }
    void SaveCompletedResume()
    {
        if(Step==SchoolChaseStep.Exploring&&Story.Has(StoryFlag.SchoolChaseCompleted))
            session.CompletedResume=SchoolChaseCheckpoint.Capture(school,SchoolCheckpointReason.Security,SchoolChaseRoute.Completed);
    }
    void BeginHomeReturn()
    {
        if(Step!=SchoolChaseStep.Exploring||Controls.IsLocked()||!Story.Has(StoryFlag.SchoolChaseCompleted)||Layout.CurrentRoom!="Floor01")return;
        if(!lamp.IsIlluminating){Notice("先按 R 举起小油灯，再进入黑雾。",4);return;}
        if(homeData==null||!Application.CanStreamedLevelBeLoaded(homeData.homeScene)){Notice("返程入口尚未就绪。",4);return;}
        var cp=SchoolChaseCheckpoint.Capture(school,SchoolCheckpointReason.FogReturn,SchoolChaseRoute.Completed);if(cp==null)return;
        session.Store(cp);session.AdvanceGeneration();session.PendingReturnFog=true;session.PendingHomeArrival=false;
        cameraTool.AbortCapture();cameraTool.SetCaptureContext(null);ClearEnemy();
        if(WhiteSceneTransition.Begin(homeData.homeScene,Controls))Step=SchoolChaseStep.FogReturn;
        else session.PendingReturnFog=false;
    }
}
