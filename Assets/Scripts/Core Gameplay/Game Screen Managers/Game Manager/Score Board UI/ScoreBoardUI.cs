using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Unity.Netcode;

public class ScoreboardUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject playerRowPrefab;
    [Space]
    [SerializeField] private Transform redTeamContainer;
    [SerializeField] private Transform blueTeamContainer;
    [Space]
    [SerializeField] private TMP_Text redTeamScoreText;
    [SerializeField] private TMP_Text blueTeamScoreText;
    [Space]
    [SerializeField] private TMP_Text redTeamAccidentalText;
    [SerializeField] private TMP_Text blueTeamAccidentalText;

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

        // ✅ Get stat objects from ScoreboardManager’s tracked list
        List<PlayerStatSync> allStats = ScoreboardManager.Instance.GetAllClientStats();
        ulong localId = NetworkManager.Singleton.LocalClientId;

        foreach (var stat in allStats)
        {
            if (!stat.IsSpawned) continue;

            ulong clientId = stat.BoundClientId.Value;
            int teamIndex = stat.TeamIndex.Value;
            int characterId = stat.CharacterId.Value;

            Transform parent = (teamIndex == 0) ? blueTeamContainer : redTeamContainer;

            GameObject rowObj = Instantiate(playerRowPrefab, parent);
            PlayerStatRow row = rowObj.GetComponent<PlayerStatRow>();

            bool isLocalPlayer = (clientId == localId);
            int totalScore = CalculateTotalScore(stat);

            row.SetData(
                stat.PlayerName.Value.ToString(),
                stat.Goals.Value,
                stat.Kills.Value,
                stat.Deaths.Value,
                stat.Assists.Value,
                stat.Saves.Value,
                isLocalPlayer,
                characterId,
                totalScore
            );
        }

        redTeamScoreText.text = $"{GameManager.Instance.RedTeamScore.Value}";
        blueTeamScoreText.text = $"{GameManager.Instance.BlueTeamScore.Value}";

        redTeamAccidentalText.text = $"Own G: {GameManager.Instance.GetAccidentalGoalCount(Team.Red)}";
        blueTeamAccidentalText.text = $"Own G: {GameManager.Instance.GetAccidentalGoalCount(Team.Blue)}";
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
