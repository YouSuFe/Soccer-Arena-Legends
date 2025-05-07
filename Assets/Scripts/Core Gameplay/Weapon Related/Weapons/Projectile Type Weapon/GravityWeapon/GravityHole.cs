using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GravityHole : NetworkBehaviour, IProjectileNetworkInitializer
{
    public GravityHoleDataSO gravityHoleData;

    public ulong WeaponOwnerClientId { get; private set; } = ulong.MaxValue;

    private GravityWeapon gravityWeapon;

    private Vector3 startPosition;
    private Rigidbody rigidBody;
    private bool isPulling = false;

    private GameObject blackHoleVFX;
    private GameObject projectileTrail;

    private SoundEmitter currentSoundEmitter; // Reference to the currently playing phasing sound for infinite looping sounds

    private HashSet<PlayerController> affectedPlayers = new HashSet<PlayerController>(); // Track affected players
    private Dictionary<ulong, bool> playerSlowState = new();

    private void Awake()
    {
        rigidBody = GetComponent<Rigidbody>(); // OK here
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        startPosition = transform.position;
        InitializeParticles(); // Client-side particles, safe here

        // 👇 Add this to listen for game state changes
        if (IsServer && MultiplayerGameStateManager.Instance != null)
        {
            MultiplayerGameStateManager.Instance.NetworkGameState.OnValueChanged += HandleGameStateChanged;
        }
    }

    private void HandleGameStateChanged(GameState previous, GameState next)
    {
        if (next != GameState.InGame && IsServer && NetworkObject.IsSpawned)
        {
            Debug.Log("[GravityHole] Game state changed out of InGame — despawning gravity hole.");
            RemoveModifiersFromAllAffectedPlayers();
            NotifyClientsOfEndClientRpc();
            CleanupState();
            NetworkObject.Despawn();
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        CleanupState(); // << Optional, good for safety

        if (IsServer && MultiplayerGameStateManager.Instance != null)
        {
            MultiplayerGameStateManager.Instance.NetworkGameState.OnValueChanged -= HandleGameStateChanged;
        }
    }
    public void InitializeNetworkedProjectile(BaseWeapon weapon)
    {
        if (weapon is GravityWeapon gravity)
        {
            gravityWeapon = gravity;
            WeaponOwnerClientId = gravity.OwnerClientId;

            AssignGravityWeaponClientRpc(gravity.NetworkObjectId, RpcUtils.ToClient(gravity.OwnerClientId));
        }
    }

    [ClientRpc]
    private void AssignGravityWeaponClientRpc(ulong networkObjectId, ClientRpcParams clientRpcParams = default)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var netObj)
                    && netObj.TryGetComponent<GravityWeapon>(out var gravity))
        {
            gravityWeapon = gravity;
            Debug.Log("GravityWeapon reference assigned via ClientRpc.");
        }
        else
        {
            Debug.LogWarning("Failed to find GravityWeapon on client when assigning via ClientRpc.");
        }
    }

    private void Update()
    {
        if (IsServer && !isPulling)
        {
            if (Vector3.Distance(startPosition, transform.position) >= gravityHoleData.maxDistance)
            {
                ActivateBlackHole();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || isPulling)
            return;

        ActivateBlackHole();
    }

    private void ActivateBlackHole()
    {
        isPulling = true;
        rigidBody.linearVelocity = Vector3.zero;
        rigidBody.isKinematic = true;

        NotifyClientsOfActivationClientRpc(transform.position);

        StartCoroutine(BlackHoleAbility());
    }


    private IEnumerator BlackHoleAbility()
    {
        float elapsedTime = 0f;

        while (elapsedTime < gravityHoleData.abilityDuration)
        {
            PullObjectsInRadius();
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        RemoveModifiersFromAllAffectedPlayers();

        NotifyClientsOfEndClientRpc();

        CleanupState();
        NetworkObject.Despawn();
    }

    private void PullObjectsInRadius()
    {
        Collider[] objects = Physics.OverlapSphere(transform.position, gravityHoleData.pullRadius, gravityHoleData.pullableObjectsLayer);
        HashSet<PlayerController> playersInRange = new();

        foreach (Collider obj in objects)
        {
            PlayerController player = obj.GetComponent<PlayerController>();
            if (player != null)
            {
                playersInRange.Add(player);

                if (!affectedPlayers.Contains(player))
                {
                    affectedPlayers.Add(player);
                    ApplySlowModifier(player.Player.Stats);
                }

                if (!playerSlowState.TryGetValue(player.OwnerClientId, out var wasSlowed) || !wasSlowed)
                {
                    NotifyClientSlowedClientRpc(true, RpcUtils.ToClient(player.OwnerClientId));
                    playerSlowState[player.OwnerClientId] = true;
                }
            }

            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb != null && rb.gameObject != gameObject)
            {
                Vector3 dir = transform.position - obj.transform.position;
                float dist = dir.magnitude;
                float strength = Mathf.Lerp(gravityHoleData.maxPullForce, gravityHoleData.minPullForce, dist / gravityHoleData.pullRadius);
                rb.AddForce(dir.normalized * strength, ForceMode.Acceleration);

                if (obj.TryGetComponent<BallOwnershipManager>(out var ballManager))
                {
                    ballManager.RegisterSkillInfluence(WeaponOwnerClientId);
                }
            }
        }

        List<PlayerController> toRemove = new();

        foreach (var player in affectedPlayers)
        {
            if (!playersInRange.Contains(player))
            {
                RemoveSlowModifier(player.Player.Stats);
                toRemove.Add(player);

                if (playerSlowState.TryGetValue(player.OwnerClientId, out var wasSlowed) && wasSlowed)
                {
                    NotifyClientSlowedClientRpc(false, RpcUtils.ToClient(player.OwnerClientId));
                    playerSlowState[player.OwnerClientId] = false;
                }
            }
        }

        foreach (var p in toRemove)
            affectedPlayers.Remove(p);
    }

    private void RemoveModifiersFromAllAffectedPlayers()
    {
        foreach (var player in affectedPlayers)
        {
            RemoveSlowModifier(player.Player.Stats);
            if (playerSlowState.TryGetValue(player.OwnerClientId, out var wasSlowed) && wasSlowed)
            {
                NotifyClientSlowedClientRpc(false, RpcUtils.ToClient(player.OwnerClientId));
                playerSlowState[player.OwnerClientId] = false;
            }
        }

        affectedPlayers.Clear();
    }

    private void ApplySlowModifier(Stats stats)
    {
        StatModifier mod = new StatModifierFactory().Create(
            OperatorType.MuliplyByPercentage,
            StatType.Speed,
            -75f,
            -1,
            ModifierSourceTag.GravitySpeedModifier
        );
        stats.Mediator.AddModifier(mod);
    }

    private void RemoveSlowModifier(Stats stats)
    {
        stats.Mediator.RemoveModifierBySourceTag(ModifierSourceTag.GravitySpeedModifier);
    }




    #region Networking

    [ClientRpc]
    private void NotifyClientsOfActivationClientRpc(Vector3 position)
    {
        if (blackHoleVFX != null)
        {
            blackHoleVFX.transform.position = position;
            blackHoleVFX.SetActive(true);
        }

        projectileTrail?.SetActive(false);
        PlaySound();
    }

    [ClientRpc]
    private void NotifyClientsOfEndClientRpc()
    {
        if (IsServer) return;

        StopSound(currentSoundEmitter);
        CleanupEffects();
    }

    [ClientRpc]
    private void NotifyClientSlowedClientRpc(bool isSlowed, ClientRpcParams rpcParams = default)
    {
        if (IsServer) return;

        var playerObj = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
        if (playerObj != null && playerObj.TryGetComponent<PlayerController>(out var controller))
        {
            Stats stats = controller.Player.Stats;

            if (isSlowed)
            {
                ApplySlowModifier(stats);
            }
            else
            {
                stats.Mediator.RemoveModifierBySourceTag(ModifierSourceTag.GravitySpeedModifier);
            }
        }
    }


    #endregion

    private void InitializeParticles()
    {
        if (gravityHoleData.blackHoleVFX != null)
        {
            blackHoleVFX = Instantiate(gravityHoleData.blackHoleVFX, transform.position, Quaternion.identity, transform);
            blackHoleVFX.SetActive(false);
        }

        if (gravityHoleData.projectileTrail != null)
        {
            projectileTrail = Instantiate(gravityHoleData.projectileTrail, transform.position, Quaternion.identity, transform);
            projectileTrail.SetActive(true);
        }
    }

    private void CleanupState()
    {
        affectedPlayers.Clear();
        playerSlowState.Clear();
    }

    private void CleanupEffects()
    {
        if (blackHoleVFX != null) Destroy(blackHoleVFX);
        if (projectileTrail != null) Destroy(projectileTrail);
    }

    #region Sounds Settings

    private void PlaySound()
    {
        currentSoundEmitter = SoundManager.Instance.CreateSoundBuilder()
                            .WithPosition(transform.position)
                            .WithParent(transform)
                            .WithRandomPitch()
                            .Play(gravityHoleData.gravityHoleSoundData);
    }

    protected void StopSound(SoundEmitter soundEmitter)
    {
        if (soundEmitter != null)
        {
            soundEmitter.StopManually();
        }
    }

    #endregion

    // Optional: You can add this to visualize the sphere in the editor
    private void OnDrawGizmos()
    {
        if (gravityHoleData == null) return;

        // Solid semi-transparent sphere
        Gizmos.color = new Color(0.2f, 0.6f, 1f, 0.25f); // blueish with alpha
        Gizmos.DrawSphere(transform.position, gravityHoleData.pullRadius);

        // Optional: also draw wireframe for clarity
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, gravityHoleData.pullRadius);
    }
}
