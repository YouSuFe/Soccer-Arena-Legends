using Unity.Netcode;
using UnityEngine;

public class BallMovementController : NetworkBehaviour
{
    private float damageCooldown = 0.1f; // Cooldown time in seconds
    private float lastDamageTime = -Mathf.Infinity; // Tracks the last time damage was applied

    private BallReference ballReference;
    private BallOwnershipManager BallOwnershipManager;
    private BallSO ballSO;
    private Rigidbody ballRigidbody;

    private PlayerAbstract ownerPlayer; // 🔹 We store the owner here instead of checking every frame

    #region NetworkBehaviour Methods

    private void Start()
    {
        BallOwnershipManager = GetComponent<BallOwnershipManager>();
        ballReference = GetComponent<BallReference>();
        ballSO = ballReference.BallSO;
        ballRigidbody = ballReference.BallRigidbody;

        MultiplayerGameStateManager.Instance.OnGameStateChanged += HandleGameStateChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        MultiplayerGameStateManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState newState)
    {
        if(newState == GameState.PreGame)
        {
            // ToDo: Currently ball is created on field after scoring, that is why I need to remove parent.
            // Delete this logic when teleport ball to the safe place when scoring.
            NetworkObject.TryRemoveParent();
            BallOwnershipManager.ResetCurrentOwnershipId();
            ownerPlayer = null;
            transform.position = new Vector3(0, 1, 0);
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer) return; // 🔹 Only the server should control movement

        if (ownerPlayer != null)
        {
            FollowOwner();
        }
    }

    #endregion

    #region Ball Pickup Control
    /// <summary>
    /// Determines if the ball can be picked up based on speed and cooldown.
    /// </summary>
    public bool CanBePickedUp(float lastInteractionTime, float pickUpCooldown)
    {
        if (!IsServer) return false; // Only the server should decide pickup logic

        bool speedCheck = ballRigidbody.linearVelocity.magnitude <= ballSO.BallData.MaxPickupSpeed;
        bool cooldownCheck = Time.time - lastInteractionTime >= pickUpCooldown;

        return speedCheck && cooldownCheck;
    }

    #endregion

    #region Ball Following & Movement

    /// <summary>
    /// Makes the ball follow the assigned player.
    /// </summary>
    public void FollowPlayer(PlayerAbstract player)
    {
        Debug.Log("Follow Player is called");
        ownerPlayer = player;
    }

    /// <summary>
    /// Smoothly follows the assigned player's ball position.
    /// </summary>
    private void FollowOwner()
    {
        if(ownerPlayer.ActiveBall != null)
        {
            transform.position = ownerPlayer.BallHolderPosition.position;
        }
    }

    /// <summary>
    /// Stops following the player without resetting ownership.
    /// </summary>
    public void StopFollowingPlayer()
    {
        ownerPlayer = null;
    }

    #endregion

    #region Throwing Ball with Physics

    /// <summary>
    /// Applies force to the ball when thrown by a player.
    /// </summary>
    public void ApplyThrowForce(Vector3 direction, float force)
    {
        if (!IsServer) return; // Only the server can apply physics

        Debug.Log($"Server: Applying force {force} in direction {direction}");
        ballRigidbody.AddForce(direction * force, ForceMode.Impulse);
    }


    #endregion


    #region Damage System

    private void OnCollisionEnter(Collision collision)
    {
        // ✅ Server-only logic: Damage + physics must only run on the server for authority
        if (!IsServer) return;

        Debug.Log("Something collided with the ball: " + collision.gameObject.name);

        // ❌ Don't process if the ball is picked up
        if (BallOwnershipManager.GetCurrentBallState() == BallState.PickedUp) return;

        // 🔄 Avoid dealing repeated damage within cooldown window
        if (Time.time - lastDamageTime < damageCooldown) return;

        // ✅ Check if collided object is damageable (i.e., likely a player)
        IDamageable damageable = collision.collider.GetComponent<IDamageable>();
        if (damageable == null) return;

        // Get current ball speed
        float speed = ballRigidbody.linearVelocity.magnitude;

        // 🧯 Prevent accidental collisions while ball is slowly rolling
        if (speed < ballSO.BallData.MaxPickupSpeed) return;

        // 🔍 Get IDs
        ulong targetClientId = GetClientIdFromDamageable(damageable);
        if (targetClientId == ulong.MaxValue) return;

        ulong skillInfluencerId = BallOwnershipManager.GetLastSkillInfluencerId();
        ulong lastTouchedId = BallOwnershipManager.GetCurrentBallOwnerId(); // fallback if skill influencer not available

        // 🛡️ Team protection: Avoid damaging teammates
        if (skillInfluencerId != ulong.MaxValue)
        {
            if (!TeamUtils.AreOpponents(skillInfluencerId, targetClientId))
            {
                Debug.Log("[Ball] No damage — same team as skill influencer.");
                return;
            }
        }
        else if (lastTouchedId != ulong.MaxValue)
        {
            if (!TeamUtils.AreOpponents(lastTouchedId, targetClientId))
            {
                Debug.Log("[Ball] No damage — same team as last toucher.");
                return;
            }
        }

        // 💥 Apply damage
        int damage = ballSO.BallData.CalculateTotalBallDamage(speed);
        damageable.TakeDamage(damage, DeathType.Ball, lastTouchedId);

        Debug.Log($"[Ball][Server] Hit {collision.collider.name} at speed {speed}, damage: {damage}");

        // ✅ Try knockback if rigidbody exists
        Rigidbody targetRb = collision.rigidbody;
        if (targetRb != null && !targetRb.isKinematic)
        {
            // 🎯 Compute direction from ball to player
            Vector3 knockbackDir = (collision.collider.transform.position - transform.position).normalized;

            // 🧪 Reduced knockback if hit was to teammate
            float forceMultiplier = 1f;
            if (skillInfluencerId != ulong.MaxValue && !TeamUtils.AreOpponents(skillInfluencerId, targetClientId))
                forceMultiplier = 0.3f;
            else if (!TeamUtils.AreOpponents(lastTouchedId, targetClientId))
                forceMultiplier = 0.3f;

            // 🧨 Compute final knockback force
            float knockbackForce = speed * ballSO.BallData.KnockbackForceMultiplier * forceMultiplier;

            // 🚀 Apply the impulse
            targetRb.AddForce(knockbackDir * knockbackForce, ForceMode.Impulse);
        }

        lastDamageTime = Time.time; // ✅ Update cooldown
    }



    #endregion

    #region Util

    private ulong GetClientIdFromDamageable(IDamageable damageable)
    {
        if (damageable is NetworkBehaviour net)
            return net.OwnerClientId;

        return ulong.MaxValue; // Not a networked player
    }

    #endregion
}
