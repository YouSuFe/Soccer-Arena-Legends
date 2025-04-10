using UnityEngine;
using System;
using Unity.Cinemachine;

public class CameraSwitchHandler : MonoBehaviour
{
    public event Action OnSwitchFromFirstPerson;

    [SerializeField] private InputReader InputReader;

    [Header("Cameras")]
    public CinemachineCamera fpsCamera;
    public CinemachineCamera lookAtCamera;

    [Header("Camera Mode")]
    public CameraMode currentCameraMode = CameraMode.FirstPerson; // Start in FPS mode

    private bool isOwnerControlled = false; // Controls if this instance should handle input to avoid other players intervain

    public void SetOwnerControlled(bool isControlled)
    {
        isOwnerControlled = isControlled;
    }

    private void Start()
    {
        Debug.Log("SetCameraMode is Calling");
        SetCameraMode(currentCameraMode);
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
                fpsCamera.Priority = 10;
                Debug.Log("FPS Camera Active");
                break;
            case CameraMode.ThirdPerson:
                fpsCamera.Priority = 5;
                lookAtCamera.Priority = 10;
                Debug.Log("Third-Person Camera Active");
                break;
            default:
                Debug.LogError("Unknown Camera Mode");
                break;
        }
    }

    public void CycleCameras()
    {
        if (!isOwnerControlled) return; // ✅ Only allow owners to change cameras

        if (fpsCamera == null || lookAtCamera == null)
        {
            Debug.LogError("CameraSwitchHandler: Cannot cycle cameras as fpsCamera or lookAtCamera is not assigned.");
            return;
        }

        currentCameraMode = (CameraMode)(((int)currentCameraMode + 1) % Enum.GetValues(typeof(CameraMode)).Length);

        OnSwitchFromFirstPerson?.Invoke();
        SetCameraMode(currentCameraMode);
    }

    public bool IsFPSCameraActive()
    {
        return currentCameraMode == CameraMode.FirstPerson;
    }
}


