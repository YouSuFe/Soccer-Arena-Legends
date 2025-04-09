using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// A centralized cooldown manager that tracks all players' cooldowns server-side.
/// Supports cooldowns for BallSkill and WeaponSkill, with full control over usage, reset, modification, and UI sync.
/// </summary>
public class CentralCooldownTracker : NetworkBehaviour
{
    #region Singleton

    public static CentralCooldownTracker Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    #endregion

    #region Internal Data

    /// <summary>
    /// Tracks cooldown state for a specific player (by ClientId).
    /// </summary>
    private class CooldownData
    {
        public float PlayerSkillRemaining;
        public float WeaponSkillRemaining;

        private bool wasBallSkillCooling = false;
        private bool wasWeaponSkillCooling = false;

        /// <summary>
        /// Reduces cooldown timers and detects when they end.
        /// </summary>
        public void Tick(float deltaTime, ulong clientId, Action<ulong, SkillType> notifyFinished)
        {
            if (PlayerSkillRemaining > 0f)
            {
                PlayerSkillRemaining = Mathf.Max(0f, PlayerSkillRemaining - deltaTime);

                if (PlayerSkillRemaining == 0f && wasBallSkillCooling)
                {
                    notifyFinished?.Invoke(clientId, SkillType.BallSkill);
                }

                wasBallSkillCooling = PlayerSkillRemaining > 0f;
            }

            if (WeaponSkillRemaining > 0f)
            {
                WeaponSkillRemaining = Mathf.Max(0f, WeaponSkillRemaining - deltaTime);

                if (WeaponSkillRemaining == 0f && wasWeaponSkillCooling)
                {
                    notifyFinished?.Invoke(clientId, SkillType.WeaponSkill);
                }

                wasWeaponSkillCooling = WeaponSkillRemaining > 0f;
            }
        }

        public float Get(SkillType type) =>
            type == SkillType.BallSkill ? PlayerSkillRemaining : WeaponSkillRemaining;

        public void Set(SkillType type, float value)
        {
            if (type == SkillType.BallSkill) PlayerSkillRemaining = value;
            else WeaponSkillRemaining = value;
        }

        public void Modify(SkillType type, float delta)
        {
            if (type == SkillType.BallSkill)
                PlayerSkillRemaining = Mathf.Max(0f, PlayerSkillRemaining + delta);
            else
                WeaponSkillRemaining = Mathf.Max(0f, WeaponSkillRemaining + delta);
        }

        public void Reset(SkillType type) => Set(type, 0f);

        public void ResetAll()
        {
            Reset(SkillType.BallSkill);
            Reset(SkillType.WeaponSkill);
        }
    }

    #endregion

    #region Storage

    private readonly Dictionary<ulong, CooldownData> cooldowns = new();

    #endregion

    #region MonoBehaviour

    private void Update()
    {
        if (!IsServer) return;

        float dt = Time.deltaTime;

        foreach (var kvp in cooldowns)
        {
            ulong clientId = kvp.Key;
            var data = kvp.Value;

            Debug.Log($"[Cooldown] Client {clientId} - BallSkill: {data.PlayerSkillRemaining:0.00}s, WeaponSkill: {data.WeaponSkillRemaining:0.00}s");

            kvp.Value.Tick(dt, clientId, (id, type) =>
            {
                NotifyCooldownEndedClientRpc(type, RpcUtils.ToClient(id));
            });
        }
    }

    #endregion

    #region Subscription

    public void RegisterPlayer(ulong clientId)
    {
        if (!cooldowns.ContainsKey(clientId))
        {
            cooldowns[clientId] = new CooldownData();
            Debug.Log($"[CooldownTracker] Registered client {clientId} to cooldown tracker.");
        }
    }

    public void UnregisterPlayer(ulong clientId)
    {
        if (cooldowns.ContainsKey(clientId))
        {
            cooldowns.Remove(clientId);
            Debug.Log($"[CooldownTracker] Unregistered client {clientId} from cooldown tracker.");
        }
    }

    #endregion
    #region Skill Usage API

    /// <summary>
    /// Attempts to use a skill for a player. Fails if still in cooldown.
    /// </summary>
    public bool TryUseSkill(ulong clientId, SkillType type, float cooldown)
    {
        var data = GetCooldownData(clientId);
        float currentRemaining = data.Get(type);

        // 🔍 Log the current cooldown state
        Debug.Log($"[Cooldown] Client {clientId} attempting to use {type}. Current cooldown remaining: {currentRemaining:0.00}s");

        if (data.Get(type) > 0f)
            return false;

        data.Set(type, cooldown);

        NotifySkillUsedClientRpc(type, cooldown, RpcUtils.ToClient(clientId));
        NotifyClientCooldownChangedClientRpc(type, cooldown, RpcUtils.ToClient(clientId));

        return true;
    }

