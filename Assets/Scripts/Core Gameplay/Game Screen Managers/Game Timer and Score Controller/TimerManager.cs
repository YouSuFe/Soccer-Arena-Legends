using System;
using Unity.Netcode;
using UnityEngine;

public class TimerManager : NetworkBehaviour
{
    public static TimerManager Instance;

    private CountdownTimer gameTimer;
    private CountdownTimer prepTimer;
    private CountdownTimer postTimer;

    private bool isGameTimerPaused;

    [SerializeField] private float gameDuration = 300f;
    [SerializeField] private float prepDuration = 5f;
    [SerializeField] private float postDuration = 3f;

    public NetworkVariable<float> GameNetworkDuration = new NetworkVariable<float>(300);
    public NetworkVariable<float> PrepNetworkDuration = new NetworkVariable<float>(10);

    public float GetGameDurationValue() => GameNetworkDuration.Value;
    public float GetPrepDurationValue() => PrepNetworkDuration.Value;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Server initializes the durations
        if (IsServer)
        {
            GameNetworkDuration.Value = gameDuration;
            PrepNetworkDuration.Value = prepDuration;
        }
    }

    private void Update()
    {
        if (!IsServer) return;

        float deltaTime = Time.deltaTime;

        // Tick prep timer
        if (prepTimer?.IsRunning == true)
        {
            prepTimer.Tick(deltaTime);
            PrepNetworkDuration.Value = prepTimer.Time;
        }
        // Tick post timer
        if (postTimer?.IsRunning == true)
        {
            postTimer.Tick(deltaTime);
        }

        // Tick game timer only if not paused
        if (gameTimer?.IsRunning == true && !isGameTimerPaused)
        {
            gameTimer.Tick(deltaTime);
            GameNetworkDuration.Value = gameTimer.Time;
        }
    }

    public void StartPrepTimer()
    {
        prepTimer = new CountdownTimer(PrepNetworkDuration.Value);
        prepTimer.OnTimeUp += OnPrepTimerFinished;
        prepTimer.Start();

        PauseGameTimer();
    }

    public void StartPostTimer()
    {
        postTimer = new CountdownTimer(postDuration);
        postTimer.OnTimeUp += OnPostTimerFinished;
        postTimer.Start();

        PauseGameTimer();
    }

    public void StartGameTimer()
    {
        gameTimer = new CountdownTimer(GameNetworkDuration.Value);
        gameTimer.OnTimeUp += OnGameTimerFinished;
        gameTimer.Start();

        ResumeGameTimer();
    }

    public void PauseGameTimer() => isGameTimerPaused = true;
    public void ResumeGameTimer() => isGameTimerPaused = false;

    private void OnPrepTimerFinished()
    {
        MultiplayerGameStateManager.Instance.SetGameState(GameState.InGame);
        ResumeGameTimer();
    }

    private void OnPostTimerFinished()
    {
        MultiplayerGameStateManager.Instance.SetGameState(GameState.PreGame);
    }

    private void OnGameTimerFinished()
    {
        MultiplayerGameStateManager.Instance.SetGameState(GameState.EndGame);
    }
}