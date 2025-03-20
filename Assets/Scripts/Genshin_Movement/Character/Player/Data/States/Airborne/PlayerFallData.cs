using System;
using UnityEngine;

[Serializable]
public class PlayerFallData 
{
    [field: Tooltip("Having higher numbers might not read collisions with shallow colliders correctly.")]
    [field: SerializeField] [field: Range(1f, 30f)] public float FallSpeedLimit { get; private set; } = 15f;
    [field: SerializeField] [field: Range(0f, 100f)] public float MinimumDistanceToBeConsideredHardFall { get; private set; } = 3f;
    [field: SerializeField] [field: Range(0f, 2f)] public float SpeedModifier { get; private set; } = 1f;
    [field: SerializeField] [field: Range(0f, 2f)] public float FallDamageModifier { get; set; } = 1.7f;
    [field: SerializeField] [field: Range(0f, 50f)] public float FallAccelerationForce { get; private set; } = 25f;
}