    /// <summary>
    /// Gets remaining cooldown for a player's skill.
    /// </summary>
    public float GetRemainingCooldown(ulong clientId, SkillType type) =>
        GetCooldownData(clientId).Get(type);

    #endregion

    #region Modify & Reset Individual

    /// <summary>
    /// Adds or subtracts time from a player's skill cooldown.
    /// </summary>
    public void ModifyCooldownForPlayer(ulong clientId, SkillType type, float delta)
    {
        var data = GetCooldownData(clientId);
        data.Modify(type, delta); 

        float remaining = data.Get(type);
        NotifyClientCooldownChangedClientRpc(type, remaining, RpcUtils.ToClient(clientId)); // ✅ sync
    }

    /// <summary>
    /// Resets a specific skill's cooldown for a player.
    /// </summary>
    public void ResetCooldownForPlayer(ulong clientId, SkillType type)
    {
        var data = GetCooldownData(clientId);
        data.Reset(type);
        NotifyClientCooldownChangedClientRpc(type, 0f, RpcUtils.ToClient(clientId)); // ✅ sync
    }

    /// <summary>
    /// Resets both skill cooldowns for a player.
    /// </summary>
    public void ResetAllCooldownsForPlayer(ulong clientId)
    {
        var data = GetCooldownData(clientId);
        data.ResetAll();

        NotifyClientCooldownChangedClientRpc(SkillType.BallSkill, 0f, RpcUtils.ToClient(clientId));
        NotifyClientCooldownChangedClientRpc(SkillType.WeaponSkill, 0f, RpcUtils.ToClient(clientId));
    }

    #endregion

    #region Modify & Reset ALL Players

    /// <summary>
    /// Resets the given skill cooldown for ALL players.
    /// </summary>
    public void ResetAllPlayersCooldown(SkillType type)
    {
        foreach (var pair in cooldowns)
        {
            pair.Value.Reset(type);
            float remaining = pair.Value.Get(type);

            // Send to the actual owner only
            NotifyClientCooldownChangedClientRpc(type, remaining, RpcUtils.ToClient(pair.Key));
        }
    }

    /// <summary>
    /// Adds time to a specific skill cooldown for all players (e.g. debuff event).
    /// </summary>
    public void ModifyAllPlayersCooldown(SkillType type, float amount)
    {
        foreach (var pair in cooldowns)
        {
            pair.Value.Modify(type, amount);
            float remaining = pair.Value.Get(type);

            NotifyClientCooldownChangedClientRpc(type, remaining, RpcUtils.ToClient(pair.Key));
        }
    }

    #endregion

    #region Internals

    private CooldownData GetCooldownData(ulong clientId)
    {
        if (!cooldowns.TryGetValue(clientId, out var data))
        {
            data = new CooldownData();
            cooldowns[clientId] = data;
        }

        return data;
    }

    [ClientRpc]
    private void NotifySkillUsedClientRpc(SkillType type, float remaining, ClientRpcParams rpcParams = default)
    {
        Debug.Log($"[Client {NetworkManager.Singleton.LocalClientId}] Skill used: {type} (cooldown = {remaining}s)");
    }


    [ClientRpc]
    private void NotifyClientCooldownChangedClientRpc(SkillType type, float remaining, ClientRpcParams rpcParams = default)
    {
        var player = GetLocalPlayer();

        if (player != null && player.IsSpawned)
        {
            player.TriggerCooldownChanged(type, remaining);
        }
        else
        {
            Debug.LogWarning($"[CooldownSync] Player not found or not spawned. Using fallback HUD update.");
            HUDCanvasManager.Instance?.PlayerUIController?.UpdateSkillCooldown(type, remaining);
        }
    }


    /// <summary>
    /// Notifies client when cooldown reaches zero (final sync/correction).
    /// </summary>
    [ClientRpc]
    private void NotifyCooldownEndedClientRpc(SkillType type, ClientRpcParams rpcParams = default)
    {
        var player = GetLocalPlayer();

        if (player != null && player.IsSpawned)
        {
            player.ForceLocalCooldownToReady(type);
        }
        else
        {
            Debug.LogWarning($"[CooldownSync] Player not found or not spawned for cooldown end.");
            HUDCanvasManager.Instance?.PlayerUIController?.ForceCooldownComplete(type);
        }
    }

    #endregion

    #region Util

    public PlayerAbstract GetLocalPlayer()
    {
        var localPlayerObject = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();

        if (localPlayerObject == null)
        {
            Debug.LogWarning("[PlayerUtils] Local player object is null! Has the player spawned yet?");
            return null;
        }

        var playerComponent = localPlayerObject.GetComponent<PlayerAbstract>();

        if (playerComponent == null)
        {
            Debug.LogWarning("[PlayerUtils] Local player object found, but it does not have a PlayerAbstract component.");
        }
        else
        {
            Debug.Log($"[PlayerUtils] Successfully resolved local PlayerAbstract: {playerComponent.name}");
        }

        return playerComponent;
    }

    #endregion
}
