using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkServer : IDisposable
{
    private NetworkManager networkManager;

    private NetworkObject playerPrefab;

    public Action<string> OnClientLeft;

    public Action<UserData> OnUserJoined;
    public Action<UserData> OnUserLeft;

    private const int MaxConnectionPayload = 1024;

    private bool gameHasStarted;

    private Dictionary<ulong, string> clientIdToAuth = new Dictionary<ulong, string>();
    private Dictionary<string, UserData> authIdToUserData = new Dictionary<string, UserData>();

    public NetworkServer(NetworkManager networkManager)
    {
        this.networkManager = networkManager;

        networkManager.ConnectionApprovalCallback += ApprovalCheck;
        networkManager.OnServerStarted += OnNetworkReady;
    }

    public bool OpenConnection(string ip, int port)
    {
        UnityTransport transport = networkManager.gameObject.GetComponent<UnityTransport>();

        transport.SetConnectionData(ip, (ushort)port);

        return networkManager.StartServer();
    }


    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        if (request.Payload.Length > MaxConnectionPayload || gameHasStarted)
        {
            response.Approved = false;
            response.CreatePlayerObject = false;
            response.Position = null;
            response.Rotation = null;
            response.Pending = false;

            return;
        }

        // We are getting byte[] array, So we need to convert it and convert back to byte as well
        string payload = System.Text.Encoding.UTF8.GetString(request.Payload);
        // When we get the name here, after this point, it does not being stored.
        // It will be thrown away by system, so we need to keep it somewhere
        UserData userData;
        try
        {
            // Attempt to deserialize the payload
            userData = JsonUtility.FromJson<UserData>(payload);
        }
        catch
        {
            Debug.LogError("Failed to deserialize user data payload.");
            response.Approved = false; // Reject the connection if payload is invalid
            return;
        }

        // clientIdToAuth.Add(request.ClientNetworkId, userData.userAuthId) ,
        // The below means that if there is no this client id, create and asign,
        // So, we do not need to explicitly Add into dictionary if it is not exist.
        // With this, if there is none, create; if there is, change it.
        clientIdToAuth[request.ClientNetworkId] = userData.userAuthId;
        // Same as above
        authIdToUserData[userData.userAuthId] = userData;

        OnUserJoined?.Invoke(userData);

        response.Approved = true;

        _ = SpawnPlayerDelayed(request.ClientNetworkId);

        // When we adjust the Approval Check in Network Manager,
        // We need to create the player object with this code,
        // beacuse Network Manager make it false when we change approval check automatically
        //// We make it false beacuse we created player inside the SpawnPlayerDelayed
        ////response.CreatePlayerObject = true;
        response.CreatePlayerObject = false;

        Debug.LogWarning($"Approval Request: ClientId {request.ClientNetworkId}, Payload {payload}");

    }

    private async Task SpawnPlayerDelayed(ulong clientId)
    {
        await Task.Delay(1000);

        // Where will the player be spawned on the scene (except host),
        // Host will be spawned on the (0,0,0) when the server first start
        //// We moved this two lines here inside Instantiate method to create player obect.
        //response.Position = SpawnPoint.GetRandomSpawnPosition();
        //response.Rotation = Quaternion.identity;

        //NetworkObject playerInstance = GameObject.Instantiate(playerPrefab, SpawnPoint.GetRandomSpawnPosition(), Quaternion.identity);
        //playerInstance.SpawnAsPlayerObject(clientId);
        Debug.Log("Player is Spawned but no object for now");
    }


    // This method is called once we've basically started up the server, once it's ready to go
    private void OnNetworkReady()
    {
        networkManager.OnClientDisconnectCallback += OnClientDisconnect;
    }

    // Here we can actually choose whether we want to remove data once player disconnect,
    // even if he comes back, there will be no data about or progress he made.
    // Or, we can keep storing it 
    private void OnClientDisconnect(ulong clientId)
    {
        if (clientIdToAuth.TryGetValue(clientId, out string authId))
        {
            clientIdToAuth.Remove(clientId);

            OnUserLeft?.Invoke(authIdToUserData[authId]);

            authIdToUserData.Remove(authId);

            OnClientLeft?.Invoke(authId);
        }
    }

    public void SetCharacter(ulong clientId, int characterId)
    {
        if (clientIdToAuth.TryGetValue(clientId, out string auth))
        {
            if (authIdToUserData.TryGetValue(auth, out UserData data))
            {
                data.characterId = characterId;

                //  Debug Log to show updated data
                Debug.Log($"[SetCharacter] Client {clientId} ({auth}) selected Character ID: {characterId}");
                Debug.Log($"[SetCharacter] Current UserData: {JsonUtility.ToJson(data, true)}");
            }
            else
            {
                Debug.LogError($"[SetCharacter] authId {auth} not found in authIdToUserData!");
            }
        }
        else
        {
            Debug.LogError($"[SetCharacter] clientId {clientId} not found in clientIdToAuth!");
        }
    }

    public void SetWeapon(ulong clientId, int weaponId)
    {
        if (clientIdToAuth.TryGetValue(clientId, out string auth))
        {
            if (authIdToUserData.TryGetValue(auth, out UserData data))
            {
                data.weaponId = weaponId;

                //  Debug Log to show updated data
                Debug.Log($"[SetWeapon] Client {clientId} ({auth}) selected Weapon ID: {weaponId}");
                Debug.Log($"[SetWeapon] Current UserData: {JsonUtility.ToJson(data, true)}");
            }
            else
            {
                Debug.LogError($"[SetWeapon] authId {auth} not found in authIdToUserData!");
            }
        }
        else
        {
            Debug.LogError($"[SetWeapon] clientId {clientId} not found in clientIdToAuth!");
        }
    }

    public UserData GetUserDataByClientId(ulong clientId)
    {
        if (clientIdToAuth.TryGetValue(clientId, out string authId))
        {
            if (authIdToUserData.TryGetValue(authId, out UserData data))
            {
                return data;
            }

            return null;
        }

        return null;
    }

    public void StartGame()
    {
        gameHasStarted = true;

        Debug.Log("All players locked in. Starting the game...");

        NetworkManager.Singleton.SceneManager.LoadScene("Gameplay", LoadSceneMode.Single);
    }


    public void Dispose()
    {
        if (networkManager == null)
        {
            Debug.LogWarning("From NetworkServer, networkManager is null");
            return;
        }

        networkManager.ConnectionApprovalCallback -= ApprovalCheck;
        networkManager.OnClientDisconnectCallback -= OnClientDisconnect;
        networkManager.OnServerStarted -= OnNetworkReady;

        if (networkManager.IsListening)
        {
            networkManager.Shutdown();
        }
    }
}
