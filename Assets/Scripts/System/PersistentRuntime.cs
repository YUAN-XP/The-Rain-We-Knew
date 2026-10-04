using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistentRuntime : MonoBehaviour
{
    private static PersistentRuntime instance;
    public static PersistentRuntime Instance => instance;

    [Header("Persistent Objects")]
    [SerializeField] private Transform player;

    private void Awake()
    {
        if (instance != null &&
            instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        DontDestroyOnLoad(gameObject);
        if(!GetComponent<GameplayClock>())gameObject.AddComponent<GameplayClock>();
        if(!GetComponent<QuestProgress>())gameObject.AddComponent<QuestProgress>();
        if(!GetComponent<AcquisitionHistory>())gameObject.AddComponent<AcquisitionHistory>();
        if(!GetComponent<GameSaveService>())gameObject.AddComponent<GameSaveService>();
        if(!GetComponent<GamePause>())gameObject.AddComponent<GamePause>();

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(
        Scene scene,
        LoadSceneMode mode)
    {
        if(P05WorldLink.IsPreloading(scene))return;
        ScenePlayerBinding binding = null;
        foreach (var root in scene.GetRootGameObjects())
        {
            binding = root.GetComponentInChildren<ScenePlayerBinding>(true);
            if (binding != null) break;
        }
        var activePlayer = binding != null ? binding.player : player != null ? player.GetComponent<PlayerController>() : null;
        if (player != null) player.gameObject.SetActive(binding == null);
        if (activePlayer != null)
        {
            activePlayer.gameObject.SetActive(true);
            activePlayer.GetComponent<PlayerInteraction>()?.BindRuntime(
                GetComponentInChildren<InteractionUI>(true), GetComponentInChildren<DialogueManager>(true));
            GetComponent<GameControlManager>()?.BindPlayer(activePlayer, binding == null || binding.allowAdultOutfits);
        }
        if (binding != null) return;
        if (GetComponent<EndingSession>()?.TryApplyArrival(scene, activePlayer) == true) return;
        PlayerSpawnPoint spawnPoint =
            FindAnyObjectByType<PlayerSpawnPoint>();

        if (spawnPoint == null)
        {
            Debug.Log(
                "PersistentRuntime: No PlayerSpawnPoint found in "
                + scene.name
            );

            return;
        }

        if (player == null)
        {
            Debug.LogWarning(
                "PersistentRuntime: Player reference is missing."
            );

            return;
        }

        player.position =
            spawnPoint.transform.position;
        GroundedPlacement.Settle(activePlayer);

        Debug.Log(
            "PersistentRuntime: Player moved to spawn point in "
            + scene.name
        );
    }
}