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
        var frame=RuntimePanelUI.Panel("MailApplication",emailPanel.transform,new Vector2(.075f,.105f),new Vector2(.925f,.92f),new Color(.12f,.16f,.17f));
        DemoPaintedFrame.Add(frame.transform,new Color(.46f,.52f,.49f),2);
        RuntimePanelUI.Panel("TitleBar",frame.transform,new Vector2(0,.90f),Vector2.one,new Color(.20f,.27f,.28f));
        RuntimePanelUI.Text("邮件",frame.transform,font,new Vector2(.025f,.91f),new Vector2(.16f,.985f),27);
        RuntimePanelUI.Text("收件箱",frame.transform,font,new Vector2(.02f,.71f),new Vector2(.17f,.81f),25);
        RuntimePanelUI.Text("通知 · 1",frame.transform,font,new Vector2(.02f,.60f),new Vector2(.17f,.69f),22);
        var paper=RuntimePanelUI.Panel("NoticeLetter",frame.transform,new Vector2(.19f,.095f),new Vector2(.975f,.875f),new Color(.88f,.86f,.79f));
        var title=RuntimePanelUI.Text(schoolComputer.emailTitle,paper.transform,font,new Vector2(.055f,.80f),new Vector2(.94f,.945f),35);title.color=new Color(.12f,.18f,.19f);title.alignment=TextAlignmentOptions.Left;
        var sender=RuntimePanelUI.Text("发件人："+schoolComputer.emailSender.Replace("（占位标签）","").Replace("(占位标签)",""),paper.transform,font,new Vector2(.055f,.695f),new Vector2(.94f,.785f),23);sender.color=new Color(.30f,.35f,.33f);sender.alignment=TextAlignmentOptions.Left;
        RuntimePanelUI.Panel("LetterDivider",paper.transform,new Vector2(.055f,.67f),new Vector2(.94f,.673f),new Color(.55f,.59f,.55f));
        var body=RuntimePanelUI.Text(schoolComputer.emailBody,paper.transform,font,new Vector2(.065f,.24f),new Vector2(.925f,.62f),29);body.color=new Color(.14f,.19f,.19f);body.textWrappingMode=TextWrappingModes.Normal;body.alignment=TextAlignmentOptions.TopLeft;
        RuntimePanelUI.Button("读完并关闭 [Esc]",paper.transform,font,new Vector2(.31f,.055f),new Vector2(.70f,.17f),Close);
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
