using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

// Call this when clients are connected.
public class PlayerSpawnManager : NetworkBehaviour
{
    #region Singleton

    public static PlayerSpawnManager Instance;


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
    }

    #endregion

    #region Fields and References

    [Header("Databases")]
    [field: SerializeField] public CharacterDatabase CharacterDatabase { get; private set; }
    [field: SerializeField] public WeaponDatabase WeaponDatabase { get; private set; }

    [Header("Cameras")]
    [SerializeField] private CinemachineCamera fpsCamera;
    [SerializeField] private CinemachineCamera lookAtCamera;

    [Header("Ball")]
    [SerializeField] private GameObject ballPrefab; // Networked ball prefab
    private GameObject spawnedBall;

    [Header("Spawn")]
    [SerializeField] private Transform[] blueTeamSpawnPoints;
    [SerializeField] private Transform[] redTeamSpawnPoints;
    [SerializeField] private Transform ballSpawnPoint;


    private Dictionary<ulong, UserData> clientUserData = new Dictionary<ulong, UserData>(); // Stores user data for each client
    private Dictionary<ulong, UserData> disconnectedUserData = new(); // Stores disconnected user's for reconnection
    private Dictionary<ulong, PlayerAbstract> activePlayers = new();

    // coroutine for dead players
    private Dictionary<ulong, Coroutine> pendingRespawnTimers = new();
    private Dictionary<ulong, float> respawnExpireTime = new(); // Used to calculate time left

    /// <summary>
    /// Holds revive timers for individual players.  
    /// If a player dies while inside this revive shield window, they will be immediately respawned.
    /// Used for "Self-Revive If Killed Soon" type skills.
    /// </summary>
    private Dictionary<ulong, float> reviveShieldPerPlayer = new();

    /// <summary>
    /// Holds revive shield state per team.  
    /// If any teammate dies while this is active, they will be instantly revived.  
    /// Used for "Team-wide Safety Bubble" skills.
    /// </summary>
    private Dictionary<int, float> reviveShieldPerTeam = new();

    private NetworkServer networkServer;
    private SpawnPointManager spawnPointManager;

    #endregion

    //void Update()
    //{
    //    if (!IsServer) return;

    //    foreach (var kvp in clientUserData)
    //    {
    //        UserData data = kvp.Value;

    //        Debug.Log($"ClientID: {data.clientId}, Team: {data.teamIndex}, Character: {data.characterId}, Weapon: {data.weaponId}");
    //    }
    //}

    #region Network Initialization

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[OnNetworkSpawn] IsServer: {IsServer}, IsHost: {IsHost}");

        if (IsServer)
        {
            networkServer = HostSingleton.Instance.GameManager.NetworkServer;
            if (networkServer == null)
            {
                Debug.LogError("Network Server could not be found!");
                return;
            }

            spawnPointManager = new SpawnPointManager(blueTeamSpawnPoints, redTeamSpawnPoints);

            SpawnBall();

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            NetworkManager.Singleton.SceneManager.OnLoadComplete += OnSceneLoaded;

        }

        //if (IsHost)
        //{
        //    UserData userData = networkServer.GetUserDataByClientId(NetworkManager.Singleton.LocalClientId);

        //    // Store Host's user data in the dictionary
        //    clientUserData[NetworkManager.Singleton.LocalClientId] = userData;

        //    SpawnPlayer(NetworkManager.Singleton.LocalClientId, userData.characterId, userData.weaponId, userData.teamIndex, false, false);
        //}
    }

    #endregion

    #region Connection Handling

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"[OnClientConnected] clientId: {clientId}");

        UserData userData;

        // Try restore from disconnectedUserData
        if (disconnectedUserData.TryGetValue(clientId, out var restoredData))
        {
            Debug.Log($"Restoring disconnected player's UserData for client {clientId}");
            userData = restoredData;
            disconnectedUserData.Remove(clientId);
        }
        else
        {
            userData = networkServer.GetUserDataByClientId(clientId); // Normal
        }

        if (userData == null)
        {
            Debug.LogError($"[OnClientConnected] No UserData found for client {clientId}");
            return;
        }

        clientUserData[clientId] = userData;

        Debug.Log($"[OnClientConnected] Stored userData for client {clientId}, will spawn on scene load.");
    }

    private void OnSceneLoaded(ulong clientId, string sceneName, LoadSceneMode mode)
    {
        if (sceneName != "Game") return;

        Debug.Log($"[OnSceneLoaded] {sceneName} loaded by client {clientId}");

        if (!clientUserData.TryGetValue(clientId, out var userData))
        {
            userData = networkServer.GetUserDataByClientId(clientId);
            if (userData == null)
            {
                Debug.LogError($"[OnSceneLoaded] No UserData found for client {clientId}");
                return;
            }

            clientUserData[clientId] = userData; // Fallback safety
        }

        // ✅ Now it's safe to spawn
        StartCoroutine(DelayedPlayerSpawn(clientId, userData.characterId, userData.weaponId, userData.teamIndex, delayInSeconds: 1.25f));
    }

    private IEnumerator DelayedPlayerSpawn(ulong clientId, int characterId, int weaponId, int teamIndex, float delayInSeconds)
    {
        yield return new WaitForSeconds(delayInSeconds);

        Debug.Log($"[DelayedPlayerSpawn] Spawning client {clientId} after {delayInSeconds} seconds...");
        SpawnPlayer(clientId, characterId, weaponId, teamIndex, false, false);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        int numPlayer = NetworkManager.Singleton.ConnectedClients.Count;
        Debug.Log($"[Server] Player {clientId} is disconnected. Number of reamaining players : {numPlayer}");

        CancelRespawn(clientId);

        clientUserData.Remove(clientId);
        activePlayers.Remove(clientId);

        // ToDo: Check if current player disconnects, and reconnects, their data lost or not. If lost, then try to use this logic.
        if (clientUserData.TryGetValue(clientId, out var userData))
        {
            disconnectedUserData[clientId] = userData; // Backup for reconnection
            clientUserData.Remove(clientId);           // Optional: only if not needed in main list
        }
    }


    #endregion

    #region Regular Spawn Logic

    #region Player Spawning


    /// <summary>
    /// Spawns a player in the game at a designated spawn point.
    /// </summary>
    /// /// <param name="clientId">The unique identifier of the client who owns this player.</param>
    /// <param name="characterId">The ID of the character selected by the player.</param>
    /// <param name="weaponId">The ID of the weapon selected by the player.</param>
    /// <param name="teamIndex">The team the player belongs to (e.g., 0 = Blue, 1 = Red).</param>
    /// /// <param name="isBulkSpawn">
    /// **True** → If spawning multiple players at once (ensures unique spawn points).  
    /// **False** → If spawning a single player (allows random spawn selection).
    /// </param>
    public void SpawnPlayer(ulong clientId, int characterId, int weaponId, int teamIndex, bool isBulkSpawn = false, bool isRespawn = true)
    {
        if (!IsServer) return;

        if (!CharacterDatabase.IsValidCharacterId(characterId))
        {
            Debug.LogError($"Invalid characterId ({characterId}) for client {clientId}");
            return;
        }

        if (!WeaponDatabase.IsValidWeaponId(weaponId))
        {
            Debug.LogError($"Invalid weaponId ({weaponId}) for client {clientId}");
            return;
        }


        Transform spawnTransform = isBulkSpawn
            ? spawnPointManager.GetUniqueSpawnPoint(teamIndex)
            : spawnPointManager.GetSingleSpawnPoint(teamIndex);
        Vector3 spawnPosition = spawnTransform.position;
        Quaternion spawnRotation = spawnTransform.rotation;

        // 🆕: Check if player already exists
        if (activePlayers.TryGetValue(clientId, out var player))
        {
            if (player.IsPlayerDeath)
            {
                Debug.Log("Player Spawn Manager : Player is death, Respawning");
                player.NetworkObject.SpawnAsPlayerObject(clientId);

                // ✅ RE-CREATE AND REASSIGN WEAPON ON RESPAWN
                StartCoroutine(DelayedResetAndRespawn(player, weaponId, spawnPosition, spawnRotation));
            }
            else
            {
                Debug.Log("Player Spawn Manager : Player is not death, Teleporting...");
                player.TeleportToSpawn(spawnPosition, spawnRotation);
            }

            AssignClientVisuals(clientId, player);
            return;
        }

        // 🆕: Fresh spawn
        Character selectedCharacter = CharacterDatabase.GetCharacterById(characterId);
        NetworkObject characterPrefab = selectedCharacter.GameplayPrefab;
        if (characterPrefab == null)
        {
            Debug.LogError($"Character {selectedCharacter.DisplayName} has no valid GameplayPrefab.");
            return;
        }

        NetworkObject newPlayer = Instantiate(characterPrefab, spawnPosition, spawnRotation);
        newPlayer.SpawnAsPlayerObject(clientId);

        var playerScript = newPlayer.GetComponent<PlayerAbstract>();
        playerScript.CreateAndAssignWeapon(weaponId);
        playerScript.SetBallOwnershipManagerAndEvents(spawnedBall.GetComponent<BallOwnershipManager>());

        activePlayers[clientId] = playerScript;
        AssignClientVisuals(clientId, playerScript);
    }

    private IEnumerator DelayedResetAndRespawn(PlayerAbstract player, int weaponId, Vector3 spawnPos, Quaternion spawnRot)
    {
        yield return null; // Wait 1 frame

        player.ResetAndRespawnPlayer(spawnPos, spawnRot);
        player.CreateAndAssignWeapon(weaponId);
    }

    /// <summary>
    /// Re-Spawns a player at once at a designed spawn point.
    /// </summary>
    public void RespawnPlayer(ulong clientId)
    {
        if (!clientUserData.TryGetValue(clientId, out var userData))
        {
            Debug.LogError($"[RespawnPlayer] No UserData for client {clientId}");
            return;
        }

        SpawnPlayer(clientId, userData.characterId, userData.weaponId, userData.teamIndex, false, true);
    }

    /// <summary>
    /// Spawns all players at once at unique spawn points.
    /// </summary>
    public void ResetAllPlayersToSpawn()
    {
        if (!IsServer) return;

        spawnPointManager.ResetSpawnPoints();

        //  Cancel all queued single respawns
        foreach (var pair in pendingRespawnTimers)
        {
            StopCoroutine(pair.Value);
        }
        pendingRespawnTimers.Clear();
        respawnExpireTime.Clear();

        foreach (var entry in clientUserData)
        {
            ulong clientId = entry.Key;
            UserData userData = entry.Value;

            SpawnPlayer(clientId, userData.characterId, userData.weaponId, userData.teamIndex, true, true);
        }
    }

    private void AssignClientVisuals(ulong clientId, PlayerAbstract playerScript)
    {
        ulong playerObjId = playerScript.NetworkObjectId;
        ulong ballObjId = spawnedBall.GetComponent<NetworkObject>().NetworkObjectId;

        AssignCinemachineCameraToClientRpc(clientId, playerObjId, ballObjId);
        AssignBallManagerToClientRpc(clientId, playerObjId, ballObjId);
    }

    #endregion

    #region Respawn Coroutines

    public void QueueRespawn(ulong clientId, float delay)
    {
        if (!IsServer) return;

        // Avoid duplicate coroutines
        if (pendingRespawnTimers.ContainsKey(clientId)) return;

        Coroutine timer = StartCoroutine(RespawnAfterDelay(clientId, delay));
        pendingRespawnTimers[clientId] = timer;
    }

    private IEnumerator RespawnAfterDelay(ulong clientId, float delay)
    {
        float targetTime = Time.time + delay;
        respawnExpireTime[clientId] = targetTime;

        while (Time.time < targetTime)
        {
            yield return null;
        }

        if (!pendingRespawnTimers.ContainsKey(clientId)) yield break;

        RespawnPlayer(clientId);
        pendingRespawnTimers.Remove(clientId);
        respawnExpireTime.Remove(clientId);
    }

    // This is for cancelling the spawn for disconnected or left client to not have errors  
    public void CancelRespawn(ulong clientId)
    {
        if (!IsServer) return;

        if (pendingRespawnTimers.TryGetValue(clientId, out Coroutine coroutine))
        {
            StopCoroutine(coroutine);
            pendingRespawnTimers.Remove(clientId);
            respawnExpireTime.Remove(clientId);
            Debug.Log($"[Server] Cancelled respawn for client {clientId}");
        }
    }

    public float GetRemainingRespawnTime(ulong clientId)
    {
        if (respawnExpireTime.TryGetValue(clientId, out float expiry))
            return Mathf.Max(0f, expiry - Time.time);

        return -1f;
    }

    #endregion

    #endregion

    #region Skill-Based Spawn Logic

    #region Revive Shields

    /// <summary>
    /// Activates a revive shield for a single player.  
    /// If this player dies within `duration`, they will automatically respawn.  
    /// Useful for personal passive skills.
    /// </summary>
    public void ActivateReviveShieldForPlayer(ulong clientId, float duration)
    {
        reviveShieldPerPlayer[clientId] = Time.time + duration;
    }

    /// <summary>
    /// Checks if the player's revive shield is active and not expired.
    /// </summary>
    public bool ShouldAutoRevivePlayer(ulong clientId)
    {
        return reviveShieldPerPlayer.TryGetValue(clientId, out float expiry) && Time.time <= expiry;
    }

    /// <summary>
    /// Removes a revive shield (after successful auto-revive).
    /// </summary>
    public void RemovePlayerReviveShield(ulong clientId)
    {
        reviveShieldPerPlayer.Remove(clientId);
    }

    /// <summary>
    /// Activates a revive shield for the entire team.  
    /// Any teammate who dies within the duration will be revived instantly.  
    /// Ideal for support-oriented AOE skills.
    /// </summary>
    public void ActivateReviveShieldForTeam(int teamIndex, float duration)
    {
        reviveShieldPerTeam[teamIndex] = Time.time + duration;
    }

    /// <summary>
    /// Checks if the given team is under a revive shield window.
    /// </summary>
    public bool ShouldAutoReviveTeam(int teamIndex)
    {
        return reviveShieldPerTeam.TryGetValue(teamIndex, out float expiry) && Time.time <= expiry;
    }


    #endregion

    #region Skill Effects (Revive, Delay, etc.)

    /// <summary>
    /// Instantly revives a teammate.  
    /// Only works if they are on the same team and currently dead.  
    /// Used in "Single Teammate Revive" type skills.
    /// </summary>
    public void ForceRespawnPlayer(ulong requesterClientId, ulong targetClientId)
    {
        if (!IsServer) return;

        if (!clientUserData.TryGetValue(requesterClientId, out var requester) ||
            !clientUserData.TryGetValue(targetClientId, out var target))
            return;

        if (requester.teamIndex != target.teamIndex) return;

        if (activePlayers.TryGetValue(targetClientId, out var player) && player.IsPlayerDeath)
        {
            CancelRespawn(targetClientId);
            RespawnPlayer(targetClientId);
        }
    }

    /// <summary>
    /// Revives all dead teammates of a given team.  
    /// Used for full team revive ultimates.
    /// </summary>
    public void ForceRespawnTeam(int teamIndex)
    {
        foreach (var pair in clientUserData)
        {
            ulong clientId = pair.Key;
            if (pair.Value.teamIndex != teamIndex) continue;

            if (activePlayers.TryGetValue(clientId, out var player) && player.IsPlayerDeath)
            {
                CancelRespawn(clientId);
                RespawnPlayer(clientId);
            }
        }
    }

    /// <summary>
    /// Finds the teammate with the least remaining respawn time  
    /// and revives them instantly.  
    /// Used for single-target support abilities.
    /// </summary>
    public void ForceRespawnBestAlly(ulong requesterClientId)
    {
        if (!clientUserData.TryGetValue(requesterClientId, out var requester)) return;

        int team = requester.teamIndex;

        if (TryGetDeadPlayerWithLeastRespawnTime(
            id => id != requesterClientId && clientUserData[id].teamIndex == team,
            out ulong bestAlly))
        {
            ForceRespawnPlayer(requesterClientId, bestAlly);
        }
    }

    /// <summary>
    /// Finds the enemy with the shortest remaining respawn timer  
    /// and extends it by a given amount.  
    /// Used for "Single Enemy Delay" type skills.
    /// </summary>
    public void ExtendRespawnOfEnemyWithLeastTime(ulong requesterClientId, float extraDelay)
    {
        if (!clientUserData.TryGetValue(requesterClientId, out var requester)) return;

        int team = requester.teamIndex;

        if (TryGetDeadPlayerWithLeastRespawnTime(
            id => clientUserData[id].teamIndex != team,
            out ulong enemyId))
        {
            ExtendRespawnTimeForSingleEnemy(requesterClientId, enemyId, extraDelay);
        }
    }

    /// <summary>
    /// Adds delay to a single dead enemy's respawn time.  
    /// Cannot be used on teammates.  
    /// Used by debuff or trap skills.
    /// </summary>
    public void ExtendRespawnTimeForSingleEnemy(ulong requesterClientId, ulong targetClientId, float extraDelay)
    {
        if (!clientUserData.TryGetValue(requesterClientId, out var requester) ||
            !clientUserData.TryGetValue(targetClientId, out var target))
            return;

        if (requester.teamIndex == target.teamIndex) return;

        if (activePlayers.TryGetValue(targetClientId, out var player) && player.IsPlayerDeath)
        {
            if (pendingRespawnTimers.TryGetValue(targetClientId, out var oldCoroutine))
            {
                StopCoroutine(oldCoroutine);

                float newExpiry = respawnExpireTime[targetClientId] + extraDelay;
                float newDelay = newExpiry - Time.time;

                Coroutine newTimer = StartCoroutine(RespawnAfterDelay(targetClientId, newDelay));
                pendingRespawnTimers[targetClientId] = newTimer;
                respawnExpireTime[targetClientId] = newExpiry;

                player.UpdateRespawnTimerClientRpc(newDelay); // ✅ Send to UI
            }
        }
    }

    /// <summary>
    /// Adds extra delay to all currently dead enemies on the opposing team.  
    /// Used for global debuff effects.
    /// </summary>
    public void ExtendRespawnTimeForEnemyTeam(int requesterTeamIndex, float extraDelay)
    {
        foreach (var pair in clientUserData)
        {
            ulong clientId = pair.Key;
            if (pair.Value.teamIndex == requesterTeamIndex) continue;

            if (activePlayers.TryGetValue(clientId, out var player) && player.IsPlayerDeath)
            {
                if (pendingRespawnTimers.TryGetValue(clientId, out var oldCoroutine))
                {
                    StopCoroutine(oldCoroutine);

                    float newExpiry = respawnExpireTime[clientId] + extraDelay;
                    float newDelay = newExpiry - Time.time;

                    Coroutine newTimer = StartCoroutine(RespawnAfterDelay(clientId, newDelay));
                    pendingRespawnTimers[clientId] = newTimer;
                    respawnExpireTime[clientId] = newExpiry;

                    player.UpdateRespawnTimerClientRpc(newDelay); // ✅ Send to UI
                }
            }
        }
    }

    #endregion

    #region Shared Utility

    /// <summary>
    /// Searches dead players and finds the one with the least remaining respawn time  
    /// based on a given filter condition (team, enemy, etc).
    /// </summary>
    /// <param name="filter">Filter to apply to each candidate (e.g., is teammate, is enemy).</param>
    /// <param name="bestClientId">Returns the best match, if found.</param>
    private bool TryGetDeadPlayerWithLeastRespawnTime(Func<ulong, bool> filter, out ulong bestClientId)
    {
        bestClientId = 0;
        float shortestTime = float.MaxValue;
        bool found = false;

        foreach (var kvp in respawnExpireTime)
        {
            ulong clientId = kvp.Key;
            if (!activePlayers.TryGetValue(clientId, out var player)) continue;
            if (!player.IsPlayerDeath) continue;
            if (!filter(clientId)) continue;

            float timeLeft = kvp.Value - Time.time;
            if (timeLeft < shortestTime)
            {
                bestClientId = clientId;
                shortestTime = timeLeft;
                found = true;
            }
        }

        return found;
    }

    #endregion

    #endregion

    #region Ball Management

    [ClientRpc]
    private void AssignBallManagerToClientRpc(ulong clientId, ulong playerObjectId, ulong ballObjectId)
    {
        if (NetworkManager.Singleton.LocalClientId != clientId) return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerObjectId, out NetworkObject playerObject) ||
            !NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(ballObjectId, out NetworkObject ballObject))
        {
            Debug.LogError($"[Client] Failed to assign BallOwnershipManager: Player or Ball object not found for client {clientId}");
            return;
        }

        PlayerAbstract playerScript = playerObject.GetComponent<PlayerAbstract>();
        BallOwnershipManager ballManager = ballObject.GetComponent<BallOwnershipManager>();

        if (playerScript != null && ballManager != null)
        {
            playerScript.SetBallOwnershipManagerAndEvents(ballManager);
            Debug.Log($"[Client] BallOwnershipManager assigned to player {playerScript.name} on Client {clientId}");
        }
        else
        {
            Debug.LogError($"[Client] Missing references for BallOwnershipManager assignment on client {clientId}");
        }
    }

    private void SpawnBall()
    {
        if (ballPrefab == null)
        {
            Debug.LogError("Ball Prefab is missing in BallSpawner!");
            return;
        }

        // ✅ Instantiate and spawn the ball on the network
        GameObject ballInstance = Instantiate(ballPrefab, ballSpawnPoint.position, Quaternion.identity);
        ballInstance.GetComponent<NetworkObject>().Spawn();

        spawnedBall = ballInstance;
    }
    #endregion

    #region Camera Assigment on Spawn

    [ClientRpc]
    private void AssignCinemachineCameraToClientRpc(ulong clientId, ulong playerObjectId, ulong ballObjectId)
    {
        if (NetworkManager.Singleton.LocalClientId != clientId) return;

        if (fpsCamera == null)
        {
            Debug.LogError("FPS Camera is not assigned in Player Spawn Manager.");
            return;
        }

        if (lookAtCamera == null)
        {
            Debug.LogError("LookAt Camera is not assigned in Player Spawn Manager.");
            return;
        }

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerObjectId, out NetworkObject playerObject) ||
            !NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(ballObjectId, out NetworkObject ballObject))
        {
            Debug.LogError($"[Client] Failed to assign BallOwnershipManager: Player or Ball object not found for client {clientId}");
            return;
        }

        PlayerAbstract playerInstance = playerObject.GetComponent<PlayerAbstract>();
        BallOwnershipManager ballManager = ballObject.GetComponent<BallOwnershipManager>();

        if (playerInstance.EyeTrackingPoint != null)
        {
            fpsCamera.Follow = playerInstance.EyeTrackingPoint.transform;
        }
        else
        {
            Debug.LogWarning($"EyeTrackingPoint not found on player {playerInstance.name}.");
        }

        if (playerInstance.FollowTrackingPoint != null)
        {
            lookAtCamera.Follow = playerInstance.FollowTrackingPoint.transform;
            lookAtCamera.LookAt = ballManager.transform;
        }
        else
        {
            Debug.LogWarning($"FollowTrackingPoint not found on player {playerInstance.name}.");
        }

        CameraSwitchHandler cameraSwitchHandler = playerInstance.GetComponentInChildren<CameraSwitchHandler>();
        if (cameraSwitchHandler != null)
        {
            Debug.Log("Assigning Camera Switcher's camera properties.");
            cameraSwitchHandler.fpsCamera = fpsCamera;
            cameraSwitchHandler.lookAtCamera = lookAtCamera;
            cameraSwitchHandler.SetCameraMode(cameraSwitchHandler.currentCameraMode);
        }
        else
        {
            Debug.LogWarning($"CameraSwitchHandler not found on player {playerInstance.name}.");
        }
    }

    #endregion

    #region Get Session User Data
    public UserData GetUserData(ulong clientId)
    {
        if(clientUserData.TryGetValue(clientId, out UserData userData))
        {
            return userData;
        }

        Debug.LogWarning($"[Server] could not found {clientId} inside Dictionary.");
        return null;
    }
    #endregion

    #region Clean Up

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (NetworkManager == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        NetworkManager.Singleton.SceneManager.OnLoadComplete -= OnSceneLoaded;

    }
    #endregion
}
