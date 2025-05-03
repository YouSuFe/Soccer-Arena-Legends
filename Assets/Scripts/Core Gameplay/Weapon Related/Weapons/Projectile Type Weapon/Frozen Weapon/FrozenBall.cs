using System;
using Unity.Netcode;
using UnityEngine;
public class FrozenBall : NetworkBehaviour, IProjectileNetworkInitializer
{
    [SerializeField] FrozenBallDataSO frozenBallData;

    private GameObject iceExplosionVFX;
    private GameObject projectileTrail;

    private FrozenWeapon frozenWeapon;

    private Rigidbody rb;

    public ulong WeaponOwnerClientId { get; private set; } = ulong.MaxValue;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void InitializeNetworkedProjectile(BaseWeapon weapon)
    {
        if (weapon is FrozenWeapon frozen)
        {
            frozenWeapon = frozen;
            WeaponOwnerClientId = frozen.OwnerClientId;

            AssignFrozenWeaponClientRpc(frozen.NetworkObjectId, RpcUtils.ToClient(frozen.OwnerClientId));
        }
    }

    [ClientRpc]
    private void AssignFrozenWeaponClientRpc(ulong frozenWeaponNetId, ClientRpcParams clientRpcParams = default)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(frozenWeaponNetId, out var netObj)
            && netObj.TryGetComponent<FrozenWeapon>(out var weapon))
        {
            frozenWeapon = weapon;
        }
    }

    void Start()
    {
        InitializeParticles();
    }

    private void InitializeParticles()
    {
        // Instantiate and activate the projectile trail effect
        if (frozenBallData.projectileTrail != null)
        {
            projectileTrail = Instantiate(frozenBallData.projectileTrail, transform.position, Quaternion.identity, transform);
            projectileTrail.SetActive(true);
        }

        // Instantiate the ice explosion effect but keep it inactive initially
        if (frozenBallData.iceExplosionVFX != null)
        {
            iceExplosionVFX = Instantiate(frozenBallData.iceExplosionVFX, transform.position, Quaternion.identity);
            iceExplosionVFX.SetActive(false);
        }
    }


    // ToDo: Add frozen state changer logic when TriggerEnter
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        // Check if the collided object is in the interactable layer
        if (((1 << collision.gameObject.layer) & frozenBallData.interactableLayerMask) != 0)
        {
            if (collision.gameObject.TryGetComponent<BallOwnershipManager>(out var ballManager))
            {
                ballManager.RegisterSkillInfluence(WeaponOwnerClientId);
                Debug.Log($"[FrozenBall] Skill influence registered on ball by Client {WeaponOwnerClientId}");
            }

            TriggerImpactClientRpc(transform.position);

            // Apply the frozen state to players within the skill radius
            Collider[] hitColliders = Physics.OverlapSphere(transform.position, frozenBallData.skillRadius, frozenBallData.enemyLayerMask);
            foreach (Collider hitCollider in hitColliders)
            {
                if (hitCollider.TryGetComponent<NetworkObject>(out var netObj))
                {
                    ulong targetId = netObj.NetworkObjectId;
                    ulong clientId = netObj.OwnerClientId;
                    ApplyFrozenStateClientRpc(targetId, RpcUtils.ToClient(clientId));
                }
            }

            // Destroy the frozen ball after a short delay to let the explosion effect play
            if (TryGetComponent<NetworkObject>(out var netObjSelf))
                netObjSelf.Despawn();
            else
                Destroy(gameObject);
        }
    }

    [ClientRpc]
    private void TriggerImpactClientRpc(Vector3 impactPosition)
    {
        if (iceExplosionVFX != null)
        {
            iceExplosionVFX.transform.position = impactPosition;
            iceExplosionVFX.SetActive(true);
        }

        if (projectileTrail != null)
        {
            projectileTrail.SetActive(false);
        }

        PlayExplosionSound();
    }

    [ClientRpc]
    private void ApplyFrozenStateClientRpc(ulong targetNetId, ClientRpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetId, out var targetObj))
        {
            if (targetObj.TryGetComponent<PlayerController>(out var enemyPlayer))
            {
                enemyPlayer.MovementStateMachine.ChangeState(PlayerState.Frozen);
            }
        }
    }

    private void PlayExplosionSound()
    {
        SoundManager.Instance.CreateSoundBuilder()
                            .WithPosition(transform.position)
                            .WithRandomPitch()
                            .Play(frozenBallData.frozenBallExpoSoundData);
    }

    public override void OnDestroy()
    {
        // Clean up particle effects if they’re still active
        if (iceExplosionVFX != null)
        {
            Destroy(iceExplosionVFX, 2f); // Optionally destroy after a short delay
        }

        if (projectileTrail != null)
        {
            Destroy(projectileTrail);
        }
    }
}
