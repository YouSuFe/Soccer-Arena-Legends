
using Unity.Services.Lobbies.Models;

[System.Serializable]
public class FilterData
{
    public string Region;
    public string Map;
    public string GameMode;
    public string MaxPlayers;

    public bool Matches(Lobby lobby)
    {
        if (Region != null && (!lobby.Data.ContainsKey(LobbyManager.KEY_REGION) || lobby.Data[LobbyManager.KEY_REGION].Value != Region))
            return false;

        if (Map != null && (!lobby.Data.ContainsKey(LobbyManager.KEY_MAP) || lobby.Data[LobbyManager.KEY_MAP].Value != Map))
            return false;

        if (GameMode != null && (!lobby.Data.ContainsKey(LobbyManager.KEY_GAME_MODE) || lobby.Data[LobbyManager.KEY_GAME_MODE].Value != GameMode))
            return false;

        if (MaxPlayers != null && (!lobby.Data.ContainsKey(LobbyManager.KEY_MAX_PLAYERS) || lobby.Data[LobbyManager.KEY_MAX_PLAYERS].Value != MaxPlayers))
            return false;

        return true;
    }
}
