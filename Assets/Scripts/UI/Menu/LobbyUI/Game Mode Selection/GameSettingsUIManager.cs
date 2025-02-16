using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameSettingsUIManager : MonoBehaviour
{
    [Header("Referances")]
    [SerializeField] private GameModeManager gameModeManager;

    [Header("UI Panels")]
    [SerializeField] private GameObject gameSettingsPanel; // Main UI panel

    [Header("Game Mode Selection")]
    [SerializeField] private ToggleGroup gameModeToggleGroup; // ToggleGroup for Game Modes
    [SerializeField] private Toggle[] gameModeToggles; // Direct references to toggles
    [SerializeField] private TextMeshProUGUI gameModeExplanationText; // Explanation text

    [Header("Ball Type Selection")]
    [SerializeField] private ToggleGroup ballTypeToggleGroup; // ToggleGroup for Ball Selection
    [SerializeField] private Toggle[] ballTypeToggles;
    [SerializeField] private TextMeshProUGUI ballExplanationText;

    [Header("Map Selection")]
    [SerializeField] private ToggleGroup mapToggleGroup; // ToggleGroup for Map Selection
    [SerializeField] private Toggle[] mapToggles;
    [SerializeField] private TextMeshProUGUI mapExplanationText; // Explanation text

    [Header("Player Amount Selection")]
    [SerializeField] private Slider playerAmountSlider;
    [SerializeField] private TextMeshProUGUI playerAmountText; // Text to show current value

    [Header("Explanations")]
    [SerializeField] private string[] gameModeExplanations; // Descriptions for each game mode
    [SerializeField] private string[] ballExplanations; // Descriptions for each ball type
    [SerializeField] private string[] mapExplanations; // Static map explanation

    [Header("Buttons")]
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button cancelButton; 

    private void Start()
    {
        cancelButton.onClick.AddListener(CloseGameSettingsUI);
        createLobbyButton.onClick.AddListener(CreateLobbyWithSettings);
        gameSettingsPanel.SetActive(false); // Hide UI at start

        // Default Player Amount
        playerAmountSlider.minValue = 2;
        playerAmountSlider.maxValue = 12;
        playerAmountSlider.value = 2;
        playerAmountSlider.wholeNumbers = true;
        playerAmountSlider.onValueChanged.AddListener(UpdatePlayerAmountText);

        UpdatePlayerAmountText(2);
    }

    private void CreateLobbyWithSettings()
    {

    }

    // 🎮 Open the Game Settings UI when clicking "Create Game"
    public void OpenGameSettingsUI()
    {
        gameSettingsPanel.SetActive(true);

        // Apply pre-selected Game Mode from GameModeManager
        int preSelectedIndex = gameModeManager.GetSelectedIndex();

        if (preSelectedIndex >= 0 && preSelectedIndex < gameModeToggles.Length)
        {
            gameModeToggles[preSelectedIndex].isOn = true;
            UpdateGameModeExplanation(preSelectedIndex);
        }
        else
        {
            gameModeToggles[0].isOn = true; // Default selection
            UpdateGameModeExplanation(0);
        }

        // Default Ball Type
        ballTypeToggles[0].isOn = true;
        UpdateBallExplanation(0);

        // Map is always selected (since only 1 exists)
        mapToggles[0].isOn = true;
        UpdateMapExplanation(0);


    }

    public void CloseGameSettingsUI()
    {
        gameSettingsPanel.SetActive(false);
        ResetGameSettings();
    }

    // 🎮 Game Mode Toggle Click Event
    public void UpdateGameModeExplanation(int index)
    {
        gameModeExplanationText.text = gameModeExplanations[index];
    }

    // ⚽ Ball Selection Toggle Click Event
    public void UpdateBallExplanation(int index)
    {
        ballExplanationText.text = ballExplanations[index];
    }

    public void UpdateMapExplanation(int index)
    {
        mapExplanationText.text = mapExplanations[index];
    }

    // 🔢 Player Amount Slider Logic (Only Even Numbers)
    public void UpdatePlayerAmountText(float playerAmount)
    {
        // Ensure only even numbers (2, 4, 6, 8, ... up to 12)
        int roundedValue = Mathf.RoundToInt(playerAmount);
        if (roundedValue % 2 != 0) roundedValue++; // Ensure even number

        playerAmountSlider.SetValueWithoutNotify(roundedValue);

        int teamSize = roundedValue / 2; // Calculate number of players per team
        playerAmountText.text = $"{teamSize}v{teamSize}"; // Format as "1v1", "2v2", etc.
    }

    public void ResetGameSettings()
    {
        // Reset all toggles to first option
        if (gameModeToggles.Length > 0)
        {
            gameModeToggles[0].isOn = true;
            UpdateGameModeExplanation(0);
        }

        if (ballTypeToggles.Length > 0)
        {
            ballTypeToggles[0].isOn = true;
            UpdateBallExplanation(0);
        }

        if (mapToggles.Length > 0)
        {
            mapToggles[0].isOn = true;
            UpdateMapExplanation(0);
        }

        // Reset Player Amount
        playerAmountSlider.SetValueWithoutNotify(2);
        UpdatePlayerAmountText(2);
    }

}
