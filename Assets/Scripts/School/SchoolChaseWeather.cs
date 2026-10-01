using UnityEngine;
using UnityEngine.UI;

// Visual rain/lightning only. It never owns gameplay time or a control lock.
public sealed class SchoolChaseWeather : MonoBehaviour
{
    public SchoolChaseController chase;
    GameObject canvas;RectTransform[] drops;Image flash;
    float time,nextThunder=6,lastLightning=-10;System.Random random=new System.Random(4268);
    void Start()
    {
        var root=RuntimePanelUI.Canvas("SchoolRoofRainAlpha",transform,35);canvas=root.gameObject;
        drops=new RectTransform[62];
        for(int i=0;i<drops.Length;i++) {
            var image=RuntimePanelUI.Panel("Rain",root,Vector2.zero,Vector2.zero,new Color(.62f,.73f,.84f,.23f),false);
            var d=image.rectTransform;d.sizeDelta=new Vector2(1.2f,19+i%11);d.localRotation=Quaternion.Euler(0,0,-12);drops[i]=d;
            d.anchoredPosition=new Vector2((float)random.NextDouble()*1600,(float)random.NextDouble()*900);
        }
        flash=RuntimePanelUI.Panel("DistantLightning",root,Vector2.zero,Vector2.one,Color.clear,false);
        canvas.SetActive(false);
    }
    void Update()
    {
        if(canvas==null)return;
        bool active=chase!=null&&chase.IsEscaping&&chase.school.layout.CurrentRoom=="Roof";
        canvas.SetActive(active);
        if(!active || chase.Paused)return;
        time+=GameplayClock.Delta;
        foreach(var d in drops){var p=d.anchoredPosition;p+=new Vector2(-100,-690)*GameplayClock.Delta;if(p.y<0)p=new Vector2((float)random.NextDouble()*1600,930);d.anchoredPosition=p;}
        if(time>=nextThunder){lastLightning=time;nextThunder=time+6+(float)random.NextDouble()*5;}
        float alpha=Mathf.Max(0,.06f-(time-lastLightning)*.1f);
        flash.color=new Color(.72f,.79f,.9f,alpha);
    }
    void OnDisable(){if(canvas!=null)canvas.SetActive(false);}

}
