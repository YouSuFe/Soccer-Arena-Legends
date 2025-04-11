using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Represents the player's camera rotation. This is used for aiming and network syncing.
/// </summary>
public class CameraLookAnchor : MonoBehaviour
{
    private Transform targetCamera;

    /// <summary>
    /// Assigns the current camera to follow.
    /// </summary>
    public void SetTargetCamera(Transform cam)
    {
        targetCamera = cam;
    }

    private void LateUpdate()
    {
        if (targetCamera == null || transform.parent == null)
            return;

        // Align with the target camera's rotation relative to the player body
        transform.localRotation = Quaternion.Inverse(transform.parent.rotation) * targetCamera.rotation;

        // Match position in world space
        transform.position = targetCamera.position;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;

        Vector3 start = transform.position;
        Vector3 dir = transform.forward * 2f;

        Gizmos.DrawRay(start, dir);

#if UNITY_EDITOR
        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.ArrowHandleCap(0, start, Quaternion.LookRotation(dir), 1f, EventType.Repaint);
        UnityEditor.Handles.Label(start + dir, "Camera Aim");
#endif
    }

}
