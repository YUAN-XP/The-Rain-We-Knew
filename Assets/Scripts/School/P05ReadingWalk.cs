using UnityEngine;
using System.Linq;
// Shared ground and coordinates; crossing an open gallery boundary updates routing without teleport/fade.
public sealed class P05ReadingWalk:MonoBehaviour {
 public SchoolSceneController school;float lastX;bool sampled;
 void Update(){if(!school||!school.Ready||GamePause.IsPaused||school.Controls.IsLocked()||!school.Controls.ActivePlayer)return;var p=school.Controls.ActivePlayer;float x=p.transform.position.x,y=p.transform.position.y;var l=school.layout;if(y<32||y>36){sampled=false;return;}if(!sampled){lastX=x;sampled=true;return;}string id=null;
 if(l.CurrentRoom=="Floor03Left"&&x>=26&&lastX<26)id="Reading_Left_AtoB";
 if(l.CurrentRoom=="ReadingPassage"&&x<26&&lastX>=26)id="Reading_Left_BtoA";
 if(l.CurrentRoom=="ReadingPassage"&&x>=48&&lastX<48)id="Reading_Right_BtoA";
 if(l.CurrentRoom=="Floor03Right"&&x<48&&lastX>=48)id="Reading_Right_AtoB";
 if(id!=null){var gate=l.GetComponentsInChildren<SchoolPortal>(true).FirstOrDefault(a=>a.portalId==id&&a.gameObject.activeInHierarchy);if(gate&&gate.CanPass()){if(school.chase&&school.chase.IsActive)school.chase.RecordWalkDoor(gate);gate.Arrived();}}
 lastX=x;
 }
}
