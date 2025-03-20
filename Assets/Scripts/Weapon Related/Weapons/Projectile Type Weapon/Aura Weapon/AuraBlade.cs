using UnityEngine;

public class AuraBlade : MonoBehaviour, IDestroyable, IDestroyer, IDamageDealer
{
    #region Fields
    public AuraBladeDataSO auraBladeData;

    private AuraWeapon auraWeapon; // Reference to the AuraWeapon

    #endregion

    void Awake()
    {

    }

    void Start()
    {
        PlayAuraBladeSound();
        // Destroy the projectile after 'lifetime' seconds
        Destroy(gameObject, auraBladeData.lifetime);
    }

    public void Initialize(AuraWeapon weapon)
    {
        auraWeapon = weapon;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Try to get the IDamageable component for dealing damage
        IDamageable target = collision.gameObject.GetComponent<IDamageable>();
        if (target != null)
        {
            DealDamage(target); // Deal damage to the target
        }

        // Try to get the IDestroyable component for destruction logic
        IDestroyable destroyable = collision.gameObject.GetComponent<IDestroyable>();

        if (destroyable != null)
        {
            Debug.Log($"Collided with destroyable object: {collision.gameObject.name}");
            TriggerDestroy(destroyable); // Trigger destruction of the other object
            Destroy(gameObject); // Destroy this object (the projectile) as well
        }
        else
        {
            Debug.LogWarning($"No IDestroyable found on {collision.gameObject.name}");
        }
    }

    #region Damage Logic
    public int CalculateDamage()
    {
        if (auraWeapon != null)
        {
            return Mathf.RoundToInt(auraWeapon.ReturnCalculatedDamage() * auraBladeData.damageMultiplier);
        }
        return 0;
    }

    public void DealDamage(IDamageable target)
    {
        int damage = CalculateDamage();

        auraWeapon.GetCurrentPlayer().PlayerUIManager.ShowFloatingDamage(Vector3.zero, damage);

        target.TakeDamage(damage);
    }

    #endregion

    #region Destroy Logic Methods
    public void TriggerDestroy(IDestroyable destroyable)
    {
        // Trigger the destruction of the other object
        destroyable.Destroy();
    }

    // ToDo: Add particle and sound effects
    public void Destroy()
    {
        Destroy(gameObject);
    }

    #endregion

    #region Sounds
    private void PlayAuraBladeSound()
    {
        SoundManager.Instance.CreateSoundBuilder()
                                    .WithPosition(transform.position)
                                    .WithParent(transform)
                                    .WithRandomPitch()
                                    .WithLoopDuration(auraBladeData.lifetime)
                                    .Play(auraBladeData.SoundData);
    }
    #endregion
}
