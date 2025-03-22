using UnityEngine;

public class PlayerSprintingState : PlayerMovingState
{
    private PlayerSprintData sprintData;

    private float startTime;

    private bool keepSprinting;

    private bool shouldResetSprintingState;


    public PlayerSprintingState(PlayerMovementStateMachine playerMovementStateMachine, CameraSwitchHandler cameraSwitchHandler) : base(playerMovementStateMachine, cameraSwitchHandler)
    {
        sprintData = groundedData.PlayerSprintData;
    }

    #region IState Method

    public override void Enter()
    {
        stateMachine.ReusableData.MovementSpeedModifier = sprintData.SpeedModifier;

        base.Enter();

        StartAnimation(stateMachine.PlayerController.AnimationData.SprintParameterHash);


        stateMachine.ReusableData.CurrentJumpForce = airborneData.PlayerJumpData.StrongForce;

        shouldResetSprintingState = true;

        startTime = Time.time;

        if (!stateMachine.ReusableData.ShouldSprint)
        {
            keepSprinting = false;
        }
    }

    public override void Exit()
    {
        base.Exit();

        StopAnimation(stateMachine.PlayerController.AnimationData.SprintParameterHash);

        if (shouldResetSprintingState)
        {
            keepSprinting = false;

            stateMachine.ReusableData.ShouldSprint = false;
        }

    }

    public override void Update()
    {
        base.Update();

        // Check if the player's stamina is depleted
        if (stateMachine.PlayerController.Player.PlayerStamina <= 0)
        {
            StopSprinting();
            return;
        }

        // Check if there is no movement input
        if (stateMachine.ReusableData.MovementInput == Vector2.zero)
        {
            stateMachine.ChangeState(PlayerState.Idle);
            return;
        }

        // Sprint duration logic
        if (!keepSprinting && Time.time >= startTime + sprintData.SprintToRunTime)
        {
            StopSprinting();
        }

    }

    #endregion


    #region Main Methods

    private void StopSprinting()
    {

        if(stateMachine.ReusableData.MovementInput == Vector2.zero)
        {
            stateMachine.ChangeState(PlayerState.Idle);

            return;
        }
        else
        {
            stateMachine.ChangeState(PlayerState.Running);
        }
    }

    #endregion

    #region Reusable Methods

    protected override void AddInputActionsCallBacks()
    {
        base.AddInputActionsCallBacks();

        inputReader.OnSprintPerformed += InputManager_OnSprintPerformed;
    }

    protected override void RemoveInputActionsCallBacks()
    {
        base.RemoveInputActionsCallBacks();

        inputReader.OnSprintPerformed -= InputManager_OnSprintPerformed;
    }

    protected override void OnFall()
    {
        shouldResetSprintingState = false;

        base.OnFall();
    }

    #endregion

    #region Input Methods

    private void InputManager_OnSprintPerformed()
    {
        keepSprinting = true;

        stateMachine.ReusableData.ShouldSprint = true;
    }

    protected override void InputManager_OnJump()
    {
        shouldResetSprintingState = false;

        base.InputManager_OnJump();
    }

    protected override void InputManager_OnMovementCanceled()
    {
        stateMachine.ChangeState(PlayerState.LightStopping);

    }
    #endregion
}
