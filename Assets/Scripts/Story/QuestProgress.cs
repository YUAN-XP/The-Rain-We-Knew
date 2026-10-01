using System;
using System.Collections.Generic;
using UnityEngine;

// Stable semantic events. Presentation in P07 subscribes to Changed; no coordinate polling.
public enum QuestEvent {
    OpeningControlGranted=10,GateKeyMissing=20,GateKeyObtained=30,ChildShadowTriggered=40,
    ChildhoodEntered=50,PhoneDialogueCompleted=60,CameraObtained=70,BoyFound=80,ReturnControlAfterZhao=85,
    HomeEnteredAfterRiver=90,FamilyPhotoAgreed=100,CameraPlaced=110,OtherworldHomeControl=115,LampObtained=120,
    SchoolEntered=130,EquipmentVoiceHeard=140,EquipmentKeyObtained=150,PencilObtained=160,
    YouthMemoryEntered=170,YouthControlGranted=175,ZhangRuoyuanDialogueCompleted=180,
    SecurityCopiedAndPasswordChanged=190,OfficeReported=200,ChaseStarted=205,XiangYanDispersed=210,
    HomeReturnedAfterSchool=220,EmailRead=230,HandoverAndOutsideSound=235,BoardingTriggered=240
}
[Serializable] public sealed class QuestSnapshot { public int currentEvent; public int[] completedEvents=new int[0]; }
[DisallowMultipleComponent]
public sealed class QuestProgress : MonoBehaviour {
    readonly HashSet<int> completed=new HashSet<int>();
    public int CurrentEvent {get;private set;}
    public string TaskName => Label(CurrentEvent);
    public bool Visible => !string.IsNullOrEmpty(TaskName);
    public event Action Changed;
    StoryStateManager story;
    void Start(){story=GetComponent<StoryStateManager>();if(story){story.Changed+=ObserveStory;ObserveStory();}}
    void OnDestroy(){if(story)story.Changed-=ObserveStory;}
    void ObserveStory(){
        if(!story||GetComponent<GameSaveService>()?.IsRestoring==true)return;
        if(story.Has(StoryFlag.Scene01GateKeyCollected))Publish(QuestEvent.GateKeyObtained);
        if(story.Has(StoryFlag.ChildShadowSequencePlayed))Publish(QuestEvent.ChildShadowTriggered);
        if(story.Has(StoryFlag.Scene03Started))Publish(QuestEvent.ChildhoodEntered);
        if(story.Has(StoryFlag.Scene03PhoneAnswered))Publish(QuestEvent.PhoneDialogueCompleted);
        if(story.Has(StoryFlag.Scene03CameraTaken))Publish(QuestEvent.CameraObtained);
        if(story.Has(StoryFlag.Scene03ReturnedHome))Publish(QuestEvent.HomeEnteredAfterRiver);
        if(story.Has(StoryFlag.Scene03FamilySetupComplete))Publish(QuestEvent.FamilyPhotoAgreed);
        if(story.Has(StoryFlag.Scene03ReturnReactionPlayed))Publish(QuestEvent.OtherworldHomeControl);
        if(story.Has(StoryFlag.OilLampObtained))Publish(QuestEvent.LampObtained);
        if(story.Has(StoryFlag.FogIntroCompleted))Publish(QuestEvent.SchoolEntered);
        if(story.Has(StoryFlag.EquipmentVoicePlayed))Publish(QuestEvent.EquipmentVoiceHeard);
        if(story.Has(StoryFlag.BasketKeyCollected))Publish(QuestEvent.EquipmentKeyObtained);
        if(story.Has(StoryFlag.PencilOwned))Publish(QuestEvent.PencilObtained);
        if(story.Has(StoryFlag.SchoolNameRevealed))Publish(QuestEvent.YouthMemoryEntered);
        if(story.Has(StoryFlag.SchoolYouthConversationCompleted))Publish(QuestEvent.YouthControlGranted);
        if(story.Has(StoryFlag.SchoolUsbReceived))Publish(QuestEvent.ZhangRuoyuanDialogueCompleted);
        if(story.Has(StoryFlag.SchoolOfficeHandoffReady))Publish(QuestEvent.OfficeReported);
        if(story.Has(StoryFlag.SchoolChaseStarted))Publish(QuestEvent.ChaseStarted);
        if(story.Has(StoryFlag.SchoolChaseCompleted))Publish(QuestEvent.XiangYanDispersed);
        if(story.Has(StoryFlag.SchoolReturnedHome))Publish(QuestEvent.HomeReturnedAfterSchool);
        if(story.Has(StoryFlag.SchoolEmailRead))Publish(QuestEvent.EmailRead);
        if(story.Has(StoryFlag.EndingHandoverCompleted))Publish(QuestEvent.HandoverAndOutsideSound);
        if(story.Has(StoryFlag.EndingBoardingCompleted))Publish(QuestEvent.BoardingTriggered);
        if(story.Has(StoryFlag.SchoolEvidenceObtained)&&story.Has(StoryFlag.SchoolMediaPasswordChanged))Publish(QuestEvent.SecurityCopiedAndPasswordChanged);
    }
    public bool Publish(QuestEvent value){
        int id=(int)value;if(!Enum.IsDefined(typeof(QuestEvent),value))throw new ArgumentOutOfRangeException(nameof(value));
        bool added=completed.Add(id);if(id<=CurrentEvent)return added;
        CurrentEvent=id;Changed?.Invoke();return true;
    }
    public QuestSnapshot Capture(){var keys=new List<int>(completed);keys.Sort();return new QuestSnapshot{currentEvent=CurrentEvent,completedEvents=keys.ToArray()};}
    public static bool Valid(QuestSnapshot snapshot){
        if(snapshot==null||snapshot.completedEvents==null)return false;
        if(snapshot.currentEvent!=0&&!Enum.IsDefined(typeof(QuestEvent),snapshot.currentEvent))return false;
        int largest=0;var seen=new HashSet<int>();foreach(int id in snapshot.completedEvents){if(!Enum.IsDefined(typeof(QuestEvent),id)||!seen.Add(id))return false;largest=Math.Max(largest,id);}return largest==snapshot.currentEvent;
    }
    public void Restore(QuestSnapshot snapshot){if(!Valid(snapshot))throw new ArgumentException("Invalid quest snapshot");completed.Clear();foreach(int id in snapshot.completedEvents)completed.Add(id);CurrentEvent=snapshot.currentEvent;Changed?.Invoke();}
    public static string Label(int id){switch((QuestEvent)id){
        case QuestEvent.OpeningControlGranted:return "探索周围";
        case QuestEvent.GateKeyMissing:return "寻找铁门钥匙";
        case QuestEvent.GateKeyObtained:return "探索家属院";
        case QuestEvent.ChildShadowTriggered:return "追上阴影";
        case QuestEvent.ChildhoodEntered:return "接电话";
        case QuestEvent.PhoneDialogueCompleted:return "前往卧室拿上相机";
        case QuestEvent.CameraObtained:return "探索周围，前往河畔寻找男孩";
        case QuestEvent.ReturnControlAfterZhao:return "回家";
        case QuestEvent.HomeEnteredAfterRiver:return "探索客厅";
        case QuestEvent.FamilyPhotoAgreed:return "将相机放至机架";
        case QuestEvent.OtherworldHomeControl:return "探索二楼次卧";
        case QuestEvent.LampObtained:return "穿越黑雾";
        case QuestEvent.SchoolEntered:return "探索学校";
        case QuestEvent.EquipmentVoiceHeard:return "寻找钥匙";
        case QuestEvent.EquipmentKeyObtained:return "探索体育器材室";
        case QuestEvent.PencilObtained:return "探索学校";
        case QuestEvent.YouthControlGranted:return "前往二楼一班";
        case QuestEvent.ZhangRuoyuanDialogueCompleted:return "探索一楼";
        case QuestEvent.SecurityCopiedAndPasswordChanged:return "前往三楼教师办公室";
        case QuestEvent.ChaseStarted:return "取得灯油，逃离学校";
        case QuestEvent.XiangYanDispersed:return "穿越黑雾";
        case QuestEvent.HomeReturnedAfterSchool:return "探索二楼次卧";
        case QuestEvent.HandoverAndOutsideSound:return "探索屋外";
        default:return "";
    }}
}
