using UnityEngine;

public interface ISpecialWeaponSkill
{
    public void ExecuteSkill(Vector3 rayOrigin, Vector3 direction);
    float GetCooldownTime();
}
