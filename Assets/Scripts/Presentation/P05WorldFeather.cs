using UnityEngine;
[ExecuteAlways,DefaultExecutionOrder(1300)]
public sealed class P05WorldFeather:MonoBehaviour {
 public Vector2 rightEdge=new Vector2(38,40.5f);MaterialPropertyBlock block;
 void LateUpdate(){var r=GetComponent<Renderer>();if(!r)return;if(block==null)block=new MaterialPropertyBlock();r.GetPropertyBlock(block);block.SetVector("_FadeEdge",rightEdge);r.SetPropertyBlock(block);}
}
