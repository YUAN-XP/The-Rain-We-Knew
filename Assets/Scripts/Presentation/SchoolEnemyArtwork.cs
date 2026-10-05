using UnityEngine;
/// <summary>Decorates the original enemy without owning AI, collisions, timing or routes.</summary>
public sealed class SchoolEnemyArtwork:MonoBehaviour {
 [SerializeField] PresentationArtLibrary art;Transform decorated;
 void LateUpdate(){
  if(!art||decorated)return;var target=transform.Find("XiangYan_ChaseActor");if(!target)return;
  var old=target.GetComponentsInChildren<SpriteRenderer>(true);
  var body=art.Add(target,"NewArt_Pursuer","xiangyan_shadow",new Vector2(0,-.8f),0,2.05f,22);if(!body)return;
  body.gameObject.AddComponent<RetiredArtVisibility>().SetRenderers(old);
  var view=P02ActorPresentation.AttachCompanion(target,body,"adult_school",2.05f);
  target.gameObject.AddComponent<P06PursuerMotion>().view=view;decorated=target;
 }
}
