using System;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    #region Constants
    public const string KEY_PLAYER_NAME = "PlayerName";
    public const string KEY_PLAYER_TEAM = "Team";
    public const string KEY_MAP = "Map";
    public const string KEY_REGION = "Region";
    public const string KEY_GAME_MODE = "GameMode";
    public const string KEY_BALL_TYPE = "BallType";
    public const string KEY_MAX_PLAYERS = "MaxPlayers";
    public const string KEY_RELAY_CODE = "RelayJoinCode";
    #endregion

    #region Events
    public Action<string> OnJoinLobbyByCodeSuccess;
    public Action<string> OnJoinLobbyByCodeFailure;

    public Action OnLeftLobby;
    public Action<Lobby> OnJoinedLobby;
    public Action<Lobby> OnJoinedLobbyUpdate;
    public Action<Lobby> OnJoinedLobbyStartsGame;
    public Action<Lobby> OnKickedFromLobby;
    public Action<List<Lobby>> OnLobbyListChanged;
    #endregion

    #region Private Fields
    private float heartbeatTimer;
    private float lobbyPollTimer;
    private float refreshLobbyListTimer = 5f;
    private Lobby joinedLobby;
    private string playerName = "Yusuf";
    #endregion

    #region Initialization
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
        //RefreshLobbyList();
        HandleLobbyHeartbeat();
        HandleLobbyPolling();
    }
    #endregion

    #region Lobby Management

    public async void CreateLobby(string lobbyName, int maxPlayers, bool isPrivate,
                        GameEnumsUtil.Region region,
                        GameEnumsUtil.GameMode gameMode,
                        GameEnumsUtil.BallType ballType,
                        GameEnumsUtil.Map map)
    {
        try
        {
            Player player = GetPlayer(null); // Replace with your logic for fetching player data
            Debug.Log("Player : " + JsonUtility.ToJson(player, true));

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                Player = player,
                IsPrivate = isPrivate, // Change this if needed
                Data = new Dictionary<string, DataObject>
            {
                { KEY_MAP, new DataObject(DataObject.VisibilityOptions.Public, GameEnumsUtil.EnumToString(map)) },
                { KEY_REGION, new DataObject(DataObject.VisibilityOptions.Public, GameEnumsUtil.EnumToString(region)) },
                { KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Public, GameEnumsUtil.EnumToString(gameMode)) },
                { KEY_BALL_TYPE, new DataObject(DataObject.VisibilityOptions.Public, GameEnumsUtil.EnumToString(ballType)) },
                { KEY_MAX_PLAYERS, new DataObject(DataObject.VisibilityOptions.Public, maxPlayers.ToString()) },
            }
            };

            lobbyName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, playerName);


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

    private void HandleLobbyHeartbeat()
    {
        if (joinedLobby == null) return;
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
                float lobbyPollTimerMax = 1.2f;
                try
                {
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
                catch(LobbyServiceException e )
                {
                    if (e.Reason == LobbyExceptionReason.RateLimited)
                    {
                        Debug.LogError($"[HandleLobbyPolling] Rate limit hit! {e.Message}");
                    }
                }

            }
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

            if (joinedLobby.Data.ContainsKey(KEY_RELAY_CODE))
            {
                string joinCode = joinedLobby.Data[KEY_RELAY_CODE].Value;

                if (!string.IsNullOrEmpty(joinCode) && await HostSingleton.Instance.GameManager.IsRelayJoinCodeValid(joinCode))
                {
                    Debug.Log($"[Client] Game already started! Connecting using Relay Join Code: {joinCode}");
                    await ClientSingleton.Instance.GameManager.StartClientAsync(joinCode);
                }
                else
                {
                    Debug.LogWarning($"[Client] Invalid or expired Relay Join Code: {joinCode}. Cannot join game.");
                }
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to join lobby by code: {e}");
            OnJoinLobbyByCodeFailure?.Invoke($"Failed to join lobby: {e.Message}");
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

            if (joinedLobby.Data.ContainsKey(KEY_RELAY_CODE))
            {
                string joinCode = joinedLobby.Data[KEY_RELAY_CODE].Value;

                if (!string.IsNullOrEmpty(joinCode) && await HostSingleton.Instance.GameManager.IsRelayJoinCodeValid(joinCode))
                {
                    Debug.Log($"[Client] Game already started! Connecting using Relay Join Code: {joinCode}");
                    await ClientSingleton.Instance.GameManager.StartClientAsync(joinCode);
                }
                else
                {
                    Debug.LogWarning($"[Client] Invalid or expired Relay Join Code: {joinCode}. Cannot join game.");
                }
            }
        }

        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to Join Lobby Directly : {e}");
        }
    }

    #endregion

    #region Lobby Updates

    public async void UpdateLobbyPlayerAmount(int newMaxPlayers)
    {
        if (!IsLobbyHost()) return;

        if (joinedLobby.Players.Count > newMaxPlayers)
        {
            Debug.LogError($"Cannot set max players to {newMaxPlayers}. There are already {joinedLobby.Players.Count} players in the lobby.");
            return;
        }

        try
        {
            Debug.Log($"[UpdateLobbyPlayerAmount] Attempting to update lobby max players to: {newMaxPlayers}");

            Lobby lobby = await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
            {
                { KEY_MAX_PLAYERS, new DataObject(DataObject.VisibilityOptions.Public, newMaxPlayers.ToString()) }
            }
            });

            joinedLobby = lobby;
            OnJoinedLobbyUpdate?.Invoke(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to update player amount: {e.Message}");
        }
    }

    public async void UpdateLobbyGameMode(GameEnumsUtil.GameMode gameMode)
    {
        if (!IsLobbyHost()) return;

        try
        {
            Lobby lobby = await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_GAME_MODE, new DataObject(DataObject.VisibilityOptions.Public, GameEnumsUtil.EnumToString(gameMode)) }
                }
            });

            joinedLobby = lobby;
            OnJoinedLobbyUpdate?.Invoke(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to update game mode: {e.Message}");
        }
    }

    public async void UpdateLobbyRegion(GameEnumsUtil.Region region)
    {
        if (!IsLobbyHost()) return;

        try
        {
            Lobby lobby = await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_REGION, new DataObject(DataObject.VisibilityOptions.Public, GameEnumsUtil.EnumToString(region)) }
                }
            });

            joinedLobby = lobby;
            OnJoinedLobbyUpdate?.Invoke(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to update region: {e.Message}");
        }
    }

    public async void UpdateLobbyBallType(GameEnumsUtil.BallType ballType)
    {
        if (!IsLobbyHost()) return;

        try
        {
            Lobby lobby = await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_BALL_TYPE, new DataObject(DataObject.VisibilityOptions.Public, GameEnumsUtil.EnumToString(ballType)) }
                }
            });

            joinedLobby = lobby;
            OnJoinedLobbyUpdate?.Invoke(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to update ball type: {e.Message}");
        }
    }

    public async void UpdateLobbyMap(GameEnumsUtil.Map map)
    {
        if (!IsLobbyHost()) return;

        try
        {
            Lobby lobby = await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_MAP, new DataObject(DataObject.VisibilityOptions.Public, GameEnumsUtil.EnumToString(map)) }
                }
            });

            joinedLobby = lobby;
            OnJoinedLobbyUpdate?.Invoke(joinedLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to update map: {e.Message}");
        }
    }

    public async void UpdateLobbyRelayCode(string relayJoinCode)
    {
        if (!IsLobbyHost()) return;

        try
        {
            string newRelayCode = string.IsNullOrEmpty(relayJoinCode) ? "" : relayJoinCode; // Allow clearing the code

            // Update the lobby with the new Relay Join Code
            Lobby updatedLobby = await LobbyService.Instance.UpdateLobbyAsync(joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
            {
                { KEY_RELAY_CODE, new DataObject(DataObject.VisibilityOptions.Member, newRelayCode) }
            }
            });

            joinedLobby = updatedLobby; // Save updated lobby data

            OnJoinedLobbyUpdate?.Invoke(joinedLobby);
            Debug.Log($"[LobbyManager] Updated Relay Join Code: {relayJoinCode}");

            // Notify all clients that the lobby data changed
            if (!string.IsNullOrEmpty(relayJoinCode))
            {
                Debug.Log("There is a valid relay code");
                OnJoinedLobbyStartsGame?.Invoke(joinedLobby);
            }
            else
            {
                Debug.Log("[LobbyManager] Relay Join Code is empty, NOT invoking event.");
            }

        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[LobbyManager] Failed to update Relay Join Code: {e.Message}");
        }
    }


    #endregion

    #region Utility Methods

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
        if (joinedLobby == null)
        {
            Debug.LogError("[LobbyManager] joinedLobby is NULL!");
            return false;
        }

        return joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
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

    #endregion

    #region Player Lobby
    private Player GetPlayer(Lobby lobby)
    {
        var assignedTeam = GameEnumsUtil.PlayerTeam.Blue;

        // Check if a lobby exists and count current players
        if (lobby != null && lobby.Players != null)
        {
            Debug.Log($"[GetPlayer] Lobby exists! Current player count: {lobby.Players.Count}");

            assignedTeam = (lobby.Players.Count % 2 == 0) ? GameEnumsUtil.PlayerTeam.Blue : GameEnumsUtil.PlayerTeam.Red;
        }
        else
        {
            Debug.LogWarning("[GetPlayer] Lobby is NULL! Assigning default Blue team.");
        }

        Debug.Log($"[GetPlayer] Assigned Team: {assignedTeam}");

        return new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject>
                {
                    { KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName) },
                    { KEY_PLAYER_TEAM, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, GameEnumsUtil.EnumToString(assignedTeam)) }
                });
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

    public async void UpdatePlayerTeam(GameEnumsUtil.PlayerTeam newTeam)
    {
        if (joinedLobby == null)
        {
            Debug.LogError("[UpdatePlayerTeam] No lobby joined.");
            return;
        }

        try
        {
            string teamString = GameEnumsUtil.EnumToString(newTeam); // Convert enum to string

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

    #endregion

    private void DebugLobby(Lobby lobby)
    {
        if (lobby == null)
        {
            Debug.LogError("[DebugLobby] Lobby is NULL!");
            return;
        }

        string debugMessage = $"===== LOBBY DEBUG INFO =====\n" +
                              $"Lobby Name: {lobby.Name}\n" +
                              $"Lobby ID: {lobby.Id}\n" +
                              $"Host ID: {lobby.HostId}\n" +
                              $"Is Private: {lobby.IsPrivate}\n" +
                              $"Max Players: {lobby.Data[KEY_MAX_PLAYERS].Value}\n" + // ✅ Ensure we read the latest value
                              $"Current Players: {lobby.Players.Count}\n" +
                              $"Game Mode: {lobby.Data[KEY_GAME_MODE].Value}\n" +
                              $"Map: {lobby.Data[KEY_MAP].Value}\n" +
                              $"Region: {lobby.Data[KEY_REGION].Value}\n" +
                              $"Ball Type: {lobby.Data[KEY_BALL_TYPE].Value}\n" +
                              $"----------------------------";

        Debug.Log(debugMessage);
    }

}
