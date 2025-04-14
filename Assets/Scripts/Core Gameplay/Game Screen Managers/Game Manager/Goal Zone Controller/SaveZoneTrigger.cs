using Unity.Netcode;
using UnityEngine;

public class SaveZoneTrigger : MonoBehaviour
{
    [Tooltip("Which team is defending this goal (used to ignore friendly shots)")]
    public Team defendingTeam;

    [Tooltip("Reference to the GoalZoneController of this goal")]
    [SerializeField] private GoalZoneController goalZoneController;

    [Tooltip("Minimum ball speed (m/s) to consider a shot dangerous")]
    [SerializeField] private float minDangerSpeed = 8f;

    [Tooltip("Minimum dot product alignment (0.65 ≈ 49° cone)")]
    [SerializeField] private float alignmentThreshold = 0.65f;

    private Collider goalCollider;

    // Cooldown fields for OnTriggerStay optimization
    private float lastDangerCheckTime = 0f;
    private float dangerCheckCooldown = 0.15f;

    private void Awake()
    {
        if (goalZoneController != null)
        {
            goalCollider = goalZoneController.GetComponent<Collider>();
        }

        if (goalCollider == null)
        {
            Debug.LogError("[SaveZone] GoalZoneController must have a collider!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryDetectDangerousShot(other, "Enter");
    }

    private void OnTriggerStay(Collider other)
    {
        if (Time.time - lastDangerCheckTime < dangerCheckCooldown) return;
        lastDangerCheckTime = Time.time;

        TryDetectDangerousShot(other, "Stay");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        if (other.TryGetComponent(out BallOwnershipManager ballManager))
        {
            ballManager.SetDangerousShot(false);
            Debug.Log("[SaveZone] Ball exited zone — dangerous shot cleared.");
        }
    }

    private void TryDetectDangerousShot(Collider other, string eventName)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        if (!other.TryGetComponent(out BallOwnershipManager ballManager)) return;
        if (ballManager.IsDangerousShot()) return;
        if (goalCollider == null) return;

        ulong attackerId = GetShooterId(ballManager);
        if (attackerId == ulong.MaxValue) return;

        Team attackerTeam = (Team)PlayerSpawnManager.Instance.GetUserData(attackerId).teamIndex;
        if (attackerTeam == defendingTeam) return; // Friendly shot

        Rigidbody ballRb = ballManager.GetComponent<Rigidbody>();
        float speed = ballRb.linearVelocity.magnitude;
        if (speed < minDangerSpeed) return;

        Vector3 direction = ballRb.linearVelocity.normalized;
        Vector3 toGoal = (goalCollider.bounds.center - ballManager.transform.position).normalized;
        float alignment = Vector3.Dot(direction, toGoal);
        float angle = Mathf.Acos(Mathf.Clamp(alignment, -1f, 1f)) * Mathf.Rad2Deg;

        Ray shotRay = new Ray(ballManager.transform.position, direction);
        bool willHitGoal = goalCollider.Raycast(shotRay, out _, 100f);

        Debug.DrawRay(ballManager.transform.position, direction * 10f, Color.red, 1f);

        Debug.Log($"[SaveZone][{eventName}] Angle: {angle:F1}°, Speed: {speed:F1}, Dot: {alignment:F2}, WillHitGoal: {willHitGoal}");

        if (alignment > alignmentThreshold && willHitGoal)
        {
            ballManager.SetDangerousShot(true);
            Debug.Log($"[SaveZone][{eventName}] DANGEROUS shot detected from player {attackerId}");
        }
    }

    private ulong GetShooterId(BallOwnershipManager manager)
    {
        ulong skillId = manager.GetLastSkillInfluencerId();
        if (skillId != ulong.MaxValue) return skillId;
        return manager.GetLastTouchedPlayerId();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.2f);
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null && box.isTrigger)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(0f, 0.8f, 1f, 0.5f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
