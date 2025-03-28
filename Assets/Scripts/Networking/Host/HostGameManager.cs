using System;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HostGameManager : IDisposable
{
    private const string CharacterSelectionSceneName = "CharacterSelection";
    private const int MaxConnections = 10;

    NetworkObject playerPrefab;

    private Allocation allocation;

    public RelayHostData RelayHostData => relayHostData;
    private RelayHostData relayHostData;

    private string lobbyId;

    public string JoinCode { get; private set; }

    public NetworkServer NetworkServer { get; private set; }

    public HostGameManager()
    {
        
    }

    public async Task StartHostAsync()
    {
        try
        {
            // To create the max connection for Relay, to create allocation
            allocation = await RelayService.Instance.CreateAllocationAsync(MaxConnections);
        }
        catch (Exception exception)
        {
            Debug.Log(exception);
            return;
        }

        relayHostData = new RelayHostData
        {
            Key = allocation.Key,
            Port = (ushort)allocation.RelayServer.Port,
            AllocationID = allocation.AllocationId,
            AllocationIDBytes = allocation.AllocationIdBytes,
            ConnectionData = allocation.ConnectionData,
            IPv4Address = allocation.RelayServer.IpV4
        };

        try
        {
            // To connect other players, we need join code for particular allocation
            JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log(JoinCode);

            LobbyManager.Instance.UpdateLobbyRelayCode(JoinCode);
        }
        catch (Exception exception)
        {
            Debug.Log(exception);
            return;
        }


        // We then set the data on the transport so it's ready to go when we host the server.
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        transport.SetRelayServerData(RelayHostData.IPv4Address,
            RelayHostData.Port,
            RelayHostData.AllocationIDBytes,
            RelayHostData.Key,
            RelayHostData.ConnectionData);

       
        if(LobbyManager.Instance.IsInLobby())
        {
            lobbyId = LobbyManager.Instance.GetJoinedLobby().Id;
        }


        // For connection approval, we created NetworkServer to handle that
        NetworkServer = new NetworkServer(NetworkManager.Singleton);

        // To make connection with correct user name we entered on Bootsrap scene,
        // We convert data to byte and pass it into NetworkManager.
        // We did the exactly reverse in getting data in NetworkServer class
        UserData userData = new UserData
        {
            userName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "Missing Name"),
            userAuthId = AuthenticationService.Instance.PlayerId,
            teamIndex = LobbyManager.Instance.GetPlayerTeamIndex(AuthenticationService.Instance.PlayerId)
        };
        string payload = JsonUtility.ToJson(userData);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);

        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;

        NetworkManager.Singleton.StartHost();

        NetworkServer.OnClientLeft += HandleClientLeft;

        NetworkManager.Singleton.SceneManager.LoadScene(CharacterSelectionSceneName, LoadSceneMode.Single);
    }

    private async void HandleClientLeft(string authId)
    {
        try
        {
            Debug.Log($"[Server] Handle Client Left from HostGameManager with client {authId}");
            await LobbyService.Instance.RemovePlayerAsync(lobbyId, authId);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public void Dispose()
    {
        ShutDown();
    }

    public async Task<bool> IsRelayJoinCodeValid(string joinCode)
    {
        try
        {
            // Try to join the relay allocation to verify its validity
            await RelayService.Instance.JoinAllocationAsync(joinCode);
            return true;
        }
        catch (RelayServiceException e)
        {
            Debug.LogWarning($"[Relay] Join Code {joinCode} is no longer valid: {e.Message}");
            return false;
        }
    }

    public async void ShutDown()
    {
        if (string.IsNullOrEmpty(lobbyId)) return;

        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(lobbyId);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

        // If it tries to delete the lobby twice, just in case we put empty
        lobbyId = string.Empty;

        NetworkServer.OnClientLeft -= HandleClientLeft;

        NetworkServer?.Dispose();

    }

}

public struct RelayHostData
{
    public string JoinCode;
    public string IPv4Address;
    public ushort Port;
    public Guid AllocationID;
    public byte[] AllocationIDBytes;
    public byte[] ConnectionData;
    public byte[] Key;
}