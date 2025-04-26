using System;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// Manages the Player's UI, including health, stats, cooldowns, and floating damage text.
/// </summary>
public class PlayerUIController : MonoBehaviour
{
    #region Fields

    [Header("Goal Announcement UI")]
    [SerializeField] private TextMeshProUGUI goalAnnouncementText;
    [SerializeField] private CanvasGroup goalAnnouncementCanvasGroup;
    [SerializeField] private Color blueTeamColor = Color.blue;
    [SerializeField] private Color redTeamColor = Color.red;
    [SerializeField] private Color neutralColor = Color.white;
    [SerializeField] private string goalScoredText = "scored a goal!";
    [SerializeField] private string ownGoalScoredText = "scored an own goal!";

    [Header("Player Info Panel Elements")]
    [Tooltip("Current Player's character name.")]
    [SerializeField] private TextMeshProUGUI characterNameText;
    [Tooltip("Current Player's weapon name.")]
    [SerializeField] private TextMeshProUGUI weaponNameText;
    [SerializeField] private Image characterIconImage;
    [SerializeField] private Image weaponIconImage;

    [Header("Floating Damage Text")]
    [Tooltip("Prefab for displaying floating damage numbers.")]
    public GameObject floatingDamageTextPrefab;

    [Tooltip("Parent container for all floating damage texts.")]
    public RectTransform floatingDamageTextsParent;

    [Header("Health Elements")]
    [Tooltip("Text for healthy state (health > 30).")]
    public TextMeshProUGUI healthyHealthText;

    [Tooltip("Text for danger state (health <= 30).")]
    public TextMeshProUGUI dangerHealthText;

    [Header("Stats Elements")]
    [Tooltip("Text for displaying normal strength stats.")]
    public TextMeshProUGUI strengthNormalText;

    [Tooltip("Text for displaying buffed strength stats.")]
    public TextMeshProUGUI strengthBuffedText;

    [Tooltip("Text for displaying debuffed strength stats.")]
    public TextMeshProUGUI strengthDebuffedText;

    [Space]

    [Tooltip("Text for displaying normal speed stats.")]
    public TextMeshProUGUI speedNormalText;

    [Tooltip("Text for displaying buffed speed stats.")]
    public TextMeshProUGUI speedBuffedText;

    [Tooltip("Text for displaying debuffed speed stats.")]
    public TextMeshProUGUI speedDebuffedText;


    [Header("Stamina Elements")]
    [Tooltip("Slider component for stamina.")]
    public Slider staminaBarSlider;

    [Tooltip("Text field displaying stamina values (current/max).")]
    public TextMeshProUGUI staminaText;


    [Header("Cooldown Text")]
    [Tooltip("Cooldown text for the weapon skill.")]
    public TextMeshProUGUI weaponSkillCooldownText;

    [Tooltip("Cooldown text for the ball skill.")]
    public TextMeshProUGUI ballSkillCooldownText;

    [Header("Cooldown Fillers")]
    [Tooltip("Cooldown filler for the weapon skill.")]
    public Image weaponSkillCooldownFiller;

    [Tooltip("Cooldown filler for the ball skill.")]
    public Image ballSkillCooldownFiller;

    [Header("Cooldown Ready Indicators")]
    [Tooltip("Ready indicator for the weapon skill.")]
    public Image weaponSkillReadyIndicator;

    [Tooltip("Ready indicator for the ball skill.")]
    public Image ballSkillReadyIndicator;

