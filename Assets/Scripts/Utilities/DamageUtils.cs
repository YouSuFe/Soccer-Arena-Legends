using UnityEngine;

public static class DamageUtils
{
    public static int CalculateBackstabMultiplier(Transform targetTransform, Vector3 attackerPosition)
    {
        // Calculate direction to the attacker
        Vector3 directionToAttacker = (attackerPosition - targetTransform.position).normalized;

        // Calculate dot product between target's forward direction and direction to attacker
        float dotProduct = Vector3.Dot(targetTransform.forward, directionToAttacker);

        // Draw a line from the target to the attacker (visualizing the direction)
        Debug.DrawLine(targetTransform.position, attackerPosition, Color.red, 2f); // Draw for 2 seconds
        Debug.DrawRay(targetTransform.position, targetTransform.forward * 2f, Color.green, 2f); // Draw the forward direction of the target for 2 seconds

        // Log the dot product
        Debug.Log($"Dot Product: {dotProduct}");

        // Determine if the attack is a backstab based on the dot product
        if (dotProduct < -0.7f) // Back (cosine of 135 degrees)
        {
            Debug.Log("Backstab detected! Applying multiplier.");
            return 2; // Apply multiplier for backstab
        }

        Debug.Log("No backstab. Regular damage.");
        return 1; // No multiplier for other directions
    }
}


