using UnityEngine;
using TMPro;
using Unity.Services.Lobbies.Models;

public class LobbyListSingleUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI playersText;
    [SerializeField] private TextMeshProUGUI gameModeText;
    [SerializeField] private TextMeshProUGUI mapText;
    [SerializeField] private TextMeshProUGUI regionText;

    private Lobby lobby;

    public void UpdateLobby(Lobby lobby)
    {
        this.lobby = lobby;

        lobbyNameText.text = lobby.Name;

        if (lobby.Data.TryGetValue(LobbyManager.KEY_MAX_PLAYERS, out var maxPlayersData))
        {
            playersText.text = $"{lobby.Players.Count}/{maxPlayersData.Value}";
        }
        else
        {
            playersText.text = $"{lobby.Players.Count}/?";
        }

        if (lobby.Data.TryGetValue(LobbyManager.KEY_GAME_MODE, out var gameMode))
        {
            gameModeText.text = gameMode.Value;
        }

        if (lobby.Data.TryGetValue(LobbyManager.KEY_MAP, out var map))
        {
            mapText.text = map.Value;
        }

        if (lobby.Data.TryGetValue(LobbyManager.KEY_REGION, out var region))
        {
            regionText.text = region.Value;
        }

        // Assign lobby to SelectableLobbiesListUI
        SelectableLobbiesListUI selectableUI = GetComponent<SelectableLobbiesListUI>();
        if (selectableUI != null)
        {
            selectableUI.SetLobby(lobby);
        }
    }
}
