using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyScreenUI : MonoBehaviour
{
    public static LobbyScreenUI Instance { get; private set; }

    [Header("Player Template UI")]
    [SerializeField] private GameObject playerSingleTemplate;
    [SerializeField] private Transform blueTeamContanier;
    [SerializeField] private Transform redTeamContanier;

    [Header("Texts")]
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI gameModeText;
    [SerializeField] private TextMeshProUGUI joinCodeText;

    private LobbyGameSettingsUI gameSettingsUI;

    private void Awake()
    {
        Instance = this;

        playerSingleTemplate.gameObject.SetActive(false);
    }

    private void Start()
    {
        gameSettingsUI = GetComponent<LobbyGameSettingsUI>();

        Debug.Log("[LobbyScreenUI] Subscribing to LobbyManager events...");

        LobbyManager.Instance.OnJoinedLobby += UpdateLobby_Event;
        LobbyManager.Instance.OnJoinedLobbyUpdate += UpdateLobby_Event;
        LobbyManager.Instance.OnLeftLobby += LobbyManager_OnLeftLobby;
        LobbyManager.Instance.OnKickedFromLobby += LobbyManager_OnLeftLobby;

        Hide();
    }

    private void LobbyManager_OnLeftLobby()
    {
        ClearLobby();
        Hide();
    }

    private void LobbyManager_OnLeftLobby(Lobby lobby)
    {
        ClearLobby();
        Hide();
    }

    private void UpdateLobby_Event(Lobby lobby)
    {
        UpdateLobby();

        OnGameStartWithLobby(lobby);
    }

    private void UpdateLobby()
    {
        UpdateLobby(LobbyManager.Instance.GetJoinedLobby());
    }

    private void UpdateLobby(Lobby lobby)
    {
        if (lobby == null)
        {
            Debug.LogError("Lobby is null!");
            return;
        }

        if (lobby.Data == null)
        {
            Debug.LogError("Lobby.Data is null!");
            return;
        }

        if (!lobby.Data.ContainsKey(LobbyManager.KEY_GAME_MODE))
        {
            Debug.LogError($"Lobby.Data does not contain key '{LobbyManager.KEY_GAME_MODE}'!");
            return;
        }

        ClearLobby();

        foreach (Player player in lobby.Players)
        {
            // Determine the player's team
            GameEnumsUtil.PlayerTeam playerTeam = player.Data.ContainsKey("Team")
                ? GameEnumsUtil.StringToEnum(player.Data["Team"].Value, GameEnumsUtil.PlayerTeam.Blue)
                : GameEnumsUtil.PlayerTeam.Blue; // Default to Blue

            // Choose the correct container based on the team
            Transform teamContainer = playerTeam == GameEnumsUtil.PlayerTeam.Blue ? blueTeamContanier : redTeamContanier;

            // Instantiate player UI in the correct team container
            GameObject playerSingleTransform = Instantiate(playerSingleTemplate, teamContainer);
            playerSingleTransform.SetActive(true);

            LobbyPlayerSingleUI lobbyPlayerSingleUI = playerSingleTransform.GetComponent<LobbyPlayerSingleUI>();

            lobbyPlayerSingleUI.SetKickPlayerandMigrateButtonsVisible(
                LobbyManager.Instance.IsLobbyHost() &&
                player.Id != AuthenticationService.Instance.PlayerId
            );

            lobbyPlayerSingleUI.UpdatePlayer(player, playerTeam);
        }

        // ✅ Load game settings into dropdowns when lobby updates
        gameSettingsUI.LoadLobbySettings();

        // ✅ Update UI Text
        lobbyNameText.text = lobby.Name;
        gameModeText.text = lobby.Data[LobbyManager.KEY_GAME_MODE].Value;
        joinCodeText.text = lobby.LobbyCode;

        Debug.Log($"Processing lobby: {lobbyNameText.text} {gameModeText.text} {joinCodeText.text}");

        Show();
    }

    private async void OnGameStartWithLobby(Lobby lobby)
    {
        if (lobby.Data.ContainsKey("RelayJoinCode"))
        {
            string joinCode = lobby.Data["RelayJoinCode"].Value;
            Debug.Log($"[LobbyScreenUI] Relay Join Code received: {joinCode}");

            if (!LobbyManager.Instance.IsLobbyHost())
            {
                if (!string.IsNullOrEmpty(joinCode))
                {
                    Debug.Log("[LobbyScreenUI] Client detected, starting connection...");
                    await ClientSingleton.Instance.GameManager.StartClientAsync(joinCode);
                }
                else
                {
                    Debug.LogWarning("[LobbyScreenUI] Relay Join Code is empty or null, client will not connect.");
                }
            }
            else
            {
                Debug.Log("[LobbyScreenUI] This is the host, skipping client start.");
            }
        }
        else
        {
            Debug.LogWarning("[LobbyScreenUI] Lobby does NOT contain RelayJoinCode!");
        }
    }

    private void ClearLobby()
    {
        foreach (Transform child in blueTeamContanier)
        {
            Debug.Log("Destroying");
            if (child == playerSingleTemplate) continue;
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }

        foreach (Transform child in redTeamContanier)
        {
            if (child == playerSingleTemplate) continue;
            if (child != null)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        // To become ensured there is only one instance of this object
        if (Instance == this)
        {
            Instance = null;
        }

        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnJoinedLobby -= UpdateLobby_Event;
            LobbyManager.Instance.OnJoinedLobbyUpdate -= UpdateLobby_Event;
            LobbyManager.Instance.OnLeftLobby -= LobbyManager_OnLeftLobby;
            LobbyManager.Instance.OnKickedFromLobby -= LobbyManager_OnLeftLobby;
        }
    }
}
