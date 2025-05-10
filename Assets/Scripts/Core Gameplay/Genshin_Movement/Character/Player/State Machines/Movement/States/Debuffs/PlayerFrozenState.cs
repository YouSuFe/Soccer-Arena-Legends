using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerFrozenState : PlayerDebuffState
{
    PlayerFrozenData frozenData;
    private float frozenDuration;  // Duration of the frozen state
    private float frozenTimer;     // Timer to track elapsed time
    public PlayerFrozenState(PlayerMovementStateMachine playerMovementStateMachine, CameraSwitchHandler cameraSwitcher) : base(playerMovementStateMachine, cameraSwitcher)
    {
        frozenData = debuffData.PlayerFrozenData;
        frozenDuration = frozenData.FrozenDurationTime;
    }

    #region IState Methods

    public override void Enter()
    {
        base.Enter();

        frozenTimer = 0f;

        stateMachine.ReusableData.isFrozen = true;

        StartAnimation(stateMachine.PlayerController.AnimationData.FrozenParameterHash);

        stateMachine.ReusableData.MovementSpeedModifier = frozenData.SpeedModifier;

        ResetVelocity();

        ResetSprintState();

        Vector3 vfxPosition = stateMachine.PlayerController.FrozenVfxAnchor.position;
        float duration = frozenData.FrozenDurationTime;

        // Only the owner calls the server
        if (stateMachine.PlayerController.IsOwner)
        {
            SpawnFrozenVfxServerRpc(vfxPosition, duration);
        }

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
        ManageFrozenStateTimer();
    }


    public override void Exit()
    {
        base.Exit();
        StopAnimation(stateMachine.PlayerController.AnimationData.FrozenParameterHash);
        stateMachine.ReusableData.isFrozen = false;

    }


    #endregion




    #region Main Methods

    [ServerRpc(RequireOwnership = false)]
    private void SpawnFrozenVfxServerRpc(Vector3 position, float duration)
    {
        SpawnFrozenVfxClientRpc(position, duration);
    }

    [ClientRpc]
    private void SpawnFrozenVfxClientRpc(Vector3 position, float duration)
    {
        if (frozenData.FrozenVfx == null) return;

        GameObject vfx = Object.Instantiate(frozenData.FrozenVfx, position, Quaternion.identity);
        Object.Destroy(vfx, duration);
    }

    private void ManageFrozenStateTimer()
    {
        frozenTimer += Time.deltaTime;  // Update the timer

        if (frozenTimer >= frozenDuration)
        {
            // Transition to another state after frozen time is exceeded
            stateMachine.ChangeState(PlayerState.Idle);
        }
    }

    #endregion
}
