using System;
using System.Collections.Generic;
using UnityEngine;

public enum StatType {Health, Strength, Speed }

public class Stats
{
    readonly PlayerBaseStats playerBaseStats; // Reference to the player's base stats
    readonly StatsMediator mediator; // Mediator to handle any active stat modifiers(power ups, debuffs etc.)
    private Dictionary<StatType, int> currentStats; // Stores the current modified stats
    private Dictionary<StatType, int> baseStats; // Stores the base stats


    public StatsMediator Mediator => mediator; // Exposes the mediator for external use

    // Constructor to initialize the Stats object with a mediator and base stats
    public Stats(StatsMediator mediator, PlayerBaseStats playerBaseStats)
    {
        this.mediator = mediator;
        this.playerBaseStats = playerBaseStats;

        playerBaseStats.OnStatChanged += PlayerBaseStats_OnStatChanged;

        InitializeBaseStats();
    }


    private void InitializeBaseStats()
    {
        baseStats = new Dictionary<StatType, int>
        {
            { StatType.Health, playerBaseStats.GetStatValue(StatType.Health) },
            { StatType.Strength, playerBaseStats.GetStatValue(StatType.Strength) },
            { StatType.Speed, playerBaseStats.GetStatValue(StatType.Speed) }
        };

        currentStats = new Dictionary<StatType, int>(baseStats);
    }

    // Retrieves the base value of a stat without any modifiers
    public int GetBaseStat(StatType statType)
    {
        return baseStats[statType];
    }

    // Retrieves the current value of a stat, applying any active modifiers
    public int GetCurrentStat(StatType statType)
    {
        var baseValue = baseStats[statType];
        var query = new Query(statType, baseValue);
        mediator.PerformQuery(this, query);
        return Mathf.RoundToInt(query.Value);
    }

    public void SetStat(StatType statType, int finalValue)
    {
        if (baseStats.ContainsKey(statType))
        {
            finalValue = Mathf.Max(0, finalValue); // Ensure stat is not negative
            currentStats[statType] = finalValue;

            if (finalValue < baseStats[statType])
            {
                baseStats[statType] = finalValue;
            }

            Debug.Log($"SetStat: Updated {statType} to {finalValue}");
        }

    }

    private void PlayerBaseStats_OnStatChanged(StatType statType)
    {
        baseStats[statType] = playerBaseStats.GetStatValue(statType);
        currentStats[statType] = baseStats[statType];

        Debug.Log($"Stat {statType} updated to {baseStats[statType]}");
    }

    public override string ToString()
    {
        return $"Health: {GetCurrentStat(StatType.Health)}, Attack: {GetCurrentStat(StatType.Strength)}, Speed: {GetCurrentStat(StatType.Speed)}";
    }
}


