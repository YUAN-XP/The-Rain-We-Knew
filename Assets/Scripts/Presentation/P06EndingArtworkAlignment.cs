using UnityEngine;
[DefaultExecutionOrder(230)]
public sealed class P06HeldPendant:MonoBehaviour {
 public EndingActorView actor;
 void LateUpdate(){if(actor&&actor.hand)transform.position=actor.hand.position;}
}
public sealed class P06EndingGroundVisibility:MonoBehaviour {
 SpriteRenderer surface;EndingDirector owner;
 void LateUpdate(){if(!surface)surface=GetComponent<SpriteRenderer>();if(!owner)owner=GetComponentInParent<EndingDirector>();
  if(surface&&owner)surface.enabled=owner.Step!=EndingStep.WalkBed&&owner.Step!=EndingStep.BedIntro&&owner.Step!=EndingStep.Focus&&owner.Step!=EndingStep.Declaration&&owner.Step!=EndingStep.Complete;}
}
public sealed class P06WardArtworkBounds:MonoBehaviour {
 SpriteRenderer plate;EndingDirector owner;
 public void Initialize(SpriteRenderer image,EndingDirector director){plate=image;owner=director;}
 void LateUpdate(){if(owner&&plate&&owner.Step==EndingStep.WalkBed)owner.AlignWardArtwork(plate.bounds);}
}
public sealed partial class EndingDirector {
 public void AlignWardArtwork(Bounds bounds){
  // Fixed native artwork coordinates define the bedside and the curtain boundary.
  wardVisualLeft=bounds.min.x;wardVisualRight=bounds.max.x;
  ApplyWalkComposition(player.transform.position.x);
  bedAnchorX=bounds.min.x+bounds.size.x*.44f-.28f;
  stageRight=bounds.min.x+bounds.size.x*.80f+.15f;
 }
}
