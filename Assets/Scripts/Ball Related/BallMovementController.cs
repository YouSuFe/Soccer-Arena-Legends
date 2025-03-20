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
        transform.position = ownerPlayer.BallHolderPosition.position;

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
        if (!IsServer) return; // 🔹 Only the server should apply damage
        Debug.Log("Something is collided with ball." + collision.gameObject.name);

        if (BallOwnershipManager.GetCurrentBallState() == BallState.PickedUp) return;

        if (Time.time - lastDamageTime < damageCooldown) return;

        IDamageable damageable = collision.collider.GetComponent<IDamageable>();

        if (damageable != null)
        {
            float speed = ballRigidbody.linearVelocity.magnitude;
            if (speed > ballSO.BallData.MaxPickupSpeed)
            {
                int damage = ballSO.BallData.CalculateTotalBallDamage(speed);
                damageable.TakeDamage(damage);

                Debug.Log($"Server: Ball hit {collision.collider.name} with speed {speed} and applied {damage} damage.");
            }
        }
    }

    #endregion
}
