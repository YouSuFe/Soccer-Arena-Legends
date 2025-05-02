using UnityEngine;

[CreateAssetMenu(fileName = "FrozenBallData", menuName = "Game Screen Data/External Datas/Frozen Ball Data", order = 1)]
public class FrozenBallDataSO : ScriptableObject
{
    public LayerMask interactableLayerMask;
    public float skillRadius = 5f;
    public LayerMask enemyLayerMask;

    public GameObject iceExplosionVFX;
    public GameObject projectileTrail;

    public SoundData frozenBallExpoSoundData;
}
