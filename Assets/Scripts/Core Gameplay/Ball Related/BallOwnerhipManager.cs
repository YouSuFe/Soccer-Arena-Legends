using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Enum representing the possible states of the ball.
/// </summary>
public enum BallState
{
    Idle,       // Ball is free to be picked up.
    PickedUp,   // Ball is held by a player.
    InAir       // Ball has been thrown and is moving.
}

/// <summary>
/// Manages ball ownership, state, and interactions in a multiplayer environment.
/// Ensures proper synchronization between players and prevents unauthorized actions.
/// </summary>
public class BallOwnershipManager : NetworkBehaviour
{
    #region Fields & Variables

    private const ulong NO_OWNER = ulong.MaxValue; // Represents "no player owns the ball"

    private PlayerDetector playerDetector;
    private BallReference ballReference;
    private BallMovementController ballMovementController;

    public BallMovementController BallMovementController => ballMovementController;

    // Tracks the ball state across the network
    private NetworkVariable<BallState> currentBallState = new NetworkVariable<BallState>(BallState.Idle);

    // Tracks the player currently holding the ball
    private NetworkVariable<ulong> currentBallOwner = new NetworkVariable<ulong>(NO_OWNER);

    private ulong lastTouchedPlayerId = NO_OWNER; // Stores the last player to interact with the ball

    private ulong lastSkillInfluencerId = NO_OWNER; // Stores the last player to interact with player's skill on the ball

    private ulong lastAssistTouchPlayerId = NO_OWNER;


    // Events triggered on ball interactions
    public event Action<PlayerAbstract> OnBallPickedUp;
    public event Action<PlayerAbstract> OnBallShot;

    // Time tracking for pickup cooldown
    private float lastInteractionTime;
    private const float pickUpCooldown = 0.3f;

    #endregion

    #region NetworkBehaviour Methods

    private void Start()
    {
        // Get necessary components
        playerDetector = GetComponent<PlayerDetector>();
        ballReference = GetComponent<BallReference>();
        ballMovementController = GetComponent<BallMovementController>();

        if(playerDetector == null || ballReference == null || ballMovementController == null)
        {
            Debug.LogWarning("Something is wrong, all referances not got assigned");
        }
        // Subscribe to player detection event
        playerDetector.OnPlayerDetected += HandlePlayerDetected;
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        // Unsubscribe to prevent memory leaks
        if (playerDetector != null)
        {
            playerDetector.OnPlayerDetected -= HandlePlayerDetected;
        }
    }

    private void Update()
    {
        if (!IsServer) return; // Only the server should manage ball detection

        // Allow ball detection only when it's idle or in air
        if (currentBallState.Value == BallState.Idle || currentBallState.Value == BallState.InAir)
        {
            playerDetector.CheckForPlayer();
        }
    }

    #endregion

    #region Player Detection & Interaction

    /// <summary>
    /// Handles the event when a player is detected near the ball.
    /// Triggers a pickup attempt if conditions are met.
    /// </summary>
    private void HandlePlayerDetected(PlayerAbstract player)
    {
        if (!IsServer) return;

        if (currentBallState.Value == BallState.Idle || currentBallState.Value == BallState.InAir)
        {
            if (ballMovementController.CanBePickedUp(lastInteractionTime, pickUpCooldown))
            {
                PlayerPicksUpBallServerRpc(player.OwnerClientId);
            }
        }
    }

    #endregion

    #region Ball Pick Up & Throwing

    /// <summary>
    /// Called when a player attempts to pick up the ball.
    /// Validates ownership and updates state accordingly.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void PlayerPicksUpBallServerRpc(ulong playerId)
    {
        if (!IsServer) return;

        // Prevent pickup spam by enforcing a cooldown
        if (Time.time - lastInteractionTime < pickUpCooldown)
        {
            Debug.LogWarning($"Server: Pickup ignored - Cooldown active for Player {playerId}");
            return;
        }

        PlayerAbstract playerBallOwner = GetPlayerById(playerId);
        if (playerBallOwner == null) return;

        // Ensure only one player can hold the ball at a time
        if (currentBallOwner.Value != NO_OWNER)
        {
            Debug.LogWarning($"Server: Pickup ignored - Ball already owned by {currentBallOwner.Value}");
            return;
        }
        this.NetworkObject.TrySetParent(playerBallOwner.transform, false);
        // Assign ownership and update state
        playerBallOwner.RegisterBall(ballReference);

        ClearSkillInfluence();

        // ✅ Assist logic
        if (lastTouchedPlayerId != NO_OWNER && lastTouchedPlayerId != playerId)
        {
            var currentTeam = PlayerSpawnManager.Instance.GetUserData(playerId).teamIndex;
            var previousTeam = PlayerSpawnManager.Instance.GetUserData(lastTouchedPlayerId).teamIndex;
            if (currentTeam == previousTeam)
                SetAssistCandidate(lastTouchedPlayerId);
        }

        currentBallOwner.Value = playerId;
        lastTouchedPlayerId = playerId;
        currentBallState.Value = BallState.PickedUp;
        lastInteractionTime = Time.time;

        // Move ball to the player
        ballMovementController.FollowPlayer(playerBallOwner);

        Debug.Log($"Server: Player {playerBallOwner.name} picked up the ball.");
        NotifyBallPickedUpClientRpc(playerId);
    }

