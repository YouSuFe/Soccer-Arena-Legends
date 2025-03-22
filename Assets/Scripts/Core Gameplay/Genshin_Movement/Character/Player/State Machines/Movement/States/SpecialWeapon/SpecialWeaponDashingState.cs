using UnityEngine;

public class SpecialWeaponDashingState : SpecialWeaponState
{
    private SpecialWeaponDashData weaponDashData;
    private float finalDashSpeed;
    private float dashTimeElapsed;
    private Vector3 dashDirection;

    public SpecialWeaponDashingState(PlayerMovementStateMachine playerMovementStateMachine, CameraSwitchHandler cameraSwitcher) : base(playerMovementStateMachine, cameraSwitcher)
    {
        weaponDashData = specialWeaponData.SpecialWeaponDashData;
    }

    #region IState Methods

    public override void Enter()
    {
        stateMachine.ReusableData.MovementSpeedModifier = weaponDashData.SpeedModifier;

        base.Enter();

        float currentDashSpeed = stateMachine.PlayerController.Player.Stats.GetCurrentStat(StatType.Speed) * weaponDashData.SpeedModifier;

        finalDashSpeed = currentDashSpeed > weaponDashData.MinDashSpeed
            ? currentDashSpeed
            : weaponDashData.MinDashSpeed;

        stateMachine.ReusableData.isDashing = true;

        stateMachine.ReusableData.SpecialSkillReusableData = SpecialSkillReusableData.IsDashing;

        StartAnimation(stateMachine.PlayerController.AnimationData.SpeacialDashinParameterHash);

        ResetVelocity();

        ResetSprintState();

        StartDash();

    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        // Only rotate the player during the dash if we're in TPS mode
        if ( !cameraSwitchHandler.IsFPSCameraActive())
        {
            RotateTowardsTargetRotation();
        }

        UpdateDash();

    }

    public override void Update()
    {
        base.Update();
    }


    public override void Exit()
    {
        base.Exit();

        StopAnimation(stateMachine.PlayerController.AnimationData.SpeacialDashinParameterHash);

        stateMachine.ReusableData.isDashing = false;
        stateMachine.ReusableData.SpecialSkillReusableData = SpecialSkillReusableData.None;

        ResetVelocity();
    }

    #endregion

    #region Collision Method

    // ToDo: Here we call EndDash method in Client side.
    // Should I move it to the Server Side just as NotifyClientsEndDashClientRpc
    public override void OnCollisionEnter(Collision collision)
    {
        Debug.LogError("Inside the Oncollision Method");
        // Check if the collision is with a layer that should stop the dash
        if (stateMachine.ReusableData.isDashing && ((weaponDashData.CollisionLayers.value & (1 << collision.gameObject.layer)) != 0))
        {
            Debug.LogError("Inside the Oncollision Method EndDash");
            EndDash(); // Stop the dash on collision
            stateMachine.PlayerController.NotifyClientsEndDashClientRpc(); //Sync the dash end across clients
        }
    }

    #endregion


    #region Main Methods

    public void StartDash()
    {
        stateMachine.ReusableData.isDashing = true;
        dashTimeElapsed = 0f;
        dashDirection = stateMachine.PlayerController.MainCameraTransform.forward.normalized;

        UpdateTargetRotation(dashDirection, false);

        ResetVelocity(); // Optional to reset velocity
    }

    public void UpdateDash()
    {
        if (!stateMachine.ReusableData.isDashing) return;

        dashTimeElapsed += Time.deltaTime;

        playerRigidbody.linearVelocity = dashDirection * finalDashSpeed;

        if (dashTimeElapsed >= weaponDashData.TimeToCompleteDash)
        {
            EndDash();
        }
    }


    public void EndDash()
    {
        // Check for ground under the player when dash ends
        if (!IsThereGroundUnderneath())
        {
            stateMachine.ChangeState(PlayerState.Falling);
        }
        else
        {
            stateMachine.ChangeState(PlayerState.LightStopping); // Change to stopping state if ground is detected
        }

        ResetVelocity();
    }

    #endregion

    private bool IsThereGroundUnderneath()
    {
        // Use the same OverlapBox ground check as in the grounded state
        BoxCollider groundCheckCollider = stateMachine.PlayerController.ColliderUtility.TriggerColliderData.GroundCheckCollider;

        Vector3 groundColliderCenterInWorldSpace = groundCheckCollider.bounds.center;

        Collider[] overlappedGroundColliders = Physics.OverlapBox(
            groundColliderCenterInWorldSpace,
            stateMachine.PlayerController.ColliderUtility.TriggerColliderData.GroundCheckColliderExtents,
            groundCheckCollider.transform.rotation,
            stateMachine.PlayerController.LayerData.GroundLayer,
            QueryTriggerInteraction.Ignore
        );

        // Return true if there are any colliders under the player
        return overlappedGroundColliders.Length > 0;
    }

}
