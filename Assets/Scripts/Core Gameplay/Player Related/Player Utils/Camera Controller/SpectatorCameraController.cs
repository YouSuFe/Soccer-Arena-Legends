using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// Local-only controller for handling spectator mode when player is dead.
/// Enables switching between FPV anchor points of other alive players.
/// </summary>
public class SpectatorCameraController : MonoBehaviour
{
    public static SpectatorCameraController Instance { get; private set; }

    [Header("Scene Camera References")]
    [SerializeField] private CinemachineCamera spectatorCamera;

    [Header("Spectator Camera Settings")]
    [SerializeField] private float transitionSpeed = 5f;

    [Header("Input Reader")]
    [SerializeField] private InputReader inputReader;

    private List<Transform> playerFpvViews = new();
    private int currentViewIndex = 0;
    private bool isSpectating = false;

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        if (inputReader != null)
        {
            inputReader.OnRegularAttackPerformed += CycleNextView;
            inputReader.OnHeavyAttackPerformed += CyclePreviousView;
        }
    }

    private void OnDisable()
    {
        if (inputReader != null)
        {
            inputReader.OnRegularAttackPerformed -= CycleNextView;
            inputReader.OnHeavyAttackPerformed -= CyclePreviousView;
        }
    }

    private void LateUpdate()
    {
        if (!isSpectating || playerFpvViews.Count == 0) return;

        Transform target = playerFpvViews[currentViewIndex];
        if (target == null) return;

        // Snap to target instantly — no lerp for precise FPV alignment
        spectatorCamera.transform.position = target.position;
        spectatorCamera.transform.rotation = target.rotation;
    }

    #endregion

    #region Spectator Mode Control

    /// <summary>
    /// Activates spectator camera and enables cycling through player FPV views.
    /// </summary>
    public void ActivateSpectatorMode()
    {
        isSpectating = true;
        spectatorCamera.gameObject.SetActive(true);
        currentViewIndex = 0;
    }

    /// <summary>
    /// Deactivates spectator camera and clears all view targets.
    /// </summary>
    public void DeactivateSpectatorMode()
    {
        isSpectating = false;
        spectatorCamera.gameObject.SetActive(false);
        playerFpvViews.Clear();
    }

    #endregion

    #region FPV View Tracking

    /// <summary>
    /// Adds a new FPV anchor to the list of spectatable views.
    /// </summary>
    public void AddFpvView(Transform viewTransform)
    {
        if (!playerFpvViews.Contains(viewTransform))
        {
            playerFpvViews.Add(viewTransform);
        }
    }

    /// <summary>
    /// Removes an FPV view from the list.
    /// </summary>
    public void RemoveFpvView(Transform viewTransform)
    {
        if (playerFpvViews.Contains(viewTransform))
        {
            playerFpvViews.Remove(viewTransform);
        }
    }

    #endregion

    #region View Cycling

    /// <summary>
    /// Cycles to the next FPV view in the list.
    /// Triggered by regular attack input during spectating.
    /// </summary>
    private void CycleNextView()
    {
        if (!isSpectating || playerFpvViews.Count == 0) return;
        currentViewIndex = (currentViewIndex + 1) % playerFpvViews.Count;
    }

    /// <summary>
    /// Cycles to the previous FPV view in the list.
    /// Triggered by heavy attack input during spectating.
    /// </summary>
    private void CyclePreviousView()
    {
        if (!isSpectating || playerFpvViews.Count == 0) return;
        currentViewIndex--;
        if (currentViewIndex < 0)
        {
            currentViewIndex = playerFpvViews.Count - 1;
        }
    }

    #endregion
}
