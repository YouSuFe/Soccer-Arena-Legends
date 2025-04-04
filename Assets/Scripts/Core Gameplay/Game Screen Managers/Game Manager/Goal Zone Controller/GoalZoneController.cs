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

        PlayerAbstract skillPlayer = GetPlayerById(ballManager.GetLastSkillInfluencerId());
        PlayerAbstract lastTouchPlayer = ballManager.GetLastTouchedPlayer();

        bool goalScored = false;
        ulong scoringPlayerId = ulong.MaxValue;

        if (skillPlayer != null)
        {
            Team skillTeam = (Team)PlayerSpawnManager.Instance.GetUserData(skillPlayer.OwnerClientId).teamIndex;
            if (skillTeam == goalTeam)
            {
                Debug.Log($"[GoalZone] Accidental skill goal — own goal by {skillPlayer.OwnerClientId}");
                GameManager.Instance.AddTeamScoreWithoutCredit(GetOpposingTeam(goalTeam));
            }
            else
            {
                Debug.Log($"[GoalZone] Skill goal — {skillPlayer.OwnerClientId} scored on {goalTeam}");
                scoringPlayerId = skillPlayer.OwnerClientId;
                goalScored = true;
            }
        }
        else if (lastTouchPlayer != null)
        {
            Team playerTeam = (Team)PlayerSpawnManager.Instance.GetUserData(lastTouchPlayer.OwnerClientId).teamIndex;
            if (playerTeam == goalTeam)
            {
                Debug.Log($"[GoalZone] Player hit own goal — no score.");
            }
            else
            {
                Debug.Log($"[GoalZone] Player {lastTouchPlayer.OwnerClientId} scored on {goalTeam}");
                scoringPlayerId = lastTouchPlayer.OwnerClientId;
                goalScored = true;
            }
        }
        else
        {
            Debug.Log($"[GoalZone] Unknown source — accidental goal for opposing team.");
            GameManager.Instance.AddTeamScoreWithoutCredit(GetOpposingTeam(goalTeam));
        }

        if (goalScored)
        {
            GameManager.Instance.AddGoal(scoringPlayerId);
        }

        if (ballResetPosition != null)
        {
            ballManager.transform.position = ballResetPosition.position;
            ballManager.GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
            ballManager.ResetOwnershipIds();
        }
    }


    private Team GetOpposingTeam(Team team)
    {
        return team == Team.Red ? Team.Blue : Team.Red;
    }

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
}

