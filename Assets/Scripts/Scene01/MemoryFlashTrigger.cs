using System.Collections;
using TMPro;
using UnityEngine;

public class MemoryFlashTrigger : MonoBehaviour
{
    [Header("Game Control")]
    [SerializeField]
    private GameControlManager gameControlManager;

    [Header("Memory UI")]
    [SerializeField]
    private CanvasGroup memoryCanvasGroup;

    [SerializeField]
    private TMP_Text memoryText;

    [Header("Timing")]
    [SerializeField]
    private float fadeDuration = 0.5f;

    [SerializeField]
    private float firstTextDuration = 1.2f;

    [SerializeField]
    private float secondTextDuration = 1.0f;

    [SerializeField]
    private float gunshotTextDuration = 0.8f;

    [SerializeField]
    private float endHoldDuration = 0.4f;

    [Header("Optional Audio")]
    [SerializeField]
    private AudioSource audioSource;

    [SerializeField]
    private AudioClip gunshotClip;

    private string memoryCanvasName;
    private bool hasTriggered = false;
    private bool sequenceRunning = false;
    private bool ownsControlLock;

    private void Awake()
    {
        memoryCanvasName=memoryCanvasGroup!=null?memoryCanvasGroup.name:null;
        if (memoryCanvasGroup != null)
        {
            memoryCanvasGroup.alpha = 0f;
            memoryCanvasGroup.interactable = false;
            memoryCanvasGroup.blocksRaycasts = false;
        }

        if (memoryText != null)
        {
            memoryText.text = "";
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (PersistentRuntime.Instance?.GetComponent<EndingSession>()?.IsEndingVisit == true) return;
        var story = PersistentRuntime.Instance != null
            ? PersistentRuntime.Instance.GetComponent<StoryStateManager>() : null;
        if (hasTriggered ||
            sequenceRunning || (story != null && story.Has(StoryFlag.Scene01MemoryFlashPlayed)))
        {
            return;
        }

        if (!other.CompareTag("Player"))
        {
            return;
        }

        StartCoroutine(
            PlayMemorySequence()
        );
    }

    private IEnumerator PlayMemorySequence()
    {
        if(gameControlManager==null)gameControlManager=FindAnyObjectByType<GameControlManager>();
        if(memoryCanvasGroup==null && gameControlManager!=null && memoryCanvasName!=null)
            foreach(var group in gameControlManager.GetComponentsInChildren<CanvasGroup>(true))
                if(group.name==memoryCanvasName){memoryCanvasGroup=group;memoryText=group.GetComponentInChildren<TMP_Text>(true);break;}

        sequenceRunning = true;
        hasTriggered = true;
        // Claim once at entry, including an interrupted scene unload. The scene's
        // local boolean is recreated on reload; the existing runtime survives it.
        PersistentRuntime.Instance?.GetComponent<StoryStateManager>()?.Set(StoryFlag.Scene01MemoryFlashPlayed);

        if (gameControlManager != null)
        {
            gameControlManager.AcquireLock(
                ControlLockReason.Interaction
            );
            ownsControlLock = true;
        }

        if (DemoArtCatalog.Current != null)
        {
            yield return DemoComicSequence.For(gameObject).Memory(() => {
                if (audioSource != null && gunshotClip != null) audioSource.PlayOneShot(gunshotClip);
            });
            if (ownsControlLock && gameControlManager != null) gameControlManager.ReleaseLock(ControlLockReason.Interaction);
            ownsControlLock=false;sequenceRunning=false;
            yield break;
        }
        if (memoryCanvasGroup != null)
        {
            memoryCanvasGroup.blocksRaycasts = true;
        }

        yield return FadeCanvas(
            0f,
            1f
        );

        if (memoryText != null)
        {
            memoryText.text =
                "Heavy breathing...";
        }

        yield return new WaitForSeconds(
            firstTextDuration
        );

        if (memoryText != null)
        {
            memoryText.text =
                "Someone:\nXiao Peng!";
        }

        yield return new WaitForSeconds(
            secondTextDuration
        );

        if (memoryText != null)
        {
            memoryText.text =
                "BANG.";
        }

        if (audioSource != null &&
            gunshotClip != null)
        {
            audioSource.PlayOneShot(
                gunshotClip
            );
        }

        yield return new WaitForSeconds(
            gunshotTextDuration
        );

        if (memoryText != null)
        {
            memoryText.text = "";
        }

        yield return new WaitForSeconds(
            endHoldDuration
        );

        yield return FadeCanvas(
            1f,
            0f
        );

        if (memoryCanvasGroup != null)
        {
            memoryCanvasGroup.blocksRaycasts = false;
        }

        if (gameControlManager != null)
        {
            gameControlManager.ReleaseLock(
                ControlLockReason.Interaction
            );
        }
        ownsControlLock = false;

        sequenceRunning = false;

        Debug.Log(
            "Scene01: Memory flash 01 completed."
        );
    }

    private void OnDisable()
    {
        if (!sequenceRunning) return;
        StopAllCoroutines();
        if (memoryCanvasGroup != null)
        {
            memoryCanvasGroup.alpha = 0f;
            memoryCanvasGroup.blocksRaycasts = false;
        }
        if (memoryText != null) memoryText.text = "";
        if (ownsControlLock && gameControlManager != null)
            gameControlManager.ReleaseLock(ControlLockReason.Interaction);
        ownsControlLock = false;
        sequenceRunning = false;
    }

    private IEnumerator FadeCanvas(
        float startAlpha,
        float targetAlpha)
    {
        if (memoryCanvasGroup == null)
        {
            yield break;
        }

        float elapsed = 0f;

        memoryCanvasGroup.alpha =
            startAlpha;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;

            float t =
                fadeDuration <= 0f
                    ? 1f
                    : elapsed / fadeDuration;

            memoryCanvasGroup.alpha =
                Mathf.Lerp(
                    startAlpha,
                    targetAlpha,
                    t
                );

            yield return null;
        }

        memoryCanvasGroup.alpha =
            targetAlpha;
    }
}
