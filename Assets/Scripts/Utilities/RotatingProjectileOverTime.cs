using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotatingProjectileOverTime : MonoBehaviour
{
    [SerializeField] private float baseRotationSpeed = 200f; // Base rotation speed in degrees per second
    [SerializeField] private float maxRotationSpeed = 1200f;
    [SerializeField] private float speedMultiplier = 1.01f; // Multiplier to increase speed exponentially

    private float currentRotationSpeed;

    void Start()
    {
        // Initialize the current rotation speed with the base value
        currentRotationSpeed = baseRotationSpeed;
    }

    void Update()
    {
        if(currentRotationSpeed < maxRotationSpeed)
        {
        // Exponentially increase the rotation speed over time
        currentRotationSpeed *= speedMultiplier;
        }

        // Rotate the projectile around its forward axis using the current rotation speed
        transform.Rotate(Vector3.forward, currentRotationSpeed * Time.deltaTime);
    }
}
