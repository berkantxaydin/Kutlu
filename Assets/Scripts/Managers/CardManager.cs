using System;
using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

public interface ICardManager
{
    UniTask<CardDrawResult> DrawCardAsync();
    UniTask ApplyChoiceAsync(CardData card, CardChoice choice);

    event Action<CardDrawResult> OnCardDrawn;
    event Action<CardData, CardChoice> OnChoiceApplied;
}

public class CardManager : ICardManager
{
    private readonly ICardRepository _cardRepo;
    private readonly ICapitalRepository _capitalRepo;
    private readonly IResourceRepository _resourceRepo;
    private readonly Random _random = new Random();

    public event Action<CardDrawResult> OnCardDrawn;
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

    public async UniTask<CardDrawResult> DrawCardAsync()
    {
        // Offload heavy work to background thread
        var cards = await Task.Run(() => _cardRepo.GetAll().ToList());
        if (cards.Count == 0)
            return null;

        var card = cards[_random.Next(cards.Count)];

        var availableChoices = await Task.Run(() => card.Choices
            .Where(c => c.IsAvailable(_capitalRepo, _resourceRepo))
            .ToList());

        var result = new CardDrawResult(card, availableChoices);

        OnCardDrawn?.Invoke(result);
        return result;
    }


    public async UniTask ApplyChoiceAsync(CardData card, CardChoice choice)
    {
        if (!choice.IsAvailable(_capitalRepo, _resourceRepo))
        {
            throw new InvalidOperationException("Choice is locked and cannot be applied.");
        }

        foreach (var effect in choice.Effects)
        {
            effect.Apply(_capitalRepo, _resourceRepo);
        }

        OnChoiceApplied?.Invoke(card, choice);

        await UniTask.Yield(); // keeps async flow friendly
    }
}

public class CardDrawResult
{
    public CardData Card { get; }
    public List<CardChoice> Choices { get; }

    public CardDrawResult(CardData card, List<CardChoice> choices)
    {
        Card = card;
        Choices = choices ?? new List<CardChoice>();
    }
}
