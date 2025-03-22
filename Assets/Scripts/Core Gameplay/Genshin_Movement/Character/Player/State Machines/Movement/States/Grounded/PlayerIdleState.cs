using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIdleState : PlayerGroundedState
{
    public PlayerIdleState(PlayerMovementStateMachine playerMovementStateMachine, CameraSwitchHandler cameraSwitchHandler) : base(playerMovementStateMachine, cameraSwitchHandler)
    {
    }

    #region IState Methods
    public override void Enter()
    {

        base.Enter();

        stateMachine.ReusableData.MovementSpeedModifier = 1f;

        StartAnimation(stateMachine.PlayerController.AnimationData.IdleParameterHash);

        stateMachine.ReusableData.CurrentJumpForce = airborneData.PlayerJumpData.StationaryForce;
        ResetVelocity();
    }

    public override void Update()
    {
        base.Update();
        if (stateMachine.ReusableData.MovementInput == Vector2.zero) return;

        OnMove();
    }

    public override void Exit()
    {
        base.Exit();

        StopAnimation(stateMachine.PlayerController.AnimationData.IdleParameterHash);
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        if (!IsMovingHorizontally()) return;

        ResetVelocity();
    }

    #endregion
}
