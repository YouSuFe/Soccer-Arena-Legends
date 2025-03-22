using UnityEngine;

public class PlayerSpringStartingState : PlayerGroundedState
{
    private PlayerSprintStartingData dashData;

    private float startTime;

    private int consecutiveDashesUsed;

    private bool shouldKeepRotating;

    public PlayerSpringStartingState(PlayerMovementStateMachine playerMovementStateMachine, CameraSwitchHandler cameraSwitchHandler) : base(playerMovementStateMachine, cameraSwitchHandler)
    {
        dashData = groundedData.PlayerSprintStartingData;
    }
    #region IState Methods

    public override void Enter()
    {
        stateMachine.ReusableData.MovementSpeedModifier = dashData.SpeedModifier;

        base.Enter();

        StartAnimation(stateMachine.PlayerController.AnimationData.DashParameterHash);

        stateMachine.ReusableData.PlayerRotationData = dashData.PlayerRotationData;

        stateMachine.ReusableData.CurrentJumpForce = airborneData.PlayerJumpData.StrongForce;

        DecreaseStamina();

        Dash();

        shouldKeepRotating = stateMachine.ReusableData.MovementInput != Vector2.zero;

        UpdateConsecutiveDashes();

        startTime = Time.time;
    }

    public override void Exit()
    {
        base.Exit();

        StopAnimation(stateMachine.PlayerController.AnimationData.DashParameterHash);

        SetBasePlayerRotationData();
    }

    public override void OnAnimationTransitionEvent()
    {
        if(stateMachine.ReusableData.MovementInput == Vector2.zero)
        {
            stateMachine.ChangeState(PlayerState.LightStopping);

            return;
        }

        stateMachine.ChangeState(PlayerState.Sprinting);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        // Only rotate the player during the dash if we're in TPS mode
        if (shouldKeepRotating && !cameraSwitchHandler.IsFPSCameraActive())
        {
            RotateTowardsTargetRotation();
        }
        else
        {
            // Dash towards to facing direction when player movements input is zero 
            Dash();
        }
    }


    #endregion

    #region Main Methods

    private void Dash()
    {
        Vector3 dashDirection;

        // If there is movement input, dash in the movement input direction
        if (stateMachine.ReusableData.MovementInput != Vector2.zero)
        {
            // Calculate dash direction based on movement input
            dashDirection = GetMovementInputDirection();

            // Update the player's rotation to face the dash direction, even if it's backward
            UpdateTargetRotation(dashDirection, false);
        }
        else
        {
            // Check if FPS or TPS camera is active
            if (cameraSwitchHandler.IsFPSCameraActive())
            {
                // In FPS, dash in the direction the camera is facing
                dashDirection = stateMachine.PlayerController.MainCameraTransform.forward;
                dashDirection.y = 0f; // Ensure no vertical movement

                // Update the player's rotation to match the camera direction
                UpdateTargetRotation(dashDirection, false);
            }
            else
            {
                // In TPS, dash in the direction the player is currently facing
                dashDirection = stateMachine.PlayerController.transform.forward;
                dashDirection.y = 0f; // Ensure no vertical movement

                // Update target rotation to keep the player dashing forward
                UpdateTargetRotation(dashDirection, false);
                dashDirection = GetTargetRotationDirection(stateMachine.ReusableData.CurrentTargetRotation.y);

            }
        }

        // Reset current velocity to ensure the dash force takes full effect
        ResetVelocity();

        // Apply the dash force
        playerRigidbody.linearVelocity = dashDirection * GetMovementSpeed(false);
    }

    private void DecreaseStamina()
    {
        stateMachine.PlayerController.Player.PlayerStamina -= 20f;
    }

    private void UpdateConsecutiveDashes()
    {
        if (!IsConsecutive()) consecutiveDashesUsed = 0;

        ++consecutiveDashesUsed;

        if (consecutiveDashesUsed == dashData.ConsecutiveDashesLimitAmount)
        {
            consecutiveDashesUsed = 0;

            inputReader.DisableActionFor(inputReader.PlayerInputActions.Player.Dash, dashData.DashLimitReachedCooldown, stateMachine.PlayerController);
        }


    }

    private bool IsConsecutive()
    {
        return Time.time < startTime + dashData.TimeToBeConsiderConsecutive;
    }


    #endregion

    #region Reusable Methods

    

    #endregion

    #region Input Methods

    protected override void InputManager_OnStartingSprint()
    {

    }

    protected override void InputManager_OnMovementCanceled()
    {
    }

    protected override void InputManager_OnMovementPerformed()
    {
        //base.InputManager_OnMovementPerformed();
        shouldKeepRotating = true;
    }

    #endregion

}
