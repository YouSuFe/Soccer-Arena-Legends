using UnityEngine;
using System;
using Unity.Cinemachine;

public enum CameraMode
{
    FirstPerson,
    ThirdPerson
}

public class CameraSwitchHandler : MonoBehaviour
{
    public event Action OnCameraSwitch;

    [SerializeField] private InputReader InputReader;

    [Header("Cameras")]
    public CinemachineCamera fpsCamera;
    public CinemachineCamera lookAtCamera;

    [Header("References")]
    public Transform playerRootTransform; // Assign player object (used for FPS rotation sync)
    public CameraLookAnchor cameraLookAnchor; // Assign the child that stores camera-like rotation

    [Header("Camera Mode")]
    public CameraMode currentCameraMode = CameraMode.FirstPerson;

    private bool isOwnerControlled = false;

    public void SetOwnerControlled(bool isControlled)
    {
        isOwnerControlled = isControlled;
    }

    private void Start()
    {
        Debug.Log("SetCameraMode is Calling");
        SetCameraMode(currentCameraMode);
        cameraLookAnchor?.SetTargetCamera(GetCurrentActiveCameraTransform());
    }

    private void OnEnable()
    {
        if (isOwnerControlled && InputReader != null)
        {
            InputReader.OnLookAtPerformed += CycleCameras;
        }
    }

    private void OnDisable()
    {
        if (isOwnerControlled && InputReader != null)
        {
            InputReader.OnLookAtPerformed -= CycleCameras;
        }
    }

    public void SetCameraMode(CameraMode mode)
    {
        if (fpsCamera == null || lookAtCamera == null)
        {
            Debug.LogWarning("CameraSwitchHandler: One or both cameras are not assigned.");
            return;
        }

        switch (mode)
        {
            case CameraMode.FirstPerson:
                lookAtCamera.Priority = 5;
                lookAtCamera.gameObject.SetActive(false);

                // Align FPS camera with the player's current rotation
                if (playerRootTransform != null)
                {
                    fpsCamera.transform.rotation = playerRootTransform.rotation;
                }

                fpsCamera.Priority = 10;
                fpsCamera.gameObject.SetActive(true);

                Debug.Log("FPS Camera Active");
                break;

            case CameraMode.ThirdPerson:
                fpsCamera.Priority = 5;
                fpsCamera.gameObject.SetActive(false);

                lookAtCamera.gameObject.SetActive(true);
                lookAtCamera.Priority = 10;

                Debug.Log("Third-Person Camera Active");
                break;

            default:
                Debug.LogError("Unknown Camera Mode");
                break;
        }

        // Update the camera look anchor to follow the new active camera
        cameraLookAnchor?.SetTargetCamera(GetCurrentActiveCameraTransform());
    }

    public void CycleCameras()
    {
        if (!isOwnerControlled) return;

        if (fpsCamera == null || lookAtCamera == null)
        {
            Debug.LogError("CameraSwitchHandler: Cannot cycle cameras as fpsCamera or lookAtCamera is not assigned.");
            return;
        }

        currentCameraMode = (CameraMode)(((int)currentCameraMode + 1) % Enum.GetValues(typeof(CameraMode)).Length);
        SetCameraMode(currentCameraMode);
        OnCameraSwitch?.Invoke();
    }

    public Transform GetCurrentActiveCameraTransform()
    {
        return currentCameraMode switch
        {
            CameraMode.FirstPerson => fpsCamera.transform,
            CameraMode.ThirdPerson => lookAtCamera.transform,
            _ => null
        };
    }

    public bool IsFPSCameraActive()
    {
        return currentCameraMode == CameraMode.FirstPerson;
    }

    public void RealignActiveCameraToLookAnchor()
    {
        Transform activeCam = GetCurrentActiveCameraTransform();
        if (activeCam != null && cameraLookAnchor != null)
        {
            Debug.Log($"[RealignActiveCameraToLookAnchor] Realigning active camera: {activeCam.name} to CameraLookAnchor position {cameraLookAnchor.transform.position} and rotation {cameraLookAnchor.transform.rotation.eulerAngles}");

            activeCam.position = cameraLookAnchor.transform.position;
            activeCam.rotation = cameraLookAnchor.transform.rotation;
        }
        else
        {
            Debug.LogWarning("[RealignActiveCameraToLookAnchor] Failed - ActiveCam or CameraLookAnchor is NULL.");
        }
    }


    public void ForceSnapActiveCamera()
    {
        if (cameraLookAnchor == null)
        {
            Debug.LogWarning("[ForceSnapActiveCamera] CameraLookAnchor is NULL.");
            return;
        }

        if (currentCameraMode == CameraMode.FirstPerson)
        {
            if (fpsCamera != null)
            {
                fpsCamera.ForceCameraPosition(cameraLookAnchor.transform.position, cameraLookAnchor.transform.rotation);
                Debug.Log($"[ForceSnapActiveCamera] Forced FPS Camera {fpsCamera.name} to CameraLookAnchor position {cameraLookAnchor.transform.position} and rotation {cameraLookAnchor.transform.rotation.eulerAngles}");
            }
            else
            {
                Debug.LogWarning("[ForceSnapActiveCamera] FPS Camera is NULL.");
            }
        }
        else
        {
            if (lookAtCamera != null)
            {
                lookAtCamera.ForceCameraPosition(cameraLookAnchor.transform.position, cameraLookAnchor.transform.rotation);
                Debug.Log($"[ForceSnapActiveCamera] Forced TPS Camera {lookAtCamera.name} to CameraLookAnchor position {cameraLookAnchor.transform.position} and rotation {cameraLookAnchor.transform.rotation.eulerAngles}");
            }
            else
            {
                Debug.LogWarning("[ForceSnapActiveCamera] TPS Camera is NULL.");
            }
        }
    }
}

