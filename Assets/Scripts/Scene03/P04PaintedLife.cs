using UnityEngine;
// Scene-owned painted loops. No character rigs, controls or story flags are changed.
public sealed class P04PaintedLife:MonoBehaviour {
 public SpriteRenderer target;public Sprite[] frames;public float period=3.8f,breath=.0035f,sway=.2f,height,phaseOffset;public bool walking;
 Vector3 scale;Quaternion rotation;bool initialized;SpriteRenderer incoming;Sprite next;float blendStart;int index=-1;
 void Awake(){if(!target)target=GetComponent<SpriteRenderer>();scale=transform.localScale;rotation=transform.localRotation;initialized=true;}
 void LateUpdate(){if(!target)return;float t=GameplayClock.Now;float phase=Mathf.Repeat(t/Mathf.Max(.2f,period)+phaseOffset,1);float k=Mathf.Sin(phase*Mathf.PI*2);
  if(frames!=null&&frames.Length>0){int i=walking?(int)(t/.23f)%frames.Length:phase<.45f?0:phase<.66f?Mathf.Min(1,frames.Length-1):phase<.82f?frames.Length-1:0;
   if(frames[i]&&index!=i){index=i;if(walking||!target.sprite){target.sprite=frames[i];next=null;}else{next=frames[i];blendStart=t;if(!incoming){var g=new GameObject("P04_PaintedGestureBlend"){hideFlags=HideFlags.DontSave};g.transform.SetParent(transform,false);incoming=g.AddComponent<SpriteRenderer>();}incoming.sprite=next;incoming.flipX=target.flipX;incoming.sortingLayerID=target.sortingLayerID;incoming.sortingOrder=target.sortingOrder+1;incoming.enabled=true;}}
  }
  if(next&&incoming){float f=Mathf.SmoothStep(0,1,Mathf.Clamp01((t-blendStart)/.28f));var c=target.color;c.a*=f;incoming.color=c;if(target.sprite)incoming.transform.localScale=Vector3.one*target.sprite.bounds.size.y/next.bounds.size.y;if(f>=1){target.sprite=next;next=null;incoming.enabled=false;}}
  Vector3 fitted=scale;if(height>0&&target.sprite){float unit=height/target.sprite.bounds.size.y;var p=transform.parent?transform.parent.lossyScale:Vector3.one;fitted=new Vector3(unit/Mathf.Abs(p.x),unit/Mathf.Abs(p.y),1);}
  transform.localScale=new Vector3(fitted.x,fitted.y*(1+k*breath),fitted.z);transform.localRotation=rotation*Quaternion.Euler(0,0,k*sway);
 }
 void OnDisable(){if(initialized){transform.localScale=scale;transform.localRotation=rotation;}if(incoming)incoming.enabled=false;next=null;index=-1;}
}
