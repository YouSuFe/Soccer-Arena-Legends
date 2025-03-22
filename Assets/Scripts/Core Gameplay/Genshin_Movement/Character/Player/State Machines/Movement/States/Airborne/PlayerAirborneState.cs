using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAirborneState : PlayerMovementState
{
    public PlayerAirborneState(PlayerMovementStateMachine playerMovementStateMachine, CameraSwitchHandler cameraSwitchHandler) : base(playerMovementStateMachine, cameraSwitchHandler)
    {
    }

    #region IState Method

    public override void Enter()
    {
        base.Enter();

        StartAnimation(stateMachine.PlayerController.AnimationData.AirborneParameterHash);

        ResetSprintState();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

    }

    public override void Exit()
    {
        base.Exit();

        StopAnimation(stateMachine.PlayerController.AnimationData.AirborneParameterHash);
    }

    #endregion


    #region Reusable Methods

    protected override void OnContactWithGround(Collider collider)
    {
        stateMachine.ChangeState(PlayerState.LightLanding);
    }

    protected virtual void ResetSprintState()
    {
        stateMachine.ReusableData.ShouldSprint = false;
    }
    #endregion
}

