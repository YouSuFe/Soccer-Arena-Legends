using UnityEngine;

public interface IProjectileBehaviour
{
    void Shoot(Vector3 shootPosition, Vector3 shootDirection, GameObject projectilePrefab, float projectileSpeed, BaseWeapon ownerWeapon);
}
