using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static PlayerInputActions;

[CreateAssetMenu(fileName = "New Input Reader", menuName = "Input/Input Reader")]
public class InputReader : ScriptableObject, IPlayerActions
{
    // Movement Events
    public event Action<Vector2> OnMoveEvent;
    public event Action OnLookAtPerformed;
    public event Action OnMovementStarted;
    public event Action OnMovementPerformed;
    public event Action OnMovementCanceled;
    public event Action OnWalkTogglePerformed;
    public event Action OnWalkToggleCanceled;

    // Jump and Sprint Events
    public event Action OnJumpEvent;
    public event Action OnSprintPerformed;
    public event Action OnStartingSprint;

    // Combat Events
    public event Action OnRegularAttackPerformed;
    public event Action OnHeavyAttackPerformed;
    public event Action OnProjectilePerformed;
    public event Action OnPlayerSkillUsed;
    public event Action OnWeaponSkillUsed;
    public event Action OnWeaponSkillHoldPerformed;
    public event Action OnWeaponSkillHoldCanceled;

    // Mouse Hold Events
    public event Action OnLeftMouseHoldPerformed;
    public event Action OnLeftMouseHoldCanceled;
    public event Action OnRightMouseHoldPerformed;
    public event Action OnRightMouseHoldCanceled;

    // UI
    public event Action<bool> OnStatisticTabOpen;
    public event Action<bool> OnStatisticTabClose;

    // Inputs
    public Vector2 CurrentMovement { get; private set; }
    public Vector2 MouseDelta { get; private set; }
    public float ScrollValue { get; private set; }

    // Coroutine 
    private readonly Dictionary<InputAction, Coroutine> activeDisableCoroutines = new();

    public PlayerInputActions PlayerInputActions { get; private set; }

    public void EnableInputActions()
    {
        if (PlayerInputActions == null)
        {
            PlayerInputActions = new PlayerInputActions();
            PlayerInputActions.Player.SetCallbacks(this);
        }
        PlayerInputActions.Enable();
    }

    public void DisableInputActions()
    {
        PlayerInputActions.Disable();
    }

