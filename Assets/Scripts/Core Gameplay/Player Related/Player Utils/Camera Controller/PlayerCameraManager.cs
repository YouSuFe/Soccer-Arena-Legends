using System;
using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;


public enum CameraMode
{
    FirstPerson,
    ThirdPerson,
    // Add more camera modes as needed
}


/// <summary>
/// Handles local camera switching between First-Person View (FPV) and Third-Person View (TPV).
/// Should be placed on the player prefab and only acts for the owning client.
/// </summary>
public class PlayerCameraManager : NetworkBehaviour
{
    [Header("Cameras")]
    [SerializeField] private CinemachineCamera fpvCamera;
    [SerializeField] private CinemachineCamera tpvCamera;

    [Header("References")]
    [SerializeField] private InputReader inputReader;

    /// <summary>
    /// Called whenever the local player's camera view changes.
    /// </summary>
    public event Action OnCameraSwitched;

    private bool isDead = false;
    private bool isUsingFPV = true;

    #region Unity / Netcode Lifecycle

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        SetCamera(true); // Start in FPV by default

        if (inputReader != null)
        {
            inputReader.OnLookAtPerformed += ToggleCameraView;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner || inputReader == null) return;

        inputReader.OnLookAtPerformed -= ToggleCameraView;
    }

    #endregion

    #region Camera Logic

    /// <summary>
    /// Toggles between FPV and TPV on input.
    /// </summary>
    private void ToggleCameraView()
    {
        if (!IsOwner || isDead) return;

        isUsingFPV = !isUsingFPV;
        SetCamera(isUsingFPV);
    }

    /// <summary>
    /// Sets the active camera based on current view mode.
    /// </summary>
    private void SetCamera(bool useFPV)
    {
        if (fpvCamera != null) fpvCamera.gameObject.SetActive(useFPV);
        if (tpvCamera != null) tpvCamera.gameObject.SetActive(!useFPV);

        OnCameraSwitched?.Invoke();
    }

    /// <summary>
    /// Marks the player as dead, disabling camera toggle.
    /// </summary>
    public void OnPlayerDeath()
    {
        isDead = true;
    }

    /// <summary>
    /// Resets the player to alive and restores FPV.
    /// </summary>
    public void OnPlayerRespawn()
    {
        isDead = false;
        SetCamera(true);
    }

    /// <summary>
    /// Returns true if currently using First-Person View.
    /// </summary>
    public bool IsFPSCameraActive() => isUsingFPV;

    #endregion
}
