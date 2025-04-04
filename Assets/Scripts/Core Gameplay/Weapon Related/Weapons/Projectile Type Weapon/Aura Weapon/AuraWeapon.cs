using UnityEngine;
using System;
using Unity.Netcode;

public class AuraWeapon : BaseWeapon
{
    #region Weapon Configuration

    [Header("Projectile Sound Data")]
    [SerializeField] SoundData projectileSoundData;

    [Header("Aura Weapon Settings")]
    [Tooltip("The prefab for the Aura Blade projectile.")]
    private GameObject auraBladePrefab;

    [Tooltip("The point where the Aura Blade projectile spawns.")]
    private Transform projectileHolder;

    [Tooltip("Reference to the player's camera.")]
    private Camera playerCamera;

    [Tooltip("The speed of the Aura Blade projectile.")]
    private float auraBladeProjectileSpeed;

    #endregion

    #region Initialization Methods

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        projectileBehaviour = GetComponent<RotatingProjectile>();

        if (projectileBehaviour == null)
        {
            projectileBehaviour = gameObject.AddComponent<RotatingProjectile>();
        }

        SetShootingBehaviour(projectileBehaviour);

    }
    // Initialize method to set up the weapon with the appropriate data and parent it to the weapon holder
    public override void Initialize(Transform weaponHolder, Camera playerCamera, Transform projectileHolder)
    {
        // Assign necessary references
        if(weaponData.ProjectilePrefab != null)
        {
            this.auraBladePrefab = weaponData.ProjectilePrefab;
        }

        this.weaponHolder = weaponHolder;
        this.projectileHolder = projectileHolder;

        // Get Camera.main in case of first initializing camera null issue
        this.playerCamera = playerCamera != null ? playerCamera : Camera.main;

        if (this.playerCamera != null)
            Debug.Log("Successfully, initialized player Camera");

        this.auraBladeProjectileSpeed = weaponData.ProjectileSpeed;

        Debug.Log($"AuraWeapon initialized. Projectile prefab: {(auraBladePrefab != null ? "Assigned" : "Missing")}");

    }

    #endregion

    #region Weapon Skills and Attacks

    // Method to execute the Aura Weapon's special skill
    protected override void ExecuteSpecialSkill()
    {
        Debug.Log("Executing Aura Skill!");

        if (auraBladePrefab == null)
        {
            Debug.LogError("auraBladePrefab is not assigned!");
            return;
        }

        if (projectileHolder == null)
        {
            Debug.LogError("projectileHolder is not assigned!");
            return;
        }

        if (playerCamera == null)
        {
            Debug.LogError("playerCamera is not assigned!");
            return;
        }

        // Execute the projectile behavior using the defined parameters
        projectileBehaviour?.Shoot(projectileHolder, playerCamera, auraBladePrefab, auraBladeProjectileSpeed, this);

        // ToDo: Make sound for all player with RPC
    }

    protected override void PlaySkillEffects()
    {
        //PlayProjectileSound();
    }

    protected override void RegularAttackBehaviour()
    {
        base.PerformRegularAttack();
        ExecuteAttack("Aura Weapon Regular Attack is Called!", DealDamage, isHeavyAttack: false);
    }

    protected override void HeavyAttackBehaviour()
    {
        base.PerformHeavyAttack();
        ExecuteAttack("Aura Weapon Heavy Attack is Called!", DealHeavyDamage, isHeavyAttack: true);
    }

    #endregion

    #region Attack Execution

    // Executes a regular or heavy attack based on the passed parameters
    private void ExecuteAttack(string attackLogMessage, Action<IDamageable> damageAction, bool isHeavyAttack)
    {
        Debug.Log(attackLogMessage);

        if (raycastHolder == null)
        {
            Debug.LogError("Raycast holder is not assigned!");
            return;
        }

        float weaponLength = weaponData.WeaponLength;
        float hitRadius = weaponData.HitRadius;

        Vector3 rayOrigin = raycastHolder.position;
        Vector3 rayDirection = -raycastHolder.forward;

        RaycastHit[] hits = Physics.SphereCastAll(rayOrigin, hitRadius, rayDirection, weaponLength, weaponData.DamageableLayer);

        if (hits.Length > 0)
        {
            // 🔊 Play local hit sound for attacker
            if (isHeavyAttack)
                PlayHeavyAttackHitSound();
            else
                PlayRegularAttackHitSound();

            // 🔊 Tell others to play hit sound
            PlayAttackHitSoundClientRpc(isHeavyAttack, OwnerClientId, transform.position);

            foreach (RaycastHit hit in hits)
            {
                IDamageable damageable = hit.collider.GetComponent<IDamageable>();
                if (damageable != null)
                {
                    damageAction(damageable);
                    Debug.Log($"Hit {hit.collider.name} and dealt damage.");
                }
                else
                {
                    Debug.Log("Hit object, but it's not damageable: " + hit.collider.name);
                }
            }
        }
        else
        {
            // 🔊 Play local miss sound for attacker
            if (isHeavyAttack)
                PlayHeavyAttackSound();
            else
                PlayRegularAttackSound();

            Debug.Log("No hit detected.");
        }
    }


    #endregion

    #region Utility Methods

    // Auto-calculates the weapon's length from its prefab
    private float GetWeaponLengthFromPrefab()
    {
        if (auraBladePrefab != null)
        {
            Debug.Log("Aura blade prefab is assigned. Calculating weapon length.");
            return auraBladePrefab.GetComponent<Renderer>().bounds.size.z;
        }

        // Fallback to the value from WeaponDataSO if prefab is unavailable
        return weaponData.WeaponLength;
    }

    #endregion

    private void PlayProjectileSound()
    {
        SoundManager.Instance.CreateSoundBuilder()
                                    .WithPosition(auraBladePrefab.transform.position)
                                    .WithParent(auraBladePrefab.transform)
                                    .WithRandomPitch()
                                    .Play(projectileSoundData);
    }


}
