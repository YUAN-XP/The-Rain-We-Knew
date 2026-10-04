using UnityEngine;
// School-only held-ball presentation. The original controller still owns mouse input and physics success.
[DefaultExecutionOrder(2000)]
public sealed class P05BasketHand:MonoBehaviour {
 public BasketballThrowController basket;public Rigidbody2D ball;public Transform launch;
 P02ActorPresentation actor;bool holding;
 void Update(){if(!basket||!PersistentRuntime.Instance)return;var p=PersistentRuntime.Instance.GetComponent<GameControlManager>()?.ActivePlayer;actor=p?p.GetComponent<P02ActorPresentation>():null;bool aim=basket.State==BasketballThrowController.ThrowState.Aiming;
  if(aim&&actor){actor.ToolKind="lamp";actor.ToolBlend=1;holding=true;}else if(holding&&actor){actor.ToolBlend=0;holding=false;}}
 void LateUpdate(){if(!holding||!actor||!actor.Rig||!ball)return;var h=actor.Rig.HandLamp;if(!h)return;var pos=h.position+new Vector3(actor.Direction=="left"?-.07f:.07f,.04f,0);ball.position=pos;ball.transform.position=pos;if(launch)launch.position=pos;}
 void OnDisable(){if(holding&&actor)actor.ToolBlend=0;holding=false;}
}
