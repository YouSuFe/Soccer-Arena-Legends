using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class ClientNetworkTransform : NetworkTransform
{
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        CanCommitToTransform = IsOwner;
    }

    public override void OnUpdate()
    {
        CanCommitToTransform = IsOwner;
        base.OnUpdate();
        if (NetworkManager != null)
        {
            if (NetworkManager.IsConnectedClient || NetworkManager.IsListening)
            {
                if (CanCommitToTransform)
                {
                    TransformToServer();
                }
            }
        }
    }

    [ServerRpc]
    private void ReplicateTransformServerRpc(Vector2 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
    }

    private void TransformToServer()
    {
        if (IsOwner)
        {
            ReplicateTransformServerRpc(transform.position, transform.rotation);
        }
    }

    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}
