using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Game Screen Data/WeaponData/Weapon Data")]
public class WeaponDataSO : ScriptableObject
{
    [Header("Weapon Prefabs")]
    [Tooltip("The prefab representing the weapon.")]
    [SerializeField] private GameObject weaponPrefab;
    public GameObject WeaponPrefab { get => weaponPrefab; private set => weaponPrefab = value; }

    [Tooltip("The prefab representing the projectile (e.g., for ranged weapons).")]
    [SerializeField] private GameObject projectilePrefab;
    public GameObject ProjectilePrefab { get => projectilePrefab; private set => projectilePrefab = value; }

    [Header("Projectile Settings")]
    [Tooltip("The speed at which the projectile moves.")]
    [SerializeField] private float projectileSpeed = 10f;
    public float ProjectileSpeed { get => projectileSpeed; private set => projectileSpeed = value; }

    [Header("Combat Parameters")]
    [Tooltip("The cooldown time between regular attacks.")]
    [SerializeField] private float regularAttackCooldown = 0.3f;
    public float RegularAttackCooldown { get => regularAttackCooldown; private set => regularAttackCooldown = value; }

    [Tooltip("The cooldown time between heavy attacks.")]
    [SerializeField] private float heavyAttackCooldown = 0.8f;
    public float HeavyAttackCooldown { get => heavyAttackCooldown; private set => heavyAttackCooldown = value; }

    [Tooltip("The base damage dealt by the weapon.")]
    [SerializeField] private int baseDamage = 10;
    public int BaseDamage { get => baseDamage; private set => baseDamage = value; }

    [Tooltip("The multiplier for heavy attack damage.")]
    [SerializeField] private int heavyDamageMultiplier = 3;
    public int HeavyDamageMultiplier { get => heavyDamageMultiplier; private set => heavyDamageMultiplier = value; }

    [Header("Damage Detection")]
    [Tooltip("The length of the weapon, which can be used for raycast or sphere cast.")]
    [SerializeField] private float weaponLength = 0.5f;
    public float WeaponLength { get => weaponLength; private set => weaponLength = value; }

    [Tooltip("The radius of the sphere or box cast for hit detection.")]
    [SerializeField] private float hitRadius = 0.05f;
    public float HitRadius { get => hitRadius; private set => hitRadius = value; }

    [Tooltip("The layer mask specifying which objects can be damaged.")]
    [SerializeField] private LayerMask damageableLayer;
    public LayerMask DamageableLayer { get => damageableLayer; private set => damageableLayer = value; }

    [Header("Position and Rotation Offsets")]
    [Tooltip("The offset applied to the weapon's position when attached to the player.")]
    [SerializeField] private Vector3 positionOffset = Vector3.zero;
    public Vector3 PositionOffset { get => positionOffset; private set => positionOffset = value; }

    [Tooltip("The offset applied to the weapon's rotation when attached to the player.")]
    [SerializeField] private Vector3 rotationOffset = Vector3.zero;
    public Vector3 RotationOffset { get => rotationOffset; private set => rotationOffset = value; }
}