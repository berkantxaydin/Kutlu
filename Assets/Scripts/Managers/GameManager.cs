using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using UnityEngine;

public class GameManager
{
    private readonly ITurnService _turnService;
    private readonly ICardManager _cardManager;
    private readonly ICapitalRepository _capitalRepo;
    private readonly IResourceRepository _resourceRepo;
    private readonly UIManager _uiManager;
    private readonly CardRepository _cardRepo;

    private readonly int _turnsPerCard = 3; // show card every 3 turns

    public GameManager(
        ITurnService turnService,
        ICardManager cardManager,
        ICapitalRepository capitalRepo,
        IResourceRepository resourceRepo,
        UIManager uiManager,
        CardRepository cardRepo) // inject repository
    {
        _turnService = turnService;
        _cardManager = cardManager;
        _capitalRepo = capitalRepo;
        _resourceRepo = resourceRepo;
        _uiManager = uiManager;
        _cardRepo = cardRepo;
    }

    public async UniTask StartGameAsync()
    {
        // Ensure cards are loaded before starting turns
        await _cardRepo.InitializeAsync();
        Debug.Log($"Loaded {_cardRepo.GetAll().Count()} cards.");

        // Subscribe to turn ended after initialization
        _turnService.OnTurnEnded += turn => UniTask.Void(async () => await HandleTurnEndedAsync(turn));

        // Start turn loop
        await _turnService.StartTurnsAsync(turnDelayMs: 1000);
    }

    private async UniTask HandleTurnEndedAsync(int turn)
    {
        Debug.Log($"Turn {turn} ended.");

        if (turn % _turnsPerCard == 0)
        {
            _turnService.Pause();

            var drawResult = await _cardManager.DrawCardAsync();

            if (drawResult?.Choices?.Count > 0)
            {
                _uiManager.DisplayCard(drawResult.Card, drawResult.Choices);

                // Wait for player's choice
                var chosen = await _uiManager.WaitForChoiceAsync();
                await _cardManager.ApplyChoiceAsync(drawResult.Card, chosen);
                _uiManager.ClearCardUI();
            }

            _turnService.Resume();
        }

        // Log resources and capitals for debugging
        foreach (var resource in _resourceRepo.GetAll())
            Debug.Log($"{resource.Type}: {resource.Amount}");

        foreach (var capital in _capitalRepo.GetAll())
            Debug.Log($"{capital.Name} Health: {capital.Health}");
    }

    public void StopGame()
    {
        _turnService.Stop();
    }
}
