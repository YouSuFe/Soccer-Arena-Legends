using Unity.Netcode;
using UnityEngine;
using System;

public class SkillCooldownManager : NetworkBehaviour
{
    public event Action<SkillType, float> OnSkillCooldownChanged;

    private PlayerAbstract player;
    private BaseWeapon weapon;

    private float playerSkillBaseCooldown;
    private float weaponSkillBaseCooldown;

    private readonly NetworkVariable<float> playerSkillCooldown = new(0f);
    private readonly NetworkVariable<float> weaponSkillCooldown = new(0f);

    public void Initialize(PlayerAbstract playerRef, BaseWeapon weaponRef)
    {
        this.player = playerRef;
        this.weapon = weaponRef;

        playerSkillBaseCooldown = player.GetBallSkillCooldownTime();
        weaponSkillBaseCooldown = weapon.GetCooldownTime();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        playerSkillCooldown.Value = 0f;
        weaponSkillCooldown.Value = 0f;
    }

    private void Update()
    {
        if (!IsServer) return;

        if (playerSkillCooldown.Value > 0f)
            playerSkillCooldown.Value -= Time.deltaTime;

        if (weaponSkillCooldown.Value > 0f)
            weaponSkillCooldown.Value -= Time.deltaTime;
    }

    public bool TryUsePlayerSkill()
    {
        if (playerSkillCooldown.Value <= 0f)
        {
            playerSkillCooldown.Value = playerSkillBaseCooldown;
            OnSkillCooldownChanged?.Invoke(SkillType.BallSkill, playerSkillBaseCooldown);
            return true;
        }
        return false;
    }

    public bool TryUseWeaponSkill()
    {
        if (weaponSkillCooldown.Value <= 0f)
        {
            weaponSkillCooldown.Value = weaponSkillBaseCooldown;
            OnSkillCooldownChanged?.Invoke(SkillType.WeaponSkill, weaponSkillBaseCooldown);
            return true;
        }
        return false;
    }

    public float GetRemainingCooldown(SkillType type)
    {
        return type == SkillType.BallSkill
            ? playerSkillCooldown.Value
            : weaponSkillCooldown.Value;
    }
}
