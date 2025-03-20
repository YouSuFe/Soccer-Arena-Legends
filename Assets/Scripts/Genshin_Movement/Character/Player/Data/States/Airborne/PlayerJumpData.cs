using System;
using UnityEngine;

[Serializable]
public class PlayerJumpData 
{
    [field: SerializeField] public PlayerRotationData PlayerRotationData { get; private set; }

    [field: SerializeField] [field: Range(0f, 5f)] public float JumpToGroundRayDistance { get; private set; } = 2f;

    [field: SerializeField] [field: Range(0f, 10f)] public float DecelerationForce { get; private set; } = 1.5f;

    [field: SerializeField] [field: Range(0f, 2f)] public float SpeedModifier { get; private set; } = 1f;

    [field: SerializeField] public AnimationCurve JumpForceModifierOnSlopeUpwards { get; private set; }
    [field: SerializeField] public AnimationCurve JumpForceModifierOnSlopeDownwards { get; private set; }

    [field: SerializeField] public Vector3 StationaryForce { get; private set; }
    [field: SerializeField] public Vector3 WeakForce { get; private set; }
    [field: SerializeField] public Vector3 MediumForce { get; private set; }
    [field: SerializeField] public Vector3 StrongForce { get; private set; }
    
}
