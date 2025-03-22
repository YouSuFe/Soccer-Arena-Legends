using System;
using UnityEngine;

public class PlayerGroundedState : PlayerMovementState
{
    private SlopeData slopeData;

    public PlayerGroundedState(PlayerMovementStateMachine playerMovementStateMachine, CameraSwitchHandler cameraSwitchHandler) : base(playerMovementStateMachine, cameraSwitchHandler)
    {
        slopeData = stateMachine.PlayerController.ColliderUtility.SlopeData;
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();

        StartAnimation(stateMachine.PlayerController.AnimationData.GroundedParameterHash);

        UpdateShouldSprintState();

    }


    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        FloatingPlayerCapsule();
    }

    public override void Exit()
    {
        base.Exit();

        StopAnimation(stateMachine.PlayerController.AnimationData.GroundedParameterHash);
    }

    #endregion

    #region Main Methods

    // Find a proper name for this method
    private void FloatingPlayerCapsule()
    {
        if (stateMachine == null)
        {
            Debug.LogError("stateMachine is NULL!");
            return;
        }

        if (stateMachine.PlayerController == null)
        {
            Debug.LogError("stateMachine.PlayerController is NULL!");
            return;
        }

        if (stateMachine.PlayerController.ColliderUtility == null)
        {
            Debug.LogError("stateMachine.PlayerController.ColliderUtility is NULL!");
            return;
        }

        if (stateMachine.PlayerController.ColliderUtility.CapsuleColliderData == null)
        {
            Debug.LogError("stateMachine.PlayerController.ColliderUtility.CapsuleColliderData is NULL!");
            return;
        }

        if (stateMachine.PlayerController.ColliderUtility.CapsuleColliderData.Collider == null)
        {
            Debug.LogError("stateMachine.PlayerController.ColliderUtility.CapsuleColliderData.Collider is NULL!");
            return;
        }
        Vector3 capsuleColliderCenterInWorldSpace = stateMachine.PlayerController.ColliderUtility.CapsuleColliderData.Collider.bounds.center;

        Ray downwardsRayFromCapsuleCenter = new Ray(capsuleColliderCenterInWorldSpace, Vector3.down);

        if (Physics.Raycast(downwardsRayFromCapsuleCenter, out RaycastHit hit, slopeData.FloatRayDistance, stateMachine.PlayerController.LayerData.GroundLayer, QueryTriggerInteraction.Ignore))
        {
            float groundAngle = Vector3.Angle(hit.normal, -downwardsRayFromCapsuleCenter.direction);

            float slopeSpeedModifier = SetSlopeSpeedModifierOnAngle(groundAngle);

            if (slopeSpeedModifier == 0f) return;

            float distanceToFloatingPoint = stateMachine.PlayerController.ColliderUtility.CapsuleColliderData.ColliderCenterInLocalSpace.y
                * stateMachine.PlayerController.transform.localScale.y - hit.distance;

            if (distanceToFloatingPoint == 0f) return;

            float amountToLift = distanceToFloatingPoint * slopeData.StepReachForce - GetPlayerVerticalVelocity().y;

            Vector3 liftForce = new Vector3(0f, amountToLift, 0f);

            playerRigidbody.AddForce(liftForce, ForceMode.VelocityChange);
        }
    }

    private float SetSlopeSpeedModifierOnAngle(float groundAngle)
    {
        float slopeSpeedModifier = groundedData.SlopeSpeedAngle.Evaluate(groundAngle);

        stateMachine.ReusableData.MovementOnSlopeSpeedModifier = slopeSpeedModifier;

        return slopeSpeedModifier;
    }

    private void UpdateShouldSprintState()
    {
        if (!stateMachine.ReusableData.ShouldSprint) return;

        if (stateMachine.ReusableData.MovementInput != Vector2.zero) return;

        stateMachine.ReusableData.ShouldSprint = false;
    }


    private bool IsThereGroundUnderneath()
    {
        BoxCollider groundCheckCollider = stateMachine.PlayerController.ColliderUtility.TriggerColliderData.GroundCheckCollider;

        Vector3 groundColliderCenterInWorldSpace = groundCheckCollider.bounds.center;

        Collider[] overlappedGroundColliders = Physics.OverlapBox(groundColliderCenterInWorldSpace, stateMachine.PlayerController.ColliderUtility.TriggerColliderData.GroundCheckColliderExtents, groundCheckCollider.transform.rotation,
            stateMachine.PlayerController.LayerData.GroundLayer, QueryTriggerInteraction.Ignore);

        return overlappedGroundColliders.Length > 0;
    }

    #endregion

    #region Reusable Methods

    protected override void AddInputActionsCallBacks()
    {
        base.AddInputActionsCallBacks();

        inputReader.OnMovementCanceled += InputManager_OnMovementCanceled;
        inputReader.OnMovementPerformed += InputManager_OnMovementPerformed;

        // ToDo: Buraya bir Weapon Skill kullanma durumu ekle sonra state değiştir içinde.
        // Reusable Data içinde ENUM oluştur. isDashing, isPulling tarzı durumlarda switch ile Q' de baktığımız skill durumuna atlarız

        inputReader.OnJumpEvent += InputManager_OnJump;

        inputReader.OnStartingSprint += InputManager_OnStartingSprint;
    }



    protected override void RemoveInputActionsCallBacks()
    {
        base.RemoveInputActionsCallBacks();

        inputReader.OnMovementCanceled -= InputManager_OnMovementCanceled;
        inputReader.OnMovementPerformed -= InputManager_OnMovementPerformed;

        inputReader.OnStartingSprint -= InputManager_OnStartingSprint;
        inputReader.OnJumpEvent -= InputManager_OnJump;

    }

    protected virtual void OnMove()
    {
        if(stateMachine.ReusableData.ShouldSprint && CanKeepSprinting())
        {
            stateMachine.ChangeState(PlayerState.Sprinting);
            return;
        }

        if (stateMachine.ReusableData.ShouldWalk)
        {
            stateMachine.ChangeState(PlayerState.Walking);
            return;
        }

        stateMachine.ChangeState(PlayerState.Running);
    }

    protected override void OnContactWithGroundExited(Collider collider)
    {
        base.OnContactWithGroundExited(collider);

        if (IsThereGroundUnderneath()) return;

        Vector3 capsuleColliderCenterInWorldSpace = stateMachine.PlayerController.ColliderUtility.CapsuleColliderData.Collider.bounds.center;

        Ray downwardsRayFromCapsuleBottom = new Ray(capsuleColliderCenterInWorldSpace - stateMachine.PlayerController.ColliderUtility.CapsuleColliderData.colliderVerticalExtents, Vector3.down);


        if (!Physics.Raycast(downwardsRayFromCapsuleBottom, out _, groundedData.GroundToFallRayDistance, stateMachine.PlayerController.LayerData.GroundLayer, QueryTriggerInteraction.Ignore))
        {
            OnFall();

        }

    }

    protected virtual void OnFall()
    {
        stateMachine.ChangeState(PlayerState.Falling);
    }
    #endregion


    #region Input Methods


    protected virtual void InputManager_OnMovementCanceled()
    {
        stateMachine.ChangeState(PlayerState.Idle);
    }

    protected virtual void InputManager_OnStartingSprint()
    {
        if (!CanStartSprinting()) return;

        stateMachine.ChangeState(PlayerState.SprintStarting);
    }

    protected virtual void InputManager_OnJump()
    {
        stateMachine.ChangeState(PlayerState.Jumping);
    }

    protected virtual void InputManager_OnMovementPerformed()
    {
        UpdateTargetRotation(GetMovementInputDirection());
    }


    #endregion

}
