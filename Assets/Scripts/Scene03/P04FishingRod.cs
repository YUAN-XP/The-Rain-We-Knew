using UnityEngine;
// A single attached rod follows the original gripping hands. No pose crossfade
// can duplicate this thin silhouette; the actor's approved painting stays intact.
[DefaultExecutionOrder(1600)]
public sealed class P04FishingRod:MonoBehaviour {
 public SpriteRenderer actor;
 public Vector2 gripPixels=new Vector2(809,689);
 public float length=1.15f,angle=36,period=5.6f;
 LineRenderer rod,line;Material ink;
 void Start(){if(!actor)actor=GetComponent<SpriteRenderer>();ink=new Material(Shader.Find("Sprites/Default"));rod=Make("P04_SingleFishingRod",8,.010f,.0035f,new Color(.49f,.32f,.15f),actor.sortingOrder+1);line=Make("P04_FishingLine",4,.002f,.0015f,new Color(.8f,.81f,.75f,.65f),actor.sortingOrder+1);}
 LineRenderer Make(string name,int count,float start,float end,Color c,int order){var g=new GameObject(name){hideFlags=HideFlags.DontSave};g.transform.SetParent(transform,false);var r=g.AddComponent<LineRenderer>();r.sharedMaterial=ink;r.useWorldSpace=true;r.positionCount=count;r.startWidth=start;r.endWidth=end;r.startColor=r.endColor=c;r.sortingLayerID=actor.sortingLayerID;r.sortingOrder=order;return r;}
 void LateUpdate(){if(!rod||!actor||!actor.sprite)return;bool visible=actor.enabled&&!actor.forceRenderingOff&&actor.color.a>.01f;rod.enabled=line.enabled=visible;if(!visible)return;
  var s=actor.sprite;var box=s.rect;float x=(gripPixels.x-box.x-s.pivot.x)/s.pixelsPerUnit;
  float y=(s.texture.height-gripPixels.y-box.y-s.pivot.y)/s.pixelsPerUnit;
  if(actor.flipX)x=-x;
  Vector3 hand=actor.transform.TransformPoint(new Vector3(x,y,0));
  float phase=GameplayClock.Now*Mathf.PI*2/Mathf.Max(1,period),bend=Mathf.Sin(phase)*1.8f;
  float a=(angle+bend)*Mathf.Deg2Rad;var forward=new Vector3(Mathf.Cos(a)*(actor.flipX?-1:1),Mathf.Sin(a),0);forward=actor.transform.rotation*forward;
  var normal=new Vector3(-forward.y,forward.x,0);Vector3 tip=hand;
  for(int i=0;i<8;i++){float k=i/7f;tip=hand+forward*(length*k)+normal*(.011f*Mathf.Sin(phase-.4f)*k*k);rod.SetPosition(i,tip);}
  for(int i=0;i<4;i++){float k=i/3f;line.SetPosition(i,tip+Vector3.down*(.65f*k)+Vector3.right*(.008f*Mathf.Sin(phase)*k));}
 }
 void OnDisable(){if(rod)rod.enabled=false;if(line)line.enabled=false;}
 void OnDestroy(){if(ink)Destroy(ink);}
}
