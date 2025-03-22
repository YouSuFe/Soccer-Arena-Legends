using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStunnedState : PlayerDebuffState
{
    PlayerStunnedData stunnedData;
    private float stunDuration;  // Duration of the frozen state
    private float stunTimer;     // Timer to track elapsed time

    public PlayerStunnedState(PlayerMovementStateMachine playerMovementStateMachine, CameraSwitchHandler cameraSwitcher) : base(playerMovementStateMachine, cameraSwitcher)
    {
        stunnedData = debuffData.PlayerStunnedData;
        stunDuration = stunnedData.StunDurationTime;
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();

        stunTimer = 0f;

        stateMachine.ReusableData.isStunned = true;

        StartAnimation(stateMachine.PlayerController.AnimationData.StunnedParameterHash);

        stateMachine.ReusableData.MovementSpeedModifier = stunnedData.SpeedModifier;
    }

    public override void PhysicsUpdate()
    {
        FloatingPlayerCapsule();
    }

    public override void CameraUpdate()
    {
    }

    public override void Update()
    {
        base.Update();
        ManageStunStateTimer();
    }

    public override void Exit()
    {
        base.Exit();
        StopAnimation(stateMachine.PlayerController.AnimationData.StunnedParameterHash);
        stateMachine.ReusableData.isStunned = false;

    }


    #endregion




    #region Main Methods

    private void ManageStunStateTimer()
    {
        stunTimer += Time.deltaTime;  // Update the timer

        if (stunTimer >= stunDuration)
        {
            // Transition to another state after frozen time is exceeded
            stateMachine.ChangeState(PlayerState.Idle);
        }
    }


    #endregion
}
