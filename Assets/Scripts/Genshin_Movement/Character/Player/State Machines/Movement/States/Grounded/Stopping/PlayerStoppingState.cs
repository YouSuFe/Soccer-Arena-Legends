using UnityEngine;

public class PlayerStoppingState : PlayerGroundedState
{
    public PlayerStoppingState(PlayerMovementStateMachine playerMovementStateMachine, CameraSwitchHandler cameraSwitchHandler) : base(playerMovementStateMachine, cameraSwitchHandler)
    {
    }

    #region IState Methods

    public override void Enter()
    {
        stateMachine.ReusableData.MovementSpeedModifier = 0f;

        base.Enter();

        StartAnimation(stateMachine.PlayerController.AnimationData.StoppingParameterHash);

        ResetVelocity();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        // Ensure rotation continues with the camera in FPS mode
        if (cameraSwitchHandler.IsFPSCameraActive())
        {
            RotateWithCamera();  // Keep rotating to match the camera's Y axis in first-person mode
        }
        else
        {
            RotateTowardsTargetRotation();
        }

        if (!IsMovingHorizontally()) return;

        // Todo: We can use this sliding effect if we need or delete it
        DecelerateHorizontally();

    }

    // This method override makes our "OnMovementCanceled" useless so override that as well
    public override void OnAnimationTransitionEvent()
    {
        stateMachine.ChangeState(PlayerState.Idle);
    }

    public override void Exit()
    {
        base.Exit();

        StopAnimation(stateMachine.PlayerController.AnimationData.StoppingParameterHash);
    }

    #endregion

    #region Reusable Methods


    protected override void AddInputActionsCallBacks()
    {
        base.AddInputActionsCallBacks();

        inputReader.OnMovementStarted += InputManager_OnMovementStarted;
    }

    protected override void RemoveInputActionsCallBacks()
    {
        base.RemoveInputActionsCallBacks();

        inputReader.OnMovementStarted -= InputManager_OnMovementStarted;
    }

    

    #endregion

    #region Input Methods

    private void InputManager_OnMovementStarted()
    {
        OnMove();
    }

    #endregion
}
