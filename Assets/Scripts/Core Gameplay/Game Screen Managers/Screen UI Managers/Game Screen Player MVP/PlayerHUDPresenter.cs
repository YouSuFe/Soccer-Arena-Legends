using UnityEngine;
/// <summary>
/// The Presenter in the MVP architecture that bridges the PlayerModelManager (Model) and PlayerUIManager (View).
/// Listens to model events and updates the UI accordingly.
/// </summary>
public class PlayerHUDPresenter
{
    #region Fields

    private PlayerModelManager modelManager; // Reference to the Model Manager
    private PlayerUIController uiManager; // Reference to the UI Manager

    #endregion

    #region Initialization

    /// <summary>
    /// Constructs the HUD Presenter and subscribes to relevant events from the Model Manager.
    /// </summary>
    /// <param name="modelManager">The PlayerModelManager providing model events.</param>
    /// <param name="uiManager">The PlayerUIManager that updates the UI.</param>
    public PlayerHUDPresenter(PlayerModelManager modelManager, PlayerUIController uiManager)
    {
        this.modelManager = modelManager;
        this.uiManager = uiManager;

        // Subscribe to model events
        modelManager.OnStatChanged += HandleStatChanged;
        modelManager.OnSkillCooldownChanged += HandleSkillCooldownChanged;
        modelManager.OnStaminaChanged += HandleStaminaChanged;
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles stat changes (e.g., Strength, Speed, Health) emitted by the Model Manager and updates the UI.
    /// </summary>
    /// <param name="statType">The type of stat (e.g., Strength, Speed, Health).</param>
    /// <param name="baseValue">The unmodified base value of the stat.</param>
    /// <param name="currentValue">The current modified value of the stat.</param>
    private void HandleStatChanged(StatType statType, int baseValue, int currentValue)
    {
        uiManager.UpdateStat(statType, baseValue, currentValue);
    }

    /// <summary>
    /// Handles skill cooldown events emitted by the Model Manager and updates the UI.
    /// </summary>
    /// <param name="skillType">The type of skill (e.g., BallSkill, WeaponSkill).</param>
    /// <param name="cooldown">The cooldown duration in seconds.</param>
    private void HandleSkillCooldownChanged(SkillType skillType, float cooldown)
    {
        uiManager.UpdateSkillCooldown(skillType, cooldown);
    }

    /// <summary>
    /// Handles stamina changes and updates the stamina bar and text in the UI.
    /// </summary>
    private void HandleStaminaChanged(float currentStamina, float maxStamina)
    {
        uiManager.UpdateStaminaBar(currentStamina, maxStamina);
    }

    #endregion

    #region Cleanup

    /// <summary>
    /// Unsubscribes from all events to prevent memory leaks.
    /// </summary>
    public void Cleanup()
    {
        // Unsubscribe from model events
        modelManager.OnStatChanged -= HandleStatChanged;
        modelManager.OnSkillCooldownChanged -= HandleSkillCooldownChanged;
        modelManager.OnStaminaChanged -= HandleStaminaChanged;
    }

    #endregion
}
