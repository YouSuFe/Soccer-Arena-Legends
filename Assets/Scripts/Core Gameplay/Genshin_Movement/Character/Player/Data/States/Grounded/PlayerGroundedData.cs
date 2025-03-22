using UnityEngine;
using System;

[Serializable]
public class PlayerGroundedData
{
    [field: SerializeField] [field: Range(0f,5f)] public float GroundToFallRayDistance { get; private set; } = 1f;

    [field: SerializeField] public AnimationCurve SlopeSpeedAngle { get; private set; }

    [field: SerializeField] public PlayerRotationData PlayerRotationData { get; private set; }

    [field: SerializeField] public PlayerWalkData WalkData { get; private set; }
    [field: SerializeField] public PlayerRunData RunData { get; private set; }
    [field: SerializeField] public PlayerSprintData PlayerSprintData { get; private set; }
    [field: SerializeField] public PlayerSprintStartingData PlayerSprintStartingData { get; private set; }
    [field: SerializeField] public PlayerStopData PlayerStopData { get; private set; }

}
