using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Native text and responsive ink geometry remain crisp at every output size.
public static class P07UI {
 public static readonly Color Paper=new Color(.94f,.925f,.865f), Ink=new Color(.075f,.085f,.09f);
 static TMP_FontAsset font;
 public static TMP_FontAsset Font {get {if(!font)font=Resources.Load<P07PresentationData>("P07Presentation")?.font;return font?font:TMP_Settings.defaultFontAsset;}}
 public static TMP_Text Text(string value,Transform parent,Vector2 min,Vector2 max,float size=30,bool dark=false){
  var t=RuntimePanelUI.Text(value,parent,Font,min,max,size);t.color=dark?Ink:Paper;t.alignment=TextAlignmentOptions.Left;t.enableAutoSizing=true;t.fontSizeMin=size*.76f;t.fontSizeMax=size;return t;
 }
 public static Button Button(string label,Transform parent,Vector2 min,Vector2 max,Action action,bool dark=false){
  var rect=RuntimePanelUI.Rect(label,parent,min,max);var g=rect.gameObject.AddComponent<P07InkGraphic>();g.raycastTarget=true;
  var b=rect.gameObject.AddComponent<Button>();b.targetGraphic=g;var c=b.colors;c.normalColor=new Color(1,1,1,0);c.highlightedColor=Paper;c.selectedColor=Paper;c.pressedColor=new Color(.8f,.79f,.73f);c.disabledColor=new Color(.6f,.6f,.6f,.10f);c.fadeDuration=.12f;b.colors=c;
  var text=Text(label,rect,new Vector2(.055f,.08f),new Vector2(.97f,.92f));var hover=rect.gameObject.AddComponent<P07ButtonInk>();hover.label=text;hover.button=b;hover.restColor=dark?Ink:Paper;text.color=hover.restColor;
  b.onClick.AddListener(()=>{GameplayInput.ConsumeFrame();action();});return b;
 }
 public static Image Sprite(string name,Transform parent,Vector2 min,Vector2 max,Sprite sprite){var i=RuntimePanelUI.Panel(name,parent,min,max,Color.white,false);i.sprite=sprite;i.preserveAspect=true;return i;}
 public static void PaperPanel(Transform parent,Vector2 min,Vector2 max){var r=RuntimePanelUI.Rect("PencilPaper",parent,min,max);var g=r.gameObject.AddComponent<P07InkGraphic>();g.color=Paper;g.raycastTarget=true;var grain=RuntimePanelUI.Rect("PencilGrain",r,Vector2.zero,Vector2.one).gameObject.AddComponent<P07PencilGrain>();grain.color=new Color(.12f,.12f,.10f,.055f);grain.raycastTarget=false;}
 public static void Back(Transform parent,Action action,bool dark=false)=>Button("[Esc] 返回",parent,new Vector2(.76f,.07f),new Vector2(.93f,.125f),action,dark);
 public static void Background(Transform parent,bool ending=false){
  RuntimePanelUI.Panel("Atmosphere",parent,Vector2.zero,Vector2.one,new Color(.08f,.12f,.16f),false);
  // Preserve the entire illustration. Wider displays use atmospheric margins,
  // avoiding distortion or amputated guitar/portraits from cover cropping.
  var r=RuntimePanelUI.Rect("P07PaintedBackground",parent,Vector2.zero,Vector2.one);
  var i=r.gameObject.AddComponent<Image>();var data=Resources.Load<P07PresentationData>("P07Presentation");i.sprite=ending?data?.endingBackground:data?.startBackground;i.preserveAspect=true;i.raycastTarget=false;
  var a=r.gameObject.AddComponent<AspectRatioFitter>();a.aspectMode=AspectRatioFitter.AspectMode.FitInParent;a.aspectRatio=i.sprite?i.sprite.rect.width/i.sprite.rect.height:16f/9;
  var overlay=RuntimePanelUI.Panel("Legibility",parent,Vector2.zero,Vector2.one,new Color(.01f,.025f,.04f,.18f),false);
  if(!ending){parent.gameObject.AddComponent<P07MenuRain>();RuntimePanelUI.Rect("SlowWaterMist",parent,Vector2.zero,Vector2.one).gameObject.AddComponent<P07MenuMist>();}
 }
}
public sealed class P07ButtonInk:MonoBehaviour,IPointerEnterHandler,IPointerExitHandler,ISelectHandler,IDeselectHandler {
 public TMP_Text label;public Button button;public Color restColor=P07UI.Paper;bool hover,selected;
 void Paint(){if(label)label.color=button&&button.interactable&&(hover||selected)?P07UI.Ink:restColor;}
 public void OnPointerEnter(PointerEventData e){hover=true;Paint();}public void OnPointerExit(PointerEventData e){hover=false;Paint();}
 public void OnSelect(BaseEventData e){selected=true;Paint();}public void OnDeselect(BaseEventData e){selected=false;Paint();}
}
[RequireComponent(typeof(CanvasRenderer))]
public sealed class P07InkGraphic:MaskableGraphic {
 public float progress=1;public bool line;
 protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();var r=rectTransform.rect;float width=r.width*Mathf.Clamp01(progress);if(width<=0)return;
  int count=line?34:18;for(int n=0;n<count;n++){float x=n/(float)count,y=(Mathf.Sin(n*2.31f)+Mathf.Sin(n*.79f))*(line?.8f:1.7f);float x2=(n+1)/(float)count;
   float lo=line?r.center.y-1.1f+y:r.yMin+2+y,hi=line?r.center.y+1.1f+y:r.yMax-2-y;int v=vh.currentVertCount;
   vh.AddVert(new Vector2(r.xMin+x*width,lo),color,Vector2.zero);vh.AddVert(new Vector2(r.xMin+x*width,hi),color,Vector2.zero);vh.AddVert(new Vector2(r.xMin+x2*width,hi+Mathf.Sin(n)),color,Vector2.zero);vh.AddVert(new Vector2(r.xMin+x2*width,lo),color,Vector2.zero);vh.AddTriangle(v,v+1,v+2);vh.AddTriangle(v,v+2,v+3);}
 }
 public void SetProgress(float value){progress=value;SetVerticesDirty();}
}
public sealed class P07MenuRain:MonoBehaviour {
 RectTransform[] drops;float clock;
 void Start(){drops=new RectTransform[60];for(int n=0;n<drops.Length;n++){float x=((n*37)%97)/97f;var i=RuntimePanelUI.Panel("MenuRain",transform,new Vector2(x,0),new Vector2(x,0),new Color(.7f,.78f,.82f,.12f),false);drops[n]=i.rectTransform;drops[n].sizeDelta=new Vector2(.65f,14+n%9);drops[n].localRotation=Quaternion.Euler(0,0,-13);}}
 void Update(){clock+=Time.unscaledDeltaTime;float height=((RectTransform)transform).rect.height;for(int n=0;drops!=null&&n<drops.Length;n++)drops[n].anchoredPosition=new Vector2(0,Mathf.Repeat(n*157-clock*(240+n%5*14),height+30)-15);}
}

