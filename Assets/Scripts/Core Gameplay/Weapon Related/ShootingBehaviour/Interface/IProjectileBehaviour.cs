using UnityEngine;

public interface IProjectileBehaviour
{
    void Shoot(Transform projectileHolder, Camera playerCamera, GameObject projectilePrefab, float projectileSpeed, BaseWeapon ownerWeapon);
}
