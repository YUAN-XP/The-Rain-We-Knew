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

    private void Awake()
    {
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        SetCanvasState(0f, false);
    }

    public IEnumerator FadeThroughBlack(
        Action actionWhileBlack)
    {
        if (IsFading)
        {
            yield break;
        }

        IsFading = true;
        SetCanvasState(
            canvasGroup != null
                ? canvasGroup.alpha
                : 0f,
            true
        );

        yield return FadeTo(1f);

        actionWhileBlack?.Invoke();

        // Keep the destination hidden until transforms and the camera
        // have completed one frame at their new positions.
        yield return null;

        yield return FadeTo(0f);

        SetCanvasState(0f, false);
        IsFading = false;
    }

    private IEnumerator FadeTo(float targetAlpha)
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
