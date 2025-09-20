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
            if (resource != null)
            {
                if (Amount >= 0)
                    resource.Add(Amount);
                else
                    resource.Spend(-Amount); // pass positive number to Spend
            }
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
    private readonly Dictionary<string, CardData> _cardsIlerleme = new();
    private readonly Dictionary<string, CardData> _cardsKaynakDestek = new();
    public CardRepository()
    {
        LoadCardsHardcoded();
    }

    private void LoadCardsHardcoded()
    {
        // Example Card 1: Government support
        var card1 = new CardData(
            id: "card_government_help",
            title: "Turnuva düzenle",
            description: "Halkın eğlenmesi için bir turnuva düzenle: +10 Altın, -10 Yemek",
            choices: new List<CardChoice>
            {
                new CardChoice(
                    label: "Kabul",
                    effects: new List<CardEffect>
                    {
                        new CardEffect(ResourceType.Money, null, 10), // +10 altın
                        new CardEffect(ResourceType.Food, null, -10), // -10 yemek
                    }
                ),
                new CardChoice(
                    label: "Red",
                    effects: new List<CardEffect>
                    {
                         // 0 gelir 0 gider
                    }
                )
            }
        );

        // Example Card 2: Military draft with conditions
        var card2 = new CardData(
            id: "card_military_draft",
            title: "Asker sayısını arttır",
            description: "Ordu komutanları daha fazla askere ihtiyaç duyuyor: +10 Asker -10 Altın -5 yemek",
            choices: new List<CardChoice>
            {
                new CardChoice(
                    label: "Kabul",
                    effects: new List<CardEffect>
                    {
                        new CardEffect(ResourceType.Power, null, +10), // Strengthen military
                        new CardEffect(ResourceType.Money, null, -10),
                        new CardEffect(ResourceType.Food, null, -5),
                    }
                ),
                new CardChoice(
                    label: "Red",
                    effects: new List<CardEffect>
                    {
                       // Lose government approval
                    }
                )
            }
        );

        // Example Card 3: Resource condition
        var card3 = new CardData(
            id: "card_food_supply",
            title: "Çiftçilere destek ver",
            description: "Çiftlik verimliliğini arttırmak için hasat zamanı çiftçilere destek ver. : -10 para +10 yemek",
            choices: new List<CardChoice>
            {
                new CardChoice(
                    label: "Kabul",
                    effects: new List<CardEffect>
                    {
                        new CardEffect(ResourceType.Food, null, +10),
                        new CardEffect(ResourceType.Money, null, -10),
                    }
                ),
                new CardChoice(
                    label: "Red",
                    effects: new List<CardEffect>
                    {
                        
                    }
                )
            }
        );
        
        var card4 = new CardData(
            id: "card_food_supply",
            title: "Ahır Yap",
            description: "Çiftlik medeniyetinin gücünü arttırmak için ahır yap. : +10f Çiftlik gücü, -20 altın",
            choices: new List<CardChoice>
            {
                new CardChoice(
                    label: "Kabul",
                    effects: new List<CardEffect>
                    {
                        new CardEffect(null, CapitalType.Population, +10),
                        new CardEffect(ResourceType.Money, null, -20),
                    }
                ),
                new CardChoice(
                    label: "Red",
                    effects: new List<CardEffect>
                    {
                        
                    }
                )
            }
        );
        
        var card5 = new CardData(
            id: "card_food_supply",
            title: "Vergi Dairesi inşa et",
            description: "Tüccar medeniyetini güçlendirmek için vergi dairesi inşa edebilirsin. : +10f Ticaret gücü, -20 yemek",
            choices: new List<CardChoice>
            {
                new CardChoice(
                    label: "Kabul",
                    effects: new List<CardEffect>
                    {
                        new CardEffect(null, CapitalType.Government, +10),
                        new CardEffect(ResourceType.Food, null, -20),
                    }
                ),
                new CardChoice(
                    label: "Red",
                    effects: new List<CardEffect>
                    {
                        
                    }
                )
            }
        );
        
        var card6 = new CardData(
            id: "card_food_supply",
            title: "Kışla inşa et",
            description: "Asker medeniyetinin gücünü arttırmak için kışla inşa edebilirsin. : 10f Ordu gücü, -20 altın, -10 yemek",
            choices: new List<CardChoice>
            {
                new CardChoice(
                    label: "Kabul",
                    effects: new List<CardEffect>
                    {
                        new CardEffect(ResourceType.Food, null, -10),
                        new CardEffect(ResourceType.Money, null, -20),
                        new CardEffect(null, CapitalType.Military, +10),
                    }
                ),
                new CardChoice(
                    label: "Red",
                    effects: new List<CardEffect>
                    {
                        
                    }
                )
            }
        );
        
        var card7 = new CardData(
            id: "card_food_supply",
            title: "Çiftlik yangını",
            description: "Asker medeniyetinin gücünü arttırmak için kışla inşa edebilirsin. : 10f Ordu gücü, -20 altın, -10 yemek",
            choices: new List<CardChoice>
            {
                new CardChoice(
                    label: "Kabul",
                    effects: new List<CardEffect>
                    {
                        new CardEffect(ResourceType.Food, null, -10),
                        new CardEffect(ResourceType.Money, null, -20),
                        new CardEffect(null, CapitalType.Military, +10),
                    }
                ),
                new CardChoice(
                    label: "Red",
                    effects: new List<CardEffect>
                    {
                        
                    }
                )
            }
        );
        
        var card8 = new CardData(
            id: "card_food_supply",
            title: "Kışla inşa et",
            description: "Asker medeniyetinin gücünü arttırmak için kışla inşa edebilirsin. : 10f Ordu gücü, -20 altın, -10 yemek",
            choices: new List<CardChoice>
            {
                new CardChoice(
                    label: "Kabul",
                    effects: new List<CardEffect>
                    {
                        new CardEffect(ResourceType.Food, null, -10),
                        new CardEffect(ResourceType.Money, null, -20),
                        new CardEffect(null, CapitalType.Military, +10),
                    }
                ),
                new CardChoice(
                    label: "Red",
                    effects: new List<CardEffect>
                    {
                        
                    }
                )
            }
        );
        
        var card9 = new CardData(
            id: "card_food_supply",
            title: "Kışla inşa et",
            description: "Asker medeniyetinin gücünü arttırmak için kışla inşa edebilirsin. : 10f Ordu gücü, -20 altın, -10 yemek",
            choices: new List<CardChoice>
            {
                new CardChoice(
                    label: "Kabul",
                    effects: new List<CardEffect>
                    {
                        new CardEffect(ResourceType.Food, null, -10),
                        new CardEffect(ResourceType.Money, null, -20),
                        new CardEffect(null, CapitalType.Military, +10),
                    }
                ),
                new CardChoice(
                    label: "Red",
                    effects: new List<CardEffect>
                    {
                        
                    }
                )
            }
        );

       
        _cardsIlerleme[card1.Id] = card1; // İlerleme
        _cardsIlerleme[card2.Id] = card2;
        _cardsIlerleme[card3.Id] = card3;
        _cardsIlerleme[card4.Id] = card4; // KaynakDestek
        _cardsIlerleme[card5.Id] = card5;
        _cardsIlerleme[card6.Id] = card6;
        _cardsIlerleme[card7.Id] = card7; // Zarar Kartları
        _cardsIlerleme[card8.Id] = card8;
        _cardsIlerleme[card9.Id] = card9;
        
        
        Debug.Log($"Loaded {_cardsIlerleme.Count} hardcoded cards.");
    }

    public IEnumerable<CardData> GetAll() => _cardsIlerleme.Values;
   
    public CardData GetById(string id) =>
        _cardsIlerleme.TryGetValue(id, out var card) ? card : null;
   
    
}