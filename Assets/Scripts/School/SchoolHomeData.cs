using UnityEngine;

[CreateAssetMenu(menuName="Game Data/School Home Return")]
public sealed class SchoolHomeData : ScriptableObject
{
    public string homeScene="Scene02_Courtyard",schoolScene="Scene04_School";
    public DialogueData guideFirst,guideRepeat,emailReaction;
    [TextArea] public string computerHint="我好像把密码设置成了我和郑涛的姓名首字母加上我的生日。";
    public string emailTitle="集合通知",emailSender="领导（占位标签）";
    [TextArea] public string emailBody="根据情报，东郊一处废弃工厂是毒贩聚集点。请带上工作证件，到该工厂外围集合。";
}
