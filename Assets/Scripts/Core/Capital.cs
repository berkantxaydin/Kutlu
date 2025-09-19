using System;
using System.Collections.Generic;

// --- Models ---

public enum ResourceType
{
    Money,
    Food,
    Power
}

public abstract class CapitalBase
{
    public string Name { get; }
    public float Health { get; private set; } // 0 - 100
    public float ProductionRate { get; protected set; }
    public ResourceType ResourceType { get; }

    protected CapitalBase(string name, ResourceType resourceType, float initialHealth = 100f)
    {
        Name = name;
        ResourceType = resourceType;
        Health = initialHealth;
    }

    public virtual int Produce()
    {
        // Example: 100% health = full production rate
        return (int)(ProductionRate * (Health / 100f));
    }

    public void ModifyHealth(float amount)
    {
        Health = Math.Clamp(Health + amount, 0f, 100f);
    }
}

public class Government : CapitalBase
{
    public Government(float initialHealth = 100f)
        : base("Government", ResourceType.Money, initialHealth)
    {
        ProductionRate = 10; // base rate for Money
    }
}

public class Population : CapitalBase
{
    public Population(float initialHealth = 100f)
        : base("Population", ResourceType.Food, initialHealth)
    {
        ProductionRate = 8; // base rate for Food
    }
}

public class Military : CapitalBase
{
    public Military(float initialHealth = 100f)
        : base("Military", ResourceType.Power, initialHealth)
    {
        ProductionRate = 6; // base rate for Power
    }
}

// --- Repository ---

public interface ICapitalRepository
{
    void Add(CapitalBase capital);
    IEnumerable<CapitalBase> GetAll();
    CapitalBase GetByName(string name);
}

public class CapitalRepository : ICapitalRepository
{
    private readonly Dictionary<string, CapitalBase> _capitals;

    public void Add(CapitalBase capital)
    {
        if (capital != null && !_capitals.ContainsKey(capital.Name))
            _capitals.Add(capital.Name, capital);
    }

    public CapitalRepository()
    {
        _capitals = new Dictionary<string, CapitalBase>
        {
            { "Government", new Government() },
            { "Population", new Population() },
            { "Military", new Military() }
        };
    }

    public IEnumerable<CapitalBase> GetAll() => _capitals.Values;

    public CapitalBase GetByName(string name) =>
        _capitals.TryGetValue(name, out var capital) ? capital : null;
}
