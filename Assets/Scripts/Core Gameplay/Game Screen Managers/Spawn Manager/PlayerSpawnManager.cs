using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

// Call this when clients are connected.
public class PlayerSpawnManager : NetworkBehaviour
{
    #region Singleton

    public static PlayerSpawnManager Instance;

    [Header("Cameras")]
    [SerializeField] private CinemachineCamera fpsCamera;
    [SerializeField] private CinemachineCamera lookAtCamera;

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

    [SerializeField] private GameObject ballPrefab; // Networked ball prefab
    private GameObject spawnedBall;

    public CharacterDatabase characterDatabase;
    public WeaponDatabase weaponDatabase;

    [SerializeField] private Transform[] blueTeamSpawnPoints;
    [SerializeField] private Transform[] redTeamSpawnPoints;
    [SerializeField] private Transform ballSpawnPoint;

    private Dictionary<ulong, UserData> clientUserData = new Dictionary<ulong, UserData>(); // Stores user data for each client
    private Dictionary<ulong, UserData> disconnectedUserData = new(); // Stores disconnected user's for reconnection

    private NetworkServer networkServer;
    private SpawnPointManager spawnPointManager;

    #endregion

    #region Network Initialization

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            SpawnBall();

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            networkServer = HostSingleton.Instance.GameManager.NetworkServer;

            if (networkServer == null)
            {
                Debug.LogError("Network Server could not be found!");
                return;
            }

            spawnPointManager = new SpawnPointManager(blueTeamSpawnPoints, redTeamSpawnPoints);
        }

        if (IsHost)
        {
            UserData userData = networkServer.GetUserDataByClientId(NetworkManager.Singleton.LocalClientId);

            // Store Host's user data in the dictionary
            clientUserData[NetworkManager.Singleton.LocalClientId] = userData;

            SpawnPlayer(NetworkManager.Singleton.LocalClientId, userData.characterId, userData.weaponId, userData.teamIndex, false, false);
        }
    }

    #endregion

    #region Connection Handling

    private void OnClientConnected(ulong clientId)
    {
        // ToDo: Check if current player disconnects, and reconnects, their data lost or not. If lost, then try to use this logic.
        // Do same thing for Sync Score Board Stats.
        //UserData userData;

        //if (disconnectedUserData.TryGetValue(clientId, out var restoredData))
        //{
        //    Debug.Log($"Restoring disconnected player's UserData for client {clientId}");
        //    userData = restoredData;
        //    disconnectedUserData.Remove(clientId);
        //}
        //else
        //{
        //    userData = networkServer.GetUserDataByClientId(clientId); // Normal case
        //}

        //if (userData == null)
        //{
        //    Debug.LogError($"[OnClientConnected] No UserData found for client {clientId}");
        //    return;
        //}
        UserData userData = networkServer.GetUserDataByClientId(clientId);

        if(userData == null)
        {
            Debug.LogError($"[OnClientConnected] No UserData found for client {clientId}");
            return;
        }

        int numPlayer = NetworkManager.Singleton.ConnectedClients.Count;
        Debug.Log($"[Server] Number of Players Connected is : {numPlayer}");

        // Store user data in the dictionary
        clientUserData[clientId] = userData;

        SpawnPlayer(clientId, userData.characterId, userData.weaponId, userData.teamIndex, false , false);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        int numPlayer = NetworkManager.Singleton.ConnectedClients.Count;
        Debug.Log($"[Server] Player {clientId} is disconnected. Number of reamaining players : {numPlayer}");

        clientUserData.Remove(clientId);

        // ToDo: Check if current player disconnects, and reconnects, their data lost or not. If lost, then try to use this logic.
        //Debug.Log($"[Server] Player {clientId} disconnected.");

        //if (clientUserData.TryGetValue(clientId, out var userData))
        //{
        //    disconnectedUserData[clientId] = userData; // Backup for reconnection
        //    clientUserData.Remove(clientId);           // Optional: only if not needed in main list
        //}
    }


    #endregion

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

        // Validate Character and Weapon IDs
        if (!characterDatabase.IsValidCharacterId(characterId) || !weaponDatabase.IsValidWeaponId(weaponId))
        {
            Debug.LogError($"Invalid characterId ({characterId}) or weaponId ({weaponId}) for client {clientId}");
            return;
        }

        // Prevent duplicate SpawnAsPlayerObject
        if (!isRespawn && NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject != null)
        {
            Debug.LogWarning($"[SpawnPlayer] Client {clientId} already has a player object.");
            return;
        }

        // Get Character Network Prefab
        Character selectedCharacter = characterDatabase.GetCharacterById(characterId);
        NetworkObject characterPrefab = selectedCharacter.GameplayPrefab;

        if (characterPrefab == null)
        {
            Debug.LogError($"Character {selectedCharacter.DisplayName} does not have a valid GameplayPrefab.");
            return;
        }

        // Get a random spawn point based on team and if it is bulk
        Vector3 spawnPosition = isBulkSpawn
                   ? spawnPointManager.GetUniqueSpawnPoint(teamIndex) // For bulk spawning (avoids overlap)
                   : spawnPointManager.GetSingleSpawnPoint(teamIndex); // For single player spawning

        // Spawn Player Character
        NetworkObject spawnedCharacter = Instantiate(characterPrefab, spawnPosition, Quaternion.identity);

        if (isRespawn)
        {
            spawnedCharacter.Spawn(); // Regular network spawn
        }
        else
        {
            spawnedCharacter.SpawnAsPlayerObject(clientId); // Assigns as player object
        }
        // Assign Weapon to PlayerCharacter Script
        PlayerAbstract playerScript = spawnedCharacter.GetComponent<PlayerAbstract>();


        playerScript.CreateAndAssignWeapon(weaponId);
        playerScript.SetBallOwnershipManagerAndEvents(spawnedBall.GetComponent<BallOwnershipManager>());

        // Assign Cameras when player spawned
        AssignCinemachineCameraToClientRpc(clientId, spawnedCharacter.NetworkObjectId, spawnedBall.GetComponent<NetworkObject>().NetworkObjectId);

        // ClientRpc to assign BallOwnershipManager to the specific client
        AssignBallManagerToClientRpc(clientId, spawnedCharacter.NetworkObjectId, spawnedBall.GetComponent<NetworkObject>().NetworkObjectId);
    }

    /// <summary>
    /// Re-Spawns a player at once at a designed spawn point.
    /// </summary>
    public void RespawnPlayer(ulong clientId)
    {
        if (!clientUserData.ContainsKey(clientId))
        {
            Debug.LogError($"[RespawnPlayer] No UserData found for client {clientId}");
            return;
        }

        UserData userData = clientUserData[clientId];

        // False = not bulk, true = isRespawn
        SpawnPlayer(clientId, userData.characterId, userData.weaponId, userData.teamIndex, false);
    }

    /// <summary>
    /// Spawns all players at once at unique spawn points.
    /// </summary>
    public void SpawnAllPlayers()
    {
        if (!IsServer) return;

        spawnPointManager.ResetSpawnPoints();

        foreach (var entry in clientUserData)
        {
            ulong clientId = entry.Key;
            UserData userData = entry.Value;

            SpawnPlayer(clientId, userData.characterId, userData.weaponId, userData.teamIndex, true);
        }
    }

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
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }
    #endregion
}
