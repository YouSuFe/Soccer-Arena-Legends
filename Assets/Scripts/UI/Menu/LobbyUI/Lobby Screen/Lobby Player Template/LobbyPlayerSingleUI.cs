using UnityEngine;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine.UI;

public class LobbyPlayerSingleUI : MonoBehaviour
{
    [Header("Team Themes")]
    [SerializeField] private TeamTheme blueTeamTheme;
    [SerializeField] private TeamTheme redTeamTheme;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI playerNameText;

    [Header("Buttons")]
    [SerializeField] private Button kickPlayerButton;
    [SerializeField] private Button migrateHostButton;

    [Header("Images")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Image kickButtonImage;

    private Player player;

    private void Awake()
    {
        kickPlayerButton.onClick.AddListener(KickPlayer);
        migrateHostButton.onClick.AddListener(MigrateHost);
    }

    public void SetKickPlayerandMigrateButtonsVisible(bool visible)
    {
        kickPlayerButton.gameObject.SetActive(visible);
        migrateHostButton.gameObject.SetActive(visible);
    }

    public void UpdatePlayer(Player player, GameEnumsUtil.PlayerTeam team)
    {
        this.player = player;

        if (player.Data.TryGetValue(LobbyManager.KEY_PLAYER_NAME, out var nameData))
        {
            playerNameText.text = nameData.Value;
        }

        ApplyTeamTheme(team);
    }


    private void ApplyTeamTheme(GameEnumsUtil.PlayerTeam team)
    {
        TeamTheme theme = team == GameEnumsUtil.PlayerTeam.Blue ? blueTeamTheme : redTeamTheme;

        backgroundImage.color = theme.backgroundColor;
        backgroundImage.sprite = theme.backgroundIcon;
        kickButtonImage.sprite = theme.kickButtonIcon;
        kickButtonImage.color = theme.kickButtonColor;
    }

    private void KickPlayer()
    {
        if (player != null)
        {
            LobbyManager.Instance.KickPlayer(player.Id);
        }
    }

    private void MigrateHost()
    {
        if (player != null)
        {
            LobbyManager.Instance.MigrateHost(player.Id);
        }
    }
}