    [Header("Death UI")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private TextMeshProUGUI respawnCountdownText;
    [SerializeField] private CanvasGroup deathPanelCanvasGroup;

    private float liveRespawnTime = -1f;

    private Coroutine ballSkillCoroutine;
    private Coroutine weaponSkillCoroutine;
    private Coroutine goalAnnouncementCoroutine;

    private PlayerModelManager modelManager;
    private PlayerHUDPresenter presenter;

    #endregion

    private void Start()
    {
        goalAnnouncementCanvasGroup.alpha = 0;
        Hide();
    }

    private void Update()
    {
        if (liveRespawnTime > 0)
        {
            liveRespawnTime -= Time.deltaTime;
            if (respawnCountdownText != null)
            {
                respawnCountdownText.text = $"{liveRespawnTime:F2} seconds";
            }
        }
    }

    #region Initializations

    /// <summary>
    /// Initializes the UI Manager with the player's stats, mediator, and model manager.
    /// </summary>
    public void Initialize(PlayerAbstract player, Stats stats, StatsMediator mediator, int characterId, int weaponId)
    {
        Show();

        modelManager = new PlayerModelManager(stats, mediator, player);
        presenter = new PlayerHUDPresenter(modelManager, this);

        InitializeUIElements(player, stats, characterId, weaponId);
    }

    /// <summary>
    /// Sets the default state of all UI elements at the start of the game.
    /// </summary>
    private void InitializeUIElements(PlayerAbstract player, Stats stats, int characterId, int weaponId)
    {

        // Character Setup
        if (characterId != -1 && characterIconImage != null)
        {
            var character = PlayerSpawnManager.Instance.CharacterDatabase.GetCharacterById(characterId);
            if (character != null && character.Icon != null)
            {
                characterIconImage.sprite = character.Icon;
                characterNameText.text = character.DisplayName;
            }
        }

        // Weapon Setup
        if (weaponId != -1 && weaponIconImage != null)
        {
            var weapon = PlayerSpawnManager.Instance.WeaponDatabase.GetWeaponById(weaponId);
            if (weapon != null && weapon.Icon != null)
            {
                weaponIconImage.sprite = weapon.Icon;
                weaponNameText.text = weapon.DisplayName;
            }
        }

        // Health Initialization
        healthyHealthText.gameObject.SetActive(true);
        dangerHealthText.gameObject.SetActive(false);
        healthyHealthText.text = $"{stats.GetCurrentStat(StatType.Health)}";

        // Strength Stat Initialization
        strengthNormalText.gameObject.SetActive(true);
        strengthBuffedText.gameObject.SetActive(false);
        strengthDebuffedText.gameObject.SetActive(false);
        strengthNormalText.text = $"{stats.GetCurrentStat(StatType.Strength)}";

        // Speed Stat Initialization
        speedNormalText.gameObject.SetActive(true);
        speedBuffedText.gameObject.SetActive(false);
        speedDebuffedText.gameObject.SetActive(false);
        speedNormalText.text = $"{stats.GetCurrentStat(StatType.Speed)}";

        // Cooldown Initialization
        ballSkillCooldownText.text = "";
        weaponSkillCooldownText.text = "";
        ballSkillCooldownFiller.fillAmount = 0;
        weaponSkillCooldownFiller.fillAmount = 0;

        // Ready Indicators (show initially since skills are ready)
        ballSkillReadyIndicator.gameObject.SetActive(true);
        weaponSkillReadyIndicator.gameObject.SetActive(true);

        // Initialize stamina UI
        staminaBarSlider.value = 1f;
        staminaText.text = $"{player.PlayerStamina} / {player.PlayerMaxStamina}";

        // Death UI
        HideDeathScreen(true); // Start hidden, forcefully
    }   

    #endregion

    #region Death UI Logic

    public void StartRespawnCountdown(float duration)
    {
        Debug.Log("Player UI Manager, Starting to the Death UI");
        liveRespawnTime = duration;

        if (deathPanelCanvasGroup != null)
        {
            deathPanel.SetActive(true);
            StartCoroutine(FadeCanvasGroup(deathPanelCanvasGroup, true, 0.5f));
        }
        else
        {
            deathPanel?.SetActive(true);
        }
    }

    public void HideDeathScreen(bool instant = false)
    {
        liveRespawnTime = -1f;

        if (instant || deathPanelCanvasGroup == null)
        {
            deathPanel?.SetActive(false);
            return;
        }

        StartCoroutine(FadeCanvasGroup(deathPanelCanvasGroup, false, 0.5f));
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, bool fadeIn, float duration)
    {
        float start = canvasGroup.alpha;
        float end = fadeIn ? 1f : 0f;
        float time = 0f;

        while (time < duration)
        {
            canvasGroup.alpha = Mathf.Lerp(start, end, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        canvasGroup.alpha = end;

        if (!fadeIn)
            deathPanel.SetActive(false);
    }

    #endregion

    #region Clean Up

    public void Cleanup()
    {
        presenter?.Cleanup();
        modelManager?.Cleanup();

        presenter = null;
        modelManager = null;
    }

    /// <summary>
    /// Cleans up resources when the player object is destroyed.
    /// </summary>
    private void OnDestroy()
    {
        presenter?.Cleanup();
        modelManager?.Cleanup();
    }

    #endregion

    #region Main Methods

    /// <summary>
    /// Updates the stats display, showing normal, buffed, or debuffed values.
    /// </summary>
    public void UpdateStat(StatType statType, int baseValue, int currentValue)
    {
        if (statType == StatType.Health)
        {
            // Handle health separately since it has two UI elements
            if (currentValue > 30)
            {
                healthyHealthText.gameObject.SetActive(true);
                dangerHealthText.gameObject.SetActive(false);
                healthyHealthText.text = $"{currentValue}";
            }
            else
            {
                healthyHealthText.gameObject.SetActive(false);
                dangerHealthText.gameObject.SetActive(true);
                dangerHealthText.text = $"{currentValue}";
            }
            return; // Exit early, since health doesn't have buffed/debuffed states
        }

        TextMeshProUGUI normalText = null, buffedText = null, debuffedText = null;

        switch (statType)
        {
            case StatType.Strength:
                normalText = strengthNormalText;
                buffedText = strengthBuffedText;
                debuffedText = strengthDebuffedText;
                break;

            case StatType.Speed:
                normalText = speedNormalText;
                buffedText = speedBuffedText;
                debuffedText = speedDebuffedText;
                break;
        }

        if (normalText == null || buffedText == null || debuffedText == null) return;

        if (currentValue > baseValue)
        {
            // Buffed
            normalText.gameObject.SetActive(false);
            buffedText.gameObject.SetActive(true);
            debuffedText.gameObject.SetActive(false);
            buffedText.text = $"{currentValue}";
        }
        else if (currentValue < baseValue)
        {
            // Debuffed
            normalText.gameObject.SetActive(false);
            buffedText.gameObject.SetActive(false);
            debuffedText.gameObject.SetActive(true);
            debuffedText.text = $"{currentValue}";
        }
        else
        {
            // Normal
            normalText.gameObject.SetActive(true);
            buffedText.gameObject.SetActive(false);
            debuffedText.gameObject.SetActive(false);
            normalText.text = $"{currentValue}";
        }
    }

    /// <summary>
    /// Updates the cooldown text and fillers for a given skill type.
    /// </summary>
    public void UpdateSkillCooldown(SkillType skillType, float cooldown)
    {
        switch (skillType)
        {
            case SkillType.BallSkill:
                if (ballSkillCoroutine != null)
                {
                    StopCoroutine(ballSkillCoroutine);
                }
                ballSkillReadyIndicator.gameObject.SetActive(false); // Reset the ready indicator
                ballSkillCoroutine = StartCoroutine(UpdateCooldownFiller(ballSkillCooldownFiller, cooldown, ballSkillReadyIndicator, ballSkillCooldownText));
                break;

            case SkillType.WeaponSkill:
                if (weaponSkillCoroutine != null)
                {
                    StopCoroutine(weaponSkillCoroutine);
                }
                weaponSkillReadyIndicator.gameObject.SetActive(false); // Reset the ready indicator
                weaponSkillCoroutine = StartCoroutine(UpdateCooldownFiller(weaponSkillCooldownFiller, cooldown, weaponSkillReadyIndicator, weaponSkillCooldownText));
                break;
        }
    }

    /// <summary>
    /// Displays a floating damage text UI element on the player's screen.
    /// </summary>
    public void ShowFloatingDamage(Vector3 position, int damage)
    {
        // Create a floating damage text instance
        GameObject damageTextInstance = Instantiate(floatingDamageTextPrefab, floatingDamageTextsParent);

        // Set the text
        TextMeshProUGUI textComponent = damageTextInstance.GetComponent<TextMeshProUGUI>();
        textComponent.text = damage.ToString();

        // Animate the text and destroy it after a short delay
        StartCoroutine(AnimateFloatingDamage(damageTextInstance));
    }

    /// <summary>
    /// Updates the stamina bar UI based on current and max stamina values.
    /// </summary>
    /// <param name="currentStamina">The player's current stamina.</param>
    /// <param name="maxStamina">The player's maximum stamina.</param>
    public void UpdateStaminaBar(float currentStamina, float maxStamina)
    {
        float value = Mathf.Clamp01(currentStamina / maxStamina);
        staminaBarSlider.value = value;

        staminaText.text = $"{Mathf.FloorToInt(currentStamina)} / {Mathf.FloorToInt(maxStamina)}";
    }

    #endregion

    #region Goal Announcement Logic

    public void ShowGoalAnnouncement(string playerName, int teamIndex, bool isOwnGoal)
    {
        if (goalAnnouncementCoroutine != null)
            StopCoroutine(goalAnnouncementCoroutine);

        goalAnnouncementCoroutine = StartCoroutine(ShowGoalTextCoroutine(playerName, teamIndex, isOwnGoal));
    }

    private IEnumerator ShowGoalTextCoroutine(string playerName, int teamIndex, bool isOwnGoal)
    {
        // Determine color for the player name
        Color playerColor = neutralColor;
        switch (teamIndex)
        {
            case 0:
                playerColor = blueTeamColor;
                break;
            case 1:
                playerColor = redTeamColor;
                break;
        }

        // Convert color to HEX
        string colorHex = ColorUtility.ToHtmlStringRGB(playerColor);

        // Build the final text
        string playerNameColored = $"<color=#{colorHex}>{playerName}</color>";
        string actionText = isOwnGoal ? ownGoalScoredText : goalScoredText;

        goalAnnouncementText.text = $"{playerNameColored} {actionText}";
        goalAnnouncementText.color = Color.white; // Overall color stays white

        // Show and Fade In
        goalAnnouncementCanvasGroup.alpha = 1f;

        yield return new WaitForSeconds(3f); // Display duration

        // Fade Out Smoothly (optional small polish)
        float fadeDuration = 1f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            goalAnnouncementCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }

        goalAnnouncementCanvasGroup.alpha = 0f;
    }

    #endregion

    #region Animation Methods

    /// <summary>
    /// Animates the cooldown filler and activates the ready indicator when the cooldown ends.
    /// </summary>
    private IEnumerator UpdateCooldownFiller(Image filler, float cooldownTime, Image readyIndicator, TextMeshProUGUI cooldownText)
    {
        float elapsedTime = 0;

        while (elapsedTime < cooldownTime)
        {
            elapsedTime += Time.deltaTime;

            // Update the fill amount
            filler.fillAmount = elapsedTime / cooldownTime;

            // Update the cooldown text
            float remainingTime = cooldownTime - elapsedTime;
            cooldownText.text = $"{Mathf.Max(0, remainingTime):F1}";

            yield return null;
        }

        filler.fillAmount = 1; // Ensure the filler is full when cooldown ends
        cooldownText.text = ""; // Reset text to indicate cooldown is over
        readyIndicator.gameObject.SetActive(true); // Show the ready indicator
    }

    /// <summary>
    /// Animates the floating damage text, making it move and fade out.
    /// </summary>
    private IEnumerator AnimateFloatingDamage(GameObject damageTextInstance)
    {
        RectTransform rectTransform = damageTextInstance.GetComponent<RectTransform>();
        Vector3 startPosition = rectTransform.localPosition; // Starting position
        Vector3 endPosition = startPosition + new Vector3(0, 50f, 0); // Move upwards

        float duration = 1f; // Animation duration
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // Interpolate position
            rectTransform.localPosition = Vector3.Lerp(startPosition, endPosition, t);

            // Optionally, fade out text
            CanvasGroup canvasGroup = damageTextInstance.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1 - t; // Gradually fade out
            }

            yield return null;
        }

        // Destroy the floating damage text after animation
        Destroy(damageTextInstance);
    }

    #endregion

    #region Force UI Update JIC
    public void ForceCooldownComplete(SkillType skillType)
    {
        Debug.Log("Player UI Controller : inside the Force Cooldown Complete");
        switch (skillType)
        {
            case SkillType.BallSkill:
                if (ballSkillCoroutine != null) StopCoroutine(ballSkillCoroutine);
                ballSkillCooldownText.text = "";
                ballSkillCooldownFiller.fillAmount = 1f;
                ballSkillReadyIndicator.gameObject.SetActive(true);
                break;

            case SkillType.WeaponSkill:
                if (weaponSkillCoroutine != null) StopCoroutine(weaponSkillCoroutine);
                weaponSkillCooldownText.text = "";
                weaponSkillCooldownFiller.fillAmount = 1f;
                weaponSkillReadyIndicator.gameObject.SetActive(true);
                break;
        }
    }

    public void ForceHealthToZero()
    {
        healthyHealthText.gameObject.SetActive(false);
        dangerHealthText.gameObject.SetActive(true);
        dangerHealthText.text = "0";
    }
    #endregion

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}