using System.Collections;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyGameSettingsUI : MonoBehaviour
{
    [Header("Dropdowns")]
    [SerializeField] private TMP_Dropdown gameModeDropdown;
    [SerializeField] private TMP_Dropdown regionDropdown;
    [SerializeField] private TMP_Dropdown ballTypeDropdown;
    [SerializeField] private TMP_Dropdown mapDropdown;
    [SerializeField] private TMP_Dropdown playerAmountDropdown;

    [Header("Warning Popup")]
    [SerializeField] private PopupMessageUI popupMessageUI;

    private void Start()
    {
        // ✅ Listen for lobby updates
        LobbyManager.Instance.OnJoinedLobby += OnLobbyUpdated;
        LobbyManager.Instance.OnJoinedLobbyUpdate += OnLobbyUpdated;
    }

    private void OnLobbyUpdated(Lobby lobby)
    {
        if (lobby == null)
        {
            Debug.LogError("[LobbyGameSettingsUI] Lobby is NULL! Cannot update settings UI.");
            return;
        }

        SetDropdownInteractivity();
        LoadLobbySettings();
    }

    private void SetDropdownInteractivity()
    {
        bool isHost = LobbyManager.Instance.IsLobbyHost();
        Debug.Log($"[LobbyGameSettingsUI] Is Host: {isHost}");

        gameModeDropdown.interactable = isHost;
        regionDropdown.interactable = isHost;
        ballTypeDropdown.interactable = isHost;
        mapDropdown.interactable = isHost;
        playerAmountDropdown.interactable = isHost;
    }

    public void LoadLobbySettings()
    {
        Lobby lobby = LobbyManager.Instance.GetJoinedLobby();
        if (lobby == null) return;

        // ✅ Temporarily disable event listeners
        DisableDropdownListeners();

        // ✅ Set dropdown values based on lobby data
        gameModeDropdown.value = (int)GameEnumsUtil.StringToEnum(lobby.Data[LobbyManager.KEY_GAME_MODE].Value, GameEnumsUtil.GameMode.SkillGameMode);
        regionDropdown.value = (int)GameEnumsUtil.StringToEnum(lobby.Data[LobbyManager.KEY_REGION].Value, GameEnumsUtil.Region.Europe);
        ballTypeDropdown.value = (int)GameEnumsUtil.StringToEnum(lobby.Data[LobbyManager.KEY_BALL_TYPE].Value, GameEnumsUtil.BallType.DefaultBall);
        mapDropdown.value = (int)GameEnumsUtil.StringToEnum(lobby.Data[LobbyManager.KEY_MAP].Value, GameEnumsUtil.Map.StadiumMap);
        int maxPlayers = int.Parse(lobby.Data[LobbyManager.KEY_MAX_PLAYERS].Value);
        playerAmountDropdown.value = (maxPlayers / 2) - 1; // Convert 2,4,6,8,10,12 → dropdown index 0,1,2,3,4,5

        // ✅ Re-enable event listeners
        EnableDropdownListeners();
    }

    private void DisableDropdownListeners()
    {
        gameModeDropdown.onValueChanged.RemoveListener(OnGameModeChanged);
        regionDropdown.onValueChanged.RemoveListener(OnRegionChanged);
        ballTypeDropdown.onValueChanged.RemoveListener(OnBallTypeChanged);
        mapDropdown.onValueChanged.RemoveListener(OnMapChanged);
        playerAmountDropdown.onValueChanged.RemoveListener(OnPlayerAmountChanged);
    }

    private void EnableDropdownListeners()
    {
        gameModeDropdown.onValueChanged.AddListener(OnGameModeChanged);
        regionDropdown.onValueChanged.AddListener(OnRegionChanged);
        ballTypeDropdown.onValueChanged.AddListener(OnBallTypeChanged);
        mapDropdown.onValueChanged.AddListener(OnMapChanged);
        playerAmountDropdown.onValueChanged.AddListener(OnPlayerAmountChanged);
    }

    public void OnGameModeChanged(int index)
    {
        if (!LobbyManager.Instance.IsLobbyHost()) return;
        GameEnumsUtil.GameMode newGameMode = (GameEnumsUtil.GameMode)index;
        LobbyManager.Instance.UpdateLobbyGameMode(newGameMode);
    }

    public void OnRegionChanged(int index)
    {
        if (!LobbyManager.Instance.IsLobbyHost()) return;
        GameEnumsUtil.Region newRegion = (GameEnumsUtil.Region)index;
        LobbyManager.Instance.UpdateLobbyRegion(newRegion);
    }

    public void OnBallTypeChanged(int index)
    {
        if (!LobbyManager.Instance.IsLobbyHost()) return;
        GameEnumsUtil.BallType newBallType = (GameEnumsUtil.BallType)index;
        LobbyManager.Instance.UpdateLobbyBallType(newBallType);
    }

    public void OnMapChanged(int index)
    {
        if (!LobbyManager.Instance.IsLobbyHost()) return;
        GameEnumsUtil.Map newMap = (GameEnumsUtil.Map)index;
        LobbyManager.Instance.UpdateLobbyMap(newMap);
    }

    public void OnPlayerAmountChanged(int index)
    {
        if (!LobbyManager.Instance.IsLobbyHost()) return;

        int newPlayerAmount = (index + 1) * 2; // Convert dropdown index 0-5 → player amount 2-12
        Debug.Log($"[OnPlayerAmountChanged] Selected index: {index}, Setting new player amount: {newPlayerAmount}");

        if (LobbyManager.Instance.GetJoinedLobby().Players.Count > newPlayerAmount)
        {
            popupMessageUI.Show("Player Limit", "Too many players! Lobby has more players than the selected amount.", PopupMessageType.Error);
            return;
        }

        LobbyManager.Instance.UpdateLobbyPlayerAmount(newPlayerAmount);
    }

    private void OnDestroy()
    {
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.OnJoinedLobbyUpdate -= OnLobbyUpdated;
            LobbyManager.Instance.OnJoinedLobby -= OnLobbyUpdated;
        }
    }
}
