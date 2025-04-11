using UnityEngine;

public interface IProjectileBehaviour
{
    void Shoot(Transform projectileHolder, Transform aimTransform, GameObject projectilePrefab, float projectileSpeed, BaseWeapon ownerWeapon);
}
