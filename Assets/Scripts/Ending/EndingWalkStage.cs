using UnityEngine;
public sealed partial class EndingDirector {
 float foregroundY,segmentTriggerX,bedAnchorX,stageCenter,stageLeft,stageRight;
 Transform walkStage;
 const float LaneTop=-1.1f;
 void PrepareWalkLane(){
  var collider=player.GetComponent<Collider2D>();foregroundY=LaneTop+(collider?player.transform.position.y-collider.bounds.min.y:1.1f);
  var tile=new GameObject("EndingWalkGround");tile.transform.SetParent(presentation,false);
  var ground=Shape("Ground",tile.transform,new Vector2(28,LaneTop-.6f),new Vector2(128,1.2f),new Color(.15f,.20f,.22f),6);ground.gameObject.AddComponent<BoxCollider2D>();
 }
 void EnterWalkStage(EndingStep next){
  Step=next;float x=player.transform.position.x;segmentTriggerX=x+Mathf.Min(5f,Mathf.Max(1f,childWatchX));
  var set=next==EndingStep.WalkChild?childSet:next==EndingStep.WalkYouth?youthSet:next==EndingStep.WalkHospital?hospitalSet:wardSet;
  stageCenter=set==childSet?9:set==youthSet?23:set==hospitalSet?38:49;walkStage=set.transform;
  walkStage.position=new Vector3(x+(next==EndingStep.WalkBed?3.2f:7.6f)-stageCenter,0,0);SetSets(set);
  stageLeft=x-.5f;stageRight=x+6.1f;
  if(next==EndingStep.WalkBed){bedAnchorX=wardSet.transform.Find("WardBed").position.x-1.5f;bedArmed=false;stageRight=bedAnchorX+.65f;}
  ApplyWalkComposition(x);
 }
 // Both the wall and its ground stay anchored; only the camera and actor travel.
 void UpdateWalkStage(){}
}
