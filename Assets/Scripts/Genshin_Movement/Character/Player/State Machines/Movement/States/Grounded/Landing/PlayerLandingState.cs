using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLandingState : PlayerGroundedState
{
    public PlayerLandingState(PlayerMovementStateMachine playerMovementStateMachine, CameraSwitchHandler cameraSwitchHandler) : base(playerMovementStateMachine, cameraSwitchHandler)
    {
    }


    #region IState Methods

    public override void Enter()
    {
        base.Enter();

        StartAnimation(stateMachine.PlayerController.AnimationData.LandingParameterHash);
    }

    public override void Exit()
    {
        base.Exit();

        StopAnimation(stateMachine.PlayerController.AnimationData.LandingParameterHash);

    }

    #endregion

    #region Input Methods

    /*
    protected override void InputManager_OnMovementCanceled()
    {
    }
    */
    #endregion
}
