using System;
using Unity.Netcode;
using UnityEngine;

public class IceManPlayer : PlayerAbstract
{
    #region Fields

    [Header("Sound & Visual")]
    [SerializeField] private SoundData iceManPlayerBallSkillSoundData;
    [SerializeField] private GameObject iceSpeedBuffVFX;

    private GameObject currentParticleInstance;
    #endregion

    #region MonoBehaviour Methods

    public override void OnNetworkDespawn()
    {
        OnBallDetached();
        base.OnNetworkDespawn();
    }
    #endregion

    #region Skills
    protected override bool PerformBallSkill(Vector3 rayOrigin, Vector3 direction)
    {
        if (BallAttachmentStatus != BallAttachmentStatus.Attached) return false;

        IceManBallSkill();

        IceManBallSkillClientRpc(RpcUtils.ToClient(OwnerClientId));

        // Broadcast VFX + SFX to client
        PerformBallSkillEffectsClientRpc(OwnerClientId, playerSkillCooldownTime);

        return true;
    }

    [ClientRpc]
    private void IceManBallSkillClientRpc(ClientRpcParams clientRpcParams = default)
    {
        IceManBallSkill();
    }

    #endregion

    #region Reusable Methods

    protected override void ShootBall()
    {
        if (CanShoot) OnBallDetachedServerRpc();
        base.ShootBall();
    }

    #endregion

    #region Attacks

    protected override void PerformHeavyAttack()
    {
        Debug.Log("Heavy Attack From IceManPlayer");
    }

    protected override void PerformRegularAttack()
    {
        Debug.Log("Regular Attack From IceManPlayer");
    }

    #endregion

    #region Main Methods



    private void IceManBallSkill()
    {
        float speedBoostPercentage = 25f; // 25% boost
        StatModifierFactory statModifierFactory = new StatModifierFactory();

        StatModifier speedModifier = statModifierFactory.Create(
            OperatorType.MuliplyByPercentage,
            StatType.Speed,
            speedBoostPercentage,
            -1, // Permanent, needed manually remove modifier
            ModifierSourceTag.BallSkillUsed
        );

        Stats.Mediator.AddModifier(speedModifier);
        Debug.Log("Speed boost applied while ball is attached.");
    }

    [ServerRpc]
    private void OnBallDetachedServerRpc()
    {
        OnBallDetached();
        Debug.Log("[Server] Speed boost removed after ball detachment.");

        OnBallDetachedClientRpc(RpcUtils.ToClient(OwnerClientId));
    }

    [ClientRpc]
    private void OnBallDetachedClientRpc(ClientRpcParams clientRpcParams = default)
    {
        OnBallDetached();
        Debug.Log("[Client] Speed boost removed after ball detachment.");

    }

    private void OnBallDetached()
    {
        // Remove the specific speed modifier with the BallAttached source tag
        Stats.Mediator.RemoveModifierBySourceTag(ModifierSourceTag.BallSkillUsed);
    }

    protected override void PlaySkillEffects()
    {
        if (currentParticleInstance != null)
        {
            Destroy(currentParticleInstance);
        }

        if (iceSpeedBuffVFX != null)
        {
            currentParticleInstance = Instantiate(iceSpeedBuffVFX, transform.position, Quaternion.identity);
            currentParticleInstance.transform.SetParent(transform);
            Destroy(currentParticleInstance, 2f);
        }

        PlaySoundWithParent(iceManPlayerBallSkillSoundData, transform.position, transform);
    }

    #endregion
}
