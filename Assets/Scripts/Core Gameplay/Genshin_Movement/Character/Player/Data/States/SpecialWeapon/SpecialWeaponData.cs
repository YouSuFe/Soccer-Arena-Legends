using UnityEngine;
using System;

[Serializable]
public class SpecialWeaponData
{
    [field: SerializeField] public SpecialWeaponFloatingCapsuleData SpecialWeaponFloatingCapsuleData { get; private set; }
    [field: SerializeField] public SpecialWeaponDashData SpecialWeaponDashData { get; private set; }

}
