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
        if (!IsServer) return; // Server-only logic

        var target = collision.gameObject.GetComponent<IDamageable>();
        if (target is NetworkBehaviour netTarget)
        {
            DealDamage(target); // 👈 Interface-required method
        }

        var destroyable = collision.gameObject.GetComponent<IDestroyable>();
        if (destroyable != null)
        {
            TriggerDestroy(destroyable);
        }

        var destroyer = collision.gameObject.GetComponent<IDestroyer>();
        if (destroyer != null)
        {
            DestroySelf();
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
            // 🔥 Get attacker + target team data
            if (!TeamUtils.AreOpponents(WeaponOwnerClientId, targetNetId))
            {
                Debug.Log("[AuraBlade] Skipping damage: target is same team.");
                return;
            }

            damageable.TakeDamage(damage, DeathType.Skill, WeaponOwnerClientId);
            ShowFloatingDamageClientRpc(damage);
        }
    }

    [ClientRpc]
    private void ShowFloatingDamageClientRpc(int damage)
    {
        if (NetworkManager.Singleton.LocalClientId != WeaponOwnerClientId) return;

        auraWeapon.GetCurrentPlayer()?.PlayerUIManager?.ShowFloatingDamage(Vector3.zero, damage);
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
