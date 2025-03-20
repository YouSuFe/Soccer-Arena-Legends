using UnityEngine;

public class PlayerJumpingState : PlayerAirborneState
{

    private PlayerJumpData jumpData;

    private bool shouldKeepRotating;
    private bool canStartFalling;
    private bool canStrongJump;

    public PlayerJumpingState(PlayerMovementStateMachine playerMovementStateMachine, CameraSwitchHandler cameraSwitchHandler) : base(playerMovementStateMachine, cameraSwitchHandler)
    {
        jumpData = airborneData.PlayerJumpData;
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();

        // ToDo: Decide whether speed modifiers will be changed in the future to use
        // with fall or jump speed modifier
        if (stateMachine.ReusableData.MovementSpeedModifier <= jumpData.SpeedModifier)
        {
            stateMachine.ReusableData.MovementSpeedModifier = jumpData.SpeedModifier;
        }

        canStrongJump = CanStrongJump();

        stateMachine.ReusableData.PlayerRotationData = jumpData.PlayerRotationData;

        stateMachine.ReusableData.MovementDecelerationForce = jumpData.DecelerationForce;

        shouldKeepRotating = stateMachine.ReusableData.MovementInput != Vector2.zero;

        Jump();
    }



    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        // Only keep rotating during the jump if we're in TPS mode
        if (shouldKeepRotating && !cameraSwitchHandler.IsFPSCameraActive())
        {
            RotateTowardsTargetRotation();
        }

        if(IsMovingUp())
        {
            DecelerateVertically();
        }
    }

    public override void Update()
    {
        base.Update();


        if (!canStartFalling && IsMovingUp(0f))
        {
            canStartFalling = true;
        }

        if (!canStartFalling || GetPlayerVerticalVelocity().y > 0)
        {
            return;
        }


        stateMachine.ChangeState(PlayerState.Falling);
    }


    public override void Exit()
    {
        base.Exit();

        SetBasePlayerRotationData();

        canStartFalling = false;
    }

    #endregion



    #region Reusable Methods

    protected override void ResetSprintState()
    {
    }

    #endregion




    #region Input Methods



    #endregion




    #region Main Methods

    private void Jump()
    {
        Vector3 jumpForce = stateMachine.ReusableData.CurrentJumpForce;

        if (canStrongJump)
        {
            jumpForce = stateMachine.ReusableData.CurrentJumpForce * 2;
        }

        Vector3 jumpDirection = stateMachine.PlayerController.transform.forward;

        if (shouldKeepRotating)
        {
            // Check camera mode to determine how to handle jump direction
            if (cameraSwitchHandler.IsFPSCameraActive())
            {
                // In first-person mode, use movement direction relative to the camera
                jumpDirection = GetMovementInputDirection();
            }
            else
            {
                // In third-person mode, update the rotation and calculate jump direction
                UpdateTargetRotation(GetMovementInputDirection());
                jumpDirection = GetTargetRotationDirection(stateMachine.ReusableData.CurrentTargetRotation.y);
            }
        }

        jumpForce.x *= jumpDirection.x;
        jumpForce.z *= jumpDirection.z;

        jumpForce = GetForceModeOnSlope(jumpForce);

        ResetVelocity();

        playerRigidbody.AddForce(jumpForce, ForceMode.VelocityChange);
    }

    private Vector3 GetForceModeOnSlope(Vector3 jumpForce)
    {
        Vector3 capsuleColliderCenterInWorldSpace = stateMachine.PlayerController.ColliderUtility.CapsuleColliderData.Collider.bounds.center;

        Ray downwardsRayFromCapsuleCollider = new Ray(capsuleColliderCenterInWorldSpace, Vector3.down);

        if (Physics.Raycast(downwardsRayFromCapsuleCollider, out RaycastHit hit, jumpData.JumpToGroundRayDistance, stateMachine.PlayerController.LayerData.GroundLayer, QueryTriggerInteraction.Ignore))
        {
            float groundAngle = Vector3.Angle(hit.normal, -downwardsRayFromCapsuleCollider.direction);

            if (IsMovingUp())
            {
                float forceModifier = jumpData.JumpForceModifierOnSlopeUpwards.Evaluate(groundAngle);

                jumpForce.x *= forceModifier;
                jumpForce.z *= forceModifier;
            }

            if (IsMovingDown())
            {
                float forceModifier = jumpData.JumpForceModifierOnSlopeDownwards.Evaluate(groundAngle);

                jumpForce.y *= forceModifier;
            }
        }

        return jumpForce;
    }

    #endregion

    public bool CanStrongJump()
    {
        return stateMachine.ReusableData.isGravityManBallSkillUsed;
    }
}
