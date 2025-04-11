using Unity.Netcode;
using UnityEngine;

public class EagleShotPlayer : PlayerAbstract
{

    [Header("Ball Skill Sound Data")]
    [SerializeField] SoundData eagleShotPlayerBallSkillSoundData;

    public GameObject activeParticleVFX;
    private GameObject currentParticleInstance; // New field to store instantiated particle reference

    protected override bool PerformBallSkill(Vector3 rayOrigin, Vector3 direction)
    {
        if (BallAttachmentStatus != BallAttachmentStatus.WhenShot)
        {
            Debug.LogError("[BallSkill] Attachment is not whenshot" + BallAttachmentStatus);
            return false;
        }

        if (activeBall == null || activeBall.transform == null || activeBall.gameObject == null)
        {
            Debug.LogError("[BallSkill] Active ball or its components are null.");
            return false;
        }

        // Do raycast to validate hit (optional, can be skipped if trust client input)
        RaycastHit hit;
        Ray ray = new Ray(rayOrigin, direction);
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.TryGetComponent<GoalZoneController>(out var goalZone))
            {
                Team playerTeam = (Team)PlayerSpawnManager.Instance.GetUserData(OwnerClientId).teamIndex;

                if (goalZone.goalTeam == playerTeam)
                {
                    Debug.Log($"[Skill] Cannot use skill toward own goal — reset cooldown.");

                    return false;
                }
            }


            Debug.Log($"[Server] There is a valid target to activate skill on Player {name} on Client {OwnerClientId}");
            // Apply multiplied speed to the ball's Rigidbody
            Rigidbody ballRigidbody = activeBall.GetComponent<Rigidbody>();

            // Calculate the current speed of the ball
            float currentSpeed = ballRigidbody.linearVelocity.magnitude;
            Debug.Log("[Server] Current ball speed: " + currentSpeed);

            // Multiply the current speed by the speedMultiplier
            float newSpeed = currentSpeed * ballSpeedMultiplier;
            Debug.Log("[Server] New ball speed after applying multiplier: " + newSpeed);

            // Apply the new velocity with the same direction as before
            ballRigidbody.linearVelocity = direction.normalized * newSpeed;

            ballOwnershipManager.RegisterSkillInfluence(OwnerClientId);

            // ✅ Notify all clients, passing the player's ClientId who activated the skill
            PerformBallSkillEffectsClientRpc(OwnerClientId, playerSkillCooldownTime);

            activeBall = null;

            NotifyPlayerActiveBallBecameNullClientRpc(RpcUtils.SendRpcToOwner(this));

            return true;
        }
        else
        {
            Debug.Log($"[Server] No valid target to activate skill on Player {name} on Client {OwnerClientId}");
            return false;
        }
    }

    [ClientRpc]
    private void NotifyPlayerActiveBallBecameNullClientRpc(ClientRpcParams clientRpcParams = default)
    {
        // ✅ Only the player who activated the skill updates their UI
        Debug.Log($"[Client] Active ball became null on Player {name} on Client {OwnerClientId}");
        activeBall = null;

    }

    protected override void PerformHeavyAttack()
    {
        Debug.Log("Heavy Attack From " + name);
    }

    protected override void PerformRegularAttack()
    {
        Debug.Log("Regular Attack From " + name);
    }

    protected override void PlaySkillEffects()
    {
        Vector3 skillDirection = TargetingSystem.GetShotDirection(CameraLookAnchor, activeBall.transform.position, activeBall.gameObject.layer);

        if (currentParticleInstance != null)
        {
            Destroy(currentParticleInstance);
        }

        // Instantiate particle effect at ball's position with rotation aligned to direction
        if (activeParticleVFX != null)
        {
            // Get ball's position and direction
            Vector3 ballPosition = activeBall.transform.position;
            Quaternion particleRotation = Quaternion.LookRotation(skillDirection);

            // Instantiate the particle effect at the ball's position with the correct rotation
            currentParticleInstance = Instantiate(activeParticleVFX, ballPosition, particleRotation);

            // Parent the particle effect to the ball to create a trailing effect
            currentParticleInstance.transform.SetParent(activeBall.transform);


            // Todo: Destroy the particle effect when ball goals or taken by another player
            Destroy(currentParticleInstance, 2f);
        }

        //ToDo: Activate it when add sound into game manager
        //PlaySoundWithParent(eagleShotPlayerBallSkillSoundData, activeBall.transform.position, activeBall.transform);
    }

}
