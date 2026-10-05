using System.Collections.Generic;
using UnityEngine;
[DefaultExecutionOrder(-105)]
public sealed class P06PursuerMotion:MonoBehaviour {
 public P02ActorPresentation view;
 readonly Dictionary<P02CharacterRig,SpriteRenderer> heads=new Dictionary<P02CharacterRig,SpriteRenderer>();
 LineRenderer[] flames;float time;
 void LateUpdate(){
  if(!view||!view.Rig)return;var rig=view.Rig;rig.sampleRunning=true;
  if(!heads.TryGetValue(rig,out var head)){
   var old=rig.transform.Find("head/skin_head");if(!old)return;var body=old.GetComponent<SpriteRenderer>();
   var sprite=DemoArtCatalog.Find("P06_XiangHeads:"+(rig.definition.direction=="left"?"1":"0"));if(!sprite)return;
   head=new GameObject("P06_XiangIdentity").AddComponent<SpriteRenderer>();head.transform.SetParent(old,false);head.sprite=sprite;head.sortingOrder=body.sortingOrder;
   float scale=body.sprite.bounds.size.y/sprite.bounds.size.y;head.transform.localScale=Vector3.one*scale;head.transform.localPosition=body.sprite.bounds.center-sprite.bounds.center*scale;heads[rig]=head;
  }
  var original=rig.transform.Find("head/skin_head").GetComponent<SpriteRenderer>();original.enabled=false;
  foreach(var r in rig.GetComponentsInChildren<SpriteRenderer>())if(r!=head)r.color=new Color(.66f,.68f,.70f,1);
  if(flames==null){flames=new LineRenderer[9];for(int i=0;i<flames.Length;i++){var f=new GameObject("P06_BlackFlame_"+i).AddComponent<LineRenderer>();f.transform.SetParent(transform,false);f.useWorldSpace=false;f.positionCount=7;f.sharedMaterial=new Material(Shader.Find("Sprites/Default"));f.sortingOrder=23;f.numCapVertices=3;f.numCornerVertices=3;f.widthCurve=new AnimationCurve(new Keyframe(0,.11f),new Keyframe(.35f,.18f),new Keyframe(1,0));f.startColor=new Color(.018f,.025f,.03f,.56f);f.endColor=new Color(.035f,.045f,.05f,0);flames[i]=f;}}
  time+=GameplayClock.Delta;float side=rig.definition.direction=="left"?-1:1;
  for(int i=0;i<flames.Length;i++){var flame=flames[i];for(int n=0;n<7;n++){float t=n/6f;flame.SetPosition(n,new Vector3(Mathf.Sin(i*2.19f)*.22f-side*t*.25f+Mathf.Sin(time*4+i+n*.55f)*.055f, -.40f+(i%4)*.37f+t*(.42f+.12f*Mathf.Sin(time*2+i)),0));}}
 }
 void OnDestroy(){if(flames!=null)foreach(var f in flames)if(f&&f.sharedMaterial)Destroy(f.sharedMaterial);}
}
