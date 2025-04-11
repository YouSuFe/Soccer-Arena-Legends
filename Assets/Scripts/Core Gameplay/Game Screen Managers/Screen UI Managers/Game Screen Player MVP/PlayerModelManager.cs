using System;
using UnityEngine;

/// <summary>
/// Manages the aggregation of events and interactions between the Player, Stats, and Mediator models.
/// Acts as the Model Manager in the MVP architecture.
/// </summary>
public class PlayerModelManager
{
    #region Events

    /// <summary>
    /// Event triggered when a player's stat changes (e.g., Health, Strength, Speed).
    /// Parameters: StatType, base stat value, current stat value.
    /// </summary>
    public event Action<StatType, int, int> OnStatChanged;

    /// <summary>
    /// Event triggered when a skill enters a cooldown period.
    /// Parameters: SkillType, cooldown duration.
    /// </summary>
    public event Action<SkillType, float> OnSkillCooldownChanged;

    /// <summary>
    /// Event triggered when player's stamina changes.
    /// Parameters: curren stamina, max stamina.
    /// </summary>
    public event Action<float, float> OnStaminaChanged; // Current stamina, Max stamina


    #endregion

    #region Fields

    private Stats stats; // Handles player's core stat logic
    private StatsMediator mediator; // Manages buffs and debuffs on stats
    private PlayerAbstract player; // Reference to the player object

    #endregion

    #region Initialization

    /// <summary>
    /// Constructs the PlayerModelManager and subscribes to model events.
    /// </summary>
    /// <param name="stats">The Stats instance representing player stats.</param>
    /// <param name="mediator">The StatsMediator managing stat modifiers.</param>
    /// <param name="player">The Player instance to link with this manager.</param>
    public PlayerModelManager(Stats stats, StatsMediator mediator, PlayerAbstract player)
    {
        this.stats = stats;
        this.mediator = mediator;
        this.player = player;

        // Subscribe to relevant events from Stats, Mediator, and Player
        mediator.OnStatChanged += HandleStatChanged;

        // 🔥 Subscribe to networked stat changes
        player.Health.OnValueChanged += (oldVal, newVal) => HandleStatChanged(StatType.Health);
        player.Strength.OnValueChanged += (oldVal, newVal) => HandleStatChanged(StatType.Strength);
        player.Speed.OnValueChanged += (oldVal, newVal) => HandleStatChanged(StatType.Speed);

        player.OnSkillCooldownChanged += HandleSkillCooldownChanged;
        player.OnStaminaChanged += HandleStaminaChanged;
    }

    #endregion

    #region Event Handlers


    /// <summary>
    /// Handles stat changes (e.g., Strength, Speed, Health) and forwards updated values to listeners.
    /// </summary>
    /// <param name="statType">The type of stat (e.g., Strength, Speed, Health).</param>
    private void HandleStatChanged(StatType statType)
    {
        // Get the base value and current modified value for the stat
        int baseValue = stats.GetBaseStat(statType);
        int currentValue = stats.GetCurrentStat(statType);

        // Emit the stat change event
        OnStatChanged?.Invoke(statType, baseValue, currentValue);
    }

    /// <summary>
    /// Handles skill cooldown events and forwards the cooldown duration to listeners.
    /// </summary>
    /// <param name="skillType">The type of skill (e.g., BallSkill, WeaponSkill).</param>
    /// <param name="cooldown">The cooldown duration in seconds.</param>
    private void HandleSkillCooldownChanged(SkillType skillType, float cooldown)
    {
        // Emit the skill cooldown event
        OnSkillCooldownChanged?.Invoke(skillType, cooldown);
    }

    /// <summary>
    /// Handles stamina changes and forwards them to listeners.
    /// </summary>
    private void HandleStaminaChanged(float currentStamina, float maxStamina)
    {
        OnStaminaChanged?.Invoke(currentStamina, maxStamina);
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Unsubscribes from all events to prevent memory leaks.
    /// </summary>
    public void Cleanup()
    {
        mediator.OnStatChanged -= HandleStatChanged;

        player.Health.OnValueChanged -= (oldVal, newVal) => HandleStatChanged(StatType.Health);
        player.Strength.OnValueChanged -= (oldVal, newVal) => HandleStatChanged(StatType.Strength);
        player.Speed.OnValueChanged -= (oldVal, newVal) => HandleStatChanged(StatType.Speed);

        player.OnSkillCooldownChanged -= HandleSkillCooldownChanged;
        player.OnStaminaChanged -= HandleStaminaChanged;
    }

    #endregion
}
