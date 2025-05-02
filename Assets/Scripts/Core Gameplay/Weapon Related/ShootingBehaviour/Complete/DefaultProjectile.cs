using Unity.Netcode;
using UnityEngine;

public class DefaultProjectile : NetworkBehaviour, IProjectileBehaviour
{
    public void Shoot(Vector3 shootPosition, Vector3 shootDirection, GameObject projectilePrefab, float projectileSpeed, BaseWeapon ownerWeapon)
    {
        if (!IsServer) return;

        GameObject projectileInstance = Instantiate(projectilePrefab, shootPosition, Quaternion.LookRotation(shootDirection));

        if (projectileInstance.TryGetComponent<NetworkObject>(out var networkObject))
            networkObject.Spawn();

        if (projectileInstance.TryGetComponent<IProjectileNetworkInitializer>(out var networkedInit))
            networkedInit.InitializeNetworkedProjectile(ownerWeapon);

        if (projectileInstance.TryGetComponent<Rigidbody>(out var rb))
            rb.AddForce(shootDirection.normalized * projectileSpeed, ForceMode.VelocityChange);
    }

}