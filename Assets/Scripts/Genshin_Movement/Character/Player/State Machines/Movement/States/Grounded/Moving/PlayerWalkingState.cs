using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWalkingState : PlayerMovingState
{
    public PlayerWalkingState(PlayerMovementStateMachine playerMovementStateMachine, CameraSwitchHandler cameraSwitchHandler) : base(playerMovementStateMachine, cameraSwitchHandler)
    {
    }

    #region IState Method
    public override void Enter()
    {
        stateMachine.ReusableData.MovementSpeedModifier = groundedData.WalkData.SpeedModifier;

        base.Enter();

        StartAnimation(stateMachine.PlayerController.AnimationData.WalkParameterHash);


        stateMachine.ReusableData.CurrentJumpForce = airborneData.PlayerJumpData.WeakForce;
    }

    public override void Exit()
    {
        base.Exit();

        StopAnimation(stateMachine.PlayerController.AnimationData.WalkParameterHash);
    }

    #endregion

    #region Input Methods
    protected override void InputManager_OnWalkToggleStarted()
    {
        base.InputManager_OnWalkToggleStarted();

        stateMachine.ChangeState(PlayerState.Running);
    }

    protected override void InputManager_OnMovementCanceled()
    {
        stateMachine.ChangeState(PlayerState.LightStopping);
    }

    #endregion

}
