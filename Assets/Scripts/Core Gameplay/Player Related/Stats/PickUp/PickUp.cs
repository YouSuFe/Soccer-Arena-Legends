using System;
using Unity.Netcode;
using UnityEngine;


/// <summary>
/// Base class for all pickups. Implements server-authoritative logic with client-side prediction.
/// Removed Visitor logic which works fine for Single Player but now for Multiplayer, beacuse Server should
/// be responsible for checking the player who took this Pick Up object.
/// </summary>
public abstract class Pickup : NetworkBehaviour
{
    /// <summary>
    /// To send infromation about to be spawned again to the server
    /// </summary>
    public event Action OnCollected;

    /// <summary>
    /// Applies the pickup effect to the entity (e.g., modifying stats).
    /// </summary>
    protected abstract void ApplyPickupEffect(Entity entity);

    /// <summary>
    /// Reverts the pickup effect (used if the server rejects the pickup).
    /// </summary>
    protected abstract void RevertPickupEffect(Entity entity);


    /// <summary>
    /// Client or Server: Detects a collision with a player.
    /// - Clients predict the pickup effect and request server validation.
    /// - The server directly applies the effect if valid.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (!other.TryGetComponent<NetworkObject>(out var networkObject)) return;

        if (!(other.gameObject.layer == LayerMask.NameToLayer("Player")
            || other.gameObject.layer == LayerMask.NameToLayer("Enemy")
            || other.gameObject.layer == LayerMask.NameToLayer("PhaseThrough"))) return;

        Entity entity = other.GetComponent<Entity>();

        if (IsServer)
        {
            Debug.Log($"[SERVER] {gameObject.name} collided with {entity.gameObject.name}. Validating pickup...");

            // Server directly validates and applies the effect
            if (ValidatePickup(entity))
            {
                ApplyPickupEffect(entity);
                Debug.Log($"[SERVER] Pickup applied to {entity.gameObject.name}");

                OnCollected?.Invoke(); // ✅ Notify zone
                GetComponent<NetworkObject>().Despawn(true); // ✅ Proper destory

                // Remove pickup for all clients
                DestroyPickupClientRpc();
            }
            else
            {
                Debug.Log($"[SERVER] Pickup rejected for {entity.gameObject.name}");
            }
        }
        else
        {
            Debug.Log($"[CLIENT] {gameObject.name} collided with {entity.gameObject.name}. Applying effect immediately...");

            // 🔹 Client-Side Prediction: Apply the effect instantly
            ApplyPickupEffect(entity);

            // 🔹 Ask the server to verify the pickup
            Debug.Log($"[CLIENT] Requesting server validation...");
            PickupItemServerRpc();
        }
    }

    /// <summary>
    /// ServerRpc: The client requests to pick up the item. The server validates the request.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void PickupItemServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        ulong playerId = rpcParams.Receive.SenderClientId;
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(playerId)) return;

        GameObject playerObj = NetworkManager.Singleton.ConnectedClients[playerId].PlayerObject.gameObject;
        Entity entity = playerObj.GetComponent<Entity>();

        if (entity != null)
        {
            Debug.Log($"[SERVER] Received pickup request from Player {playerId} ({entity.gameObject.name}). Validating...");

            if (ValidatePickup(entity))
            {
                ApplyPickupEffect(entity);
                Debug.Log($"[SERVER] Pickup confirmed for {entity.gameObject.name}");
                OnCollected?.Invoke(); // ✅ Notify zone
                GetComponent<NetworkObject>().Despawn(true); // ✅ Proper despawn
            }
            else
            {
                Debug.Log($"[SERVER] Pickup rejected for {entity.gameObject.name}. Sending rollback request.");
                RevertPickupEffectClientRpc(RpcUtils.ToClient(playerId));
            }

            DestroyPickupClientRpc();
        }
    }

    /// <summary>
    /// ClientRpc: If the server rejects the pickup, revert the effect.
    /// </summary>
    [ClientRpc]
    private void RevertPickupEffectClientRpc(ClientRpcParams clientRpcParams = default)
    {
        Entity entity = GetComponent<Entity>();
        Debug.Log($"[CLIENT] Server rejected the pickup. Rolling back effect on {entity.gameObject.name}");
        RevertPickupEffect(entity); // 🔹 Undo only the modifier applied
    }

    /// <summary>
    /// ClientRpc: The server tells all clients to remove the pickup.
    /// ToDo: The problem is that if the Pick Up is not validated as pick upable, destroyed Pick Up object does not instantiate again...
    /// </summary>
    [ClientRpc]
    private void DestroyPickupClientRpc()
    {
        Debug.Log($"[ALL CLIENTS] Destroying pickup: {gameObject.name}");
    }

    private bool ValidatePickup(Entity entity)
    {
        if (entity == null) return false;
        if (!gameObject.activeSelf) return false;

        return true;
    }
}
