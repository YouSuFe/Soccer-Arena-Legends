using System;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkClient : IDisposable
{
    private const string MenuSceneName = "Menu";

    private NetworkManager networkManager;
    private RelayJoinData relayJoinData;
    private JoinAllocation allocation;
    private const int TimeoutDuration = 10;

    public NetworkClient(NetworkManager networkManager)
    {
        this.networkManager = networkManager;

        networkManager.OnClientDisconnectCallback += OnClientDisconnect;
    }

    public async Task<JoinAllocation> StartClient(string joinCode)
    {
        allocation = null;

        try
        {
            allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        }
        catch (Exception e)
        {
            Debug.Log(e);
            return null;
        }

        relayJoinData = new RelayJoinData
        {
            Key = allocation.Key,
            Port = (ushort)allocation.RelayServer.Port,
            AllocationID = allocation.AllocationId,
            AllocationIDBytes = allocation.AllocationIdBytes,
            ConnectionData = allocation.ConnectionData,
            HostConnectionData = allocation.HostConnectionData,
            IPv4Address = allocation.RelayServer.IpV4
        };

        UnityTransport unityTransport = networkManager.gameObject.GetComponent<UnityTransport>();

        unityTransport.SetRelayServerData(relayJoinData.IPv4Address,
            relayJoinData.Port,
            relayJoinData.AllocationIDBytes,
            relayJoinData.Key,
            relayJoinData.ConnectionData,
            relayJoinData.HostConnectionData);

        ConnectClient();

        return allocation;
    }

    private void ConnectClient()
    {
        UserData userData = ClientSingleton.Instance.GameManager.UserData;

        string payload = JsonUtility.ToJson(userData);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);

        networkManager.NetworkConfig.ConnectionData = payloadBytes;
        networkManager.NetworkConfig.ClientConnectionBufferTimeout = TimeoutDuration;

        if (networkManager.StartClient())
        {
            Debug.Log("Starting Client!");
        }
        else
        {
            Debug.LogWarning("Could not start Client!");
        }
    }

    // Here when the client is disconnected, we need to shutdown the network and change the scene
    private void OnClientDisconnect(ulong clientId)
    {
        // Means Host is disconnected
        if (clientId != 0 && clientId != networkManager.LocalClientId) return;

        Disconnect();
    }

    public void Disconnect()
    {
        if (networkManager.IsServer) // Ensure server cleans up lobby data
        {
            LobbyManager.Instance.ClearLobbyServerDetails();
            Debug.Log("Server clearing lobby server details.");
        }

        if (SceneManager.GetActiveScene().name != MenuSceneName)
        {
            SceneManager.LoadScene(MenuSceneName);
        }

        if (networkManager.IsConnectedClient)
        {
            networkManager.Shutdown();
            Debug.LogWarning("Shutting Down");
        }
    }

    public void Dispose()
    {
        if (networkManager != null)
        {
            networkManager.OnClientDisconnectCallback -= OnClientDisconnect;
        }
    }

}

public struct RelayJoinData
{
    public string JoinCode;
    public string IPv4Address;
    public ushort Port;
    public Guid AllocationID;
    public byte[] AllocationIDBytes;
    public byte[] ConnectionData;
    public byte[] HostConnectionData;
    public byte[] Key;
}
