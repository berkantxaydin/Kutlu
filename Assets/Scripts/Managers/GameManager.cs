using System;
using Cysharp.Threading.Tasks;

public class GameManager
{
    private readonly ITurnService _turnService;
    private readonly ICardManager _cardManager;
    private readonly ICapitalRepository _capitalRepo;
    private readonly IResourceRepository _resourceRepo;
    private readonly UIManager _uiManager;

    private readonly int _turnsPerCard = 3; // show card every 3 turns

    public GameManager(
        ITurnService turnService,
        ICardManager cardManager,
        ICapitalRepository capitalRepo,
        IResourceRepository resourceRepo,
        UIManager uiManager)
    {
        _turnService = turnService;
        _cardManager = cardManager;
        _capitalRepo = capitalRepo;
        _resourceRepo = resourceRepo;
        _uiManager = uiManager;

        // Subscribe to turn end event
        _turnService.OnTurnEnded += HandleTurnEnded;
    }

    public async UniTask StartGameAsync()
    {
        await _turnService.StartTurnsAsync(turnDelayMs: 1000);
    }

    private async void HandleTurnEnded(int turn)
    {
        Console.WriteLine($"Turn {turn} ended.");

        // Show a card every _turnsPerCard turns
        if (turn % _turnsPerCard == 0)
        {
            // Pause turn loop
            _turnService.Pause();

            var card = await _cardManager.DrawCardAsync();
            if (card != null)
            {
                // Show card in UI and wait for player choice
                _uiManager.DisplayCard(card, card.Choices);
                var chosen = await _uiManager.WaitForChoiceAsync();

                // Apply choice effects
                await _cardManager.ApplyChoiceAsync(card, chosen);

                // Clear card UI
                _uiManager.ClearCardUI();
            }

            // Resume turn loop
            _turnService.Resume();
        }

        // Print resource states for debugging
        foreach (var resource in _resourceRepo.GetAll())
        {
            Console.WriteLine($"{resource.Type}: {resource.Amount}");
        }

        // Print capitals' health
        foreach (var capital in _capitalRepo.GetAll())
        {
            Console.WriteLine($"{capital.Name} Health: {capital.Health}");
        }
    }

    public void StopGame()
    {
        _turnService.Stop();
    }
}
