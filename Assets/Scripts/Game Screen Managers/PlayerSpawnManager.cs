using Unity.Netcode;
using UnityEngine;

// Call this when clients are connected.
public class PlayerSpawnManager : NetworkBehaviour
{
    public static PlayerSpawnManager Instance;

    [SerializeField] private GameObject ballPrefab; // Assign the Networked Ball Prefab
    private GameObject spawnedBall;

    public CharacterDatabase characterDatabase;
    public WeaponDatabase weaponDatabase;

    private void Awake()
    {
        if(Instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            SpawnBall();

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }

        if (IsHost)
        {
            SpawnPlayer(NetworkManager.Singleton.LocalClientId, 1, 1); // Default character & weapon IDs
        }

    }

    private void OnClientConnected(ulong clientId)
    {
        int numPlayer = NetworkManager.Singleton.ConnectedClients.Count;
        Debug.Log($"[Server] Number of Players Connected is : {++numPlayer}");
        int characterId = numPlayer; // Retrieve from player selection system
        int weaponId = numPlayer;    // Retrieve from player selection system
        SpawnPlayer(clientId, characterId, weaponId);
    }

    public void SpawnPlayer(ulong clientId, int characterId, int weaponId)
    {
        if (!IsServer) return;

        // Validate Character and Weapon IDs
        if (!characterDatabase.IsValidCharacterId(characterId) || !weaponDatabase.IsValidWeaponId(weaponId))
        {
            Debug.LogError($"Invalid characterId ({characterId}) or weaponId ({weaponId}) for client {clientId}");
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

        // Spawn Player Character
        NetworkObject spawnedCharacter = Instantiate(characterPrefab, GetSpawnPoint(), Quaternion.identity);
        spawnedCharacter.SpawnAsPlayerObject(clientId);

        // Assign Weapon to PlayerCharacter Script
        PlayerAbstract playerScript = spawnedCharacter.GetComponent<PlayerAbstract>();

        playerScript.CreateAndAssignWeapon(weaponId);
        playerScript.SetBallOwnershipManagerAndEvents(spawnedBall.GetComponent<BallOwnershipManager>());

        // ClientRpc to assign BallOwnershipManager to the specific client
        AssignBallManagerToClientRpc(clientId, spawnedCharacter.NetworkObjectId, spawnedBall.GetComponent<NetworkObject>().NetworkObjectId);
    }

    [ClientRpc]
    private void AssignBallManagerToClientRpc(ulong clientId, ulong playerObjectId, ulong ballObjectId)
    {
        if (NetworkManager.Singleton.LocalClientId != clientId) return;

        NetworkObject playerObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[playerObjectId];
        NetworkObject ballObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[ballObjectId];

        if (playerObject == null || ballObject == null)
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
        GameObject ballInstance = Instantiate(ballPrefab, new Vector3(0,2,10), Quaternion.identity);
        ballInstance.GetComponent<NetworkObject>().Spawn();

        spawnedBall = ballInstance;
    }

    private Vector3 GetSpawnPoint()
    {
        // Implement spawn logic (random or fixed positions)
        return new Vector3(Random.Range(-5f, 5f), 0, Random.Range(-5f, 5f));
    }
}
