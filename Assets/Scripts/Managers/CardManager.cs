using System;
using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading.Tasks;

public interface ICardManager
{
    UniTask<CardDrawResult> DrawCardAsync();               // auto deck selection
    UniTask<CardDrawResult> DrawCardAsync(DeckType deck);  // specific deck
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

    private DeckType _lastDeck = DeckType.BigEvent; // track last used deck

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

    /// <summary>
    /// Draws a card automatically by cycling through deck types.
    /// </summary>
    public async UniTask<CardDrawResult> DrawCardAsync()
    {
        _lastDeck = GetNextDeck(_lastDeck);
        return await DrawCardAsync(_lastDeck);
    }

    /// <summary>
    /// Draws a card from a specific deck type.
    /// </summary>
    public async UniTask<CardDrawResult> DrawCardAsync(DeckType deck)
    {
        // Get cards from the requested deck
        var cards = await Task.Run(() => _cardRepo.GetAll(deck).ToList());
        if (cards.Count == 0)
            return null;

        // Pick a random card
        var card = cards[_random.Next(cards.Count)];

        // Filter available choices
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
            throw new InvalidOperationException("Choice is locked and cannot be applied.");

        foreach (var effect in choice.Effects)
            effect.Apply(_capitalRepo, _resourceRepo);

        OnChoiceApplied?.Invoke(card, choice);

        await UniTask.Yield(); // keeps async-friendly flow
    }

    /// <summary>
    /// Cycles deck types in enum order.
    /// </summary>
    private DeckType GetNextDeck(DeckType current)
    {
        var values = Enum.GetValues(typeof(DeckType)).Cast<DeckType>().ToList();
        var idx = values.IndexOf(current);
        return values[(idx + 1) % values.Count];
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
