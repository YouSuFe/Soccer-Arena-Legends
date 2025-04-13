using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public enum GameState
{
    WaitingForPlayers,
    PreGame,
    InGame,
    PostGame,
    EndGame
}

public class MultiplayerGameStateManager : NetworkBehaviour
{
    public static MultiplayerGameStateManager Instance;

    public NetworkVariable<GameState> NetworkGameState = new NetworkVariable<GameState>(GameState.WaitingForPlayers);

    private float startCountDown = 3f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"Game state Manager's OnNetworkSpawn is called on {name}");
        NetworkGameState.OnValueChanged += HandleGameStateChanged;

        // ToDo : Check if this code necessary for late joiners. Try it on Prep State, if Late Joiners also call the regular method, then do not need this.
        HandleGameStateChanged(NetworkGameState.Value, NetworkGameState.Value);

        if (IsServer)
        {
            StartCoroutine(CheckForAllPlayersConnected());
        }

    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        NetworkGameState.OnValueChanged -= HandleGameStateChanged;

    }

    #region Game State Handling

    private void HandleGameStateChanged(GameState previous, GameState next)
    {
        Debug.Log($"Game state changed from {previous} to {next}");

        switch (next)
        {
            case GameState.PreGame:
                HandlePreGame();
                break;
            case GameState.InGame:
                HandleInGame();
                break;
            case GameState.PostGame:
                HandlePostGame();
                break;
            case GameState.EndGame:
                HandleEndGame();
                break;
        }
    }

    public void SetGameState(GameState newState)
    {
        if (IsServer)
        {
            Debug.Log($"The game state changed from {NetworkGameState.Value} to {newState} from SetGameState");
            NetworkGameState.Value = newState;
        }
    }

    public GameState GetCurrentState()
    {
        return NetworkGameState.Value;
    }

    #endregion

    private IEnumerator CheckForAllPlayersConnected()
    {
        Debug.Log("[MultiplayerGameStateManager] Waiting for players to connect...");

        int expectedPlayers = GetExpectedPlayerCount();
        float timeout = 10f; // time to wait before reducing expectation
        float timer = 0f;

        Debug.Log($"[MultiplayerGameStateManager] Starting with expected {expectedPlayers} players.");

        while (expectedPlayers > 0)
        {
            int connected = NetworkManager.Singleton.ConnectedClients.Count;
            if (connected >= expectedPlayers)
            {
                Debug.Log($"[MultiplayerGameStateManager] {connected} players connected. Starting game.");
                break;
            }

            timer += Time.deltaTime;

            if (timer >= timeout)
            {
                expectedPlayers--; // Lower expected number over time
                timer = 0f;
                Debug.LogWarning($"[MultiplayerGameStateManager] Reducing expected players to {expectedPlayers} due to timeout.");
            }

            yield return null;
        }
        StartCoroutine(StartGame());
    }

    IEnumerator StartGame()
    {
        yield return new WaitForSeconds(startCountDown);
        Debug.Log("Starting to game with changing the state to PreGame");

        SetGameState(GameState.PreGame);
    }

    private int GetExpectedPlayerCount()
    {
        return LobbyManager.Instance.GetCurrentPlayerCount();
    }

    private void HandlePreGame()
    {
        Debug.Log("Trying to start Prep Timer from Handle Pre Game");
        TimerManager.Instance.StartPrepTimer(); // 🔥 Show countdown UI

        // Spawn all players on corresponding positions.
        PlayerSpawnManager.Instance.ResetAllPlayersToSpawn();
    }

    private void HandleInGame()
    {
        if(TimerManager.Instance.GetGameDurationValue() <= 0)
        {
            SetGameState(GameState.EndGame);
        }
        TimerManager.Instance.StartGameTimer(); // 🔥 Show countdown UI

    }

    private void HandlePostGame()
    {
        TimerManager.Instance.StartPostTimer(); // 🔥 Show countdown UI
    }

    private void HandleEndGame()
    {
        HostSingleton.Instance.GameManager.NetworkServer.EndGame();
    }
}
