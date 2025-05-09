using System;
using Unity.Netcode;
using UnityEngine;

public class GravityManPlayer : PlayerAbstract
{
    #region Fields

    [Header("Sound & Visual")]
    [SerializeField] private SoundData gravityManPlayerBallSkillSoundData;
    [SerializeField] private GameObject gravityEffectVFX;

    private GameObject currentParticleInstance;

    #endregion

    #region MonoBehaviour Methods

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        ResetBallSkillUsedState();
    }

    #endregion

    #region Skills

    protected override bool PerformBallSkill(Vector3 rayOrigin, Vector3 direction)
    {
        if (BallAttachmentStatus != BallAttachmentStatus.Attached) return false;

        GravityManBallSkill(); // server-side logic

        GravityManBallSkillClientRpc(RpcUtils.ToClient(OwnerClientId));

        // VFX & SFX only for owner
        PerformBallSkillEffectsClientRpc(OwnerClientId, playerSkillCooldownTime);
        return true;
    }

    protected override void ShootBall()
    {
        base.ShootBall();

        // Todo : Check if the player should use its skill even if he shoots
        ResetBallSkillUsedStateServerRpc();

    }

    [ClientRpc]
    private void GravityManBallSkillClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (IsServer) return;
        GravityManBallSkill();
    }

    #endregion


    #region Main Methods

    private void GravityManBallSkill()
    {
        PlayerController.MovementStateMachine.ReusableData.isGravityManBallSkillUsed = true;

    }

    private void ResetBallSkillUsedState()
    {
        if (PlayerController.MovementStateMachine.ReusableData.isGravityManBallSkillUsed == true)
        {
            PlayerController.MovementStateMachine.ReusableData.isGravityManBallSkillUsed = false;
        }
    }

    #endregion

    #region Reusable Methods

    protected override void BallOwnershipManager_OnBallPickedUp(PlayerAbstract picker)
    {
        if (picker != this)
        {
            ResetBallSkillUsedStateServerRpc();

            activeBall = null;
            Debug.LogWarning($"{this.name} lost ball ownership as {picker.name} picked up the ball.");
        }
        else
        {
            Debug.LogWarning($"{this.name} picked up the ball and is now the owner.");
        }
    }

    [ServerRpc]
    private void ResetBallSkillUsedStateServerRpc()
    {
        ResetBallSkillUsedState();

        ResetBallSkillUsedStateClientRpc(RpcUtils.ToClient(OwnerClientId));
    }

    [ClientRpc]
    private void ResetBallSkillUsedStateClientRpc(ClientRpcParams clientRpcParams = default)
    {
        ResetBallSkillUsedState();
    }

    protected override void PlaySkillEffects()
    {
        if (currentParticleInstance != null)
        {
            Destroy(currentParticleInstance);
            currentParticleInstance = null;
        }

        if (gravityEffectVFX != null)
        {
            currentParticleInstance = Instantiate(gravityEffectVFX, transform.position, Quaternion.identity);
            currentParticleInstance.transform.SetParent(transform);
            Destroy(currentParticleInstance, 2f);
        }

        PlaySoundWithParent(gravityManPlayerBallSkillSoundData, transform.position, transform);
    }

    #endregion
}
