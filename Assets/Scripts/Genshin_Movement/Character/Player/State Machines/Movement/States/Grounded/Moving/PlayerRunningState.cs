using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRunningState : PlayerMovingState
{
    private PlayerSprintData sprintData;

    private float startTime;

    public PlayerRunningState(PlayerMovementStateMachine playerMovementStateMachine, CameraSwitchHandler cameraSwitchHandler) : base(playerMovementStateMachine, cameraSwitchHandler)
    {
        sprintData = groundedData.PlayerSprintData;
    }

    #region IState Method
    public override void Enter()
    {
        stateMachine.ReusableData.MovementSpeedModifier = groundedData.RunData.SpeedModifier;

        base.Enter();

        StartAnimation(stateMachine.PlayerController.AnimationData.RunParameterHash);


        stateMachine.ReusableData.CurrentJumpForce = airborneData.PlayerJumpData.MediumForce;


        startTime = Time.time;
    }

    public override void Update()
    {
        base.Update();

        // Means Running
        if (!stateMachine.ReusableData.ShouldWalk) return;

        if (Time.time < startTime + sprintData.RunToWalkTime) return;

        StopRunning();
    }

    public override void Exit()
    {
        base.Exit();

        StopAnimation(stateMachine.PlayerController.AnimationData.RunParameterHash);
    }

    #endregion


    #region Input Methods
    protected override void InputManager_OnWalkToggleStarted()
    {
        base.InputManager_OnWalkToggleStarted();

        stateMachine.ChangeState(PlayerState.Walking);
    }

    protected override void InputManager_OnMovementCanceled()
    {
        stateMachine.ChangeState(PlayerState.LightStopping);
    }


    #endregion

    #region Main Methods

    private void StopRunning()
    {
        if(stateMachine.ReusableData.MovementInput == Vector2.zero)
        {
            stateMachine.ChangeState(PlayerState.LightStopping);

            return;
        }

        stateMachine.ChangeState(PlayerState.Walking);
    }


    #endregion
}
