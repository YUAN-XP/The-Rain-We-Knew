using UnityEngine;
public sealed class Scene03HomeReturn:MonoBehaviour,IInteractable {
 public Scene03Chapter chapter;
 public string GetInteractionPrompt()=>chapter&&chapter.Ready&&chapter.Story.Has(StoryFlag.Scene03RiverSequenceCompleted)?"[E] 回家吃晚餐":"";
 public void Interact(){if(GetInteractionPrompt()==""||chapter.Busy||chapter.Controls.IsLocked())return;chapter.StartCoroutine(Return());}
 System.Collections.IEnumerator Return(){chapter.Controls.AcquireLock(ControlLockReason.Interaction,this);try{yield return chapter.Fade(1,.35f);chapter.ReturnHomeFromRiver();yield return chapter.Fade(0,.45f);}finally{chapter.Controls.ReleaseLock(ControlLockReason.Interaction,this);}}
}
