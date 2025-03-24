using TMPro;
using Unity.Netcode;
using UnityEngine;

public class TeamScoreUIController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text redTeamMainScoreText;
    [SerializeField] private TMP_Text blueTeamMainScoreText;

    [SerializeField] private TMP_Text redTeamScoreboardText;
    [SerializeField] private TMP_Text blueTeamScoreboardText;

    private void Start()
    {
        if (!NetworkManager.Singleton.IsClient) return;

        // Subscribe to score updates
        GameManager.Instance.RedTeamScore.OnValueChanged += OnRedScoreChanged;
        GameManager.Instance.BlueTeamScore.OnValueChanged += OnBlueScoreChanged;

        // Set initial values
        OnRedScoreChanged(0, GameManager.Instance.RedTeamScore.Value);
        OnBlueScoreChanged(0, GameManager.Instance.BlueTeamScore.Value);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.RedTeamScore.OnValueChanged -= OnRedScoreChanged;
        GameManager.Instance.BlueTeamScore.OnValueChanged -= OnBlueScoreChanged;
    }

    private void OnRedScoreChanged(int previous, int current)
    {
        string scoreText = $"Red Team - {current}";
        if (redTeamMainScoreText != null) redTeamMainScoreText.text = scoreText;
        if (redTeamScoreboardText != null) redTeamScoreboardText.text = scoreText;
    }

    private void OnBlueScoreChanged(int previous, int current)
    {
        string scoreText = $"Blue Team - {current}";
        if (blueTeamMainScoreText != null) blueTeamMainScoreText.text = scoreText;
        if (blueTeamScoreboardText != null) blueTeamScoreboardText.text = scoreText;
    }
}

