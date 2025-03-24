using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Unity.Netcode;

public class ScoreboardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject playerRowPrefab;
    [SerializeField] private Transform redTeamContainer;
    [SerializeField] private Transform blueTeamContainer;
    [SerializeField] private TMP_Text redTeamScoreText;
    [SerializeField] private TMP_Text blueTeamScoreText;

    private void Start()
    {
        Refresh();

        // To allow properly call Refresh when game first starts, we leave object active in the scene
        // then we make it inactive here.
        gameObject.SetActive(false);
    }

    public void Refresh()
    {
        ClearContainers();

        // Get all stats from GameManager (bound by clientId)
        Dictionary<ulong, PlayerStatSync> allStats = GameManager.Instance.GetAllBoundStats();
        ulong localId = NetworkManager.Singleton.LocalClientId;

        foreach (var kvp in allStats)
        {
            ulong clientId = kvp.Key;
            PlayerStatSync stats = kvp.Value;

            // Retrieve the corresponding UserData from the PlayerSpawnManager
            UserData userData = PlayerSpawnManager.Instance.GetUserData(clientId);
            if (userData == null)
                continue;

            // Determine which container to place the row based on team index.
            // (Assuming teamIndex 0 = Blue and 1 = Red)
            Transform parent = (userData.teamIndex == 0) ? blueTeamContainer : redTeamContainer;

            GameObject rowObj = Instantiate(playerRowPrefab, parent);
            PlayerStatRow row = rowObj.GetComponent<PlayerStatRow>();


            bool isLocalPlayer = (clientId == localId);
            int totalScore = CalculateTotalScore(stats);

            // Pass the characterId from userData into SetData (to look up the icon)
            row.SetData(
                stats.PlayerName.Value.ToString(),
                stats.Goals.Value,
                stats.Kills.Value,
                stats.Deaths.Value,
                stats.Assists.Value,
                stats.Saves.Value,
                isLocalPlayer,
                userData.characterId,
                totalScore
            );
        }

        // Update team scores from GameManager’s NetworkVariables.
        redTeamScoreText.text = $"{GameManager.Instance.RedTeamScore.Value}";
        blueTeamScoreText.text = $"{GameManager.Instance.BlueTeamScore.Value}";
    }

    // ToDo : Calculate really how much score we are making.
    private int CalculateTotalScore(PlayerStatSync stats)
    {
        // 🎯 You can customize this formula
        return stats.Goals.Value * 5 + stats.Kills.Value * 3 + stats.Assists.Value * 2 + stats.Saves.Value * 2 - stats.Deaths.Value;
    }

    private void ClearContainers()
    {
        foreach (Transform child in redTeamContainer)
            Destroy(child.gameObject);
        foreach (Transform child in blueTeamContainer)
            Destroy(child.gameObject);
    }

    public void ToggleVisibility(bool show)
    {
        gameObject.SetActive(show);
        if (show)
            Refresh();
    }
}
