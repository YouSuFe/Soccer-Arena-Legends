using System;
using UnityEngine;

public class GravityWeapon : BaseWeapon
{
    #region Weapon Configuration

    [Header("Projectile Sound Data")]
    [SerializeField] SoundData projectileSoundData;

    [Header("Builder Weapon Settings")]
    [Tooltip("The prefab for the gravityHoleProjectile.")]
    private GameObject gravityHoleProjectile;

    [Tooltip("The point where the gravityHoleProjectile spawns.")]
    private Transform projectileHolder;

    [Tooltip("Reference to the player's camera/aim.")]
    private Transform anchorAimTransform;

    [Tooltip("The speed of the gravity projectile.")]
    private float gravityProjectileSpeed;

    #endregion

    #region Initialization Methods

    protected override void Awake()
    {
        base.OnNetworkSpawn();

        projectileBehaviour = GetComponent<DefaultProjectile>();
        if (projectileBehaviour == null)
            projectileBehaviour = gameObject.AddComponent<DefaultProjectile>();

        SetShootingBehaviour(projectileBehaviour);
    }

    // Initialize method to set up the weapon with the appropriate data and parent it to the weapon holder
    public override void Initialize(Transform weaponHolder, Transform aimTransform, Transform projectileHolder)
    {
        this.gravityHoleProjectile = weaponData.ProjectilePrefab;
        this.weaponHolder = weaponHolder;
        this.projectileHolder = projectileHolder;
        this.anchorAimTransform = aimTransform;
        this.gravityProjectileSpeed = weaponData.ProjectileSpeed;

        Debug.Log($"GravityWeapon initialized. Projectile prefab: {(gravityHoleProjectile != null ? "Assigned" : "Missing")}");
    }

    #endregion

    #region Weapon Skills and Attacks

    // Method to execute the Aura Weapon's special skill
    protected override void ExecuteSpecialSkill(Vector3 rayOrigin, Vector3 direction)
    {
        Debug.Log("Executing Gravity Skill with direction!");

        if (gravityHoleProjectile == null || projectileHolder == null)
        {
            Debug.LogError("Missing prefab or projectile holder.");
            return;
        }

        projectileBehaviour?.Shoot(projectileHolder.position, direction, gravityHoleProjectile, gravityProjectileSpeed, this);
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
                                    .WithPosition(gravityHoleProjectile.transform.position)
                                    .WithParent(gravityHoleProjectile.transform)
                                    .WithRandomPitch()
                                    .Play(projectileSoundData);
    }

}
