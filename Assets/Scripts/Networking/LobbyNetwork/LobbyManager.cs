using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{

    public static LobbyManager Instance { get; private set; }

    public const string KEY_PLAYER_NAME = "PlayerName";
    public const string KEY_PLAYER_TEAM = "Team";
    public const string KEY_GAME_MODE = "GameMode";
    public const string MATCHMAKING_STATE = "MatchmakingState";

    public Action<string> OnJoinLobbyByCodeSuccess;
    public Action<string> OnJoinLobbyByCodeFailure;

    public Action OnLeftLobby;
    public Action<Lobby> OnMatchmakingStatusChanged;
    public Action<Lobby> OnJoinedLobby;
    public Action<Lobby> OnJoinedLobbyUpdate;
    public Action<Lobby> OnKickedFromLobby;
    public Action<Lobby> OnLobbyGameModeChanged;
    public Action<List<Lobby>> OnLobbyListChanged;

    public enum LobbyGameMode
    {
        SkillGameMode,
        CoreGameMode,
        Training
    }

    public enum PlayerStatus
    {
        Başlangıç,
        Acemi,
        Usta,
        Uzman
    }

    private float heartbeatTimer;
    private float lobbyPollTimer;
    private float refreshLobbyListTimer = 5f;
    private Lobby joinedLobby;
    private string playerName = "Yusuf";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        //HandleRefreshLobbyList(); // Disabled Auto Refresh for testing with multiple builds
        HandleLobbyHeartbeat();
        HandleLobbyPolling();
    }

    private void HandleLobbyHeartbeat()
    {
        if (IsLobbyHost())
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f)
            {
                float heartbeatTimerMax = 15f;
                heartbeatTimer = heartbeatTimerMax;

                Debug.Log("Heartbeat");
                LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            }
        }
    }

    private async void HandleLobbyPolling()
    {
        if (joinedLobby != null)
        {
            lobbyPollTimer -= Time.deltaTime;
            if (lobbyPollTimer < 0f)
            {
                float lobbyPollTimerMax = 1.1f;
                lobbyPollTimer = lobbyPollTimerMax;

                joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                OnJoinedLobbyUpdate?.Invoke(joinedLobby);

                Debug.Log($"Host ID: {joinedLobby.HostId}, Player ID: {AuthenticationService.Instance.PlayerId}");

                DebugLobby(joinedLobby);

                if (!IsPlayerInLobby())
                {
                    Debug.Log("Kicked from Lobby!");
                    OnKickedFromLobby?.Invoke(joinedLobby);
                    joinedLobby = null;
                }
            }
        }
    }

    public Lobby GetJoinedLobby()
    {
        return joinedLobby;
    }

    public bool IsInLobby()
    {
        return Instance != null && Instance.GetJoinedLobby() != null;
    }

    public bool IsLobbyHost()
    {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    private bool IsPlayerInLobby()
    {
        if (joinedLobby != null && joinedLobby.Players != null)
        {
            foreach (Player player in joinedLobby.Players)
            {
                if (player.Id == AuthenticationService.Instance.PlayerId)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private Player GetPlayer(Lobby lobby)
    {
        var assignedTeam = TeamUtils.PlayerTeam.Blue;

        // Check if a lobby exists and count current players
        if (lobby != null && lobby.Players != null)
        {
            Debug.Log($"[GetPlayer] Lobby exists! Current player count: {lobby.Players.Count}");

            assignedTeam = (lobby.Players.Count % 2 == 0) ? TeamUtils.PlayerTeam.Blue : TeamUtils.PlayerTeam.Red;
        }
        else
        {
            Debug.LogWarning("[GetPlayer] Lobby is NULL! Assigning default Blue team.");
        }

        Debug.Log($"[GetPlayer] Assigned Team: {assignedTeam}");

        return new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject>
                {
                    { KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName) },
                    { KEY_PLAYER_TEAM, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, TeamUtils.TeamToString(assignedTeam)) }
                });
    }


    public LobbyGameMode GetLobbyGameMode()
    {
        if (joinedLobby == null) return LobbyGameMode.SkillGameMode; // Default or fallback mode.
        return Enum.Parse<LobbyGameMode>(joinedLobby.Data[KEY_GAME_MODE].Value);
    }

    public GameMode ConvertLobbyGameModeToGameMode(LobbyGameMode lobbyGameMode)
    {
        // Use a switch statement to map LobbyGameMode to GameMode
        switch (lobbyGameMode)
        {
            case LobbyGameMode.SkillGameMode:
                return GameMode.SKillGameMode;
            case LobbyGameMode.CoreGameMode:
                return GameMode.CoreGameMode;
            case LobbyGameMode.Training:
                return GameMode.Training;
            default:
                throw new ArgumentOutOfRangeException(nameof(lobbyGameMode), lobbyGameMode, null);
        }
    }

    public GameMode GetAndConvertLobbyGameModeToGameMode()
    {
        LobbyGameMode lobbyGameMode = GetLobbyGameMode();

        // Use a switch statement to map LobbyGameMode to GameMode
        switch (lobbyGameMode)
        {
            case LobbyGameMode.SkillGameMode:
                return GameMode.SKillGameMode;
            case LobbyGameMode.CoreGameMode:
                return GameMode.CoreGameMode;
            case LobbyGameMode.Training:
                return GameMode.Training;
            default:
                throw new ArgumentOutOfRangeException(nameof(lobbyGameMode), lobbyGameMode, null);
        }
    }

    public void ChangeGameMode()
    {
        if (IsLobbyHost())
        {
            LobbyGameMode gameMode = Enum.Parse<LobbyGameMode>(joinedLobby.Data[KEY_GAME_MODE].Value);

            gameMode = gameMode == LobbyGameMode.SkillGameMode ? LobbyGameMode.CoreGameMode : LobbyGameMode.SkillGameMode;

            UpdateLobbyGameMode(gameMode);
        }
    }

    public async void CreateLobby(string lobbyName, int maxPlayers, bool isPrivate, LobbyGameMode gameMode)
    {
        try
        {
            Player player = GetPlayer(null); // Replace with your logic for fetching player data
            Debug.Log("Player : " + JsonUtility.ToJson(player, true));

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                Player = player,
                IsPrivate = isPrivate,
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Public, gameMode.ToString()) },
                    { MATCHMAKING_STATE, new DataObject(DataObject.VisibilityOptions.Public, false.ToString()) }, // Initialize MatchmakingState
                    { "MatchResult", new DataObject(DataObject.VisibilityOptions.Public, MatchmakerPollingResult.MatchAssignmentError.ToString()) }

                }
            };

            lobbyName = player.Id;


            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            Debug.Log($"Lobby '{lobby.Name}' created with mode '{gameMode}'. Lobby's privecy {isPrivate} with max {maxPlayers}");

            // Assign to joinedLobby
            joinedLobby = lobby;

            OnJoinedLobby?.Invoke(lobby);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create lobby: {e.Message}");
        }
    }

    public async void RefreshLobbyList()
    {
        try
        {
            QueryResponse lobbyListQueryResponse = await LobbyService.Instance.QueryLobbiesAsync();

            OnLobbyListChanged?.Invoke(lobbyListQueryResponse.Results);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            Debug.Log($"[JoinLobbyByCode] Trying to join lobby with code: {lobbyCode}");

            // Try fetching the lobby first
            Lobby lobby = await LobbyService.Instance.GetLobbyAsync(lobbyCode);

            if (lobby == null)
            {
                Debug.LogError("[JoinLobbyByCode] Failed to fetch lobby. It might not exist.");
                OnJoinLobbyByCodeFailure?.Invoke($"Failed to join lobby: {lobbyCode}");
                return;
            }

            // Get the correct player data
            Player player = GetPlayer(lobby);
            joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, new JoinLobbyByCodeOptions { Player = player });

            OnJoinedLobby?.Invoke(lobby);
            OnJoinLobbyByCodeSuccess?.Invoke($"Successfully joined lobby: {lobby.Name}");
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to join lobby by code: {e}");
            OnJoinLobbyByCodeFailure?.Invoke($"Failed to join lobby: {e.Message}");
        }
    }

    public async void JoinLobby(Lobby lobby)
    {
        try
        {
            Player player = GetPlayer(lobby);
            joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, new JoinLobbyByIdOptions { Player = player });
            Debug.Log($"Player {player} is joined to Lobby '{joinedLobby.Name}' with mode" +
                $" '{joinedLobby.Data["GameMode"].Value}'. Lobby's privecy '{joinedLobby.IsPrivate}'" +
                $" with max '{joinedLobby.MaxPlayers}'");

            OnJoinedLobby?.Invoke(joinedLobby);
        }

        catch(LobbyServiceException e)
        {
            Debug.LogError($"Failed to Join Lobby Directly : {e}");
        }

    }

    public async void JoinLobby(Lobby lobby, Action onJoinLobbySuccess, Action<string> onJoinLobbyFailed)
    {
        try
        {
            Player player = GetPlayer(lobby);
            joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, new JoinLobbyByIdOptions { Player = player });
            Debug.Log($"Player {player} is joined to Lobby '{joinedLobby.Name}' with mode" +
                $" '{joinedLobby.Data["GameMode"].Value}'. Lobby's privecy '{joinedLobby.IsPrivate}'" +
                $" with max '{joinedLobby.MaxPlayers}'");

            OnJoinedLobby?.Invoke(joinedLobby);
        }

        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to Join Lobby Directly : {e}");
        }
    }

    public async void UpdatePlayerName(string playerName)
    {
        this.playerName = playerName;

        if (joinedLobby != null)
        {
            try
            {
                UpdatePlayerOptions options = new UpdatePlayerOptions
                {
                    Data = new Dictionary<string, PlayerDataObject> {
                        { KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName) }
                    }
                };

                string playerId = AuthenticationService.Instance.PlayerId;
                joinedLobby = await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, playerId, options);
                OnJoinedLobbyUpdate?.Invoke(joinedLobby);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    public async void UpdatePlayerTeam(TeamUtils.PlayerTeam newTeam)
    {
        if (joinedLobby == null)
        {
            Debug.LogError("[UpdatePlayerTeam] No lobby joined.");
            return;
        }

        try
        {
            string teamString = TeamUtils.TeamToString(newTeam); // Convert enum to string

            UpdatePlayerOptions options = new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject> {
                { KEY_PLAYER_TEAM, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, teamString) }
            }
            };

            string playerId = AuthenticationService.Instance.PlayerId;
            joinedLobby = await LobbyService.Instance.UpdatePlayerAsync(joinedLobby.Id, playerId, options);

            Debug.Log($"[UpdatePlayerTeam] Player {playerId} switched to {newTeam}");

            OnJoinedLobbyUpdate?.Invoke(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[UpdatePlayerTeam] Failed: {e.Message}");
        }
    }



    public async void MigrateHost(string newHostPlayerId)
    {
        if (!IsLobbyHost())
        {
            Debug.LogWarning("Only the current host can migrate host ownership.");
            return;
        }

        if (joinedLobby == null || string.IsNullOrEmpty(newHostPlayerId))
        {
            Debug.LogError("Cannot migrate host: Invalid lobby or player ID.");
            return;
        }

        try
        {
            // Update the host of the lobby
            joinedLobby = await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                HostId = newHostPlayerId
            });

            Debug.Log($"Host migrated to player: {newHostPlayerId}");

            // Notify listeners about the updated lobby
            OnJoinedLobbyUpdate?.Invoke(joinedLobby);
        }
        catch (LobbyServiceException ex)
        {
            Debug.LogError($"Failed to migrate host: {ex.Message}");
        }
    }


    public async void LeaveLobby()
    {
        if (joinedLobby != null)
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
                joinedLobby = null;

                OnLeftLobby?.Invoke();
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    public async void KickPlayer(string playerId)
    {
        if (IsLobbyHost())
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    public async void UpdateLobbyGameMode(LobbyGameMode gameMode)
    {
        try
        {
            Lobby lobby = await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject> {
                    { KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Public, gameMode.ToString()) }
                }
            });

            joinedLobby = lobby;
            OnLobbyGameModeChanged?.Invoke(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void UpdateLobbyServerDetails(MatchmakerPollingResult result, string ip, int port)
    {
        if (joinedLobby != null)
        {
            try
            {
                await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                {
                    { "MatchResult", new DataObject(DataObject.VisibilityOptions.Public, result.ToString()) },
                    { "ServerIP", new DataObject(DataObject.VisibilityOptions.Member, ip) },
                    { "ServerPort", new DataObject(DataObject.VisibilityOptions.Member, port.ToString()) }
                }
                });

                Debug.Log($"Updated lobby with server details: {ip}:{port}");

                // Trigger event after clearing details
                OnJoinedLobbyUpdate?.Invoke(joinedLobby);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Failed to update lobby with server details: {e.Message}");
            }
        }
    }

    private  void DebugLobby(Lobby lobby)
    {
        // Check if the data contains the keys
        if (lobby.Data.TryGetValue("ServerIP", out DataObject serverIP) &&
            lobby.Data.TryGetValue("ServerPort", out DataObject serverPort))
        {
            // Log the values for debugging
            Debug.Log($"Server IP: {serverIP.Value}");
            Debug.Log($"Server Port: {serverPort.Value}");
        }
        else
        {
            Debug.LogWarning("ServerIP or ServerPort not found in the lobby data.");
        }
    }

    public async void ClearLobbyServerDetails()
    {
        if (joinedLobby != null)
        {
            try
            {
                await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
                {
                    Data = new Dictionary<string, DataObject>
                {
                    { "ServerIP", new DataObject(DataObject.VisibilityOptions.Member, null) },
                    { "ServerPort", new DataObject(DataObject.VisibilityOptions.Member, null) },
                    { "MatchResult", new DataObject(DataObject.VisibilityOptions.Public, MatchmakerPollingResult.MatchAssignmentError.ToString()) }

                }
                });
                Debug.Log("Cleared server details from the lobby.");

                // Trigger event after clearing details
                OnJoinedLobbyUpdate?.Invoke(joinedLobby);
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Failed to clear lobby server details: {e.Message}");
            }
        }
    }

    public async void UpdateMatchmakingState(bool isMatchmaking)
    {
        try
        {
            Lobby lobby = await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject> {
                    { MATCHMAKING_STATE, new DataObject(DataObject.VisibilityOptions.Public, isMatchmaking.ToString()) }
                }
            });

            Debug.Log("We called Update State Event");

            joinedLobby = lobby;
            OnMatchmakingStatusChanged?.Invoke(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
        
    }
}
