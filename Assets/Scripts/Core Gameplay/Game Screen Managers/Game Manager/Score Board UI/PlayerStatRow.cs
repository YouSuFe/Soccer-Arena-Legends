using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerStatRow : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text goalsText;
    [SerializeField] private TMP_Text assistsText;
    [SerializeField] private TMP_Text savesText;
    [SerializeField] private TMP_Text killsText;
    [SerializeField] private TMP_Text deathsText;
    [SerializeField] private TMP_Text totalScoreText;

    [Header("Visuals")]
    [SerializeField] private Image backgroundImage;  // For highlighting the local player
    [SerializeField] private Image characterIconImage; // To show the character icon

    [SerializeField] private Color localPlayerColor = new Color(1f, 1f, 0.5f); // e.g., light yellow
    [SerializeField] private Color defaultColor = Color.white;

    public void SetData(
        string playerName,
        int goals,
        int kills,
        int deaths,
        int assists,
        int saves,
        bool isLocalPlayer,
        int characterId,
        int totalScore)
    {
        playerNameText.text = playerName;
        goalsText.text = goals.ToString();
        killsText.text = kills.ToString();
        deathsText.text = deaths.ToString();
        assistsText.text = assists.ToString();
        savesText.text = saves.ToString();
        totalScoreText.text = totalScore.ToString();

        if (backgroundImage != null)
            backgroundImage.color = isLocalPlayer ? localPlayerColor : defaultColor;

        playerNameText.fontSize = isLocalPlayer ? playerNameText.fontSize + 5 : playerNameText.fontSize;

        // Set character icon from CharacterDatabase via PlayerSpawnManager
        Character character = PlayerSpawnManager.Instance.characterDatabase.GetCharacterById(characterId);
        if (character != null && character.Icon != null)
            characterIconImage.sprite = character.Icon;
    }
}
