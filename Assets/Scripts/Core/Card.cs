using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// --- Models ---

public enum CapitalType
{
    Government,
    Population,
    Military
}

public class CardData
{
    public string Id { get; }
    public string Title { get; }
    public string Description { get; }
    public List<CardChoice> Choices { get; }

    public CardData(string id, string title, string description, List<CardChoice> choices)
    {
        Id = id;
        Title = title;
        Description = description;
        Choices = choices ?? new List<CardChoice>();
    }
}

public class CardChoice
{
    public string Label { get; }
    public List<CardEffect> Effects { get; }
    public List<CardCondition> Conditions { get; }

    public CardChoice(string label, List<CardEffect> effects, List<CardCondition> conditions = null)
    {
        Label = label;
        Effects = effects ?? new List<CardEffect>();
        Conditions = conditions ?? new List<CardCondition>();
    }

    public bool IsAvailable(ICapitalRepository capitalRepo, IResourceRepository resourceRepo)
    {
        foreach (var condition in Conditions)
        {
            if (!condition.IsMet(capitalRepo, resourceRepo))
                return false;
        }
        return true;
    }
}

public class CardEffect
{
    public ResourceType? ResourceType { get; }
    public CapitalType? CapitalType { get; }
    public int Amount { get; }

    public CardEffect(ResourceType? resourceType, CapitalType? capitalType, int amount)
    {
        ResourceType = resourceType;
        CapitalType = capitalType;
        Amount = amount;
    }

    public void Apply(ICapitalRepository capitalRepo, IResourceRepository resourceRepo)
    {
        if (ResourceType.HasValue)
        {
            var resource = resourceRepo.GetByType(ResourceType.Value);
            resource?.Add(Amount);
        }

        if (CapitalType.HasValue)
        {
            var capital = capitalRepo.GetByName(CapitalType.Value.ToString());
            capital?.ModifyHealth(Amount);
        }
    }
}

// --- Conditions ---

public abstract class CardCondition
{
    public abstract bool IsMet(ICapitalRepository capitalRepo, IResourceRepository resourceRepo);
}

// Requires minimum resource
public class ResourceCondition : CardCondition
{
    private readonly ResourceType _type;
    private readonly int _minAmount;

    public ResourceCondition(ResourceType type, int minAmount)
    {
        _type = type;
        _minAmount = minAmount;
    }

    public override bool IsMet(ICapitalRepository capitalRepo, IResourceRepository resourceRepo)
    {
        var resource = resourceRepo.GetByType(_type);
        return resource != null && resource.Amount >= _minAmount;
    }
}

// Requires minimum capital health
public class CapitalCondition : CardCondition
{
    private readonly CapitalType _capitalType;
    private readonly float _minHealth;

    private static bool _hasWarned = false;

    public CapitalCondition(CapitalType capitalType, float minHealth)
    {
        _capitalType = capitalType;
        _minHealth = minHealth;
    }

    public override bool IsMet(ICapitalRepository capitalRepo, IResourceRepository resourceRepo)
    {
        if (capitalRepo == null)
        {
            if (!_hasWarned)
            {
                Debug.LogWarning("CapitalRepository is null in CapitalCondition!");
                _hasWarned = true;  
            }
            return false;
        }

        var capital = capitalRepo.GetByName(_capitalType.ToString());
        if (capital == null)
        {
            if (!_hasWarned)
            {
                Debug.LogWarning($"Capital '{_capitalType}' not found in repository!");
                _hasWarned = true;
            }
            return false;
        }

        return capital.Health >= _minHealth;
    }

}

// --- Repository ---

public interface ICardRepository
{
    IEnumerable<CardData> GetAll();
    CardData GetById(string id);
}

public class CardRepository : ICardRepository
{
    private readonly Dictionary<string, CardData> _cards = new();

    public CardRepository()
    {
        LoadCardsHardcoded();
    }

    private void LoadCardsHardcoded()
    {
        // Example Card 1: Government support
        var card1 = new CardData(
            id: "card_government_help",
            title: "Government Support",
            description: "The government offers financial assistance.",
            choices: new List<CardChoice>
            {
                new CardChoice(
                    label: "Accept funds",
                    effects: new List<CardEffect>
                    {
                        new CardEffect(ResourceType.Money, null, 50), // +50 Money
                    }
                ),
                new CardChoice(
                    label: "Reject support",
                    effects: new List<CardEffect>
                    {
                        new CardEffect(null, CapitalType.Government, -10), // -5 Government health
                    }
                )
            }
        );

        // Example Card 2: Military draft with conditions
        var card2 = new CardData(
            id: "card_military_draft",
            title: "Military Draft",
            description: "The military wants to draft new recruits.",
            choices: new List<CardChoice>
            {
                new CardChoice(
                    label: "Approve Draft",
                    effects: new List<CardEffect>
                    {
                        new CardEffect(null, CapitalType.Military, +10) // Strengthen military
                    },
                    conditions: new List<CardCondition>
                    {
                        new CapitalCondition(CapitalType.Population, 20f) // Requires population health >= 20
                    }
                ),
                new CardChoice(
                    label: "Reject Draft",
                    effects: new List<CardEffect>
                    {
                        new CardEffect(null, CapitalType.Government, -10) // Lose government approval
                    }
                )
            }
        );

        // Example Card 3: Resource condition
        var card3 = new CardData(
            id: "card_food_supply",
            title: "Food Supply",
            description: "Distribute food to the people.",
            choices: new List<CardChoice>
            {
                new CardChoice(
                    label: "Distribute food",
                    effects: new List<CardEffect>
                    {
                        new CardEffect(null, CapitalType.Population, +5)
                    },
                    conditions: new List<CardCondition>
                    {
                        new ResourceCondition(ResourceType.Food, 10) // Needs at least 10 food
                    }
                ),
                new CardChoice(
                    label: "Do nothing",
                    effects: new List<CardEffect>
                    {
                        new CardEffect(null, CapitalType.Population, -5)
                    }
                )
            }
        );

        // Add all cards to dictionary
        _cards[card1.Id] = card1;
        _cards[card2.Id] = card2;
        _cards[card3.Id] = card3;

        Debug.Log($"Loaded {_cards.Count} hardcoded cards.");
    }

    public IEnumerable<CardData> GetAll() => _cards.Values;

    public CardData GetById(string id) =>
        _cards.TryGetValue(id, out var card) ? card : null;
}