using Unity.Netcode;
using UnityEngine;

public class RotatingProjectile : NetworkBehaviour, IProjectileBehaviour
{
    public void Shoot(Transform projectileHolder, Transform aimTransform, GameObject projectilePrefab, float projectileSpeed, BaseWeapon ownerWeapon)
    {
        Debug.Log($"Is server: {IsServer} IsClient {IsClient} IsOwner {IsOwner}");
        if (!IsServer) return; // Ensure only the server spawns projectiles

        TargetingSystem targetingSystem = new TargetingSystem();
        Vector3 shootDirection = targetingSystem.GetShotDirection(aimTransform, projectileHolder.position);
        Debug.Log("Shoot direction: " + shootDirection);

        GameObject projectileInstance = Instantiate(projectilePrefab, projectileHolder.position, Quaternion.identity);
        projectileInstance.transform.forward = shootDirection;

        NetworkObject networkObject = projectileInstance.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            Debug.Log("Spawning the Projectile for "+ ownerWeapon.name);
            networkObject.Spawn(); // Syncs the projectile across all clients
        }
        else
        {
            Debug.LogError("No NetworkObject found on projectile prefab.");
            return;
        }

        if (projectileInstance.TryGetComponent<IProjectileNetworkInitializer>(out var networkedInit))
        {
            Debug.Log("InitializeNetworkedProjectile the Projectile for " + ownerWeapon.name);

            networkedInit.InitializeNetworkedProjectile(ownerWeapon); // 💡 Correct initialization here
        }
        else
        {
            Debug.LogWarning("InitializeNetworkedProjectile the Projectile for not happenning." + ownerWeapon.name);

        }

        Rigidbody rb = projectileInstance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(shootDirection * projectileSpeed, ForceMode.VelocityChange);
            Debug.Log("Projectile velocity set to: " + rb.linearVelocity);
        }
        else
        {
            Debug.LogError("No Rigidbody found on projectile prefab.");
        }

        // Optionally add rotation or other effects
        RotatingProjectileOverTime rotatingProjectile = projectileInstance.GetComponent<RotatingProjectileOverTime>();
        if (rotatingProjectile == null)
        {
            projectileInstance.AddComponent<RotatingProjectileOverTime>();
        }
    }
}

