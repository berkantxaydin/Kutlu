using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

public class GameBootstrap : MonoBehaviour
{
    [Header("Camera Prefab")]
    public Camera cameraPrefab;

    private Camera uiCamera;
    private GameObject eventSystemObject;

    public UIManager UIManager;


    void Awake()
    {
        Initialize().Forget();
    }

    async UniTaskVoid Initialize()
    {
        await UniTask.Yield(); // wait one frame if needed

        // 1. Create UI Camera if prefab not assigned
        if (cameraPrefab != null)
        {
            uiCamera = Instantiate(cameraPrefab);
        }
        else
        {
            GameObject camObj = new GameObject("UICamera");
            uiCamera = camObj.AddComponent<Camera>();
            uiCamera.clearFlags = CameraClearFlags.Depth;
            uiCamera.orthographic = true;
            uiCamera.cullingMask = LayerMask.GetMask("UI"); // only render UI layer
        }

        eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }

    private async void Start()
    {
        // Repositories
        var capitalRepo = new CapitalRepository();
        var resourceRepo = new ResourceRepository();
        var cardRepo = new CardRepository();

        // Systems
        var turnService = new TurnService(capitalRepo, resourceRepo);
        var cardManager = new CardManager(cardRepo, capitalRepo, resourceRepo);

        // UI
        UIManager.Initialize(turnService, resourceRepo, cardManager, capitalRepo);

        // Game Manager
        var gameManager = new GameManager(turnService, cardManager, capitalRepo, resourceRepo, UIManager, cardRepo);

        // Start the game loop
        await gameManager.StartGameAsync();
    }
    public void DestroyInitializer()
    {
        if (uiCamera != null) Destroy(uiCamera.gameObject);
        if (eventSystemObject != null) Destroy(eventSystemObject);
    }
}