[RequireComponent(typeof(CanvasRenderer))]
public sealed class P07PencilGrain:MaskableGraphic {
 protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();var r=rectTransform.rect;
  for(int n=0;n<150;n++){float x=r.xMin+r.width*Mathf.Repeat(n*.618034f,1),y=r.yMin+r.height*Mathf.Repeat(n*.414214f,1);float len=3+(n%8);int v=vh.currentVertCount;
   vh.AddVert(new Vector2(x,y),color,Vector2.zero);vh.AddVert(new Vector2(Mathf.Min(x+len,r.xMax),Mathf.Min(y+len*.25f,r.yMax)),color,Vector2.zero);vh.AddVert(new Vector2(Mathf.Min(x+len,r.xMax),Mathf.Min(y+len*.25f+.4f,r.yMax)),color,Vector2.zero);vh.AddVert(new Vector2(x,Mathf.Min(y+.4f,r.yMax)),color,Vector2.zero);vh.AddTriangle(v,v+1,v+2);vh.AddTriangle(v,v+2,v+3);}
 }
}
[RequireComponent(typeof(CanvasRenderer))]
public sealed class P07MenuMist:MaskableGraphic {
 float age;protected override void Awake(){base.Awake();raycastTarget=false;}
 void Update(){age+=Time.unscaledDeltaTime;SetVerticesDirty();}
 protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();var r=rectTransform.rect;
  for(int band=0;band<3;band++){float y=r.yMin+r.height*(.20f+band*.12f+.018f*Mathf.Sin(age*.12f+band));float h=r.height*.14f;float alpha=.018f+.012f*Mathf.Sin(age*.08f+band);var clear=new Color(.68f,.77f,.8f,0);var fog=new Color(.68f,.77f,.8f,alpha);
   int v=vh.currentVertCount;vh.AddVert(new Vector2(r.xMin,y-h),clear,Vector2.zero);vh.AddVert(new Vector2(r.xMin,y),fog,Vector2.zero);vh.AddVert(new Vector2(r.xMax,y),fog,Vector2.zero);vh.AddVert(new Vector2(r.xMax,y-h),clear,Vector2.zero);vh.AddTriangle(v,v+1,v+2);vh.AddTriangle(v,v+2,v+3);
   v=vh.currentVertCount;vh.AddVert(new Vector2(r.xMin,y),fog,Vector2.zero);vh.AddVert(new Vector2(r.xMin,y+h),clear,Vector2.zero);vh.AddVert(new Vector2(r.xMax,y+h),clear,Vector2.zero);vh.AddVert(new Vector2(r.xMax,y),fog,Vector2.zero);vh.AddTriangle(v,v+1,v+2);vh.AddTriangle(v,v+2,v+3);}
 }
}

// Small ink symbols are geometry, so they do not depend on missing font glyphs.
[RequireComponent(typeof(CanvasRenderer))]
public sealed class P07InkSymbol:MaskableGraphic {
 public bool arrow;
 protected override void OnPopulateMesh(VertexHelper vh){vh.Clear();var r=rectTransform.rect;var c=r.center;
  if(arrow){float a=Mathf.Min(r.width,r.height)*.28f;vh.AddVert(c+new Vector2(-a,a*.5f),color,Vector2.zero);vh.AddVert(c+new Vector2(a,a*.5f),color,Vector2.zero);vh.AddVert(c+new Vector2(0,-a),color,Vector2.zero);vh.AddTriangle(0,1,2);}
  else{int v=0;vh.AddVert(c,color,Vector2.zero);for(int n=0;n<=8;n++){float angle=n*Mathf.PI/4;float a=Mathf.Min(r.width,r.height)*(n%2==0?.46f:.14f);vh.AddVert(c+new Vector2(Mathf.Sin(angle),Mathf.Cos(angle))*a,color,Vector2.zero);if(n>0)vh.AddTriangle(v,n,n+1);}}
 }
}
