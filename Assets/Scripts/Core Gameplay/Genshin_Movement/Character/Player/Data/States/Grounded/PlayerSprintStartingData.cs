using UnityEngine;
using System;

[Serializable]
public class PlayerSprintStartingData
{
    // Players Sprint Start data which is close to dash logic
    [field: SerializeField] [field: Range(1f, 3f)] public float SpeedModifier { get; private set; } = 2f;
    [field: SerializeField] public PlayerRotationData PlayerRotationData { get; private set; }
    [field: SerializeField] [field: Range(0f, 2f)] public float TimeToBeConsiderConsecutive { get; private set; } = 1f;
    [field: SerializeField] [field: Range(0, 10)] public int ConsecutiveDashesLimitAmount { get; private set; } = 3;
    [field: SerializeField] [field: Range(1f, 3f)] public float DashLimitReachedCooldown { get; private set; } = 1.75f;
}
