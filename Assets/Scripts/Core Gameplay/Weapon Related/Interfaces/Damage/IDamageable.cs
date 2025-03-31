public interface IDamageable
{
    void TakeDamage(int amount, DeathType deathType, ulong clientId = ulong.MaxValue);
}
