using UnityEngine;
using System;

[Serializable]
public class SpecialWeaponDashData
{
    [field: SerializeField] [field: Range(0f, 2f)] public float SpeedModifier { get; private set; } = 2f;
    [field: SerializeField] [field: Range(30f, 50f)] public float MinDashSpeed { get; private set; } = 40f;
    [field: SerializeField] [field: Range(0f, 2f)] public float TimeToCompleteDash { get; private set; } = 0.8f;
    [field: SerializeField] [field: Range(0f, 50f)] public float DashAcceleration { get; private set; } = 10f;
    [field: SerializeField]  public LayerMask CollisionLayers { get; private set; }
}
