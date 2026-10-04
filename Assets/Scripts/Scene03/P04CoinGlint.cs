using UnityEngine;
// A restrained clue on the still-hidden coin; no inventory or quest state is authored here.
public sealed class P04CoinGlint:MonoBehaviour {
 public Scene03Chapter chapter;LineRenderer ink;Material material;
 void Start(){var g=new GameObject("P04_RareCoinGlint"){hideFlags=HideFlags.DontSave};g.transform.SetParent(transform,false);ink=g.AddComponent<LineRenderer>();material=new Material(Shader.Find("Sprites/Default"));ink.sharedMaterial=material;ink.useWorldSpace=true;ink.widthMultiplier=.004f;ink.sortingOrder=-3;ink.positionCount=6;}
 void LateUpdate(){if(!ink||!chapter.Ready)return;float age=Mathf.Repeat(GameplayClock.Now+4,18);bool show=!chapter.Story.Has(StoryFlag.GameCoinObtained)&&age<.7f;ink.enabled=show;if(!show)return;var p=new Vector3(37.12f,30.885f,0);float a=Mathf.Sin(age/.7f*Mathf.PI)*.38f;var c=new Color(.84f,.78f,.49f,a);ink.startColor=ink.endColor=c;ink.SetPositions(new[]{p+Vector3.left*.03f,p+Vector3.right*.03f,p,p+Vector3.up*.025f,p+Vector3.down*.025f,p});}
 void OnDestroy(){if(material)Destroy(material);}
}
