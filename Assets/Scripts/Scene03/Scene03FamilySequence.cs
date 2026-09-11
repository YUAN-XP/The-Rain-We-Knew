using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Scene03FamilySequence : MonoBehaviour
{
    public Scene03Chapter chapter;
    public Scene03RiverSequence river;
    public DialogueData invitation, timerExplanation, callBoy, readyForPhoto;
    public Transform mother, father, homeBoyPoint, roomBoyPoint, motherPose, fatherPose, childPose, boyEdge, boyOutside, viewPoint;
    public GameObject cameraPlacement, placedCamera;
    public AudioClip mechanicalTick, shutter, rain;
    public string returnSceneName;
    public bool Busy {get;private set;}
    public bool PhotoFrozen {get;private set;}
    public bool AwaitingPlacement=>chapter.Story!=null&&chapter.Story.Has(StoryFlag.Scene03FamilySetupComplete)&&!chapter.Story.Has(StoryFlag.Scene03FamilyPhotoTaken);
    bool evening,ownsLock;
    Image flash;
    CanvasGroup frame;
    AudioSource timer,rainSound;
    IEnumerator Start()
    {
        while(!chapter.Ready)yield return null;
        chapter.Story.Changed+=ApplyEvening;ApplyEvening();
        var canvas=RuntimePanelUI.Canvas("FamilyCameraPresentation",transform,160);
        flash=RuntimePanelUI.Panel("PhotoWhiteFlash",canvas,Vector2.zero,Vector2.one,new Color(1,1,1,0));
        flash.raycastTarget=false;
        var framing=RuntimePanelUI.Rect("FamilyCameraFrame",canvas,Vector2.zero,Vector2.one);frame=framing.gameObject.AddComponent<CanvasGroup>();frame.alpha=0;
        foreach(float y in new[]{.025f,.975f})RuntimePanelUI.Panel("PhotoFrame",framing,new Vector2(.025f,y),new Vector2(.975f,y+.002f),new Color(.96f,.93f,.80f),false);
        foreach(float x in new[]{.025f,.975f})RuntimePanelUI.Panel("PhotoFrame",framing,new Vector2(x,.025f),new Vector2(x+.001f,.975f),new Color(.96f,.93f,.80f),false);
        flash.transform.SetAsLastSibling();
        timer=gameObject.AddComponent<AudioSource>();timer.playOnAwake=false;rainSound=gameObject.AddComponent<AudioSource>();rainSound.playOnAwake=false;
        cameraPlacement.SetActive(AwaitingPlacement);placedCamera.SetActive(false);
    }
    void ApplyEvening()
    {
        if(evening||chapter.Story==null||!chapter.Story.Has(StoryFlag.Scene03ReturnedHome))return;
        evening=true;
        foreach(var root in new[]{chapter.homeRoot,chapter.livingRoomRoot})
            foreach(var renderer in root.GetComponentsInChildren<SpriteRenderer>(true))renderer.color*=new Color(1,.86f,.68f,1);
        var faceTemplate=father.Find("头").GetComponent<SpriteRenderer>();
        foreach(var actor in new[]{mother,father})
            foreach(var face in actor.GetComponentsInChildren<SpriteRenderer>(true))if(face.name=="头")
            {face.enabled=true;face.gameObject.SetActive(true);face.sprite=faceTemplate.sprite;face.sharedMaterial=faceTemplate.sharedMaterial;face.sortingOrder=10;face.color=new Color(.98f,.73f,.49f);}
        river.boy.SetParent(chapter.homeRoot.transform,true);river.boy.position=homeBoyPoint.position;
        river.boy.gameObject.SetActive(true);river.guitar.SetActive(false);
        var neck=river.boy.Find("GuitarNeck");if(neck!=null)neck.gameObject.SetActive(false);
        var talk=river.boy.GetComponent<RiverTalkInteractable>();if(talk!=null)talk.enabled=false;
        var collider=river.boy.GetComponent<Collider2D>();if(collider!=null)collider.enabled=false;
        chapter.neighbours.clip=chapter.content.home;chapter.neighbours.Play();
    }
    public void EnterLivingRoom()
    {
        if(!evening||Busy||chapter.Controls.IsLocked()||chapter.Story.Has(StoryFlag.Scene03FamilyPhotoTaken))return;
        river.boy.SetParent(chapter.livingRoomRoot.transform,true);river.boy.position=roomBoyPoint.position;
        if(!chapter.Story.Has(StoryFlag.Scene03FamilySetupComplete))StartCoroutine(Prepare());
        else cameraPlacement.SetActive(true);
    }
    IEnumerator Prepare()
    {
        Busy=true;Lock();
        try {yield return chapter.Say(invitation);yield return chapter.Say(timerExplanation);chapter.Story.Set(StoryFlag.Scene03FamilySetupComplete);cameraPlacement.SetActive(true);}
        finally {Busy=false;Unlock();}
    }
    public void PlaceCamera()
    {
        if(Busy||!AwaitingPlacement||chapter.Controls.IsLocked())return;
        StartCoroutine(Photograph());
    }
    IEnumerator Photograph()
    {
        Busy=true;Lock();
        try
        {
            chapter.carriedCamera.SetActive(false);cameraPlacement.SetActive(false);placedCamera.SetActive(true);
            var mode=PersistentRuntime.Instance.GetComponent<CameraModeController>();mode.Lower();mode.ContextAvailable=false;
            mother.position=motherPose.position;father.position=fatherPose.position;
            SetChild(childPose.position);river.boy.position=boyOutside.position;
            Camera.main.transform.position=new Vector3(viewPoint.position.x,viewPoint.position.y,Camera.main.transform.position.z);Camera.main.orthographicSize=4.2f;frame.alpha=1;
            timer.clip=mechanicalTick;timer.loop=true;timer.volume=.5f;timer.Play();
            yield return new WaitForSecondsRealtime(1.5f);yield return chapter.Say(callBoy);
            Vector3 from=river.boy.position;Vector3 childFrom=chapter.child.transform.position;
            for(float t=0;t<1.2f;t+=Time.unscaledDeltaTime)
            {float k=Mathf.Clamp01(t/1.2f);river.boy.position=Vector3.Lerp(from,boyEdge.position,k);SetChild(childFrom+Vector3.left*(.25f*Mathf.SmoothStep(0,1,k)));yield return null;}
            river.boy.position=boyEdge.position;
            yield return chapter.Say(readyForPhoto);timer.Stop();chapter.effects.PlayOneShot(shutter,.6f);
            yield return White(1,.08f);yield return White(0,.22f);frame.alpha=0;PhotoFrozen=true;
            rainSound.clip=rain;rainSound.loop=true;rainSound.volume=0;rainSound.Play();
            float ambient=chapter.ambience.volume,home=chapter.neighbours.volume;
            for(float t=0;t<2.6f;t+=Time.unscaledDeltaTime)
            {float k=t/2.6f;rainSound.volume=k*.4f;chapter.ambience.volume=ambient*(1-k);chapter.neighbours.volume=home*(1-k);yield return null;}
            yield return White(1,.65f);
            if(!Application.CanStreamedLevelBeLoaded(returnSceneName)){Debug.LogWarning("Family photo needs its Scene02 return scene.",this);yield return White(0,.3f);yield break;}
            chapter.Story.Set(StoryFlag.Scene03FamilyPhotoTaken);chapter.Story.Set(StoryFlag.Scene03Completed);chapter.Story.Set(StoryFlag.Scene03BirthdayKnown);
            chapter.ClearChildhoodItems();WhiteSceneTransition.Begin(returnSceneName,chapter.Controls);
        }
        finally{Busy=false;Unlock();}
    }
    IEnumerator White(float alpha,float duration)
    {float before=flash.color.a;for(float t=0;t<duration;t+=Time.unscaledDeltaTime){flash.color=new Color(1,1,1,Mathf.Lerp(before,alpha,t/duration));yield return null;}flash.color=new Color(1,1,1,alpha);}
    void SetChild(Vector3 p){var rb=chapter.child.GetComponent<Rigidbody2D>();rb.linearVelocity=Vector2.zero;rb.position=p;chapter.child.transform.position=p;}
    void Lock(){chapter.Controls.AcquireLock(ControlLockReason.Cutscene);ownsLock=true;}
    void Unlock(){if(ownsLock&&chapter!=null&&chapter.Controls!=null)chapter.Controls.ReleaseLock(ControlLockReason.Cutscene);ownsLock=false;}
    void OnDisable(){StopAllCoroutines();Unlock();if(chapter!=null&&chapter.Story!=null)chapter.Story.Changed-=ApplyEvening;}
}
