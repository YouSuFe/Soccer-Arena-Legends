using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;


public class ClientGameManager : IDisposable
{
    private const string MenuSceneName = "Menu";

    private NetworkClient networkClient;

    public UserData UserData { get; private set; }

    public async Task<bool> InitAsync()
    {
        await UnityServices.InitializeAsync();

        networkClient = new NetworkClient(NetworkManager.Singleton);

        AuthState authState = await AuthenticationWrapper.DoAuth();

        Debug.Log(AuthenticationService.Instance.PlayerId);

        if (authState == AuthState.Authenticated)
        {
            // To make connection with correct user name we entered on Bootsrap scene,
            // We convert data to byte and pass it into NetworkManager.
            // We did the exactly reverse in getting data in NetworkServer class
            UserData = new UserData
            {
                userName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "Missing Name"),
                userAuthId = AuthenticationService.Instance.PlayerId
            };
            return true;
        }

        return false;
    }

    #region Connection Handle


    public async Task<JoinAllocation> StartClientAsync(string joinCode)
    {
        Debug.Log($"Starting networkClient with join code {joinCode}\nWith : {UserData}");

        // Get the player's team index from the lobby
        int teamIndex = LobbyManager.Instance.GetPlayerTeamIndex(AuthenticationService.Instance.PlayerId);

        // Assign the team index to UserData before connecting
        UserData = new UserData
        {
            userName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "Missing Name"),
            userAuthId = AuthenticationService.Instance.PlayerId,
            teamIndex = teamIndex
        };

        return await networkClient.StartClient(joinCode);
    }


    public void GoToMenu()
    {
        SceneManager.LoadScene(MenuSceneName);
    }

    #endregion

    #region Clean Up

    public void Disconnect()
    {
        Debug.Log("Disconnect from ClientGameManager");
        networkClient.Disconnect();
    }

    public void Dispose()
    {
        networkClient?.Dispose();
    }

    #endregion


}
