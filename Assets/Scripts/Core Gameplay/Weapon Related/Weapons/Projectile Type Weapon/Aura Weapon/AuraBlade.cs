using Unity.Netcode;
using UnityEngine;

public class AuraBlade : NetworkBehaviour, IProjectileNetworkInitializer, IDestroyable, IDestroyer, IDamageDealer
{
    #region Fields
    public AuraBladeDataSO auraBladeData;

    public ulong WeaponOwnerClientId { get; private set; } = ulong.MaxValue;

    private AuraWeapon auraWeapon; // Reference to the AuraWeapon

    #endregion

    void Awake()
    {

    }

    void Start()
    {
        if (IsServer)
        {
            Destroy(gameObject, auraBladeData.lifetime);
        }

        PlayAuraBladeSound();
    }

    public void InitializeNetworkedProjectile(BaseWeapon weapon)
    {
        if (weapon is AuraWeapon aura)
        {
            auraWeapon = aura;
            WeaponOwnerClientId = aura.OwnerClientId;
        }
    }

    public void Initialize(AuraWeapon weapon)
    {
        auraWeapon = weapon;
        WeaponOwnerClientId = weapon.OwnerClientId;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        // Damageable logic
        if (collision.gameObject.TryGetComponent<IDamageable>(out var target) && target is NetworkBehaviour netTarget)
        {
            DealDamage(target);
        }

        // Destroyable logic
        if (collision.gameObject.TryGetComponent<IDestroyable>(out var destroyable))
        {
            TriggerDestroy(destroyable);
        }

        // Self-destruction trigger
        if (collision.gameObject.TryGetComponent<IDestroyer>(out var destroyer))
        {
            DestroySelf();
        }

        // Ball interaction tracking
        if (collision.gameObject.TryGetComponent<BallOwnershipManager>(out var ballManager))
        {
            ballManager.RegisterSkillInfluence(WeaponOwnerClientId);
        }
    }


    #region Damage Logic
    public int CalculateDamage()
    {
        if (auraWeapon != null)
        {
            return Mathf.RoundToInt(auraWeapon.ReturnCalculatedDamage() * auraBladeData.damageMultiplier);
        }
        return 0;
    }

    public void DealDamage(IDamageable target)
    {
        if (target is NetworkBehaviour netTarget)
        {
            int damage = CalculateDamage();
            DealDamageServerRpc(netTarget.NetworkObjectId, damage);
        }
        else
        {
            Debug.LogError("Target is not a NetworkBehaviour — cannot deal networked damage.");
        }
    }

    [ServerRpc]
    private void DealDamageServerRpc(ulong targetNetId, int damage)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetId, out var targetObj)) return;

        var damageable = targetObj.GetComponent<IDamageable>();
        if (damageable != null)
        {
            // ✅ Get target ClientId from the NetworkObject
            ulong targetClientId = targetObj.OwnerClientId;

            // ✅ Team check
            if (!TeamUtils.AreOpponents(WeaponOwnerClientId, targetClientId))
            {
                Debug.Log("[AuraBlade] Skipping damage: target is same team.");
                return;
            }

            // ✅ Apply damage
            damageable.TakeDamage(damage, DeathType.Skill, WeaponOwnerClientId);

            // ✅ Send floating damage UI only to the attacker
            ShowFloatingDamageClientRpc(damage, RpcUtils.ToClient(WeaponOwnerClientId));
        }
    }

    [ClientRpc]
    private void ShowFloatingDamageClientRpc(int damage, ClientRpcParams rpcParams = default)
    {
        auraWeapon.GetCurrentPlayer()?.PlayerUIController?.ShowFloatingDamage(Vector3.zero, damage);
    }


    #endregion

    #region Destroy Logic Methods
    public void TriggerDestroy(IDestroyable destroyable)
    {
        // Trigger the destruction of the other object
        destroyable.Destroy();
    }

    
    // ToDo: Add particle and sound effects
    public void Destroy()
    {
        DestroySelf();
    }

    private void DestroySelf()
    {
        if (IsServer)
        {
            if (TryGetComponent<NetworkObject>(out var netObj))
                netObj.Despawn();
            else
                Destroy(gameObject);
        }
    }

    #endregion

    #region Sounds
    private void PlayAuraBladeSound()
    {
        SoundManager.Instance.CreateSoundBuilder()
                                    .WithPosition(transform.position)
                                    .WithParent(transform)
                                    .WithRandomPitch()
                                    .WithLoopDuration(auraBladeData.lifetime)
                                    .Play(auraBladeData.SoundData);
    }
    #endregion
}
