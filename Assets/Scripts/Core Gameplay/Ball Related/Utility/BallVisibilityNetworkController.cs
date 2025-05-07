using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BallVisibilityNetworkController : NetworkBehaviour
{
    [Header("Assign Visuals & Colliders")]
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Collider[] colliders;

    private Rigidbody rb;

    private NetworkVariable<bool> isBallVisible = new NetworkVariable<bool>(true);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        rb = GetComponent<Rigidbody>();

        // Safety fallback if not assigned in Inspector
        if (renderers == null || renderers.Length == 0)
            renderers = GetComponentsInChildren<Renderer>(includeInactive: true);

        if (colliders == null || colliders.Length == 0)
            colliders = GetComponentsInChildren<Collider>(includeInactive: true);

        isBallVisible.OnValueChanged += HandleVisibilityChanged;

        if (IsServer)
        {
            SetBallVisibility(true); // Make sure ball is visible at start
        }
        else
        {
            HandleVisibilityChanged(false, isBallVisible.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsSpawned)
        {
            isBallVisible.OnValueChanged -= HandleVisibilityChanged;
        }
    }

    private void HandleVisibilityChanged(bool oldValue, bool newValue)
    {
        SetBallActiveState(newValue);
    }

    private void SetBallActiveState(bool isVisible)
    {
        rb.isKinematic = !isVisible;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        foreach (var renderer in renderers)
            renderer.enabled = isVisible;

        foreach (var collider in colliders)
            collider.enabled = isVisible;

        gameObject.SetActive(isVisible); // This only affects local GameObject activation
    }

    [ServerRpc(RequireOwnership = false)]
    public void HideBallServerRpc()
    {
        SetBallVisibility(false);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ShowBallServerRpc(Vector3 spawnPosition)
    {
        transform.position = spawnPosition;
        SetBallVisibility(true);
    }

    private void SetBallVisibility(bool visible)
    {
        isBallVisible.Value = visible;
    }

    public bool IsBallCurrentlyVisible() => isBallVisible.Value;
}

