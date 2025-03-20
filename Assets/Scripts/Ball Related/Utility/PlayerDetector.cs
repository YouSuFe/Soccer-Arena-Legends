using System;
using Unity.Netcode;
using UnityEngine;

public class PlayerDetector : NetworkBehaviour
{
    [SerializeField] private float detectionRadius = 2f;
    [SerializeField] private LayerMask playerLayer;

    public event Action<PlayerAbstract> OnPlayerDetected;

    private Collider[] hitColliders = new Collider[10]; // Preallocate based on expected max hits

    public void CheckForPlayer()
    {
        // does not clear the array after each call; it simply overwrites elements up to the number of objects detected.
        // Use the returned hitsCount to loop only through valid entries in the array, ignoring any leftover data from previous calls.
        int hitsCount = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, hitColliders, playerLayer);

        for (int i = 0; i < hitsCount; i++)
        {
            if (hitColliders[i].TryGetComponent(out PlayerAbstract detectedPlayer) && detectedPlayer.gameObject != gameObject)
            {
                NotifyPlayerDetectedClientRpc(detectedPlayer.OwnerClientId);
                return; // Exit the loop as soon as a valid player is found
            }
        }
    }

    [ClientRpc]
    private void NotifyPlayerDetectedClientRpc(ulong clientId)
    {
        if (!IsClient) return;

        Debug.Log($"Player is detected : {clientId}");

        PlayerAbstract detectedPlayer = GetPlayerById(clientId);

        if(detectedPlayer != null)
        {
            Debug.Log($"Player is detected : {detectedPlayer}");

            OnPlayerDetected?.Invoke(detectedPlayer);
        }
        else
        {
            Debug.Log($"Player is not detected ");
        }
    }

    private PlayerAbstract GetPlayerById(ulong clientId)
    {
        if(NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            return client.PlayerObject.GetComponent<PlayerAbstract>();
        }

        Debug.LogError($"Player could not found with {clientId} id.");
        return null;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
