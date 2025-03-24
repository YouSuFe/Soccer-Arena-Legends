using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Triggers damage over time to enemy players that enter the penalty zone.
/// Stops damage when they leave or die. Only the server runs this logic.
/// </summary>
public class PenaltyZoneDamageTracker : NetworkBehaviour
{
    [Header("Zone Configuration")]
    [Tooltip("This zone belongs to a team. Only players from the opposing team will take damage.")]
    [SerializeField] private Team owningTeam;

    [Tooltip("Time (in seconds) between each damage tick.")]
    [SerializeField] private float damageIntervalSeconds = 0.7f;

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

    private void Update()
    {
        if (!IsServer) return;

        float deltaTime = Time.deltaTime;
        playersToRemove.Clear();

        foreach (var entry in playersInsideZone)
        {
            ulong clientId = entry.Key;
            PlayerZoneTimerData zoneData = entry.Value;

            if (zoneData.PlayerInstance == null)
            {
                playersToRemove.Add(clientId);
                continue;
            }

            zoneData.RemainingDamageTime -= deltaTime;

            if (zoneData.RemainingDamageTime <= 0f)
            {
                UserData userData = PlayerSpawnManager.Instance.GetUserData(clientId);
                if (userData == null || (Team)userData.teamIndex == owningTeam)
                {
                    playersToRemove.Add(clientId);
                    continue;
                }

                int damageAmount = zoneData.PlayerInstance.ActiveBall != null ? 500 : 20;

                zoneData.PlayerInstance.TakeDamage(damageAmount);

                // 🔥 Play effect on the damaged player only
                PlayPenaltyEffectClientRpc(clientId, zoneData.PlayerInstance.transform.position);

                zoneData.RemainingDamageTime = damageIntervalSeconds;
                playersInsideZone[clientId] = zoneData; // Update the struct back
            }
        }

        // Cleanup removed players (dead or invalid)
        for (int i = 0; i < playersToRemove.Count; i++)
        {
            playersInsideZone.Remove(playersToRemove[i]);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (!other.TryGetComponent<PlayerAbstract>(out var player)) return;

        ulong clientId = player.OwnerClientId;

        if (playersInsideZone.ContainsKey(clientId)) return;

        var userData = PlayerSpawnManager.Instance.GetUserData(clientId);
        if (userData == null || (Team)userData.teamIndex == owningTeam) return;

        // Create death callback bound to this specific client
        void OnPlayerDeath()
        {
            if (playersInsideZone.ContainsKey(clientId))
            {
                playersInsideZone.Remove(clientId);
                player.OnDeath -= OnPlayerDeath;
            }
        }

        // Subscribe to death
        player.OnDeath += OnPlayerDeath;

        // Track the player
        playersInsideZone.Add(clientId, new PlayerZoneTimerData
        {
            PlayerInstance = player,
            RemainingDamageTime = 0f, // Trigger immediate damage
            OnPlayerDeathCallback = OnPlayerDeath
        });
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
        }
    }

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

    /// <summary>
    /// Tracks data for each player in the zone.
    /// </summary>
    private struct PlayerZoneTimerData
    {
        public PlayerAbstract PlayerInstance;
        public float RemainingDamageTime;
        public System.Action OnPlayerDeathCallback;
    }
}
