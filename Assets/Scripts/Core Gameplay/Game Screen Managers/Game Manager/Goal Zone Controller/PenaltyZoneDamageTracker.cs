using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Triggers damage over time to enemy players that enter the penalty zone.
/// Stops damage when they leave or die. Handles cleanup when game state changes.
/// Only runs on the server.
/// </summary>
public class PenaltyZoneDamageTracker : NetworkBehaviour
{
    #region === Zone Configuration ===

    [Header("Zone Configuration")]
    [Tooltip("This zone belongs to a team. Only players from the opposing team will take damage.")]
    [SerializeField] private Team owningTeam;

    [Tooltip("Time (in seconds) between each damage tick.")]
    [SerializeField] private float damageIntervalSeconds = 0.7f;
    [SerializeField] private int regularDamage = 10;
    [SerializeField] private int damageWithBall = 500;

    #endregion

    #region === Zone Tracking ===

    /// <summary>
    /// Tracks all enemy players currently inside this zone.
    /// Key: clientId, Value: player info including timer and death handler.
    /// </summary>
    private readonly Dictionary<ulong, PlayerZoneTimerData> playersInsideZone = new();

    /// <summary>
    /// Reused list to track players that should be removed at the end of Update().
    /// Prevents GC allocation.
    /// </summary>
    private readonly List<ulong> playersToRemove = new();

    private readonly List<ulong> tempPlayerKeys = new(); // Declare at the top of your class to avoid GC

    #endregion

