using UnityEngine;
// Sparse surface movement, without adding audio or changing the approved wide camera.
[DefaultExecutionOrder(1500)]
public sealed class P04RiverLife:MonoBehaviour {
 public Scene03RiverSequence sequence;public SpriteRenderer tree;
 LineRenderer[] ripples;Transform[] leaves;Material ink;Quaternion rest;bool initialized;
 void Start(){ink=new Material(Shader.Find("Sprites/Default"));rest=tree?tree.transform.localRotation:Quaternion.identity;initialized=true;
  ripples=new LineRenderer[5];for(int i=0;i<ripples.Length;i++){var g=new GameObject("P04_WaterRipple"){hideFlags=HideFlags.DontSave};g.transform.SetParent(transform,false);var l=g.AddComponent<LineRenderer>();l.sharedMaterial=ink;l.positionCount=20;l.loop=false;l.useWorldSpace=true;l.widthMultiplier=.008f;l.sortingOrder=-21;ripples[i]=l;}
  leaves=new Transform[2];for(int i=0;i<2;i++){var g=new GameObject("P04_OccasionalLeaf"){hideFlags=HideFlags.DontSave};g.transform.SetParent(transform,false);var mesh=new Mesh();mesh.vertices=new[]{new Vector3(0,.045f),new Vector3(.020f,.007f),new Vector3(0,-.045f),new Vector3(-.020f,-.007f)};mesh.uv=new[]{Vector2.up,Vector2.one,Vector2.right,Vector2.zero};mesh.triangles=new[]{0,1,2,0,2,3};mesh.RecalculateBounds();g.AddComponent<MeshFilter>().sharedMesh=mesh;var r=g.AddComponent<MeshRenderer>();r.sharedMaterial=ink;r.sortingOrder=-3;var b=new MaterialPropertyBlock();b.SetColor("_Color",new Color(.38f,.40f,.16f,.68f));r.SetPropertyBlock(b);leaves[i]=g.transform;}
 }
 void LateUpdate(){if(ripples==null)return;float t=GameplayClock.Now;
  if(sequence&&sequence.Appreciating){Breathe(sequence.chapter.child.transform.Find("Demo2026_PlayerPainting"),t);Breathe(sequence.boy.Find("NewArt_Actor"),t+.8f);}
  if(tree)tree.transform.localRotation=rest*Quaternion.Euler(0,0,Mathf.Sin(t*.7f)*.085f);
  for(int i=0;i<ripples.Length;i++){float age=Mathf.Repeat(t*.22f+i*.39f,1);float x=292+i*4.4f,y=72.2f+(i%3)*.54f;float w=.20f+age*.44f;for(int j=0;j<20;j++){float a=(j/19f)*Mathf.PI*1.6f+.3f;ripples[i].SetPosition(j,new Vector3(x+Mathf.Cos(a)*w,y+Mathf.Sin(a)*w*.12f,0));}float alpha=Mathf.Sin(age*Mathf.PI)*.13f;var c=new Color(.72f,.78f,.67f,alpha);ripples[i].startColor=ripples[i].endColor=c;}
  for(int i=0;i<leaves.Length;i++){float a=Mathf.Repeat(t+i*7.5f,23);bool visible=a<6;leaves[i].gameObject.SetActive(visible);if(visible){leaves[i].position=new Vector3(309.6f+i*.6f+Mathf.Sin(a*1.2f)*.22f,74.2f-a*.52f,0);leaves[i].rotation=Quaternion.Euler(0,0,a*35);}}
 }
 void Breathe(Transform t,float phase){if(!t)return;var r=t.GetComponent<SpriteRenderer>();if(!r||!r.enabled||r.forceRenderingOff)return;var s=t.localScale;s.y*=1+Mathf.Sin(phase*1.35f)*.0025f;t.localScale=s;}
 void OnDisable(){if(initialized&&tree)tree.transform.localRotation=rest;}
 void OnDestroy(){if(ink)Destroy(ink);if(leaves!=null)foreach(var t in leaves)if(t){var f=t.GetComponent<MeshFilter>();if(f&&f.sharedMesh)Destroy(f.sharedMesh);}}
}

