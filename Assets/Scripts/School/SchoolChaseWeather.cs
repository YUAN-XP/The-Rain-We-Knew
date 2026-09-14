using UnityEngine;
using UnityEngine.UI;

// Procedural Alpha rain/thunder. It never owns gameplay time or a control lock.
public sealed class SchoolChaseWeather : MonoBehaviour
{
    public SchoolChaseController chase;
    GameObject canvas;RectTransform[] drops;Image flash;AudioSource rain,thunder;AudioClip rainClip,thunderClip;
    float time,nextThunder=6;bool wasActive,wasPaused;System.Random random=new System.Random(4268);
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
        rain=gameObject.AddComponent<AudioSource>();rain.playOnAwake=false;rain.loop=true;rain.volume=.085f;
        thunder=gameObject.AddComponent<AudioSource>();thunder.playOnAwake=false;thunder.volume=.16f;
        const int rate=22050;float[] a=new float[rate*2],b=new float[rate*3];float smooth=0;
        for(int i=0;i<a.Length;i++)a[i]=((float)random.NextDouble()*2-1)*.24f;
        for(int i=0;i<b.Length;i++){smooth=Mathf.Lerp(smooth,(float)random.NextDouble()*2-1,.028f);b[i]=smooth*Mathf.Exp(-i/(float)rate)*.9f;}
        rainClip=AudioClip.Create("SchoolRain_SyntheticAlpha",a.Length,1,rate,false);rainClip.SetData(a,0);rain.clip=rainClip;
        thunderClip=AudioClip.Create("SchoolThunder_SyntheticAlpha",b.Length,1,rate,false);thunderClip.SetData(b,0);thunder.clip=thunderClip;
        canvas.SetActive(false);
    }
    void Update()
    {
        if(canvas==null)return;
        bool active=chase!=null&&chase.IsEscaping&&chase.school.layout.CurrentRoom=="Roof";
        canvas.SetActive(active);
        if(!active){if(wasActive){rain.Stop();thunder.Stop();}wasActive=false;wasPaused=false;return;}
        if(!wasActive){rain.Play();wasActive=true;}
        bool paused=chase.Paused;
        if(paused){if(!wasPaused){rain.Pause();thunder.Pause();}wasPaused=true;return;}
        if(wasPaused){rain.UnPause();thunder.UnPause();wasPaused=false;}
        time+=Time.unscaledDeltaTime;
        foreach(var d in drops){var p=d.anchoredPosition;p+=new Vector2(-100,-690)*Time.unscaledDeltaTime;if(p.y<0)p=new Vector2((float)random.NextDouble()*1600,930);d.anchoredPosition=p;}
        if(time>=nextThunder){thunder.Play();nextThunder=time+6+(float)random.NextDouble()*5;}
        float alpha=thunder.isPlaying?Mathf.Max(0,.06f-thunder.time*.1f):0;
        flash.color=new Color(.72f,.79f,.9f,alpha);
    }
    void OnDisable(){if(rain!=null)rain.Stop();if(thunder!=null)thunder.Stop();if(canvas!=null)canvas.SetActive(false);}
    void OnDestroy(){if(rainClip!=null)Destroy(rainClip);if(thunderClip!=null)Destroy(thunderClip);}
}
