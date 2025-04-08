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

        Debug.Log($"[SkillCooldownManager][{OwnerClientId}] Initialized: PlayerCooldown={playerSkillBaseCooldown}, WeaponCooldown={weaponSkillBaseCooldown}");

    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

    }

    private void Update()
    {
        if (!IsServer) return;

        if (playerSkillCooldown.Value > 0f)
        {
            playerSkillCooldown.Value -= Time.deltaTime;
            Debug.Log($"[SkillCooldownManager][Server] Decreasing PlayerSkillCooldown for {OwnerClientId} => {playerSkillCooldown.Value:F2}");
        }

        if (weaponSkillCooldown.Value > 0f)
        {
            weaponSkillCooldown.Value -= Time.deltaTime;
            Debug.Log($"[SkillCooldownManager][Server] Decreasing WeaponSkillCooldown for {OwnerClientId} => {weaponSkillCooldown.Value:F2}");
        }
    }
    public bool TryUsePlayerSkill()
    {
        Debug.Log($"[SkillCooldownManager][{OwnerClientId}] TryUsePlayerSkill called. Current Cooldown: {playerSkillCooldown.Value}");

        if (playerSkillCooldown.Value <= 0f)
        {
            playerSkillCooldown.Value = playerSkillBaseCooldown;
            Debug.Log($"[SkillCooldownManager][{OwnerClientId}] PlayerSkill USED. Cooldown set to {playerSkillBaseCooldown}");
            OnSkillCooldownChanged?.Invoke(SkillType.BallSkill, playerSkillBaseCooldown);
            return true;
        }

        return false;
    }

    public bool TryUseWeaponSkill()
    {
        Debug.Log($"[SkillCooldownManager][{OwnerClientId}] TryUseWeaponSkill called. Current Cooldown: {weaponSkillCooldown.Value}");

        if (weaponSkillCooldown.Value <= 0f)
        {
            weaponSkillCooldown.Value = weaponSkillBaseCooldown;
            Debug.Log($"[SkillCooldownManager][{OwnerClientId}] WeaponSkill USED. Cooldown set to {weaponSkillBaseCooldown}");
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

    public bool CanUseSkill(SkillType type)
    {
        return type == SkillType.BallSkill ? !(playerSkillCooldown.Value > 0) : !(weaponSkillCooldown.Value > 0);
    }

    public void ResetPlayerSkillCooldownServer()
    {
        playerSkillCooldown.Value = 0f;
        OnSkillCooldownChanged?.Invoke(SkillType.BallSkill, 0f); // Inform listeners/UI
    }

    public void ResetWeaponSkillCooldownServer()
    {
        playerSkillCooldown.Value = 0f;
        OnSkillCooldownChanged?.Invoke(SkillType.WeaponSkill, 0f); // Inform listeners/UI
    }
}
