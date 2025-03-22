using System;
using UnityEngine;

public class PlayerFallingState : PlayerAirborneState
{
    private PlayerFallData fallData;

    private Vector3 playerPositionOnEnter;

    private float maxFallVelocity;  // Variable to store the maximum velocity during fall

    public PlayerFallingState(PlayerMovementStateMachine playerMovementStateMachine, CameraSwitchHandler cameraSwitchHandler) : base(playerMovementStateMachine, cameraSwitchHandler)
    {
        fallData = airborneData.PlayerFallData;
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();

        // ToDo: Decide whether speed modifiers will be changed in the future to use
        // with fall or jump speed modifier
        if (stateMachine.ReusableData.MovementSpeedModifier <= fallData.SpeedModifier)
        {
            stateMachine.ReusableData.MovementSpeedModifier = fallData.SpeedModifier;
        }

        StartAnimation(stateMachine.PlayerController.AnimationData.FallParameterHash);

        playerPositionOnEnter = stateMachine.PlayerController.transform.position;

        maxFallVelocity = 0f; // Reset the max fall velocity at the start

        ResetVerticalVelocity();
    }

    public override void PhysicsUpdate()
    {
        base.PhysicsUpdate();

        // Continuously track the highest vertical fall speed
        float currentVerticalVelocity = GetPlayerVerticalVelocity().y;

        // We're only interested in the downward velocity (negative values)
        if (currentVerticalVelocity < maxFallVelocity)
        {
            maxFallVelocity = currentVerticalVelocity;
        }

        LimitVerticalVelocity();
    }

    public override void Exit()
    {
        base.Exit();

        StopAnimation(stateMachine.PlayerController.AnimationData.FallParameterHash);

    }

    #endregion

    #region Reusable Methods

    protected override void ResetSprintState()
    {
    }

    protected override void OnContactWithGround(Collider collider)
    {
        float fallDistance = playerPositionOnEnter.y - stateMachine.PlayerController.transform.position.y;

        if (fallDistance < fallData.MinimumDistanceToBeConsideredHardFall)
        {
            Debug.Log("On contact with ground from Falling State with low fall distance");
            stateMachine.ChangeState(PlayerState.LightLanding);

            PlaySound(stateMachine.PlayerController.PlayerFallingSoundData, stateMachine.PlayerController.transform.position);
            
            return;
        }

        //Player player = stateMachine.PlayerController.GetComponent<Player>();

        // Calculate the fall damage based on the max vertical velocity recorded
        float hardFallVelocity = Mathf.Abs(maxFallVelocity) - fallData.MinimumDistanceToBeConsideredHardFall;

        // Apply non-linear damage scaling
        float damage = Mathf.Pow(hardFallVelocity, 1.7f) * fallData.FallDamageModifier;

        if (stateMachine.ReusableData.isGravityManBallSkillUsed)
        {
            damage *= 0.1f;
            Debug.LogError("After decreasing the fall damage" + damage);
        }

        PlaySound(stateMachine.PlayerController.PlayerHardFallingSoundData, stateMachine.PlayerController.transform.position);

        // Apply damage to the player
        //player.TakeDamage(Mathf.CeilToInt(damage));

        Debug.Log("Max vertical velocity: " + maxFallVelocity + " | Damage: " + damage);

        stateMachine.ChangeState(PlayerState.LightLanding);
    }

    #endregion


    #region Main Methods


    private void LimitVerticalVelocity()
    {
        Vector3 playerVerticalVelocity = GetPlayerVerticalVelocity();
        // Apply an additional downward force to speed up falling
        if (playerVerticalVelocity.y <= -fallData.FallSpeedLimit) return;

        // Increase the falling force for acceleration
        Vector3 extraFallForce = new Vector3(0f, -fallData.FallAccelerationForce, 0f);
        playerRigidbody.AddForce(extraFallForce, ForceMode.Acceleration);

        // Limit the fall speed to avoid it becoming too extreme
        if (playerVerticalVelocity.y < -fallData.FallSpeedLimit)
        {
            playerRigidbody.linearVelocity = new Vector3(playerRigidbody.linearVelocity.x, -fallData.FallSpeedLimit, playerRigidbody.linearVelocity.z);
        }
    }
    #endregion
}
