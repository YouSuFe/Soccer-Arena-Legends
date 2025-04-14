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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        BallOwnershipManager = GetComponent<BallOwnershipManager>();
        ballReference = GetComponent<BallReference>();
        ballSO = ballReference.BallSO;
        ballRigidbody = ballReference.BallRigidbody;

        MultiplayerGameStateManager.Instance.NetworkGameState.OnValueChanged += HandleGameStateChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        MultiplayerGameStateManager.Instance.NetworkGameState.OnValueChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState previous, GameState newState)
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
            Vector3 worldPos = ownerPlayer.BallHolderPosition.position;
            Vector3 localPos = ownerPlayer.transform.InverseTransformPoint(worldPos);

            transform.localPosition = localPos;
        }
        else
        {
            Debug.LogWarning($"[BallMovementController] Player Active Ball is null");
            ownerPlayer = null;
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
        // ✅ All ball physics & damage should happen server-side
        if (!IsServer) return;

        // ✅ Bail early if this collision shouldn't be processed
        if (!CanProcessCollision(collision)) return;

        // ✅ Check for a valid target (something we can damage)
        IDamageable damageable = collision.collider.GetComponent<IDamageable>();
        if (damageable == null) return;

        float speed = ballRigidbody.linearVelocity.magnitude;
        if (speed < ballSO.BallData.MaxPickupSpeed) return;

        ulong targetClientId = GetClientIdFromDamageable(damageable);
        if (targetClientId == ulong.MaxValue) return;

        // ❌ Skip if we're hitting a teammate (either from skill or regular throw)
        if (IsFriendlyFire(targetClientId)) return;

        // ✅ Try to get the actual player script from the hit object
        if (collision.collider.TryGetComponent<PlayerAbstract>(out var defender))
        {
            // 🟦 SAVE CHECK GOES HERE
            if (BallOwnershipManager != null && BallOwnershipManager.IsDangerousShot())
            {
                ulong defenderId = defender.OwnerClientId;
                GameManager.Instance.AddSave(defenderId);
                BallOwnershipManager.SetDangerousShot(false);
                Debug.Log($"[Ball] SAVE registered for player {defenderId}");
            }
        }

        // ✅ Do the damage & feedback
        ApplyBallDamage(damageable, targetClientId, speed);

        // ✅ Knock back the target (scaled if friendly)
        ApplyKnockback(collision, targetClientId, speed);

        // 🕒 Reset damage cooldown
        lastDamageTime = Time.time;
    }


    private bool CanProcessCollision(Collision collision)
    {
        if (BallOwnershipManager.GetCurrentBallState() == BallState.PickedUp)
            return false;

        if (Time.time - lastDamageTime < damageCooldown)
            return false;

        return true;
    }

    private bool IsFriendlyFire(ulong targetClientId)
    {
        ulong skillInfluencerId = BallOwnershipManager.GetLastSkillInfluencerId();
        ulong lastTouchedId = BallOwnershipManager.GetCurrentBallOwnerId();

        if (skillInfluencerId != ulong.MaxValue &&
            !TeamUtils.AreOpponents(skillInfluencerId, targetClientId))
        {
            Debug.Log("[Ball] No damage — same team as skill influencer.");
            return true;
        }

        if (lastTouchedId != ulong.MaxValue &&
            !TeamUtils.AreOpponents(lastTouchedId, targetClientId))
        {
            Debug.Log("[Ball] No damage — same team as last toucher.");
            return true;
        }

        if (targetClientId == skillInfluencerId || targetClientId == lastTouchedId)
        {
            Debug.Log("[Ball] No damage — self-hit.");
            return true;
        }

        return false;
    }

    private void ApplyBallDamage(IDamageable damageable, ulong targetClientId, float speed)
    {
        int damage = ballSO.BallData.CalculateTotalBallDamage(speed);
        ulong attackerId = BallOwnershipManager.GetLastSkillInfluencerId();
        if (attackerId == ulong.MaxValue)
            attackerId = BallOwnershipManager.GetLastTouchedPlayerId();

        damageable.TakeDamage(damage, DeathType.Ball, attackerId);

        Debug.Log($"[Ball][Server] Hit {damageable} at speed {speed}, dealt {damage} damage");
    }

    private void ApplyKnockback(Collision collision, ulong targetClientId, float speed)
    {
        Rigidbody targetRb = collision.rigidbody;
        if (targetRb == null || targetRb.isKinematic) return;

        Vector3 direction = (collision.collider.transform.position - transform.position).normalized;

        float forceMultiplier = 1f;
        ulong skillInfluencerId = BallOwnershipManager.GetLastSkillInfluencerId();
        ulong lastTouchedId = BallOwnershipManager.GetLastTouchedPlayerId();

        if ((skillInfluencerId != ulong.MaxValue &&
             !TeamUtils.AreOpponents(skillInfluencerId, targetClientId)) ||
            (lastTouchedId != ulong.MaxValue &&
             !TeamUtils.AreOpponents(lastTouchedId, targetClientId)))
        {
            forceMultiplier = 0.3f; // 🧯 Reduce knockback to teammates
        }

        float knockbackForce = speed * ballSO.BallData.KnockbackForceMultiplier * forceMultiplier;
        targetRb.AddForce(direction * knockbackForce, ForceMode.Impulse);

        Debug.Log($"[Ball][Server] Hit {targetClientId} at speed {speed}, knockback {knockbackForce} force");


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