    #region === Network Lifecycle ===

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        MultiplayerGameStateManager.Instance.OnGameStateChanged += OnGameStateChanged;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        if (MultiplayerGameStateManager.Instance != null)
            MultiplayerGameStateManager.Instance.OnGameStateChanged -= OnGameStateChanged;
    }

    #endregion

    #region === Game State Handling ===

    /// <summary>
    /// Called when the game state changes. Clears zone data if game is not in an active damage phase.
    /// </summary>
    private void OnGameStateChanged(GameState newState)
    {
        if (newState == GameState.PreGame || newState == GameState.EndGame)
        {
            ForceClearPenaltyZone();
        }
    }

    /// <summary>
    /// Clears all players from the zone and unsubscribes from their death events.
    /// Useful during game state transitions or resets.
    /// </summary>
    private void ForceClearPenaltyZone()
    {
        foreach (var kvp in playersInsideZone)
        {
            var player = kvp.Value.PlayerInstance;
            if (player != null)
            {
                player.OnDeath -= kvp.Value.OnPlayerDeathCallback;
            }
        }

        playersInsideZone.Clear();
        playersToRemove.Clear(); // Ensure clean state
    }

    #endregion

    #region === Zone Logic ===

    private void Update()
    {
        if (!IsServer) return;
        if (MultiplayerGameStateManager.Instance.GetCurrentState() != GameState.InGame) return;

        float deltaTime = Time.deltaTime;
        playersToRemove.Clear();
        tempPlayerKeys.Clear();
        tempPlayerKeys.AddRange(playersInsideZone.Keys);

        foreach (ulong clientId in tempPlayerKeys)
        {
            if (!playersInsideZone.TryGetValue(clientId, out var zoneData))
            {
                Debug.LogWarning($"[PenaltyZone] Client {clientId} missing from dictionary.");
                continue;
            }

            if (zoneData.PlayerInstance == null)
            {
                Debug.LogWarning($"[PenaltyZone] Client {clientId}'s PlayerInstance is null. Scheduling removal.");
                playersToRemove.Add(clientId);
                continue;
            }

            // Tick timer
            zoneData.RemainingDamageTime -= deltaTime;

            if (zoneData.RemainingDamageTime <= 0f)
            {
                var userData = PlayerSpawnManager.Instance.GetUserData(clientId);
                if (userData == null || (Team)userData.teamIndex == owningTeam)
                {
                    Debug.Log($"[PenaltyZone] Client {clientId} no longer valid (team or data). Removing.");
                    playersToRemove.Add(clientId);
                    continue;
                }

                int damageAmount = zoneData.PlayerInstance.CheckIfCurrentlyHasBall() ? damageWithBall : regularDamage;
                Debug.Log($"[PenaltyZone] Client {clientId} takes damage: {damageAmount}");

                zoneData.PlayerInstance.TakeDamage(damageAmount);
                PlayPenaltyEffectClientRpc(clientId, zoneData.PlayerInstance.transform.position);

                zoneData.RemainingDamageTime = damageIntervalSeconds;
            }

            // ✅ Always assign updated struct back
            playersInsideZone[clientId] = zoneData;
        }

        for (int i = 0; i < playersToRemove.Count; i++)
        {
            playersInsideZone.Remove(playersToRemove[i]);
            Debug.Log($"[PenaltyZone] Removed client {playersToRemove[i]} from zone.");
        }

        tempPlayerKeys.Clear();
    }


    #endregion

    #region === Trigger Handlers ===

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (!other.TryGetComponent<PlayerAbstract>(out var player)) return;

        ulong clientId = player.OwnerClientId;

        if (playersInsideZone.ContainsKey(clientId)) return;

        var userData = PlayerSpawnManager.Instance.GetUserData(clientId);
        if (userData == null || (Team)userData.teamIndex == owningTeam) return;

        void OnPlayerDeath()
        {
            if (playersInsideZone.ContainsKey(clientId))
            {
                playersInsideZone.Remove(clientId);
                player.OnDeath -= OnPlayerDeath;
                Debug.Log($"[PenaltyZone] Player {clientId} died and was removed from zone.");
            }
        }

        player.OnDeath += OnPlayerDeath;

        playersInsideZone.Add(clientId, new PlayerZoneTimerData
        {
            PlayerInstance = player,
            RemainingDamageTime = 0f,
            OnPlayerDeathCallback = OnPlayerDeath
        });

        Debug.Log($"[PenaltyZone] Client {clientId} ENTERED the zone.");
    }


    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;

        if (!other.TryGetComponent<PlayerAbstract>(out var player)) return;

        ulong clientId = player.OwnerClientId;

        if (playersInsideZone.TryGetValue(clientId, out var zoneData))
        {
            player.OnDeath -= zoneData.OnPlayerDeathCallback;
            playersInsideZone.Remove(clientId);

            Debug.Log($"[PenaltyZone] Client {clientId} EXITED the zone.");
        }
    }

    #endregion

    #region === Utilities ===

    /// <summary>
    /// Removes a player manually from the zone. Useful if player is moved/teleported outside.
    /// For now, nothing to do. But, maybe become usefull
    /// </summary>
    public void PlayerLeftZone(PlayerAbstract player)
    {
        ulong clientId = player.OwnerClientId;

        if (playersInsideZone.TryGetValue(clientId, out var zoneData))
        {
            player.OnDeath -= zoneData.OnPlayerDeathCallback;
            playersInsideZone.Remove(clientId);
        }
    }

    #endregion

    #region === Visual Effects ===

    [ClientRpc]
    private void PlayPenaltyEffectClientRpc(ulong targetClientId, Vector3 hitPosition)
    {
        Debug.Log($"Client : {targetClientId}, gets hit from damage zone on penalty. From {owningTeam} side.");
        // ToDo: Make these workable when player gets hit,
        //VFXManager.Instance.SpawnHitEffect(hitPosition); // Your custom VFX call

        //SoundManager.Instance.CreateSoundBuilder()
        //    .WithPosition(hitPosition)
        //    .WithRandomPitch()
        //    .Play(SoundLibrary.Instance.penaltyZoneDamage); // Your custom SFX
    }

    #endregion

    #region === Data Structs ===

    /// <summary>
    /// Tracks data for each player in the zone.
    /// </summary>
    private struct PlayerZoneTimerData
    {
        public PlayerAbstract PlayerInstance;
        public float RemainingDamageTime;
        public System.Action OnPlayerDeathCallback;
    }

    #endregion
}
