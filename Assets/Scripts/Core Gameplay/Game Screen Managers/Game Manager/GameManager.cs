using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [SerializeField] private GameObject playerStatePrefab;

    // Persistent stats keyed by player identity (authId)
    private Dictionary<string, PlayerStatSync> persistentStats = new();

    // Current session mapping (clientId → authId)
    private Dictionary<ulong, string> clientToUserMap = new();

    public NetworkVariable<int> BlueTeamScore = new();
    public NetworkVariable<int> RedTeamScore = new();

    private Dictionary<Team, int> accidentalGoals = new()
    {
        { Team.Blue, 0 },
        { Team.Red, 0 }
    };

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }

        CursorController.LockCursor();
    }

    public override void OnNetworkSpawn()
    {
        MultiplayerGameStateManager.Instance.NetworkGameState.OnValueChanged += HandleGameStateChanged;

        if (!IsServer) return;
        Debug.Log("[Server:] Game Manager is initialized on On Network Spawn");

        NetworkManager.OnClientConnectedCallback += HandleClientConnected;
        NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;

        NetworkManager.SceneManager.OnLoadComplete += HandleSceneLoaded;
    }

    private void Start()
    {
        if (!IsServer) return;
        Debug.Log("[Server:] Game Manager is initialized on Start");
    }

    public override void OnNetworkDespawn()
    {
        MultiplayerGameStateManager.Instance.NetworkGameState.OnValueChanged -= HandleGameStateChanged;

        if (!IsServer) return;

        NetworkManager.OnClientConnectedCallback -= HandleClientConnected;
        NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;

        NetworkManager.SceneManager.OnLoadComplete -= HandleSceneLoaded;


    }

    private void HandleGameStateChanged(GameState oldState, GameState newState)
    {
        if (!IsServer) return;

        bool showBall = newState == GameState.PreGame || newState == GameState.InGame;
        ResetBall(showBall);
    }



    private void HandleSceneLoaded(ulong clientId, string sceneName, LoadSceneMode mode)
    {
        if (sceneName != "Game") return;

        StartCoroutine(DelayedStatObjectSetup(clientId));
    }

    private void HandleClientConnected(ulong clientId)
    {
        StartCoroutine(DelayedStatObjectSetup(clientId));
    }

    private IEnumerator DelayedStatObjectSetup(ulong clientId)
    {
        yield return null;

        var userData = HostSingleton.Instance.GameManager.NetworkServer.GetUserDataByClientId(clientId);
        if (userData == null)
        {
            Debug.LogError($"[GameManager] No UserData found for client {clientId}");
            yield break;
        }

        string authId = userData.userAuthId;

        if (!persistentStats.ContainsKey(authId))
        {
            GameObject statObj = Instantiate(playerStatePrefab);
            var netObj = statObj.GetComponent<NetworkObject>();
            netObj.Spawn();

            var statSync = statObj.GetComponent<PlayerStatSync>();
            statSync.Initialize(
                userData.userName,
                clientId,
                userData.teamIndex,
                userData.characterId,
                userData.weaponId
            );
            persistentStats[authId] = statSync;
        }

        clientToUserMap[clientId] = authId;

        Debug.Log($"[GameManager] Created stat sync object for client {clientId} ({userData.userName})");
    }



    private void HandleClientDisconnected(ulong clientId)
    {
        if (clientToUserMap.TryGetValue(clientId, out var authId))
        {
            clientToUserMap.Remove(clientId);

            // Optional: remove stats after timeout
            StartCoroutine(RemovePlayerStatsAfterTimeout(authId, 30f)); // 30 seconds for example
        }
    }

    private IEnumerator RemovePlayerStatsAfterTimeout(string authId, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Check if player reconnected
        if (persistentStats.ContainsKey(authId) &&
            !clientToUserMap.ContainsValue(authId)) // still disconnected
        {
            var statSync = persistentStats[authId];
            if (statSync != null && statSync.NetworkObject != null)
                statSync.NetworkObject.Despawn();

            persistentStats.Remove(authId);

            Debug.Log($"[GameManager] Cleaned up stats for {authId}");
        }
    }

    public PlayerStatSync GetPlayerStats(ulong clientId)
    {
        if (clientToUserMap.TryGetValue(clientId, out var authId))
        {
            return persistentStats.TryGetValue(authId, out var stats) ? stats : null;
        }

        return null;
    }

    public Dictionary<ulong, PlayerStatSync> GetAllBoundStats()
    {
        Dictionary<ulong, PlayerStatSync> result = new();
        foreach (var kvp in clientToUserMap)
        {
            if (persistentStats.TryGetValue(kvp.Value, out var stat))
                result[kvp.Key] = stat;
        }
        return result;
    }


    public void ResetBall(bool isVisible)
    {
        GameObject ball = PlayerSpawnManager.Instance.GetSpawnedBall();
        if (ball == null)
        {
            Debug.LogWarning("[GameManager] No spawned ball to reset.");
            return;
        }

        var ballVisibility = ball.GetComponent<BallVisibilityNetworkController>();
        var ballOwnership = ball.GetComponent<BallOwnershipManager>();
        var netObj = ball.GetComponent<NetworkObject>();

        if (ballVisibility == null || ballOwnership == null || netObj == null)
        {
            Debug.LogError("[GameManager] Ball is missing required components.");
            return;
        }

        ballOwnership.ResetOwnershipIds();
        netObj.TryRemoveParent();

        if (isVisible)
        {
            Vector3 spawnPosition = PlayerSpawnManager.Instance.GetBallSpawnPoint();
            ballVisibility.ShowBallServerRpc(spawnPosition);
        }
        else
        {
            ballVisibility.HideBallServerRpc();
        }
    }


    [ClientRpc]
    public void ShowGoalAnnouncementClientRpc(string playerName, int teamIndex, bool isOwnGoal)
    {
        HUDCanvasManager.Instance?.PlayerUIController?.ShowGoalAnnouncement(playerName, teamIndex, isOwnGoal);
    }

    // Called when a goal is scored
    public void AddGoal(ulong clientId)
    {
        var stats = GetPlayerStats(clientId);
        if (stats != null)
        {
            stats.Goals.Value++;
        }

        var userData = PlayerSpawnManager.Instance.GetUserData(clientId);
        if (userData != null)
        {
            ShowGoalAnnouncementClientRpc(userData.userName, userData.teamIndex, false);

            if (userData.teamIndex == 0)
                BlueTeamScore.Value++;
            else if (userData.teamIndex == 1)
                RedTeamScore.Value++;
        }
        // Set the state post game for the game
        MultiplayerGameStateManager.Instance.SetGameState(GameState.PostGame);
    }

    public void AddTeamScoreWithoutCredit(Team scoringTeam, ulong clientId)
    {
        accidentalGoals[scoringTeam]++;

        if (scoringTeam == Team.Blue)
            BlueTeamScore.Value++;
        else if (scoringTeam == Team.Red)
            RedTeamScore.Value++;

        Debug.Log($"[GameManager] Accidental goal — Team {scoringTeam} score incremented. Total Accidental: {accidentalGoals[scoringTeam]}");

        var userData = PlayerSpawnManager.Instance.GetUserData(clientId);
        if (userData != null)
        {
            ShowGoalAnnouncementClientRpc(userData.userName, userData.teamIndex, true);
        }

        /// <summary>
        /// Prevents forced opening during gameplay while ensuring UI reflects accidental goal updates in real-time.
        /// </summary>
        if (ScoreboardManager.Instance != null)
        {
            ScoreboardManager.Instance.RefreshIfVisible();
        }
        else
        {
            Debug.LogError("[Game Manager] Scoreboard Manager is null");
        }

        MultiplayerGameStateManager.Instance.SetGameState(GameState.PostGame);
    }

    public void AddKill(ulong clientId)
    {
        var stats = GetPlayerStats(clientId);
        if (stats != null)
            stats.Kills.Value++;
    }

    public void AddDeath(ulong clientId)
    {
        var stats = GetPlayerStats(clientId);
        if(stats != null)
        {
            stats.Deaths.Value++;
        }
    }

    public void AddAssist(ulong clientId)
    {
        var stats = GetPlayerStats(clientId);
        if(stats != null)
        {
            stats.Assists.Value++;
        }
    }

    public void AddSave(ulong clientId)
    {
        var stats = GetPlayerStats(clientId);
        if(stats != null)
        {
            stats.Saves.Value++;
        }
    }

    public void CleanupStats()
    {
        foreach (var stat in persistentStats.Values)
        {
            if (stat != null && stat.NetworkObject != null)
                stat.NetworkObject.Despawn();
        }
        persistentStats.Clear();
        clientToUserMap.Clear();
    }

    public int GetAccidentalGoalCount(Team team)
    {
        return accidentalGoals.TryGetValue(team, out int count) ? count : 0;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        CursorController.UnlockCursor();

        if (!IsServer || NetworkManager == null) return;

        // Just in case, it is unnecessary but lets leave it here
        NetworkManager.OnClientConnectedCallback -= HandleClientConnected;
        NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        NetworkManager.SceneManager.OnLoadComplete -= HandleSceneLoaded;

    }
}
