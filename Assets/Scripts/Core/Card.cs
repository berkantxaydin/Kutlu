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

public enum DeckType
{
    Ilerleme,
    KaynakDestek,
    Zarar,
    BigEvent
}

public class CardData
{
    public string Id { get; }
    public string Title { get; }
    public string Description { get; }
    public List<CardChoice> Choices { get; }
    public Sprite Artwork { get; }
    
    public CardData(string id, string title, string description, List<CardChoice> choices, Sprite artwork = null)
    {
        Id = id;
        Title = title;
        Description = description;
        Choices = choices ?? new List<CardChoice>();
        Artwork = artwork;
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
    IEnumerable<CardData> GetAll(DeckType deck);
    CardData GetById(DeckType deck, string id);
    Dictionary<DeckType, IEnumerable<CardData>> GetAllDecks();
    
    
}

public class CardRepository : ICardRepository
{
    private readonly Dictionary<DeckType, Dictionary<string, CardData>> _decks = new();

    public CardRepository()
    {
        LoadCardsHardcoded();
    }

    private void LoadCardsHardcoded()
    {
        foreach (DeckType deckType in Enum.GetValues(typeof(DeckType)))
            _decks[deckType] = new Dictionary<string, CardData>();

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
                    },
                    conditions: new List<CardCondition>
                    {
                        new ResourceCondition(ResourceType.Food, 20)
                    }
                ),
                new CardChoice(
                    label: "Red",
                    effects: new List<CardEffect>
                    {
                         // 0 gelir 0 gider
                    }
                )
            },
            Resources.Load<Sprite>("CardsSprite/Arena")
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
                    },
                    conditions: new List<CardCondition>
                    {
                        new ResourceCondition(ResourceType.Money, 20),
                        new ResourceCondition(ResourceType.Food, 10)
                    }
                ),
                
                new CardChoice(
                    label: "Red",
                    effects: new List<CardEffect>
                    {
                       // Lose government approval
                    }
                )
            },
        Resources.Load<Sprite>("CardsSprite/Asker")
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
                    },
                    conditions: new List<CardCondition>
                    {
                    new ResourceCondition(ResourceType.Money, 20)
                    }
                ),
                new CardChoice(
                    label: "Red",
                    effects: new List<CardEffect>
                    {
                        
                    }
                )
            },
            Resources.Load<Sprite>("CardsSprite/ÇiftçiDestek")
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
                    },
                    conditions: new List<CardCondition>
                    {
                    new ResourceCondition(ResourceType.Money, 30)
                    }
                ),
                new CardChoice(
                    label: "Red",
                    effects: new List<CardEffect>
                    {
                        
                    }
                )
            },
            Resources.Load<Sprite>("CardsSprite/Çiftlikİnşa")
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
                    },
                    conditions: new List<CardCondition>
                    {
                        new ResourceCondition(ResourceType.Food, 30)
                    }
                ),
                new CardChoice(
                    label: "Red",
                    effects: new List<CardEffect>
                    {
                        
                    }
                )
            },
            Resources.Load<Sprite>("CardsSprite/VergiDairesi")
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
                    },
                    conditions: new List<CardCondition>
                    {
                        new ResourceCondition(ResourceType.Money, 30),
                        new ResourceCondition(ResourceType.Food, 20)
                    }
                ),
                new CardChoice(
                    label: "Red",
                    effects: new List<CardEffect>
                    {
                        
                    }
                )
            },
            Resources.Load<Sprite>("CardsSprite/DarbeAsker")
        );
        
        var card7 = new CardData(
            id: "card_food_supply",
            title: "Çiftlik yangını!",
            description: "Çiftlik medeniyetinde yangın çıktı,bu yangın sana epey yemek ve hasara sebep olucak. : -10f çiftlik gücü, -20 yemek",
            choices: new List<CardChoice>
            {
                new CardChoice(
                    label: "Tamam",
                    effects: new List<CardEffect>
                    {
                        new CardEffect(null, CapitalType.Population, -10),
                        new CardEffect(ResourceType.Food, null, -20),
                    },
                    conditions: new List<CardCondition>
                    {
                    new ResourceCondition(ResourceType.Food, 30),
                    new CapitalCondition(CapitalType.Population, 20)
                    }
                )  
            },
            Resources.Load<Sprite>("CardsSprite/KöyYangını")
        );
        
        var card8 = new CardData(
            id: "card_food_supply",
            title: "Hazine soygunu!",
            description: "Tüccar medeniyetinde hırsızlık oldu. Politikacılardan biri yaptığı yönünde söylenti var...: -10f tüccar gücü, -20 para",
            choices: new List<CardChoice>
            {
                new CardChoice(
                    label: "Tamam",
                    effects: new List<CardEffect>
                    {
                        new CardEffect(null, CapitalType.Government, -10),
                        new CardEffect(ResourceType.Money, null, -20),
                    },
                    conditions: new List<CardCondition>
                    {
                        new ResourceCondition(ResourceType.Money, 30),
                        new CapitalCondition(CapitalType.Government, 20)
                    }
                )
            },
            Resources.Load<Sprite>("CardsSprite/HazineSoygunu")
        );
        
        var card9 = new CardData(
            id: "card_food_supply",
            title: "Asker kampına baskın!",
            description: "Asker medeniyetinin bir kampına baskın : -10f Ordu gücü, -20 asker",
            choices: new List<CardChoice>
            {
                new CardChoice(
                    label: "Tamam",
                    effects: new List<CardEffect>
                    {
                        new CardEffect(ResourceType.Power, null, -20),
                        new CardEffect(null, CapitalType.Military, -10),
                    },
                    conditions: new List<CardCondition>
                    {
                        new ResourceCondition(ResourceType.Power, 30),
                        new CapitalCondition(CapitalType.Military, 20)
                    }
                )
            },
            Resources.Load<Sprite>("CardsSprite/Baskın")
        );
        
        var card10 = new CardData(
            id: "card_food_supply",
            title: "Kış geliyor...",
            description: "İklim değişiyor fakat hasatların buna hazır değil.: -20f çiftlik medeniyeti , -20 yemek",
            choices: new List<CardChoice>
            {
                new CardChoice(
                    label: "Tamam",
                    effects: new List<CardEffect>
                    {
                        new CardEffect(ResourceType.Food, null, -20),
                        new CardEffect(null, CapitalType.Population, -20),
                    }
                )
            },
            Resources.Load<Sprite>("CardsSprite/KışGeliyor")
        );
        var card11 = new CardData(
            id: "card_food_supply",
            title: "Ejderha saldırısı!",
            description: "Tüccar medeniyet şehrine bir ejderha ağzından alevler çıkararak saldırıyor!: -30f tüccar medeniyeti, -20 altın",
            choices: new List<CardChoice>
            {
                new CardChoice(
                    label: "Tamam",
                    effects: new List<CardEffect>
                    {
                        new CardEffect(ResourceType.Money, null, -20),
                        new CardEffect(null, CapitalType.Government, -30),
                    }
                )
            },
        Resources.Load<Sprite>("CardsSprite/Ejderha")
        );
        var card12 = new CardData(
            id: "card_food_supply",
            title: "Kale Kuşatması!",
            description: "Düşman medeniyetler sana karşı devasa bir ordu kurmuş ve asker medeniyetin bu saldırıya hazırlanmalı. : -20f asker medeniyeti, -20 asker",
            choices: new List<CardChoice>
            {
                new CardChoice(
                    label: "Tamam",
                    effects: new List<CardEffect>
                    {
                        new CardEffect(ResourceType.Power, null, -10),
                        new CardEffect(null, CapitalType.Military, -20),
                    }
                )
            },
            Resources.Load<Sprite>("CardsSprite/KaleKuşatma")
        );
        var card13 = new CardData(
            id: "card_food_supply",
            title: "Meydan Muharrebesi!",
            description: "Düşman medeniyetler sana karşı devasa bir ordu kurmuş ve komutanların savaşa destek istiyor. Kabul : -30 asker, -20 altın , -20 yemek. Red : -30f ordu gücü",
            choices: new List<CardChoice>
            {
                new CardChoice(
                    label: "Kabul",
                    effects: new List<CardEffect>
                    {
                        new CardEffect(ResourceType.Power, null, -30),
                        new CardEffect(ResourceType.Food, null, -20),
                        new CardEffect(ResourceType.Money, null, -20),
                    }
                ),
                new CardChoice(
                    label: "Red",
                    effects: new List<CardEffect>
                {
                    
                    new CardEffect(null, CapitalType.Military, -30),
                }
                )
            },
            Resources.Load<Sprite>("CardsSprite/Asker")
        );
        var card14 = new CardData(
            id: "card_food_supply",
            title: "Düşman köyüne saldırı!",
            description: "Subaylar yağma yapmak istiyor. Kabul : -10 asker. +10 altın + 10 yemek  Red :  -10f ordu gücü",
            choices: new List<CardChoice>
            {
                new CardChoice(
                    label: "Tamam",
                    effects: new List<CardEffect>
                    {
                        new CardEffect(ResourceType.Power, null, -10),
                        new CardEffect(ResourceType.Food, null, +10),
                        new CardEffect(ResourceType.Money, null, +10),
                    }
                )
            },
            Resources.Load<Sprite>("CardsSprite/DenizAdamYağma")
        );
        
       
        _decks[DeckType.Ilerleme][card1.Id] = card1;
        _decks[DeckType.Ilerleme][card2.Id] = card2;
        _decks[DeckType.Ilerleme][card3.Id] = card3;

        _decks[DeckType.KaynakDestek][card4.Id] = card4;
        _decks[DeckType.KaynakDestek][card5.Id] = card5;
        _decks[DeckType.KaynakDestek][card6.Id] = card6;

        _decks[DeckType.Zarar][card7.Id] = card7;
        _decks[DeckType.Zarar][card8.Id] = card8;
        _decks[DeckType.Zarar][card9.Id] = card9;

        _decks[DeckType.BigEvent][card10.Id] = card10;
        _decks[DeckType.BigEvent][card11.Id] = card11;
        _decks[DeckType.BigEvent][card12.Id] = card12;
        _decks[DeckType.BigEvent][card13.Id] = card13;
        _decks[DeckType.BigEvent][card14.Id] = card14;
        Debug.Log($"Loaded {_decks.Sum(d => d.Value.Count)} hardcoded cards across {_decks.Count} decks.");
    }

    public IEnumerable<CardData> GetAll(DeckType deck) =>
           _decks.TryGetValue(deck, out var cards) ? cards.Values : Enumerable.Empty<CardData>();

    public CardData GetById(DeckType deck, string id) =>
        _decks.TryGetValue(deck, out var cards) && cards.TryGetValue(id, out var card) ? card : null;

    public Dictionary<DeckType, IEnumerable<CardData>> GetAllDecks() =>
        _decks.ToDictionary(d => d.Key, d => d.Value.Values.AsEnumerable());

}