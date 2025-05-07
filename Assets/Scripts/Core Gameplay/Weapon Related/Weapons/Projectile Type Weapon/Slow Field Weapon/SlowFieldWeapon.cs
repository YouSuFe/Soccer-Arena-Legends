using System;
using UnityEngine;


public class SlowFieldWeapon : BaseWeapon
{
    #region Weapon Configuration

    [Header("Projectile Sound Data")]
    [SerializeField] SoundData projectileSoundData;

    [Header("Slow Field Weapon Settings")]
    [Tooltip("The prefab for the Slow Field projectile.")]
    private GameObject slowFieldPrefab;

    [Tooltip("The point where the Slow Field projectile spawns.")]
    private Transform projectileHolder;

    [Tooltip("Reference to the player's camera.")]
    private Transform anchorAimTransform;

    [Tooltip("The speed of the Slow Field projectile.")]
    private float slowFieldProjectileSpeed;

    #endregion

    #region Initialization Methods

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        projectileBehaviour = GetComponent<DefaultProjectile>();

        if (projectileBehaviour == null)
        {
            projectileBehaviour = gameObject.AddComponent<DefaultProjectile>();
        }

        SetShootingBehaviour(projectileBehaviour);

    }

    // Initialize method to set up the weapon with the appropriate data and parent it to the weapon holder
    public override void Initialize(Transform weaponHolder, Transform aimTransform, Transform projectileHolder)
    {
        // Assign necessary references
        if (weaponData.ProjectilePrefab != null)
        {
            this.slowFieldPrefab = weaponData.ProjectilePrefab;
        }

        this.weaponHolder = weaponHolder;
        this.projectileHolder = projectileHolder;
        this.anchorAimTransform = aimTransform;

        this.slowFieldProjectileSpeed = weaponData.ProjectileSpeed;

    }

    #endregion

    #region Weapon Skills and Attacks

    // Method to execute the Slowfield Weapon's special skill
    protected override void ExecuteSpecialSkill(Vector3 rayOrigin, Vector3 direction)
    {
        Debug.Log("Executing Aura Skill with direction!");

        if (slowFieldPrefab == null || projectileHolder == null)
        {
            Debug.LogError("Missing prefab or holder");
            return;
        }

        // Shoot with already calculated direction
        projectileBehaviour?.Shoot(projectileHolder.position, direction, slowFieldPrefab, slowFieldProjectileSpeed, this);
    }

    protected override void PlaySkillEffects()
    {
        PlayProjectileSound();
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


    private void PlayProjectileSound()
    {
        SoundManager.Instance.CreateSoundBuilder()
                                    .WithPosition(slowFieldPrefab.transform.position)
                                    .WithParent(slowFieldPrefab.transform)
                                    .WithRandomPitch()
                                    .Play(projectileSoundData);
    }
}
