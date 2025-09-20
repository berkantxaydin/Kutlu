using System;
using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

public interface ICardManager
{
    UniTask<CardData> DrawCardAsync();
    event Action<CardData, List<CardChoice>> OnCardDrawn;
    UniTask ApplyChoiceAsync(CardData card, CardChoice choice);
}

public class CardManager : ICardManager
{
    private readonly ICardRepository _cardRepo;
    private readonly ICapitalRepository _capitalRepo;
    private readonly IResourceRepository _resourceRepo;
    private readonly Random _random = new Random();

    public event Action<CardData, List<CardChoice>> OnCardDrawn;
    public event Action<CardData, CardChoice> OnChoiceApplied;

    public CardManager(
        ICardRepository cardRepo,
        ICapitalRepository capitalRepo,
        IResourceRepository resourceRepo)
    {
        _cardRepo = cardRepo;
        _capitalRepo = capitalRepo;
        _resourceRepo = resourceRepo;
    }

    public async UniTask<CardData> DrawCardAsync()
    {
        // Offload the CPU-bound task to a background thread
        var cards = await Task.Run(() => _cardRepo.GetAll().ToList());

        if (cards.Count == 0)
        {
            return null; // Return null if there are no cards.
        }

        var card = cards[_random.Next(cards.Count)];

        // CPU-bound work can also be offloaded if needed
        var availableChoices = await Task.Run(() => card.Choices
            .Where(c => c.IsAvailable(_capitalRepo, _resourceRepo))
            .ToList());

        OnCardDrawn?.Invoke(card, availableChoices);

        return card;
    }

    public async UniTask ApplyChoiceAsync(CardData card, CardChoice choice)
    {
        if (!choice.IsAvailable(_capitalRepo, _resourceRepo))
        {
            throw new InvalidOperationException("Choice is locked and cannot be applied.");
        }

        // Apply effects
        foreach (var effect in choice.Effects)
        {
            effect.Apply(_capitalRepo, _resourceRepo);
        }

        OnChoiceApplied?.Invoke(card, choice);

        await UniTask.Yield(); // keep async-friendly
    }
}
