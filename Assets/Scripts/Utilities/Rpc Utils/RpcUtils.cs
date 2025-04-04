using System.Collections.Generic;
using Unity.Netcode;

/// <summary>
/// Utility class for clean and readable ClientRpc targeting using Unity Netcode.
/// </summary>
public static class RpcUtils
{
    /// <summary>
    /// Creates a ClientRpcParams targeting a single client.
    /// </summary>
    public static ClientRpcParams ToClient(ulong clientId)
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { clientId }
            }
        };
    }

    /// <summary>
    /// Creates a ClientRpcParams targeting multiple clients.
    /// </summary>
    public static ClientRpcParams ToClients(params ulong[] clientIds)
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = clientIds
            }
        };
    }

    /// <summary>
    /// Targets the owner of a single NetworkBehaviour.
    /// </summary>
    public static ClientRpcParams SendRpcToOwner(NetworkBehaviour behaviour)
    {
        return ToClient(behaviour.OwnerClientId);
    }

    /// <summary>
    /// Targets the owners of multiple NetworkBehaviours.
    /// </summary>
    public static ClientRpcParams SendRpcToOwners(params NetworkBehaviour[] networkBehaviours)
    {
        var targetClientIds = new List<ulong>();

        foreach (var behaviour in networkBehaviours)
        {
            if (behaviour != null && behaviour.IsSpawned)
            {
                targetClientIds.Add(behaviour.OwnerClientId);
            }
        }

        return ToClients(targetClientIds.ToArray());
    }
}
