using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CameraModeController : MonoBehaviour
{
    [SerializeField] private ItemData requiredItem;
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private AudioClip shutterClip;
    [SerializeField, Range(.15f,.25f)] private float blackHold=.2f;
    private GameControlManager controls;
    private InventoryManager inventory;
    private StoryStateManager story;
    private StoryNoticeUI notice;
    private GameObject viewfinder;
    private Image flash;
    private AudioSource sound;
    private Texture2D lensTexture;
    private Sprite lensSprite;
    private bool ownsRevealLock;
    private CameraCaptureContext captureContext;
    public void SetCaptureContext(CameraCaptureContext value)
    {
        if (captureContext == value) return;
        if (IsRaised) Lower();
        captureContext = value;
    }
    public bool ContextAvailable { get; set; } = true;
    public bool IsRaised { get; private set; }
    public bool IsBusy { get; private set; }
    public Camera CurrentCamera => Camera.main;
    // Checkpoint/scene owners cancel a pending exposure before restoring state.
    // Lower alone intentionally remains a normal preview cancellation.
    public void AbortCapture()
    {
        StopAllCoroutines(); Lower(); IsBusy=false;
        if(flash!=null)flash.color=Color.clear;
        if(ownsRevealLock && controls!=null)controls.ReleaseLock(ControlLockReason.Cutscene);
        ownsRevealLock=false;
    }
    private void Start()
    {
        controls=GetComponent<GameControlManager>(); inventory=GetComponent<InventoryManager>();
        story=GetComponent<StoryStateManager>(); notice=GetComponent<StoryNoticeUI>();
        sound=gameObject.AddComponent<AudioSource>(); sound.playOnAwake=false;
        var canvas=RuntimePanelUI.Canvas("CameraViewfinderCanvas",transform,110);
        viewfinder=RuntimePanelUI.Rect("Viewfinder",canvas,Vector2.zero,Vector2.one).gameObject;
        var lens=RuntimePanelUI.Panel("OldLens",viewfinder.transform,Vector2.zero,Vector2.one,Color.white,false);
        lensTexture=new Texture2D(256,144,TextureFormat.RGBA32,false); var pixels=new Color[256*144];
        var random=new System.Random(715);
        for(int y=0;y<144;y++) for(int x=0;x<256;x++) {
            float edge=Mathf.Max(Mathf.Abs(x/255f-.5f)*2,Mathf.Abs(y/143f-.5f)*2);
            float a=Mathf.Pow(edge,5)*.57f+(float)random.NextDouble()*.045f;
            pixels[y*256+x]=new Color(.11f,.12f,.10f,a);
        }
        lensTexture.SetPixels(pixels); lensTexture.Apply();
        lensSprite=Sprite.Create(lensTexture,new Rect(0,0,256,144),Vector2.one*.5f);
        lens.sprite=lensSprite;
        RuntimePanelUI.Text("[Space] 拍照    [Q] 放下相机",viewfinder.transform,font,new Vector2(.22f,.04f),new Vector2(.78f,.1f),25);
        flash=RuntimePanelUI.Panel("ShutterFlash",canvas,Vector2.zero,Vector2.one,Color.clear,false);
        viewfinder.SetActive(false);
        SceneManager.activeSceneChanged+=SceneChanged;
    }
    private void Update()
    {
        if (IsBusy) return;
        // OilLampController owns R. Do not also process Q/Space on that frame.
        if (Input.GetKeyDown(KeyCode.R)) return;
        if(Input.GetKeyDown(KeyCode.Q)) { if(IsRaised) Lower(); else Raise(); }
        else if(IsRaised && Input.GetKeyDown(KeyCode.Space)) Shoot();
    }
    public bool Raise()
    {
        if(!ContextAvailable || IsBusy || IsRaised || controls==null || controls.IsLocked() || inventory==null || requiredItem==null || !inventory.HasItem(requiredItem)) return false;
        if (captureContext != null && !captureContext.CanRaise) return false;
        var lamp = GetComponent<OilLampController>();
        if (lamp != null) lamp.LowerLamp();
        controls.AcquireLock(ControlLockReason.Camera); IsRaised=true; viewfinder.SetActive(true);
        if (captureContext != null) { captureContext.Preview(CurrentCamera); return true; }
        if(controls.AllowsAdultOutfits && !story.Has(StoryFlag.CameraDoorRevealed))
            notice.Show("二楼似乎有点不对劲");
        story.Set(StoryFlag.CameraFirstRaised); notice.Tutorial("");
        foreach(var target in FindObjectsByType<CameraRevealTarget>()) target.Preview(true);
        return true;
    }
    public void Lower()
    {
        if (IsRaised && captureContext != null) captureContext.CancelPreview(CurrentCamera);
        IsRaised=false; if(viewfinder!=null)viewfinder.SetActive(false);
        foreach(var target in FindObjectsByType<CameraRevealTarget>()) target.Preview(false);
        if(controls!=null)controls.ReleaseLock(ControlLockReason.Camera);
    }
    public void Shoot() { if(IsRaised && !IsBusy) StartCoroutine(Exposure()); }
    private IEnumerator Exposure()
    {
        IsBusy=true;
        if (captureContext != null)
        {
            var context = captureContext;
            if (shutterClip != null) sound.PlayOneShot(shutterClip, .55f);
            flash.color = Color.white;
            yield return new WaitForSecondsRealtime(.075f);
            if (context != null) context.CommitCapture(CurrentCamera);
            yield return new WaitForSecondsRealtime(.12f);
            Lower(); flash.color = Color.clear; IsBusy = false;
            yield break;
        }
        var targets=FindObjectsByType<CameraRevealTarget>();
        var hits=new System.Collections.Generic.List<CameraRevealTarget>();
        foreach(var target in targets) if(target.CanCapture(CurrentCamera))hits.Add(target);
        if(shutterClip!=null) sound.PlayOneShot(shutterClip,.55f);
        flash.color=Color.white; yield return new WaitForSecondsRealtime(.075f);
        flash.color=Color.black; yield return new WaitForSecondsRealtime(blackHold);
        // Transfer from camera to reveal lock without a frame of unlocked input.
        if(hits.Count>0) {controls.AcquireLock(ControlLockReason.Cutscene);ownsRevealLock=true;}
        Lower(); flash.color=Color.clear;
        foreach(var target in hits) if(target!=null) yield return target.Reveal();
        if(ownsRevealLock){controls.ReleaseLock(ControlLockReason.Cutscene);ownsRevealLock=false;}
        IsBusy=false;
    }
    private void SceneChanged(Scene oldScene,Scene newScene)
    {
        StopAllCoroutines(); Lower(); captureContext=null; IsBusy=false;
        if(flash!=null)flash.color=Color.clear;
        if(ownsRevealLock && controls!=null)controls.ReleaseLock(ControlLockReason.Cutscene);ownsRevealLock=false;
    }
    private void OnDisable() { StopAllCoroutines(); Lower(); if(ownsRevealLock && controls!=null)controls.ReleaseLock(ControlLockReason.Cutscene);ownsRevealLock=false; IsBusy=false; }
    private void OnDestroy() { SceneManager.activeSceneChanged-=SceneChanged; if(lensSprite!=null)Destroy(lensSprite); if(lensTexture!=null)Destroy(lensTexture); }
}
