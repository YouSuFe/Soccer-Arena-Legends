using System;
using UnityEngine;

[Serializable]
public class PlayerShockedData 
{
    [field: SerializeField] [field: Range(0f, 3f)] public float SpeedModifier { get; private set; } = 0f;
    [field: SerializeField] [field: Range(0f, 3f)] public float ShockedDurationTime { get; private set; } = 1.5f;
}
