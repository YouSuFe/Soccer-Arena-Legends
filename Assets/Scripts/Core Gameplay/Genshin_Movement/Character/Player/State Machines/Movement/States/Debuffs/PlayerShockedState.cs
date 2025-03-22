using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShockedState : PlayerDebuffState
{
    PlayerShockedData shockedData;
    private float shockedDuration;  // Duration of the frozen state
    private float shockedTimer;     // Timer to track elapsed time
    public PlayerShockedState(PlayerMovementStateMachine playerMovementStateMachine, CameraSwitchHandler cameraSwitcher) : base(playerMovementStateMachine, cameraSwitcher)
    {
        shockedData = debuffData.PlayerShockedData;
        shockedDuration = debuffData.PlayerShockedData.ShockedDurationTime;
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();

        shockedTimer = 0f;

        stateMachine.ReusableData.isShocked = true;

        StartAnimation(stateMachine.PlayerController.AnimationData.ShockedParameterHash);

        stateMachine.ReusableData.MovementSpeedModifier = shockedData.SpeedModifier;

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
        ManageShockedStateTimer();
    }


    public override void Exit()
    {
        base.Exit();
        StopAnimation(stateMachine.PlayerController.AnimationData.ShockedParameterHash);
        stateMachine.ReusableData.isShocked = false;

    }


    #endregion




    #region Main Methods


    private void ManageShockedStateTimer()
    {
        shockedTimer += Time.deltaTime;  // Update the timer

        if (shockedTimer >= shockedDuration)
        {
            // Transition to another state after frozen time is exceeded
            stateMachine.ChangeState(PlayerState.Idle);
        }
    }

    #endregion
}
