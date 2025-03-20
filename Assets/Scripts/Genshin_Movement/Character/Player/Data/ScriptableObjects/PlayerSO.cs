
using UnityEngine;

[CreateAssetMenu(fileName = "Player", menuName = "Game Screen Data/Custom/Characters/Player")]
public class PlayerSO : ScriptableObject
{
    [field: SerializeField] public PlayerGroundedData GroundedData { get; private set; }
    [field: SerializeField] public PlayerAirborneData AirborneData { get; private set; }
    [field: SerializeField] public PlayerDebuffData DebuffData { get; private set; }
    [field: SerializeField] public SpecialWeaponData SpecialWeaponData { get; private set; }

}
