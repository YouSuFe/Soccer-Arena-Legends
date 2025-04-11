using Unity.Netcode;
using UnityEngine;

public class RotatingProjectile : NetworkBehaviour, IProjectileBehaviour
{
    public void Shoot(Vector3 shootPosition, Vector3 direction, GameObject projectilePrefab, float projectileSpeed, BaseWeapon ownerWeapon)
    {
        if (!IsServer) return; // Ensure only the server spawns projectiles

        GameObject projectileInstance = Instantiate(projectilePrefab, shootPosition, Quaternion.LookRotation(direction));
        if (projectileInstance.TryGetComponent<NetworkObject>(out var networkObject))
        {
            Debug.Log("Spawning the Projectile for " + ownerWeapon.name);
            networkObject.Spawn();
        }
        else
        {
            Debug.LogError(" No NetworkObject on projectile prefab.");
            return;
        }

        if (projectileInstance.TryGetComponent<IProjectileNetworkInitializer>(out var networkedInit))
        {
            networkedInit.InitializeNetworkedProjectile(ownerWeapon);
        }

        if (projectileInstance.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.AddForce(direction.normalized * projectileSpeed, ForceMode.VelocityChange);
            Debug.Log("➡Projectile velocity set to: " + rb.linearVelocity);
        }
        else
        {
            Debug.LogError("No Rigidbody on projectile prefab.");
        }

        if (projectileInstance.GetComponent<RotatingProjectileOverTime>() == null)
        {
            projectileInstance.AddComponent<RotatingProjectileOverTime>();
        }
    }
}

