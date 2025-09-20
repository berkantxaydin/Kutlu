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

[Serializable]
public class CardJson
{
    public string id;
    public string title;
    public string description;
    public List<CardChoiceJson> choices;
}

[Serializable]
public class CardChoiceJson
{
    public string label;
    public List<CardEffectJson> effects;
    public List<CardConditionJson> conditions;
}

[Serializable]
public class CardEffectJson
{
    public string resourceType;   // e.g., "Money"
    public string capitalType;    // e.g., "Government"
    public int amount;
}

[Serializable]
public class CardConditionJson
{
    public string type;           // "Resource" or "Capital"
    public string resourceType;
    public string capitalType;
    public int minAmount;
    public float minHealth;
}

public class CardRepository : ICardRepository
{
    private readonly Dictionary<string, CardData> _cards = new();

    public CardRepository()
    {
        LoadCardsFromJson();
    }

    private void LoadCardsFromJson()
    {
        var jsonAssets = Resources.LoadAll<TextAsset>("Cards"); // Cards folder inside Resources
        foreach (var asset in jsonAssets)
        {
            try
            {
                var cardJson = JsonUtility.FromJson<CardJson>(asset.text);
                var cardData = ConvertToCardData(cardJson);
                _cards[cardData.Id] = cardData;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load card from {asset.name}: {e}");
            }
        }
    }

    private CardData ConvertToCardData(CardJson json)
    {
        var choices = json.choices?.Select(c =>
        {
            var effects = c.effects?.Select(e =>
            {
                ResourceType? resType = string.IsNullOrEmpty(e.resourceType) ? (ResourceType?)null :
                    Enum.Parse<ResourceType>(e.resourceType);
                CapitalType? capType = string.IsNullOrEmpty(e.capitalType) ? (CapitalType?)null :
                    Enum.Parse<CapitalType>(e.capitalType);
                return new CardEffect(resType, capType, e.amount);
            }).ToList() ?? new List<CardEffect>();

            var conditions = c.conditions?.Select(cond =>
            {
                if (cond.type == "Resource")
                {
                    var resType = Enum.Parse<ResourceType>(cond.resourceType);
                    return (CardCondition)new ResourceCondition(resType, cond.minAmount);
                }
                else if (cond.type == "Capital")
                {
                    var capType = Enum.Parse<CapitalType>(cond.capitalType);
                    return (CardCondition)new CapitalCondition(capType, cond.minHealth);
                }
                return null;
            }).Where(x => x != null).ToList() ?? new List<CardCondition>();

            return new CardChoice(c.label, effects, conditions);
        }).ToList() ?? new List<CardChoice>();

        return new CardData(json.id, json.title, json.description, choices);
    }

    public IEnumerable<CardData> GetAll() => _cards.Values;

    public CardData GetById(string id) =>
        _cards.TryGetValue(id, out var card) ? card : null;
}
