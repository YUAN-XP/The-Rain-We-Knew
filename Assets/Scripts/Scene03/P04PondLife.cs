using UnityEngine;
// Scene-owned small pond ripples; no ambience source or story changes.
public sealed class P04PondLife:MonoBehaviour {
 public Vector2 centre;public float width=6.8f;LineRenderer[] lines;Material ink;
 void Start(){ink=new Material(Shader.Find("Sprites/Default"));lines=new LineRenderer[3];for(int i=0;i<3;i++){var g=new GameObject("P04_PondRipple"){hideFlags=HideFlags.DontSave};g.transform.SetParent(transform,false);var l=g.AddComponent<LineRenderer>();l.sharedMaterial=ink;l.useWorldSpace=true;l.positionCount=18;l.widthMultiplier=.008f;l.sortingOrder=-9;lines[i]=l;}}
 void LateUpdate(){if(lines==null)return;for(int i=0;i<lines.Length;i++){float age=Mathf.Repeat(GameplayClock.Now*.21f+i*.37f,1),w=.14f+age*.25f;for(int j=0;j<18;j++){float a=j/17f*Mathf.PI*1.7f;lines[i].SetPosition(j,new Vector3(centre.x+(i-1)*width*.29f+Mathf.Cos(a)*w,centre.y+i*.045f+Mathf.Sin(a)*w*.09f,0));}lines[i].startColor=lines[i].endColor=new Color(.79f,.82f,.64f,Mathf.Sin(age*Mathf.PI)*.14f);}}
 void OnDestroy(){if(ink)Destroy(ink);}
}
