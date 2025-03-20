using UnityEngine;

public interface IPositionBasedDamageable : IDamageable
{
    void TakeDamage(int amount, Vector3 attackerPosition);
}
