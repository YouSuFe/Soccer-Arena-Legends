using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DamageProcessor
{
    private Stats stats;
    private StatsMediator mediator;

    public DamageProcessor(Stats stats, StatsMediator mediator)
    {
        this.stats = stats ?? throw new System.ArgumentNullException(nameof(stats), "Stats object cannot be null.");
        this.mediator = mediator ?? throw new System.ArgumentNullException(nameof(mediator), "Mediator object cannot be null.");
    }

    public void ProcessDamage(int damageAmount)
    {
        int remainingDamage = damageAmount;
        var modifiersToDispose = new List<StatModifier>();

        Debug.Log($"Initial Damage: {damageAmount}");
        mediator.LogAllModifiers(); // Log all modifiers before applying damage

        // Process Additive Modifiers only if they exist
        var additiveModifiers = mediator.GetAdditiveModifiers(StatType.Health);
        if (additiveModifiers.Any())
        {
            ProcessAdditiveModifiers(ref remainingDamage, additiveModifiers, modifiersToDispose);
        }
        RemoveDisposedModifiers(modifiersToDispose);

        // Process Multiplicative Modifiers only if they exist and there is remaining damage
        var multiplicativeModifiers = mediator.GetMultiplicativeModifiers(StatType.Health);
        if (multiplicativeModifiers.Any() && remainingDamage > 0)
        {
            ProcessMultiplicativeModifiers(ref remainingDamage, multiplicativeModifiers, modifiersToDispose);
        }
        RemoveDisposedModifiers(modifiersToDispose);


        // Process MultiplyByPercentage Modifiers only if they exist and there is remaining damage
        var percentageMultipliers = mediator.GetMultiplyByPercentageModifiers(StatType.Health);
        if (percentageMultipliers.Any() && remainingDamage > 0)
        {
            ProcessMultiplyByPercentageModifiers(ref remainingDamage, percentageMultipliers, modifiersToDispose);
        }
        RemoveDisposedModifiers(modifiersToDispose);

        // If there are no modifiers left or damage remains after processing modifiers, apply it directly to base health
        if (remainingDamage > 0)
        {
            mediator.InvalidateCache(StatType.Health);
            stats.SetStat(StatType.Health, stats.GetCurrentStat(StatType.Health) - remainingDamage);
            Debug.Log($"Remaining Damage applied directly to Health: {remainingDamage}");
        }

        mediator.LogAllModifiers(); // Log all modifiers after applying damage
    }

    private void ProcessAdditiveModifiers(ref int remainingDamage, IEnumerable<StatModifier> additiveModifiers, List<StatModifier> modifiersToDispose)
    {
        float combineCumulativeMultiplier = CalculateCombinedMultiplier();
        float effectiveDamage = remainingDamage / combineCumulativeMultiplier;
        Debug.Log($"Cumulative Multiplier: {combineCumulativeMultiplier}, Effective Damage: {effectiveDamage}");

        foreach (var modifier in additiveModifiers)
        {
            float modifierValue = modifier.OperationStrategy.GetValue(); // Additive health contribution

            if (effectiveDamage > modifierValue && modifierValue > 0)
            {
                remainingDamage -= Mathf.FloorToInt(modifierValue * combineCumulativeMultiplier);
                modifiersToDispose.Add(modifier); // Mark this modifier for disposal
                Debug.Log("Remaining damage from Addition Modifier : " + remainingDamage);

            }
            else
            {
                float newModifierValue = modifierValue - effectiveDamage;
                modifier.OperationStrategy = new AddOperation(newModifierValue);
                remainingDamage = 0;
                Debug.Log($"Adjusted Additive Modifier Value: {newModifierValue}. Remaining Damage: {remainingDamage}");

                // Dispose of the modifier if its value drops to zero or below
                if (newModifierValue <= 0)
                {
                    modifiersToDispose.Add(modifier);
                }
                break;
            }
        }
    }

    private void ProcessMultiplicativeModifiers(ref int remainingDamage, IEnumerable<StatModifier> multiplicativeModifiers, List<StatModifier> modifiersToDispose)
    {
        float currentHealth = stats.GetCurrentStat(StatType.Health);

        // Calculate total current multiplier
        float totalMultiplier = CalculateCumulativeMultiplier();
        Debug.Log("Current TotalMultiplier  " + totalMultiplier);

        // Calculate the effective base health after applying all multipliers
        float effectiveHealth = currentHealth;
        Debug.Log("effectiveHealth " + effectiveHealth);

        foreach (var modifier in multiplicativeModifiers)
        {
            // Calculate the proportion of health that should be reduced
            float proportion = (effectiveHealth - remainingDamage) / effectiveHealth;
            float currentMultiplier = modifier.OperationStrategy.GetValue();
            float newMultiplier = currentMultiplier * proportion;

            if (newMultiplier < 1f)
            {
                // Calculate the exact point where the multiplier drops to 1
                float healthIfModifierValueIsOne = currentHealth * (1 / currentMultiplier);
                float lastDamageToDisposeMultiplier = effectiveHealth - healthIfModifierValueIsOne;

                // Calculate remaining damage that should be applied to the next modifier
                remainingDamage -= Mathf.FloorToInt(lastDamageToDisposeMultiplier);
                Debug.Log("Remaining damage from Multiplier Modifier : " + remainingDamage);

                // Dispose of this modifier since it's fully used
                modifiersToDispose.Add(modifier);
            }
            else
            {
                modifier.OperationStrategy = new MultiplyOperation(newMultiplier);
                Debug.Log($"Adjusted Additive Modifier Value: {newMultiplier}");
                remainingDamage = 0;
                break;
            }

        }
    }

    private void ProcessMultiplyByPercentageModifiers(ref int remainingDamage, IEnumerable<StatModifier> percentageMultipliers, List<StatModifier> modifiersToDispose)
    {
        float currentHealth = stats.GetCurrentStat(StatType.Health);

        // Use the current health after all modifiers are applied as the effective health
        float effectiveHealth = currentHealth;
        Debug.Log("effectiveHealth " + effectiveHealth);

        foreach (var modifier in percentageMultipliers)
        {
            float currentPercentageValue = modifier.OperationStrategy.GetValue();

            // Calculate the health that this modifier is contributing to
            float healthWithoutCurrentModifier = effectiveHealth / (1 + currentPercentageValue / 100f);
            float healthContribution = effectiveHealth - healthWithoutCurrentModifier;

            if (remainingDamage >= healthContribution)
            {
                // Damage exceeds or equals the health contribution, so fully use this modifier
                modifiersToDispose.Add(modifier);
                remainingDamage -= Mathf.FloorToInt(healthContribution);
                effectiveHealth = healthWithoutCurrentModifier;  // Update effective health after disposing this modifier
                Debug.Log("Remaining damage from Percentage Multiplier Modifier : " + remainingDamage);

            }
            else
            {
                // Adjust the current percentage value based on the remaining damage
                float proportionOfHealthUsed = remainingDamage / healthContribution;
                float newPercentageValue = currentPercentageValue * (1 - proportionOfHealthUsed);
                modifier.OperationStrategy = new MultiplyByPercentageOperation(newPercentageValue);
                Debug.Log($"Adjusted Additive Modifier Value: {newPercentageValue}");
                remainingDamage = 0;
                break; // Process only one modifier in this loop
            }
        }
    }


    private void RemoveDisposedModifiers(List<StatModifier> modifiersToDispose)
    {
        foreach (var modifier in modifiersToDispose)
        {
            mediator.RemoveModifier(modifier);
            Debug.Log($"Modifier {modifier.StatType} removed.");
        }
    }

    private float CalculateCumulativeMultiplier()
    {
        var multiplicativeModifiers = mediator.GetMultiplicativeModifiers(StatType.Health);
        return multiplicativeModifiers.Aggregate(1f, (acc, mod) => acc * mod.OperationStrategy.GetValue());
    }

    private float CalculateCumulativePercentageMultiplier()
    {
        var percentageMultipliers = mediator.GetMultiplyByPercentageModifiers(StatType.Health);
        return percentageMultipliers.Aggregate(1f, (acc, mod) => acc * (1 + mod.OperationStrategy.GetValue() / 100f));
    }

    private float CalculateCombinedMultiplier()
    {
        float cumulativeMultiplier = CalculateCumulativeMultiplier();
        float cumulativePercentageMultiplier = CalculateCumulativePercentageMultiplier();

        return cumulativeMultiplier * cumulativePercentageMultiplier;
    }
}
