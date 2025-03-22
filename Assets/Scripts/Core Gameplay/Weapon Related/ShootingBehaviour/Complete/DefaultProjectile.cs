using Unity.Netcode;
using UnityEngine;

public class DefaultProjectile : NetworkBehaviour, IProjectileBehaviour
{
    public void Shoot(Transform projectileHolder, Camera playerCamera, GameObject projectilePrefab, float projectileSpeed)
    {
        if (!IsServer) return; // Ensure only the server spawns projectiles

        TargetingSystem targetingSystem = new TargetingSystem();
        Vector3 shootDirection = targetingSystem.GetShotDirection(playerCamera, projectileHolder.position);
        Debug.Log("Shoot direction: " + shootDirection);

        GameObject projectileInstance = Instantiate(projectilePrefab, projectileHolder.position, Quaternion.identity);
        projectileInstance.transform.forward = shootDirection;

        NetworkObject networkObject = projectileInstance.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.Spawn(); // Syncs the projectile across all clients
        }
        else
        {
            Debug.LogError("No NetworkObject found on projectile prefab.");
            return;
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
    }
}
