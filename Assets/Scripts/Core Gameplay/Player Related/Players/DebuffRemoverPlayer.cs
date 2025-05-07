using Unity.Netcode;
using UnityEngine;

public class DebuffRemoverPlayer : PlayerAbstract
{
    #region Fields

    [Header("Sound & Visuals")]
    [SerializeField] private SoundData debuffRemoverPlayerBallSkillSoundData;
    [SerializeField] private GameObject debuffRemoveVFX;

    private GameObject currentEffectInstance;

    #endregion

    #region MonoBehaviour Methods

    #endregion

    #region Skills

    protected override bool PerformBallSkill(Vector3 rayOrigin, Vector3 direction)
    {
        if (BallAttachmentStatus != BallAttachmentStatus.Attached)
            return false;

        ExecuteDebuffRemoval();

        ExecuteDebuffRemovalClientRpc(RpcUtils.ToClient(OwnerClientId));
        PerformBallSkillEffectsClientRpc(OwnerClientId, playerSkillCooldownTime);

        return true;
    }

    [ClientRpc]
    private void ExecuteDebuffRemovalClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (IsServer) return;

        ExecuteDebuffRemoval();
    }

    private void ExecuteDebuffRemoval()
    {
        Stats.Mediator.RemoveModifiersBySourceTag(ModifierSourceTag.Debuff);
        Debug.Log("[DebuffRemoverPlayer] Debuffs removed.");

    }

    #endregion

    #region Attacks

    protected override void PerformHeavyAttack()
    {
        Debug.Log("Heavy Attack From DebuffRemoverPlayer");
    }

    protected override void PerformRegularAttack()
    {
        Debug.Log("Regular Attack From DebuffRemoverPlayer");
    }

    #endregion

    #region Main Methods

    protected override void PlaySkillEffects()
    {
        if (debuffRemoveVFX != null)
        {
            if (currentEffectInstance != null)
                Destroy(currentEffectInstance);

            currentEffectInstance = Instantiate(debuffRemoveVFX, transform.position, Quaternion.identity);
            currentEffectInstance.transform.SetParent(transform);
            Destroy(currentEffectInstance, 2f);
        }

        PlaySoundWithParent(debuffRemoverPlayerBallSkillSoundData, transform.position, transform);
    }

    #endregion
}
