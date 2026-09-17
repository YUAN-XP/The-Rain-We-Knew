using System.Collections;
using UnityEngine;

public sealed partial class EndingDirector
{
    IEnumerator ChildMemory(){
        // Independent action and dialogue gates: advancing text cannot skip the gift motion.
        bool actionDone=false;StartCoroutine(Gift(()=>actionDone=true));
        yield return Say("F01");if(!speechCompleted)yield break;
        while(!actionDone)yield return null;
        yield return Say("F02");if(!speechCompleted)yield break;
        Sound(data.thunder);yield return Cover(new Color(1,1,1,.9f),.1f);yield return Wait(.2f);
        story.Set(StoryFlag.EndingChildCompleted);SetSets(youthSet);Step=EndingStep.WalkYouth;yield return Cover(Color.clear,.5f);
    }
    IEnumerator Gift(System.Action done){
        var gift=EndingActorView.Part("取出的拨片项链",presentation,data.bodySprite,Vector2.zero,new Vector2(.13f,.17f),new Color(.95f,.77f,.38f),30).transform;
        Vector3 pocket=child.transform.TransformPoint(new Vector3(.3f,-.35f,0));Vector3 chest=child.transform.TransformPoint(new Vector3(.45f,.45f,0));Vector3 neck=boy.pendant.position;
        gift.position=pocket;float t=0;
        while(t<data.actionSeconds){t+=Delta;float f=Mathf.Clamp01(t/data.actionSeconds);Vector3 pos=f<.35f?Vector3.Lerp(pocket,chest,f/.35f):Vector3.Lerp(chest,neck,(f-.35f)/.65f);child.Reach(pos);gift.position=pos;yield return null;}
        boy.pendant.gameObject.SetActive(true);Destroy(gift.gameObject);yield return Wait(.6f);done();
    }
    IEnumerator YouthMemory(){
        bool actionDone=false;StartCoroutine(SupportArm(()=>actionDone=true));
        yield return Say("F03");if(!speechCompleted)yield break;while(!actionDone)yield return null;
        yield return Say("F04");if(!speechCompleted)yield break;
        Sound(data.thunder);yield return Cover(Color.white,.12f);yield return Wait(.2f);
        story.Set(StoryFlag.EndingYouthCompleted);SetSets(hospitalSet);Step=EndingStep.WalkHospital;yield return Cover(Color.clear,.5f);
    }
    IEnumerator SupportArm(System.Action done){
        Vector3 start=youth.hand.position;Vector3 end=friend.transform.TransformPoint(new Vector3(-.35f,.2f,0));float t=0;
        while(t<data.actionSeconds){t+=Delta;youth.Reach(Vector3.Lerp(start,end,Mathf.Clamp01(t/data.actionSeconds)));yield return null;}yield return Wait(.5f);done();
    }
    IEnumerator HospitalMemory(){
        bool actionDone=false;StartCoroutine(PassBeds(()=>actionDone=true));
        yield return Say("F05");if(!speechCompleted)yield break;while(!actionDone)yield return null;
        story.Set(StoryFlag.EndingHospitalCompleted);SetSets(wardSet);Step=EndingStep.WalkBed;bedArmed=false;
    }
    IEnumerator PassBeds(System.Action done){
        footsteps=Sound(data.wheelsLoop,true,.28f);ActiveStretchers=2;
        Vector3[] starts={stretchers[0].position,stretchers[1].position};float t=0;float seconds=data.actionSeconds*2.2f;
        while(t<seconds){t+=Delta;for(int i=0;i<2;i++)stretchers[i].position=starts[i]+Vector3.left*20*Mathf.Clamp01(t/seconds);yield return null;}
        foreach(var bed in stretchers)bed.gameObject.SetActive(false);ActiveStretchers=0;if(footsteps!=null)footsteps.Stop();done();
    }
}
