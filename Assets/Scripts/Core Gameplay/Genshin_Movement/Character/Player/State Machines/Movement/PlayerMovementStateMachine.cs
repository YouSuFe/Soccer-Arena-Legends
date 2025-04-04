using System.Collections.Generic;
using UnityEngine;

public enum PlayerState
{
    Idle,
    SprintStarting,
    Walking,
    Running,
    Sprinting,
    LightStopping,
    LightLanding,
    Jumping,
    Falling,
    Stunned,
    Frozen,
    SpecialWeaponDashing
}

public class PlayerMovementStateMachine : StateMachine
{
    public PlayerController PlayerController { get; }
    public PlayerStateReusableData ReusableData { get; }

    public bool IsServer { get; }
    public bool IsOwner { get; }
    public bool IsClient { get; }

    public Dictionary<PlayerState, IState> StateMapping { get; private set; }

    public PlayerMovementStateMachine(PlayerController playerController, CameraSwitchHandler cameraSwitchHandler)
    {
        PlayerController = playerController;
        ReusableData = new PlayerStateReusableData();

        IsServer = playerController.IsServer;
        IsOwner = playerController.IsOwner;
        IsClient = playerController.IsClient;

        // **State Dictionary**
        StateMapping = new Dictionary<PlayerState, IState>
        {
            { PlayerState.Idle, new PlayerIdleState(this, cameraSwitchHandler) },
            { PlayerState.SprintStarting, new PlayerSpringStartingState(this, cameraSwitchHandler) },
            { PlayerState.Walking, new PlayerWalkingState(this, cameraSwitchHandler) },
            { PlayerState.Running, new PlayerRunningState(this, cameraSwitchHandler) },
            { PlayerState.Sprinting, new PlayerSprintingState(this, cameraSwitchHandler) },

            { PlayerState.LightStopping, new PlayerLightStoppingState(this, cameraSwitchHandler) },

            { PlayerState.LightLanding, new PlayerLightLandingState(this, cameraSwitchHandler) },

            { PlayerState.Jumping, new PlayerJumpingState(this, cameraSwitchHandler) },
            { PlayerState.Falling, new PlayerFallingState(this, cameraSwitchHandler) },

            { PlayerState.Stunned, new PlayerStunnedState(this, cameraSwitchHandler) },
            { PlayerState.Frozen, new PlayerFrozenState(this, cameraSwitchHandler) },

            { PlayerState.SpecialWeaponDashing, new SpecialWeaponDashingState(this, cameraSwitchHandler) }
        };
    }

    public void ChangeState(PlayerState newState)
    {
        if (StateMapping.TryGetValue(newState, out var state))
        {
            base.ChangeState(state);
        }
    }

    public IState GetState(PlayerState state)
    {
        if (StateMapping.TryGetValue(state, out var foundState))
        {
            return foundState;
        }

        throw new KeyNotFoundException($"State {state} not found in StateMapping.");
    }
}
