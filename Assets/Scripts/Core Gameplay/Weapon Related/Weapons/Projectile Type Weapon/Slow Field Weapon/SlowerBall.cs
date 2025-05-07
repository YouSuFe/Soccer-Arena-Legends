using Unity.Netcode;
using UnityEngine;

public class SlowerBall : NetworkBehaviour, IProjectileNetworkInitializer
{
    #region Fields

    [SerializeField] private SlowerBallDataSO slowerBallData;

    private SlowFieldWeapon slowfieldWeapon;

    private GameObject projectileTrail;

    private ulong WeaponOwnerClientId = ulong.MaxValue;


    #endregion

    #region Networked Setup

    public void InitializeNetworkedProjectile(BaseWeapon weapon)
    {
        if (weapon is SlowFieldWeapon slow)
        {
            slowfieldWeapon = slow;
            WeaponOwnerClientId = slow.OwnerClientId;

            AssignSlowWeaponClientRpc(slow.NetworkObjectId, RpcUtils.ToClient(slow.OwnerClientId));
        }
    }

    [ClientRpc]
    private void AssignSlowWeaponClientRpc(ulong slowWeaponNetId, ClientRpcParams rpcParams = default)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(slowWeaponNetId, out var netObj)
            && netObj.TryGetComponent<SlowFieldWeapon>(out var weapon))
        {
            slowfieldWeapon = weapon;
        }
    }

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        InitializeParticles();
    }

    #endregion

    #region Particle Initialization

    private void InitializeParticles()
    {
        if (slowerBallData.projectileTrail != null)
        {
            projectileTrail = Instantiate(slowerBallData.projectileTrail, transform.position, Quaternion.identity, transform);
            projectileTrail.SetActive(true);
        }
    }

    #endregion

    #region Collision Detection

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        if (((1 << collision.gameObject.layer) & slowerBallData.interactableLayerMask) != 0)
        {
            // Optional: Register skill influence on ball
            if (collision.gameObject.TryGetComponent<BallOwnershipManager>(out var ballManager))
            {
                ballManager.RegisterSkillInfluence(WeaponOwnerClientId);
            }

            // Tell clients to spawn VFX and hide trail
            TriggerImpactClientRpc(transform.position);

            // Server spawns the slow field object
            if (slowerBallData.slowFieldObject != null)
            {
                if (slowerBallData.slowFieldObject.TryGetComponent<NetworkObject>(out var netObjPrefab))
                {
                    GameObject slowFieldInstance = Instantiate(slowerBallData.slowFieldObject, transform.position, Quaternion.identity);
                    NetworkObject netObjInstance = slowFieldInstance.GetComponent<NetworkObject>();
                    netObjInstance.Spawn();
                }
                else
                {
                    Debug.LogWarning("[SlowerBall] Slow field prefab is missing NetworkObject.");
                }
            }

            // Despawn networked object (safer than Destroy)
            if (TryGetComponent<NetworkObject>(out var netObj))
                netObj.Despawn();
            else
                Destroy(gameObject);
        }
    }

    [ClientRpc]
    private void TriggerImpactClientRpc(Vector3 position)
    {
        if (projectileTrail != null)
        {
            projectileTrail.SetActive(false);
            Destroy(projectileTrail);
        }
    }

    #endregion

    #region Cleanup

    public override void OnDestroy()
    {
        base.OnDestroy();

        if (projectileTrail != null)
        {
            Destroy(projectileTrail);
        }
    }

    #endregion
}
