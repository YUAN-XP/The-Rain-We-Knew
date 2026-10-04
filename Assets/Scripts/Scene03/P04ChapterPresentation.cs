using System.Collections;
using UnityEngine;
// Rebuilt from persistent story state, including free exploration saves.
public sealed class P04ChapterPresentation:MonoBehaviour {
 public Scene03Chapter chapter;public Scene03RiverSequence river;public GameObject familyPhoto,pondFisher,leftoverRod;public Vector3 streetReturn=new Vector3(106,31.23f);public float riverExitX=294.70f;bool returning;float lastChildX;int facing=-1;
 IEnumerator Start(){while(!chapter.Ready)yield return null;chapter.Story.Changed+=Sync;Sync();lastChildX=chapter.child.transform.position.x;var observer=river.boy.GetComponent<P02CompanionFollow>();if(!observer)observer=river.boy.gameObject.AddComponent<P02CompanionFollow>();observer.Begin(chapter.child.transform);}
 void Sync(){if(familyPhoto)familyPhoto.SetActive(chapter.Story.Has(StoryFlag.Scene03FamilyPhotoTaken));bool gone=chapter.Story.Has(StoryFlag.Scene03RiverSequenceCompleted);if(pondFisher)pondFisher.SetActive(!gone);if(leftoverRod)leftoverRod.SetActive(gone);}
 void Update(){if(!chapter.Ready||chapter.Busy||chapter.Controls.IsLocked()||river.Busy)return;bool following=chapter.Story.Has(StoryFlag.Scene03RiverSequenceCompleted)&&!chapter.Story.Has(StoryFlag.Scene03ReturnedHome);if(!following)return;
  var player=chapter.child.transform.position;float dx=player.x-lastChildX;if(Mathf.Abs(dx)>.002f)facing=dx<0?-1:1;lastChildX=player.x;Transform area=chapter.maze.IsAtRiver?chapter.maze.riverRoot.transform:chapter.streetRoot.transform;if(river.boy.parent!=area)river.boy.SetParent(area,true);river.boy.gameObject.SetActive(true);
  float distance=player.x-river.boy.position.x;float target=player.x-facing*1.1f;if(Mathf.Abs(distance)>1.28f){float x=Mathf.MoveTowards(river.boy.position.x,target,chapter.child.WalkSpeed*GameplayClock.Delta);river.boy.position=new Vector3(x,player.y-.03f,0);} // accepted rig observes real movement
  if(chapter.maze.IsAtRiver&&player.x<=riverExitX&&!returning)StartCoroutine(ReturnStreet());
 }
 IEnumerator ReturnStreet(){returning=true;chapter.Controls.AcquireLock(ControlLockReason.Interaction,this);try{yield return chapter.Fade(1,.3f);chapter.ReturnToStreet(streetReturn);river.boy.SetParent(chapter.streetRoot.transform,true);river.boy.position=streetReturn+new Vector3(1.1f,-.03f,0);river.boy.GetComponent<P02CompanionFollow>()?.Relocated();yield return chapter.Fade(0,.4f);}finally{returning=false;chapter.Controls.ReleaseLock(ControlLockReason.Interaction,this);}}
 void OnDisable(){if(chapter&&chapter.Story!=null)chapter.Story.Changed-=Sync;StopAllCoroutines();if(chapter&&chapter.Controls)chapter.Controls.ReleaseLock(ControlLockReason.Interaction,this);}
}

