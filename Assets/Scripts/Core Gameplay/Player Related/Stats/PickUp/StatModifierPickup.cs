using UnityEngine;

/// <summary>
/// Handles stat-modifying pickups (e.g., Strength, Speed buffs).
/// </summary>
public class StatModifierPickup : Pickup
{
    [SerializeField] private StatModifierConfig config;

    StatModifierFactory statModifierFactory = new StatModifierFactory();

    private StatModifier appliedModifier; // Store the applied modifier reference

    protected override void ApplyPickupEffect(Entity entity)
    {
        appliedModifier = statModifierFactory.Create(
            config.OperatorType,
            config.StatType,
            config.Value,
            config.Duration,
            config.ModifierSourceTag
        );
        if (entity == null)
        {
            Debug.LogError("entity is null!");
        }
        else
        {
            Debug.Log("entity is NOT null");

            if (entity.Stats == null)
            {
                Debug.LogError("entity.Stats is null!");
            }
            else
            {
                Debug.Log("entity.Stats is NOT null");

                if (entity.Stats.Mediator == null)
                {
                    Debug.LogError("entity.Stats.Mediator is null!");
                }
                else
                {
                    Debug.Log("entity.Stats.Mediator is NOT null");

                    if (appliedModifier == null)
                    {
                        Debug.LogError("appliedModifier is null!");
                    }
                    else
                    {
                        Debug.Log("appliedModifier is NOT null");

                        // Now it's safe to call the method
                        entity.Stats.Mediator.AddModifier(appliedModifier);
                    }
                }
            }
        }
        Debug.Log($"[APPLY EFFECT] Applied {config.OperatorType} {config.StatType} {config.Value} to {entity.gameObject.name}");
    }

    /// <summary>
    /// Reverts the stat modifier effect (used when the server rejects the pickup).
    /// </summary>
    protected override void RevertPickupEffect(Entity entity)
    {
        if (appliedModifier != null)
        {
            entity.Stats.Mediator.RemoveModifier(appliedModifier); //  Remove only this modifier

            Debug.Log($"[ROLLBACK] Removed {config.OperatorType} {config.StatType} {config.Value} from {entity.gameObject.name}");

            appliedModifier = null; // Clear reference to prevent double removal
        }
    }
}