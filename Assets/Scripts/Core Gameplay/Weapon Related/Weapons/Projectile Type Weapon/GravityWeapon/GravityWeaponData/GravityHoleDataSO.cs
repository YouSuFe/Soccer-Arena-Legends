using UnityEngine;

[CreateAssetMenu(fileName = "GravityHoleData", menuName = "Game Screen Data/External Datas/Hole Data", order = 1)]
public class GravityHoleDataSO : ScriptableObject
{
    public LayerMask pullableObjectsLayer;
    public float maxDistance = 20f;
    public float pullRadius = 15f;
    public float abilityDuration = 10f;
    public float maxPullForce = 50f;  // Maximum pull force
    public float minPullForce = 10f;  // Minimum pull force to balance distant objects

    public GameObject blackHoleVFX;
    public GameObject projectileTrail;

    public SoundData gravityHoleSoundData;
}
