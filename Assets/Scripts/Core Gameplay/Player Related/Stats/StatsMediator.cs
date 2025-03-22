using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StatsMediator
{
    public event Action<StatType> OnStatChanged;

    readonly List<StatModifier> listModifiers = new();
    readonly Dictionary<StatType, IEnumerable<StatModifier>> modifiersCache = new();
    IStatModifierApplicationOrder order = new NormalStatModifierOrder(); // INJECT IT

    // It apply operations by order,
    // for example, +10 , *2 , + 10 was 30
    // now it is 40 as it should be
    public void PerformQuery(object sender, Query query)
    {
        //var applicableModifiers = listModifiers.Where(modifier => modifier.StatType == query.StatType).ToList();

        if(!modifiersCache.ContainsKey(query.StatType))
        {
            modifiersCache[query.StatType] = listModifiers.Where(modifier => modifier.StatType == query.StatType).ToList();
        }
        query.Value = order.Apply(modifiersCache[query.StatType], query.Value);
    }

    public void InvalidateCache(StatType statType)
    {
        modifiersCache.Remove(statType);
    }

    public void AddModifier(StatModifier modifier)
    {
        listModifiers.Add(modifier);
        InvalidateCache(modifier.StatType);

        modifier.MarkedForRemoval = false;

        modifier.OnDispose += _ =>
        {
            InvalidateCache(modifier.StatType);
            listModifiers.Remove(modifier);

            // Notify that the stat has changed
            OnStatChanged?.Invoke(modifier.StatType);
        };

        // Notify that the stat has changed when the modifier is added
        OnStatChanged?.Invoke(modifier.StatType);

    }

    public void Update(float deltaTime)
    {
        foreach(var modifier in listModifiers)
        {
            modifier.Update(deltaTime);
        }

        foreach (var modifier in listModifiers.Where(modifier => modifier.MarkedForRemoval).ToList())
        {
            modifier.Dispose();
        }

        //LogAllModifiers();

    }

    public void LogAllModifiers()
    {
        var allModifiers = GetAllModifiers();
        foreach (var modifier in allModifiers)
        {
            Debug.Log($"Modifier Type: {modifier.StatType}, Operation: {modifier.OperationStrategy.GetType().Name}");
        }
    }

    public void RemoveModifier(StatModifier modifier)
    {
        listModifiers.Remove(modifier);
        InvalidateCache(modifier.StatType);

        OnStatChanged?.Invoke(modifier.StatType);

        Debug.Log($"Modifier {modifier.StatType} removed from mediator.");
    }

    public void RemoveModifierBySourceTag(ModifierSourceTag sourceTag)
    {
        var modifierToRemove = listModifiers.FirstOrDefault(modifier => modifier.SourceTag == sourceTag);
        if (modifierToRemove != null)
        {
            listModifiers.Remove(modifierToRemove);
            InvalidateCache(modifierToRemove.StatType);

            // Notify that the stat has changed
            OnStatChanged?.Invoke(modifierToRemove.StatType);

            Debug.Log($"Modifier with source tag {sourceTag} removed from mediator.");
        }
    }

    public void RemoveModifiersBySourceTag(ModifierSourceTag sourceTag)
    {
        var modifiersToRemove = listModifiers.Where(modifier => modifier.SourceTag == sourceTag).ToList();

        foreach (var modifier in modifiersToRemove)
        {
            listModifiers.Remove(modifier);
            InvalidateCache(modifier.StatType); // Ensure cache is invalidated for each StatType

            // Notify that the stat has changed
            OnStatChanged?.Invoke(modifier.StatType);
        }

        Debug.Log($"All modifiers with source tag {sourceTag} removed from mediator.");
    }

    public void RemoveAllModifiers()
    {
        var affectedStats = listModifiers.Select(mod => mod.StatType).Distinct().ToList();

        listModifiers.Clear(); // Clear all modifiers
        modifiersCache.Clear(); // Invalidate the entire cache

        // Notify about changes for affected stat types
        foreach (var statType in affectedStats)
        {
            OnStatChanged?.Invoke(statType);
            Debug.Log($"All modifiers removed for stat {statType}.");
        }
    }

    // New method to get all modifiers
    public IEnumerable<StatModifier> GetAllModifiers()
    {
        return listModifiers;
    }

    // New method to get modifiers filtered by stat type
    public IEnumerable<StatModifier> GetModifiersByType(StatType statType)
    {
        return listModifiers.Where(mod => mod.StatType == statType);
    }

    public StatModifier GetModifierBySourceTag(ModifierSourceTag sourceTag)
    {
        return listModifiers.FirstOrDefault(modifier => modifier.SourceTag == sourceTag);
    }

    // Method to get additive modifiers for a specific stat type
    public IEnumerable<StatModifier> GetAdditiveModifiers(StatType statType)
    {
        return listModifiers.Where(modifier => modifier.StatType == statType && modifier.OperationStrategy is AddOperation);
    }

    // Method to get multiplicative modifiers for a specific stat type
    public IEnumerable<StatModifier> GetMultiplicativeModifiers(StatType statType)
    {
        return listModifiers.Where(modifier => modifier.StatType == statType && modifier.OperationStrategy is MultiplyOperation);
    }

    // Method to get multiplicative modifiers for a specific stat type
    public IEnumerable<StatModifier> GetMultiplyByPercentageModifiers(StatType statType)
    {
        return listModifiers.Where(modifier => modifier.StatType == statType && modifier.OperationStrategy is MultiplyByPercentageOperation);
    }
}

public class Query
{
    public readonly StatType StatType;
    public float Value;

    public Query(StatType statType, float value)
    {
        StatType = statType;
        Value = value;
    }
}
