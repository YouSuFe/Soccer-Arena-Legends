using UnityEngine;

public class SpecialWeaponState : PlayerMovementState
{
    private SpecialWeaponFloatingCapsuleData weaponData;

    public SpecialWeaponState(PlayerMovementStateMachine playerMovementStateMachine, CameraSwitchHandler cameraSwitcher) : base(playerMovementStateMachine, cameraSwitcher)
    {
        weaponData = stateMachine.PlayerController.PlayerData.SpecialWeaponData.SpecialWeaponFloatingCapsuleData;
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();
        StartAnimation(stateMachine.PlayerController.AnimationData.WeaponSkillParamaterHash);

    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        FloatingPlayerCapsule();
    }

    public override void Exit()
    {
        base.Exit();
        StopAnimation(stateMachine.PlayerController.AnimationData.WeaponSkillParamaterHash);
    }

    public override void OnCollisionEnter(Collision collision)
    {
    }

    #endregion

    #region Main Methods

    // Find a proper name for this method
    protected void FloatingPlayerCapsule()
    {
        Vector3 capsuleColliderCenterInWorldSpace = stateMachine.PlayerController.ColliderUtility.CapsuleColliderData.Collider.bounds.center;

        Ray downwardsRayFromCapsuleCenter = new Ray(capsuleColliderCenterInWorldSpace, Vector3.down);

        if (Physics.Raycast(downwardsRayFromCapsuleCenter, out RaycastHit hit, weaponData.FloatRayDistance, stateMachine.PlayerController.LayerData.GroundLayer, QueryTriggerInteraction.Ignore))
        {
            float groundAngle = Vector3.Angle(hit.normal, -downwardsRayFromCapsuleCenter.direction);

            float slopeSpeedModifier = SetSlopeSpeedModifierOnAngle(groundAngle);

            if (slopeSpeedModifier == 0f) return;

            float distanceToFloatingPoint = stateMachine.PlayerController.ColliderUtility.CapsuleColliderData.ColliderCenterInLocalSpace.y
                * stateMachine.PlayerController.transform.localScale.y - hit.distance;

            if (distanceToFloatingPoint == 0f) return;

            float amountToLift = distanceToFloatingPoint * weaponData.StepReachForce - GetPlayerVerticalVelocity().y;

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

    #endregion

    #region Reusable Methods

    protected virtual void ResetSprintState()
    {
        stateMachine.ReusableData.ShouldSprint = false;
    }
    #endregion

}
