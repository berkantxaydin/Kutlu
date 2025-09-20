using UnityEngine;
using Cysharp.Threading.Tasks;

public class GameBootstrap : MonoBehaviour
{
    public UIManager UIManager;

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
        UIManager.Initialize(turnService, resourceRepo, cardManager);

        // Game Manager
        var gameManager = new GameManager(turnService, cardManager, capitalRepo, resourceRepo, UIManager);

        // Start the game loop
        await gameManager.StartGameAsync();
    }
}
