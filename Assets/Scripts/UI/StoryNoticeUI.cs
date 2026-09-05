using TMPro;
using UnityEngine;

public class StoryNoticeUI : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset font;
    private TMP_Text notice, tutorial;
    private float until;
    private void Awake()
    {
        var canvas=RuntimePanelUI.Canvas("StoryNoticeCanvas",transform,120);
        notice=RuntimePanelUI.Text("",canvas,font,new Vector2(.18f,.70f),new Vector2(.82f,.80f),32);
        tutorial=RuntimePanelUI.Text("",canvas,font,new Vector2(.24f,.12f),new Vector2(.76f,.19f),26);
    }
    public void Show(string text,float seconds=3f) { notice.text=text; until=Time.unscaledTime+seconds; }
    public void Tutorial(string text) { tutorial.text=text; }
    private void Update() { if(Time.unscaledTime>until) notice.text=""; }
}
