using System;
using System.Collections.Generic;

// --- Models ---

public class Resource
{
    public ResourceType Type { get; }
    public int Amount { get; private set; }

    public Resource(ResourceType type, int initialAmount = 0)
    {
        Type = type;
        Amount = Math.Max(0, initialAmount);
    }

    public void Add(int value)
    {
        if (value < 0) throw new ArgumentException("Use Spend() for negative values.");
        Amount += value;
    }

    public bool Spend(int value)
    {
        if (value <= 0) return false;
        if (Amount < value) return false;

        Amount -= value;
        return true;
    }

    public void SetAmount(int newValue)
    {
        Amount = Math.Max(0, newValue);
    }
}

// --- Repository ---

public interface IResourceRepository
{
    IEnumerable<Resource> GetAll();
    Resource GetByType(ResourceType type);
}

public class ResourceRepository : IResourceRepository
{
    private readonly Dictionary<ResourceType, Resource> _resources;

    public ResourceRepository()
    {
        _resources = new Dictionary<ResourceType, Resource>
        {
            { ResourceType.Money, new Resource(ResourceType.Money, 0) },
            { ResourceType.Food, new Resource(ResourceType.Food, 0) },
            { ResourceType.Power, new Resource(ResourceType.Power, 0) }
        };
    }

    public IEnumerable<Resource> GetAll() => _resources.Values;

    public Resource GetByType(ResourceType type) =>
        _resources.TryGetValue(type, out var res) ? res : null;
}
