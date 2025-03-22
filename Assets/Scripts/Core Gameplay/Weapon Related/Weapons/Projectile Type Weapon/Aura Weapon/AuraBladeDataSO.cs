using UnityEngine;

[CreateAssetMenu(fileName = "AuraBladeData", menuName = "Game Screen Data/External Datas/Aura Blade Data")]
public class AuraBladeDataSO : ScriptableObject
{
    public float damageMultiplier = 10.0f; // Multiplier for damage
    public float lifetime = 3.0f; // Lifetime before destruction
    public SoundData SoundData;

}
