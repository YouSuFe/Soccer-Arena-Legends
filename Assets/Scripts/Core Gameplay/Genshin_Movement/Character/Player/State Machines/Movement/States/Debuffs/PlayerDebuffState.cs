using System;
using UnityEngine;

public class PlayerDebuffState : PlayerMovementState
{
    private SlopeData slopeData;

    public PlayerDebuffState(PlayerMovementStateMachine playerMovementStateMachine, CameraSwitchHandler cameraSwitcher) : base(playerMovementStateMachine, cameraSwitcher)
    {
        slopeData = stateMachine.PlayerController.ColliderUtility.SlopeData;
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();
        StartAnimation(stateMachine.PlayerController.AnimationData.DebuffParamaterHash);

    }

    public override void Exit()
    {
        base.Exit();
        StopAnimation(stateMachine.PlayerController.AnimationData.DebuffParamaterHash);
    }

    #endregion

    #region Main Methods

    // Find a proper name for this method
    protected void FloatingPlayerCapsule()
    {
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

    #endregion

    #region Reusable Methods

    protected virtual void ResetSprintState()
    {
        stateMachine.ReusableData.ShouldSprint = false;
    }
    #endregion
}
