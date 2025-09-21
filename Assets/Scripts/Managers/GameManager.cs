using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class GameManager
{
    private readonly ITurnService _turnService;
    private readonly ICardManager _cardManager;
    private readonly ICapitalRepository _capitalRepo;
    private readonly IResourceRepository _resourceRepo;
    private readonly UIManager _uiManager;

    private readonly int _turnsPerCard = 3; // show card every 3 turns

    private readonly DeckType[] _deckOrder =
    {
        DeckType.Ilerleme,
        DeckType.KaynakDestek,
        DeckType.Zarar,
        DeckType.BigEvent
    };

    private int _deckIndex = 0;


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
        _turnService.OnTurnEnded += turn => UniTask.Void(async () => await HandleTurnEndedAsync(turn));
    }

    private async UniTask HandleTurnEndedAsync(int turn)
    {
        Debug.Log($"Turn {turn} ended.");

        // Show card every _turnsPerCard turns
        if (turn % _turnsPerCard == 0)
        {
            _turnService.Pause();

            // pick deck based on cycle
            var deck = _deckOrder[_deckIndex];
            _deckIndex = (_deckIndex + 1) % _deckOrder.Length;

            var drawResult = await _cardManager.DrawCardAsync(deck);

            if (drawResult?.Choices?.Count > 0)
            {
                var chosen = await _uiManager.WaitForChoiceAsync();
                await _cardManager.ApplyChoiceAsync(drawResult.Card, chosen);
                _uiManager.ClearCardUI();
            }

            await _turnService.Resume();
        }

        // Debug output for resources
        foreach (var resource in _resourceRepo.GetAll())
            Debug.Log($"{resource.Type}: {resource.Amount}");

        // Debug output for capitals
        foreach (var capital in _capitalRepo.GetAll())
            Debug.Log($"{capital.Name} Health: {capital.Health}");
    }

    public async UniTask StartGameAsync()
    {
        await _turnService.StartTurnsAsync(turnDelayMs: 1000);
    }

    public void StopGame()
    {
        _turnService.Stop();
    }
}
