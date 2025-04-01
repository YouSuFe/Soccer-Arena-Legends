using UnityEngine;

public class BallHolderController : MonoBehaviour
{
    [Header("Ball Holder Settings")]
    public Transform ballHolder;
    public float maxDistance = 2f;
    public float minDistance = 0.5f;
    public float wallDetectionDistance = 2.0f;
    public LayerMask wallLayerMask;

    private PlayerAbstract playerComponent;
    private Transform player;
    private Vector3 initialLocalPosition;
    private bool isBallPickedUp = false;

    // Constants
    private const float RayOriginOffset = 0.1f;
    private static readonly Color DebugRayColor = Color.green;

    // Smooth movement variables
    [Header("Smoothing Settings")]
    public float smoothTime = 0.3f;  // Controls smoothness (editable in Inspector)
    public float maxOffset = 0.5f;   // Max allowed movement from original position
    public float velocityInfluence = 0.3f; // How much velocity affects delay


    // Added velocity tracking
    private float targetDistance;
    private Vector3 velocity = Vector3.zero;
    private Vector3 localPositionVelocity = Vector3.zero;
    private Vector3 previousPlayerPosition;
    private Vector3 playerVelocity; // Stores player's velocity

    #region Monobehaviour Methods
    private void Start()
    {
        Initialize();

        if (!playerComponent.IsOwner) return;
        SubscribeToEvents();

        previousPlayerPosition = player.position;
    }

    private void OnDestroy()
    {
        if (!playerComponent.IsOwner) return;

        UnsubscribeFromEvents();
    }

    private void Update()
    {
        if (!playerComponent.IsOwner) return;
        if (isBallPickedUp)
        {
            CalculatePlayerVelocity();

            AdjustBallHolderPosition();
        }
    }
    #endregion

    #region Initialization and Event Handling
    private void Initialize()
    {
        playerComponent = GetComponent<PlayerAbstract>();
        if (playerComponent == null)
        {
            return;
        }

        player = playerComponent.transform;
        initialLocalPosition = ballHolder.localPosition;
        maxDistance = initialLocalPosition.z;
    }

    // ToDo: These are mostly redundant, Delete after test.
    private void SubscribeToEvents()
    {
        if (playerComponent != null)
        {
            playerComponent.OnTakeBall += HandleBallPickedUp;
            playerComponent.OnLoseBall += HandleBallLost;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (playerComponent != null)
        {
            playerComponent.OnTakeBall -= HandleBallPickedUp;
            playerComponent.OnLoseBall -= HandleBallLost;
        }
    }

    private void HandleBallPickedUp()
    {
        if (playerComponent != null)
        {
            isBallPickedUp = true;
        }
    }

    private void HandleBallLost()
    {
        if (playerComponent != null)
        {
            isBallPickedUp = false;
        }
    }
    #endregion

    #region Core Methods
    private void AdjustBallHolderPosition()
    {
        if (playerComponent.ActiveBall == null) return;

        Vector3 rayOrigin = player.position + Vector3.up * RayOriginOffset;
        float ballRadius = playerComponent.ActiveBall.BallSO.BallData.BallRadius;

        // Perform sphere cast to check for walls
        if (Physics.SphereCast(rayOrigin, ballRadius, player.forward, out RaycastHit sphereHit, wallDetectionDistance, wallLayerMask))
        {
            // If a wall is detected, immediately set the holder position
            targetDistance = Mathf.Clamp(sphereHit.distance - ballRadius, minDistance, maxDistance);
        }
        else
        {
            // If no wall is detected, smoothly adjust the position
            targetDistance = maxDistance;
        }

        // Debugging visualization
        Debug.DrawRay(rayOrigin, player.forward * wallDetectionDistance, DebugRayColor);

        SmoothAdjustHolderPosition(targetDistance);
    }

    private void SmoothAdjustHolderPosition(float newHolderDistance)
    {
        Vector3 targetWorldPosition = player.TransformPoint(new Vector3(initialLocalPosition.x, initialLocalPosition.y, newHolderDistance));

        // Scale velocity-based offset
        Vector3 velocityOffset = -playerVelocity * velocityInfluence;

        float forwardMovement = Vector3.Dot(playerVelocity.normalized, player.forward);

        if (forwardMovement > 0.9f) // If moving strictly forward
        {
            velocityOffset = Vector3.zero; // No offset at all when strictly forward
        }
        else
        {
            // Clamp the offset to prevent extreme movement
            velocityOffset.x = Mathf.Clamp(velocityOffset.x, -maxOffset, maxOffset);
            velocityOffset.y = Mathf.Clamp(velocityOffset.y, -maxOffset, maxOffset);
            velocityOffset.z = Mathf.Clamp(velocityOffset.z, -maxOffset, maxOffset);
        }

        // Smoothly move with adjustable smoothTime
        Vector3 smoothedWorldPosition = Vector3.SmoothDamp(
            ballHolder.position,
            targetWorldPosition + velocityOffset,
            ref localPositionVelocity,
            smoothTime
        );

        // Convert back to local position
        ballHolder.localPosition = player.InverseTransformPoint(smoothedWorldPosition);
    }

    // Calculate the player's velocity based on movement
    private void CalculatePlayerVelocity()
    {
        playerVelocity = (player.position - previousPlayerPosition) / Time.deltaTime;
        previousPlayerPosition = player.position;
    }

    #endregion
    //private void OnDrawGizmos()
    //{
    //    Gizmos.color = Color.green;
    //    Vector3 rayOrigin = player.position + Vector3.up * RayOriginOffset;
    //    Gizmos.DrawWireSphere(rayOrigin, 0.6f);
    //}
}
