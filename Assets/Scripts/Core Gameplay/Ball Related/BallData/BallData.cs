using System;
using UnityEngine;

[Serializable]
public class BallData 
{
    // Properties with backing fields
    [field: SerializeField] public string BallName { get; private set; } = "Regular Ball";
    [field: SerializeField] public float BallRadius { get; private set; } = 0.5f;   // Default radius
    [field: SerializeField] public float BallMass { get; private set; } = 1f;       // Default mass
    [field: SerializeField] public float BallSpeed { get; private set; } = 10f;     // Default speed
    [field: SerializeField] public float MaxPickupSpeed { get; private set; } = 5f; // Max speed for pickup
    [field: SerializeField] public int BallDamage { get; private set; } = 5;

    public int CalculateTotalBallDamage(float ballSpeed)
    {
        return BallDamage * (int)ballSpeed;
    }
}
