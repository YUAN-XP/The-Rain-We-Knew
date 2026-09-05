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
        PlayerSpawnPoint spawnPoint =
            FindFirstObjectByType<PlayerSpawnPoint>();

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

        Debug.Log(
            "PersistentRuntime: Player moved to spawn point in "
            + scene.name
        );
    }
}