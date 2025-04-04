using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

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
        if (!IsServer) return;
        Debug.Log("[Server:] Game Manager is initialized on On Network Spawn");

        if(IsHost)
        {
            UserData userData = HostSingleton.Instance.GameManager.NetworkServer.GetUserDataByClientId(NetworkManager.Singleton.LocalClientId);
            if (userData == null)
            {
                Debug.LogError($"[GameManager] No UserData found for client {NetworkManager.Singleton.LocalClientId}");
                return;
            }

            string authId = userData.userAuthId;

            // First time? Create server-owned stat object
            if (!persistentStats.ContainsKey(authId))
            {
                GameObject statObj = Instantiate(playerStatePrefab);
                NetworkObject netObj = statObj.GetComponent<NetworkObject>();
                netObj.Spawn(); // server-owned

                var statSync = statObj.GetComponent<PlayerStatSync>();
                statSync.Initialize(userData.userName);
                persistentStats[authId] = statSync;
            }

            // Map current session to persistent identity
            clientToUserMap[NetworkManager.Singleton.LocalClientId] = authId;
        }
        NetworkManager.OnClientConnectedCallback += HandleClientConnected;
        NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;
    }

    private void Start()
    {
        if (!IsServer) return;
        Debug.Log("[Server:] Game Manager is initialized on Start");
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        NetworkManager.OnClientConnectedCallback -= HandleClientConnected;
        NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;

    }

    private void HandleClientConnected(ulong clientId)
    {
        UserData userData = HostSingleton.Instance.GameManager.NetworkServer.GetUserDataByClientId(clientId);

        if (userData == null)
        {
            Debug.LogError($"[GameManager] No UserData found for client {clientId}");
            return;
        }

        string authId = userData.userAuthId;

        // First time? Create server-owned stat object
        if (!persistentStats.ContainsKey(authId))
        {
            GameObject statObj = Instantiate(playerStatePrefab);
            NetworkObject netObj = statObj.GetComponent<NetworkObject>();
            netObj.Spawn(); // server-owned

            var statSync = statObj.GetComponent<PlayerStatSync>();
            statSync.Initialize(userData.userName);
            persistentStats[authId] = statSync;
        }

        // Map current session to persistent identity
        clientToUserMap[clientId] = authId;
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        clientToUserMap.Remove(clientId);
        // Do NOT despawn stat object → keep it alive for reconnect
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

    // Called when a goal is scored
    public void AddGoal(ulong clientId)
    {
        var stats = GetPlayerStats(clientId);
        if (stats != null)
        {
            stats.Goals.Value++;
        }

        int team = PlayerSpawnManager.Instance.GetUserData(clientId).teamIndex;

        if (team == 0) BlueTeamScore.Value++;
        else if (team == 1) RedTeamScore.Value++;

        // Set the state post game for the game
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

    public override void OnDestroy()
    {
        base.OnDestroy();

        CursorController.UnlockCursor();

        if (!IsServer || NetworkManager == null) return;

        // Just in case, it is unnecessary but lets leave it here
        NetworkManager.OnClientConnectedCallback -= HandleClientConnected;
        NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
    }
}
