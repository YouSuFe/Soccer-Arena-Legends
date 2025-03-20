using UnityEngine;
using System;

[Serializable]
public class SpecialWeaponFloatingCapsuleData
{
    [field: SerializeField] [field: Range(0f, 5f)] public float FloatRayDistance { get; private set; } = 1f;
    [field: SerializeField] [field: Range(0f, 50f)] public float StepReachForce { get; private set; } = 25f;
}
