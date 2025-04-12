using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkServer : IDisposable
{
    private const string Game_Scene_Name = "Game";
    private const string Menu_Scene_Name = "Menu";

    private NetworkManager networkManager;

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

    // ToDo: Delete this, this is for dedicated server
    public bool OpenConnection(string ip, int port)
    {
        UnityTransport transport = networkManager.gameObject.GetComponent<UnityTransport>();

        transport.SetConnectionData(ip, (ushort)port);

        return networkManager.StartServer();
    }


    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // *********************
        // If want to prevent further joining for the game session, we can add gameHasStarted to prevent connection.
        // We can use it to prevent connection after Selection Scene.
        // *********************
        if (request.Payload.Length > MaxConnectionPayload)
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
            userData.clientId = request.ClientNetworkId;
        }
        catch
        {
            Debug.LogError("[Payload] Failed to deserialize user data payload.");
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

        // When we adjust the Approval Check in Network Manager,
        // We need to create the player object with this code,
        // beacuse Network Manager make it false when we change approval check automatically
        //// We make it false beacuse we created player inside the SpawnPlayerDelayed
        ////response.CreatePlayerObject = true;
        response.CreatePlayerObject = false;

        Debug.LogWarning($"Approval Request: ClientId {request.ClientNetworkId}, Payload {payload}");

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
        Debug.Log($"[NetworkServer] Client {clientId} is disconnecting...");

        if (clientIdToAuth.TryGetValue(clientId, out string authId))
        {
            clientIdToAuth.Remove(clientId);
            Debug.Log($"[NetworkServer] Removed clientId {clientId} from clientIdToAuth.");

            if (authIdToUserData.ContainsKey(authId))
            {
                OnUserLeft?.Invoke(authIdToUserData[authId]);
                authIdToUserData.Remove(authId);
                Debug.Log($"[NetworkServer] Removed authId {authId} from authIdToUserData.");
            }
            else
            {
                Debug.LogWarning($"[NetworkServer] authId {authId} not found in authIdToUserData.");
            }

            OnClientLeft?.Invoke(authId);
            Debug.Log($"[NetworkServer] OnClientLeft invoked for authId {authId}.");
        }
        else
        {
            Debug.LogWarning($"[NetworkServer] clientId {clientId} not found in clientIdToAuth.");
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

        NetworkManager.Singleton.SceneManager.LoadScene(Game_Scene_Name, LoadSceneMode.Single);
    }

    public void EndGame()
    {
        gameHasStarted = false;

        Debug.Log("The game is ended. Making all players left!");

        if (networkManager == null)
        {
            Debug.LogWarning("From NetworkServer, networkManager is null");
            return;
        }
        NetworkManager.Singleton.SceneManager.LoadScene(Menu_Scene_Name, LoadSceneMode.Single);

        NetworkManager.Singleton.SceneManager.OnLoadComplete += SceneManager_OnLoadComplete;

    }

    private void SceneManager_OnLoadComplete(ulong clientId, string sceneName, LoadSceneMode loadSceneMode)
    {
        Dispose();

        // Make the lobby's relay code resetted.
        LobbyManager.Instance.UpdateLobbyRelayCode("");
    }

    public void Dispose()
    {
        if (networkManager == null)
        {
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
