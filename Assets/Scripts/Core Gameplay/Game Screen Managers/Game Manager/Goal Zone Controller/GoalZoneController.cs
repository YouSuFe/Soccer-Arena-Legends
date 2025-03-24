using Unity.Netcode;
using UnityEngine;

public enum Team
{
    Blue = 0,
    Red = 1
}
/// <summary>
/// Handles goal detection via collision. 
/// Ball bounces off if same team, triggers score if opponent.
/// </summary>
public class GoalZoneController : NetworkBehaviour
{
    [Tooltip("This goal belongs to: Blue or Red team")]
    public Team goalTeam;

    [SerializeField] private Transform ballResetPosition;

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer) return;

        if (!collision.gameObject.TryGetComponent(out BallOwnershipManager ballManager)) return;

        PlayerAbstract lastPlayer = ballManager.GetLastTouchedPlayer();
        if (lastPlayer == null) return;

        var playerTeam = (Team)PlayerSpawnManager.Instance.GetUserData(lastPlayer.OwnerClientId).teamIndex;

        if (playerTeam == goalTeam)
        {
            Debug.Log($"[GoalZone] Ball hit by own team — bounce only.");
        }
        else
        {
            Debug.Log($"[GoalZone] Team {playerTeam} scored on {goalTeam}");

            GameManager.Instance.AddGoal(lastPlayer.OwnerClientId);

            if (ballResetPosition != null)
            {
                ballManager.transform.position = ballResetPosition.position;
                ballManager.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
                ballManager.ResetOwnershipIds();
            }
        }
    }
}
