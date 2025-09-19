using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Networking;

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

    public CardRepository(){}

    public async UniTask InitializeAsync()
    {
        await LoadCardsFromXmlAsync();
    }

    private async UniTask LoadCardsFromXmlAsync()
    {
        string cardsDir = Path.Combine(Application.streamingAssetsPath, "Cards");
        List<string> xmlFiles;

#if UNITY_WEBGL && !UNITY_EDITOR
    // WebGL: use manifest
    string manifestPath = Path.Combine(cardsDir, "cardsList.json");
    string manifestJson = await LoadTextAsync(manifestPath);
    if (string.IsNullOrEmpty(manifestJson))
    {
        Debug.LogError("Card manifest is missing!");
        return;
    }
    xmlFiles = JsonUtility.FromJson<CardListWrapper>(manifestJson).files;
#else
        // Windows/Android/Editor: read directory normally
        if (!Directory.Exists(cardsDir))
        {
            Debug.LogWarning($"Cards folder not found: {cardsDir}");
            return;
        }
        xmlFiles = Directory.GetFiles(cardsDir, "*.xml").Select(Path.GetFileName).ToList();
#endif

        foreach (var fileName in xmlFiles)
        {
            string fullPath = Path.Combine(cardsDir, fileName);
            string xmlText = await LoadTextAsync(fullPath);
            if (string.IsNullOrEmpty(xmlText)) continue;

            try
            {
                XDocument doc = XDocument.Parse(xmlText); // parse from string
                var cardElements = doc.Root?.Elements("Card");
                if (cardElements == null) continue;

                foreach (var cardEl in cardElements)
                {
                    var cardData = ParseCard(cardEl);
                    _cards[cardData.Id] = cardData;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to parse card {fileName}: {e}");
            }
        }

        Debug.Log($"Loaded {_cards.Count} cards from XML.");
    }

    private async UniTask<string> LoadTextAsync(string path)
    {
        using var req = UnityWebRequest.Get(path);
        await req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Failed to load {path}: {req.error}");
            return null;
        }

        return req.downloadHandler.text;
    }

    [Serializable]
    public class CardListWrapper
    {
        public List<string> files;
    }


    private CardData ParseCard(XElement cardEl)
    {
        string id = cardEl.Attribute("id")?.Value;
        string title = cardEl.Element("Title")?.Value ?? "No Title";
        string description = cardEl.Element("Description")?.Value ?? "";

        var choices = cardEl.Element("Choices")?.Elements("Choice").Select(choiceEl =>
        {
            string label = choiceEl.Element("Label")?.Value ?? "No Label";

            var effects = choiceEl.Element("Effects")?.Elements("Effect").Select(effEl =>
            {
                ResourceType? resType = null;
                CapitalType? capType = null;

                string resStr = effEl.Element("ResourceType")?.Value;
                if (!string.IsNullOrEmpty(resStr))
                    resType = Enum.Parse<ResourceType>(resStr);

                string capStr = effEl.Element("CapitalType")?.Value;
                if (!string.IsNullOrEmpty(capStr))
                    capType = Enum.Parse<CapitalType>(capStr);

                int amount = int.Parse(effEl.Element("Amount")?.Value ?? "0");

                return new CardEffect(resType, capType, amount);
            }).ToList() ?? new List<CardEffect>();

            var conditions = choiceEl.Element("Conditions")?.Elements("Condition").Select(condEl =>
            {
                string type = condEl.Attribute("type")?.Value;
                if (type == "Resource")
                {
                    var resType = Enum.Parse<ResourceType>(condEl.Element("ResourceType")?.Value);
                    int minAmount = int.Parse(condEl.Element("MinAmount")?.Value ?? "0");
                    return (CardCondition)new ResourceCondition(resType, minAmount);
                }
                else if (type == "Capital")
                {
                    var capType = Enum.Parse<CapitalType>(condEl.Element("CapitalType")?.Value);
                    float minHealth = float.Parse(condEl.Element("MinHealth")?.Value ?? "0");
                    return (CardCondition)new CapitalCondition(capType, minHealth);
                }
                return null;
            }).Where(x => x != null).ToList() ?? new List<CardCondition>();

            return new CardChoice(label, effects, conditions);
        }).ToList() ?? new List<CardChoice>();

        return new CardData(id, title, description, choices);
    }

    public IEnumerable<CardData> GetAll() => _cards.Values;

    public CardData GetById(string id) =>
        _cards.TryGetValue(id, out var card) ? card : null;
}