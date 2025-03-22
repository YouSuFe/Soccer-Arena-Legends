using System;
using UnityEngine;

[Serializable]
public class PlayerStunnedData
{
    [field: SerializeField] [field: Range(0f, 3f)] public float SpeedModifier { get; private set; } = 0f;
    [field: SerializeField] [field: Range(0f, 3f)] public float StunDurationTime { get; private set; } = 1.5f;

}
