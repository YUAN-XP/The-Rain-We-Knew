using System;
using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CanvasGroup))]
public class ScreenFadeController : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Timing")]
    [Min(0f)]
    [SerializeField] private float fadeDuration = 0.35f;

    public bool IsFading { get; private set; }
    private object fadeOwner;
    private int fadeGeneration;
    public void CancelOwnedFade(object owner)
    {
        if(owner==null||!ReferenceEquals(fadeOwner,owner))return;
        fadeGeneration++;fadeOwner=null;IsFading=false;SetCanvasState(0,false);
    }

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        SetCanvasState(0f, false);
    }

    public IEnumerator FadeThroughBlack(
        Action actionWhileBlack, object owner = null)
    {
        if (IsFading)
        {
            yield break;
        }

        IsFading = true;
        fadeOwner=owner;int run=++fadeGeneration;
        SetCanvasState(
            canvasGroup != null
                ? canvasGroup.alpha
                : 0f,
            true
        );

        yield return FadeTo(1f,run);
        if(run!=fadeGeneration)yield break;

        actionWhileBlack?.Invoke();
        RoomPresentationFraming.PrepareCurrentView();

        // Keep the destination hidden until transforms and the camera
        // have completed one frame at their new positions.
        yield return null;
        if(run!=fadeGeneration)yield break;
        RoomPresentationFraming.PrepareCurrentView();
        yield return null;

        yield return FadeTo(0f,run);
        if(run!=fadeGeneration)yield break;

        SetCanvasState(0f, false);
        IsFading = false;
        fadeOwner=null;
    }

    // Bridge endpoints deliberately keep the existing fade canvas opaque.
    public IEnumerator FadeOutAndHold()
    {
        while (IsFading) yield return null;
        IsFading = true;
        fadeOwner=null;int run=++fadeGeneration;
        SetCanvasState(canvasGroup != null ? canvasGroup.alpha : 0f, true);
        yield return FadeTo(1f,run);
        if(run!=fadeGeneration)yield break;
        IsFading = false;
    }

    private IEnumerator FadeTo(float targetAlpha,int run)
    {
        if (canvasGroup == null)
        {
            yield break;
        }

        float startAlpha = canvasGroup.alpha;

        if (fadeDuration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            if(run!=fadeGeneration)yield break;
            elapsed += Time.unscaledDeltaTime;

            canvasGroup.alpha = Mathf.Lerp(
                startAlpha,
                targetAlpha,
                Mathf.Clamp01(elapsed / fadeDuration)
            );

            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    private void SetCanvasState(
        float alpha,
        bool blocksRaycasts)
    {
        if (canvasGroup == null)
        {
            return;
        }

        canvasGroup.alpha = alpha;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = blocksRaycasts;
    }
}
