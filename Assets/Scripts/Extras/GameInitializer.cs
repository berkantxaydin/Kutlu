using UnityEngine;
using Cysharp.Threading.Tasks;

public class GameInitializer : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject playerPrefab;
    public GameObject cameraPrefab;
    public GameObject uiPrefab;

    [Header("Spawn")]
    public Transform playerSpawn;

    GameObject currentPlayer;

    void Awake()
    {
        // Optional: validate required fields
        if (playerSpawn == null) Debug.LogWarning("PlayerSpawn not set on GameInitializer.");
    }

    void Start()
    {
        Initialize().Forget();
    }

    async UniTaskVoid Initialize()
    {
        // allow one frame if you need to wait for other systems (safe for WebGL)
        await UniTask.Yield();

        // instantiate player
        if (playerPrefab != null)
        {
            Vector3 spawnPos = (playerSpawn != null) ? playerSpawn.position : Vector3.zero;
            currentPlayer = Instantiate(playerPrefab, spawnPos, Quaternion.identity);
        }

        // instantiate camera and assign target
        if (cameraPrefab != null)
        {
            GameObject camObj = Instantiate(cameraPrefab);
            var follow = camObj.GetComponent<CameraFollow>();
            if (follow != null && currentPlayer != null)
                follow.target = currentPlayer.transform;
        }
        else
        {
            // if you use the scene Camera, you can find and assign it:
            var sceneCam = Camera.main;
            var follow = sceneCam?.GetComponent<CameraFollow>();
            if (follow != null && currentPlayer != null)
                follow.target = currentPlayer.transform;
        }

        // instantiate UI
        if (uiPrefab != null) Instantiate(uiPrefab);

        // Do any other initialization (audio, managers, etc.) here
    }
}