    /// <summary>
    /// Notifies all clients that a player has picked up the ball.
    /// </summary>
    [ClientRpc]
    private void NotifyBallPickedUpClientRpc(ulong playerId)
    {
        PlayerAbstract picker = GetPlayerById(playerId);
        if (picker != null)
        {
            picker.RegisterBall(ballReference);
            Debug.Log($"Client: Player {picker.name} picked up the ball.");
            OnBallPickedUp?.Invoke(picker);
        }
    }

    /// <summary>
    /// Called when a player attempts to throw the ball.
    /// Validates ownership and applies force to the ball.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void PlayerShootsBallServerRpc(Vector3 direction, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        ulong clientId = rpcParams.Receive.SenderClientId;

        PlayerAbstract playerShooter = GetPlayerById(clientId);
        if (playerShooter == null || currentBallOwner.Value != clientId)
        {
            Debug.LogWarning($"Server: Shot ignored - Player {clientId} does not own the ball.");
            return;
        }

        this.NetworkObject.TryRemoveParent();

        // 🔹 Update the player's shoot status
        playerShooter.BallAttachmentStatus = BallAttachmentStatus.WhenShot;
        playerShooter.isPlayerHoldingBall = false;

        // ToDo: Every client will be get this event, no need to actually beacuse we only change shooter status.
        NotifyPlayerShotBallClientRpc(clientId);

        // Reset ownership and update state
        ClearSkillInfluence();

        currentBallOwner.Value = NO_OWNER;
        lastTouchedPlayerId = clientId;
        currentBallState.Value = BallState.InAir;

        // Apply physics and detach the ball from the player
        ballMovementController.StopFollowingPlayer();

        // Calculate throw force which will be applied on ball for player
        float force = playerShooter.CalculateThrowForce();
        // 🔹 Apply force (only on the server)
        ballMovementController.ApplyThrowForce(direction, force);

        Debug.Log($"Server: Player {playerShooter.name} shot the ball.");
    }

    /// <summary>
    /// Notifies all clients that a player has thrown the ball.
    /// </summary>
    [ClientRpc]
    private void NotifyPlayerShotBallClientRpc(ulong clientId)
    {
        PlayerAbstract shooter = GetPlayerById(clientId);
        if (shooter != null)
        {
            Debug.Log($"Client: Player {shooter.name} threw the ball.");
            OnBallShot?.Invoke(shooter);
        }
    }

    public void ResetOwnershipIds()
    {
        currentBallOwner.Value = NO_OWNER;
        lastTouchedPlayerId = NO_OWNER;
        lastSkillInfluencerId = NO_OWNER;
        lastAssistTouchPlayerId = NO_OWNER;
        currentBallState.Value = BallState.Idle;
    }

    public void ResetCurrentOwnershipId()
    {
        ClearSkillInfluence();
        currentBallOwner.Value = NO_OWNER;
        currentBallState.Value = BallState.Idle;
    }


    public void RegisterSkillInfluence(ulong clientId)
    {
        lastSkillInfluencerId = clientId;
    }

    public void ClearSkillInfluence()
    {
        lastSkillInfluencerId = NO_OWNER;
    }


    public void SetAssistCandidate(ulong clientId)
    {
        if (clientId != NO_OWNER)
            lastAssistTouchPlayerId = clientId;
    }

    public void ClearAssistCandidate()
    {
        lastAssistTouchPlayerId = NO_OWNER;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Retrieves a player instance by their network ID.
    /// </summary>
    public PlayerAbstract GetPlayerById(ulong clientId)
    {
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
        {
            Debug.LogError($"[GetPlayerById] ERROR: Player {clientId} is NOT a connected client.");
            return null;
        }

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client))
        {
            Debug.LogError($"[GetPlayerById] ERROR: Player {clientId} exists in the network but has NO registered client.");
            return null;
        }

        if (client.PlayerObject == null)
        {
            Debug.LogError($"[GetPlayerById] ERROR: Player {clientId} exists but their PlayerObject is NULL.");
            return null;
        }

        PlayerAbstract player = client.PlayerObject.GetComponent<PlayerAbstract>();
        if (player == null)
        {
            Debug.LogError($"[GetPlayerById] ERROR: PlayerObject for {clientId} exists but does NOT have a Player component.");
            return null;
        }

        Debug.Log($"[GetPlayerById] SUCCESS: Found Player {clientId} - {player.name}");
        return player;
    }

    /// <summary>
    /// Returns the ID of the current ball owner.
    /// </summary>
    public ulong GetCurrentBallOwnerId()
    {
        if (currentBallOwner.Value == ulong.MaxValue)
        {
            Debug.LogWarning($"No current ball holder. {currentBallOwner.Value} is a placeholder for the free state.");
        }
        return currentBallOwner.Value;
    }

    public ulong GetLastTouchedPlayerId()
    {
        return lastTouchedPlayerId;
    }

    /// <summary>
    /// Retrieves the last player who touched the ball.
    /// </summary>
    public PlayerAbstract GetLastTouchedPlayer()
    {
        return GetPlayerById(lastTouchedPlayerId);
    }

    public ulong GetLastSkillInfluencerId()
    {
        return lastSkillInfluencerId;
    }

    public ulong GetAssistCandidate()
    {
        return lastAssistTouchPlayerId;
    }

    public BallState GetCurrentBallState()
    {
        return currentBallState.Value;
    }
    #endregion
}