    // Movement
    public void OnMovement(InputAction.CallbackContext context)
    {
        CurrentMovement = context.ReadValue<Vector2>();

        OnMoveEvent?.Invoke(CurrentMovement);

        if (context.started)
        {
            OnMovementStarted?.Invoke();
        }
        if (context.performed)
        {
            OnMovementPerformed?.Invoke();
        }
        if (context.canceled)
        {
            OnMovementCanceled?.Invoke();
        }
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed) OnJumpEvent?.Invoke();
    }

    public void OnSprint(InputAction.CallbackContext context)
    {
        if (context.performed) OnSprintPerformed?.Invoke();
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.performed) OnStartingSprint?.Invoke();
    }

    public void OnWalkToggle(InputAction.CallbackContext context)
    {
        if (context.performed) OnWalkTogglePerformed?.Invoke();
        if (context.canceled) OnWalkToggleCanceled?.Invoke();
    }

    // Combat
    public void OnRegularAttack(InputAction.CallbackContext context)
    {
        if (context.performed) OnRegularAttackPerformed?.Invoke();
    }

    public void OnHeavyAttack(InputAction.CallbackContext context)
    {
        if (context.performed) OnHeavyAttackPerformed?.Invoke();
    }

    public void OnProjectile(InputAction.CallbackContext context)
    {
        if (context.performed) OnProjectilePerformed?.Invoke();
    }

    public void OnPlayerSkill(InputAction.CallbackContext context)
    {
        if (context.performed) OnPlayerSkillUsed?.Invoke();
    }

    public void OnWeaponSkill(InputAction.CallbackContext context)
    {
        if (context.performed) OnWeaponSkillUsed?.Invoke();
    }

    //public void OnJump(InputAction.CallbackContext context)
    //{
    //    if (context.performed)
    //    {
    //        Debug.Log("Jump action performed");
    //        OnJumpEvent?.Invoke();
    //    }
    //}

    //public void OnSprint(InputAction.CallbackContext context)
    //{
    //    if (context.performed)
    //    {
    //        Debug.Log("Sprint action performed");
    //        OnSprintPerformed?.Invoke();
    //    }
    //}

    //public void OnDash(InputAction.CallbackContext context)
    //{
    //    if (context.performed)
    //    {
    //        Debug.Log("Dash action performed");
    //        OnStartingSprint?.Invoke();
    //    }
    //}

    //public void OnWalkToggle(InputAction.CallbackContext context)
    //{
    //    if (context.performed)
    //    {
    //        Debug.Log("Walk toggle activated");
    //        OnWalkTogglePerformed?.Invoke();
    //    }
    //    if (context.canceled)
    //    {
    //        Debug.Log("Walk toggle deactivated");
    //        OnWalkToggleCanceled?.Invoke();
    //    }
    //}

    //// Combat
    //public void OnRegularAttack(InputAction.CallbackContext context)
    //{
    //    if (context.performed)
    //    {
    //        Debug.Log("Regular attack performed");
    //        OnRegularAttackPerformed?.Invoke();
    //    }
    //}

    //public void OnHeavyAttack(InputAction.CallbackContext context)
    //{
    //    if (context.performed)
    //    {
    //        Debug.Log("Heavy attack performed");
    //        OnHeavyAttackPerformed?.Invoke();
    //    }
    //}

    //public void OnProjectile(InputAction.CallbackContext context)
    //{
    //    if (context.performed)
    //    {
    //        Debug.Log("Projectile attack performed");
    //        OnProjectilePerformed?.Invoke();
    //    }
    //}

    //public void OnPlayerSkill(InputAction.CallbackContext context)
    //{
    //    if (context.performed)
    //    {
    //        Debug.Log("Player skill used");
    //        OnPlayerSkillUsed?.Invoke();
    //    }
    //}

    //public void OnWeaponSkill(InputAction.CallbackContext context)
    //{
    //    if (context.performed)
    //    {
    //        Debug.Log("Weapon skill used");
    //        OnWeaponSkillUsed?.Invoke();
    //    }
    //}

    public void OnWeaponSkillHold(InputAction.CallbackContext context)
    {
        if (context.performed) OnWeaponSkillHoldPerformed?.Invoke();
        if (context.canceled) OnWeaponSkillHoldCanceled?.Invoke();
    }

    // Mouse Hold Actions
    public void OnLeftMouseHoldAction(InputAction.CallbackContext context)
    {
        if (context.performed) OnLeftMouseHoldPerformed?.Invoke();
        if (context.canceled) OnLeftMouseHoldCanceled?.Invoke();
    }

    public void OnRightMouseHoldAction(InputAction.CallbackContext context)
    {
        if (context.performed) OnRightMouseHoldPerformed?.Invoke();
        if (context.canceled) OnRightMouseHoldCanceled?.Invoke();
    }

    // Looking Input
    public void OnLook(InputAction.CallbackContext context)
    {
        MouseDelta = context.ReadValue<Vector2>();
    }

    public void OnLookAt(InputAction.CallbackContext context)
    {
        if (context.performed) OnLookAtPerformed?.Invoke();
    }


    public void OnMouseScrool(InputAction.CallbackContext context)
    {
        ScrollValue = context.ReadValue<Vector2>().y;
    }

    // UI
    public void OnStatisticsTab(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnStatisticTabOpen?.Invoke(true);
        }
        else if (context.canceled)
        {
            OnStatisticTabClose?.Invoke(false);
        }
    }

    #region Utility Methods
    // Get Mouse Scroll value for wall position control
    public float GetScrollValue()
    {
        return ScrollValue;
    }

    // Get x and y vectors for player movement
    public Vector2 GetPlayerMovement()
    {
        return CurrentMovement;
    }

    // Get mouse x and y positions for camera control
    public Vector2 GetMouseDelta()
    {
        Debug.Log($"Current MouseDelta Values are : {MouseDelta}");
        return MouseDelta;
    }

    // This needs to be called from a MonoBehaviour
    public void DisableActionFor(InputAction action, float seconds, MonoBehaviour caller)
    {
        if (action == null || caller == null) return;

        // If there's already a coroutine disabling this action, stop it
        if (activeDisableCoroutines.ContainsKey(action))
        {
            caller.StopCoroutine(activeDisableCoroutines[action]);
        }

        Coroutine newCoroutine = caller.StartCoroutine(DisableActionCoroutine(action, seconds, caller));
        activeDisableCoroutines[action] = newCoroutine;
    }

    // 🔹 Disable multiple actions using `params` (Cleaner Syntax)
    public void DisableMultipleActionsFor(float seconds, MonoBehaviour caller, params InputAction[] actions)
    {
        if (actions == null || actions.Length == 0) return;

        foreach (var action in actions)
        {
            DisableActionFor(action, seconds, caller);
        }
    }

    private IEnumerator DisableActionCoroutine(InputAction action, float seconds, MonoBehaviour caller)
    {
        action.Disable();
        Debug.Log($"Disabled {action.name} for {seconds} seconds");

        yield return new WaitForSeconds(seconds);

        action.Enable();
        activeDisableCoroutines.Remove(action);
        Debug.Log($"Enabled {action.name} after {seconds} seconds");
    }

    public void EnableAction(InputAction action, MonoBehaviour caller)
    {
        if (action == null || caller == null) return;

        if (activeDisableCoroutines.ContainsKey(action))
        {
            caller.StopCoroutine(activeDisableCoroutines[action]);
            activeDisableCoroutines.Remove(action);
        }

        action.Enable();
        Debug.Log($"Manually enabled {action.name}");
    }

    public void EnableAllActions()
    {
        foreach (var action in activeDisableCoroutines.Keys)
        {
            action.Enable();
            Debug.Log($"Manually enabled {action.name}");
        }

        activeDisableCoroutines.Clear();
    }

    #endregion
}
