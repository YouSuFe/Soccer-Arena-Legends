using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBaseStats
{
    public event Action<StatType> OnStatChanged;

    private BaseStats baseStats; // Reference to the base stats defined in BaseStats
    private Dictionary<StatType, int> additionalStats = new Dictionary<StatType, int>(); // Stores additional stat values

    public int DistributablePoints { get; private set; } = 0; // Points available for distribution to stats

    private float baseStamina = 20f;

    // Constructor initializes PlayerBaseStats with the provided base stats
    public PlayerBaseStats(BaseStats baseStats)
    {
        this.baseStats = baseStats;

        // Initialize the additional stats to zero
        additionalStats[StatType.Health] = 0;
        additionalStats[StatType.Strength] = 0;
        additionalStats[StatType.Speed] = 0;
    }

    // Retrieves the value of a stat, including base and additional values
    public int GetStatValue(StatType statType)
    {
        int baseValue = statType switch
        {
            StatType.Health => baseStats.health,
            StatType.Strength => baseStats.strength,
            StatType.Speed => baseStats.speed,
            _ => throw new ArgumentOutOfRangeException(nameof(statType), statType, null)
        };

        // Return the base value plus any additional stat value
        return baseValue + additionalStats[statType];
    }

    // Adds distributable points when the player levels up
    public void LevelUp(int pointsToAdd)
    {
        DistributablePoints += pointsToAdd;
    }

    // Allocates a distributable point to a specific stat, increasing it by 10% of the base value
    public bool AllocatePoint(StatType statType)
    {
        if (DistributablePoints > 0)
        {
            int baseValue = statType switch
            {
                StatType.Health => baseStats.health,
                StatType.Strength => baseStats.strength,
                StatType.Speed => baseStats.speed,
                _ => throw new ArgumentOutOfRangeException(nameof(statType), statType, null)
            };

            // Calculate a 10% increase and add it to the additional stat value
            int increase = Mathf.CeilToInt(baseValue * 0.1f);
            additionalStats[statType] += increase;

            DistributablePoints--;

            // Notify listeners that the stat has changed
            OnStatChanged?.Invoke(statType);

            return true; // Point was successfully allocated
        }
        return false; // No points were allocated
    }

    public float GetStamina()
    {
        float health = GetStatValue(StatType.Health);
        return baseStamina + health/2;
    }
}
