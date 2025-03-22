using System;
using UnityEngine;

[Serializable]
public class PlayerStopData 
{
    [field: SerializeField] [field: Range(0f, 15f)] public float LightDecelerationForce { get; private set; } = 5f;
}
