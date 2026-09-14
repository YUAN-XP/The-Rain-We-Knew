using System;
using TMPro;
using UnityEngine;

public partial class DeskCloseupController
{
    [SerializeField] private SchoolHomeData schoolComputer;
    GameObject emailPanel;
    TMP_Text computerHint;
    int emailPresentedFrame;
    bool suppressEmailCompletion;
    public bool CanLoginSchoolComputer=>story!=null&&schoolComputer!=null&&story.Has(StoryFlag.SchoolMediaPasswordChanged)
        &&story.Has(StoryFlag.SchoolChaseCompleted)&&story.Has(StoryFlag.SchoolReturnedHome);
    public void ConfigureSchoolComputer(SchoolHomeData data)
    {
        schoolComputer=data;if(canvas!=null)BuildSchoolComputer();
    }
    void BuildSchoolComputer()
    {
        if(schoolComputer==null||emailPanel!=null)return;
        computerHint=RuntimePanelUI.Text("肖鹏："+schoolComputer.computerHint,computerPanel.transform,font,new Vector2(.15f,.79f),new Vector2(.85f,.94f),27);
        computerHint.textWrappingMode=TextWrappingModes.Normal;
        emailPanel=PageRoot("SchoolEmail",canvas.transform);
        RuntimePanelUI.Panel("邮件纸",emailPanel.transform,new Vector2(.12f,.13f),new Vector2(.88f,.88f),new Color(.10f,.15f,.15f));
        RuntimePanelUI.Text(schoolComputer.emailTitle,emailPanel.transform,font,new Vector2(.18f,.74f),new Vector2(.82f,.84f),39);
        RuntimePanelUI.Text("发件人："+schoolComputer.emailSender,emailPanel.transform,font,new Vector2(.18f,.63f),new Vector2(.82f,.72f),25);
        var body=RuntimePanelUI.Text("肖鹏："+schoolComputer.emailBody,emailPanel.transform,font,new Vector2(.20f,.31f),new Vector2(.80f,.60f),32);
        body.textWrappingMode=TextWrappingModes.Normal;
        body.alignment=TextAlignmentOptions.TopLeft;
        RuntimePanelUI.Button("读完并关闭 [Esc]",emailPanel.transform,font,new Vector2(.36f,.17f),new Vector2(.64f,.26f),Close);
        emailPanel.SetActive(false);
    }
    void SyncSchoolPage(string page)
    {
        BuildSchoolComputer();if(emailPanel!=null)emailPanel.SetActive(page=="Email");
        if(computerHint!=null)computerHint.gameObject.SetActive(CanLoginSchoolComputer);
        if(page=="Email")emailPresentedFrame=Time.frameCount;
    }
    bool TryComputerLogin()
    {
        if(!CanLoginSchoolComputer||!string.Equals(password.text,story.SchoolChangedMediaPassword,StringComparison.OrdinalIgnoreCase))return false;
        password.DeactivateInputField();story.Set(StoryFlag.SchoolComputerLoggedIn);ShowPage("Email");return true;
    }
    bool FinishSchoolEmail()
    {
        if(suppressEmailCompletion||Page!="Email"||!CanLoginSchoolComputer)return false;
        if(Time.frameCount<=emailPresentedFrame)return true;
        bool first=story.Set(StoryFlag.SchoolEmailRead);ShowPage("Desktop");Close();
        if(first&&schoolComputer.emailReaction!=null)GetComponentInChildren<DialogueManager>(true).StartDialogue(schoolComputer.emailReaction);
        return true;
    }
}
