using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SlowField : NetworkBehaviour
{
    #region Fields

    [SerializeField] private LayerMask interactableLayerMask;

    [SerializeField] private GameObject slowFieldVFX;

    [SerializeField] private SoundData soundData;

    private readonly HashSet<PlayerController> affectedPlayers = new();

    private readonly Dictionary<ulong, bool> playerSlowState = new();

    private float lifeTime = 10f;

    private GameObject spawnedVFX;
    private SoundEmitter soundEmitter;

    #endregion

    #region Unity Lifecycle

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Invoke(nameof(DespawnSelf), lifeTime); // server controls cleanup

            MultiplayerGameStateManager.Instance.NetworkGameState.OnValueChanged += HandleGameStateChanged;
        }

        if (IsClient)
        {
            TriggerVisualsClientRpc(transform.position);
        }
    }

    private void HandleGameStateChanged(GameState previous, GameState newState)
    {
        if (newState != GameState.InGame)
        {
            Debug.Log("[SlowField] Game state exited InGame. Despawning slow field.");
            DespawnSelf();
        }
    }

    private void DespawnSelf()
    {
        // Server only — clean up modifiers and despawn
        foreach (var player in affectedPlayers)
        {
            var stats = player.Player.Stats;

            RemoveSlowModifier(stats);
            SlowModifier(stats, 10f);

            if (playerSlowState.TryGetValue(player.OwnerClientId, out var wasSlowed) && wasSlowed)
            {
                NotifyClientSlowedClientRpc(false, RpcUtils.ToClient(player.OwnerClientId));
                playerSlowState[player.OwnerClientId] = false;
            }
        }

        affectedPlayers.Clear();
        playerSlowState.Clear();

        if (IsServer)
        {
            NetworkObject.Despawn();
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (IsServer && MultiplayerGameStateManager.Instance != null)
            MultiplayerGameStateManager.Instance.NetworkGameState.OnValueChanged -= HandleGameStateChanged;

        if (spawnedVFX != null) Destroy(spawnedVFX);
        if (soundEmitter != null) soundEmitter.StopManually();
    }

    #endregion



    #region Trigger Logic

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if ((interactableLayerMask.value & (1 << other.gameObject.layer)) == 0) return;

        var player = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
        if (player != null)
        {
            var stats = player.Player.Stats;

            RemoveSlowModifier(stats);
            SlowModifier(stats, -1f); // Permanent while in field
            affectedPlayers.Add(player);

            if (!playerSlowState.TryGetValue(player.OwnerClientId, out var wasSlowed) || !wasSlowed)
            {
                NotifyClientSlowedClientRpc(true, RpcUtils.ToClient(player.OwnerClientId));
                playerSlowState[player.OwnerClientId] = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;
        if ((interactableLayerMask.value & (1 << other.gameObject.layer)) == 0) return;

        var player = other.GetComponent<PlayerController>() ?? other.GetComponentInParent<PlayerController>();
        if (player != null && affectedPlayers.Contains(player))
        {
            var stats = player.Player.Stats;

            RemoveSlowModifier(stats);
            SlowModifier(stats, 10f); // Temporary after leaving
            affectedPlayers.Remove(player);

            if (playerSlowState.TryGetValue(player.OwnerClientId, out var wasSlowed) && wasSlowed)
            {
                NotifyClientSlowedClientRpc(false, RpcUtils.ToClient(player.OwnerClientId));
                playerSlowState[player.OwnerClientId] = false;
            }
        }
    }


    #endregion

    #region Client Modifier Sync

    [ClientRpc]
    private void NotifyClientSlowedClientRpc(bool isSlowed, ClientRpcParams rpcParams = default)
    {
        if (IsServer) return;

        var playerObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (playerObj != null && playerObj.TryGetComponent<PlayerController>(out var controller))
        {
            var stats = controller.Player.Stats;

            RemoveSlowModifier(stats);

            if (isSlowed)
            {
                SlowModifier(stats, -1f); // Permanent
            }
            else
            {
                SlowModifier(stats, 10f); // Exit slow
            }
        }
    }

    #endregion

    #region Modifier Methods

    // Apply a slow modifier with a specific duration
    private void SlowModifier(Stats stats, float duration)
    {
        float slowPercentage = -50f; // 50% slow
        StatModifierFactory statModifierFactory = new StatModifierFactory();

        StatModifier speedModifier = statModifierFactory.Create(
            OperatorType.MuliplyByPercentage,
            StatType.Speed,
            slowPercentage,
            duration, // -1 for permanent or a positive value for temporary
            ModifierSourceTag.SlowFieldWeaponSkill
        );

        stats.Mediator.AddModifier(speedModifier);
    }

    // Method to remove the slow modifier
    private void RemoveSlowModifier(Stats playerStats)
    {
        // Check if the player has the slow modifier
        StatModifier existingModifier = playerStats.Mediator.GetModifierBySourceTag(ModifierSourceTag.SlowFieldWeaponSkill);

        if (existingModifier != null)
        {
            // Remove the slow modifier
            playerStats.Mediator.RemoveModifier(existingModifier);

        }
        else
        {
            Debug.LogWarning("Error: No slow modifier found.");
        }
    }

    #endregion

    #region Visuals and Audio

    [ClientRpc]
    private void TriggerVisualsClientRpc(Vector3 position)
    {
        if (slowFieldVFX != null)
        {
            spawnedVFX = Instantiate(slowFieldVFX, position, Quaternion.identity);
        }

        soundEmitter = SoundManager.Instance.CreateSoundBuilder()
                            .WithPosition(position)
                            .WithRandomPitch()
                            .WithLoopDuration(lifeTime)
                            .Play(soundData);
    }

    #endregion
}
