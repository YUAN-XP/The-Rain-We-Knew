using System.Linq;
using UnityEngine;

/// <summary>Decorates the existing pursuer; never owns AI, timing, collisions or locks.</summary>
public sealed class SchoolEnemyArtwork : MonoBehaviour
{
 [SerializeField] PresentationArtLibrary art;
 Transform decorated;
 void LateUpdate(){
  if(!art||decorated)return;
  var target=transform.Find("XiangYan_ChaseActor");if(!target)return;
  var old=target.GetComponentsInChildren<SpriteRenderer>(true);
  var view=art.Add(target,"NewArt_Pursuer","xiangyan_shadow",new Vector2(0,-1.2f),0,2.3f,22);
  if(!view)return;
  var hide=view.gameObject.AddComponent<RetiredArtVisibility>();hide.SetRenderers(old);
  decorated=target;
 }
}
