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

    private NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(GameState.WaitingForPlayers);

    private float startCountDown = 3f;

    public event Action<GameState> OnGameStateChanged;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"Game state Manager's OnNetworkSpawn is called on {name}");
        gameState.OnValueChanged += HandleGameStateChanged;

        // ToDo : Check if this code necessary for late joiners. Try it on Prep State, if Late Joiners also call the regular method, then do not need this.
        //HandleGameStateChanged(gameState.Value, gameState.Value);

        if (IsServer)
        {
            StartCoroutine(CheckForAllPlayersConnected());
        }

    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        gameState.OnValueChanged -= HandleGameStateChanged;

    }

    #region Game State Handling

    private void HandleGameStateChanged(GameState previous, GameState next)
    {
        Debug.Log($"Game state changed from {previous} to {next}");
        OnGameStateChanged?.Invoke(next);

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
            Debug.Log($"The game state changed from {gameState.Value} to {newState} from SetGameState");
            gameState.Value = newState;
        }
    }

    public GameState GetCurrentState()
    {
        return gameState.Value;
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
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            ulong clientId = client.ClientId;

            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var clientObj))
            {
                //var player = clientObj.PlayerObject.GetComponent<PlayerController>();
            }
        }

        Debug.Log("Trying to start Prep Timer from Handle Pre Game");
        TimerManager.Instance.StartPrepTimer(); // 🔥 Show countdown UI
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
        // Show post-game stats, etc.
    }

    private void HandleEndGame()
    {
        // Show post-game stats, etc.
    }
}
