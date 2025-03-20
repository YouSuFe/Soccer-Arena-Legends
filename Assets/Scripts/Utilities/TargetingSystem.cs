using UnityEngine;

public class TargetingSystem
{
    // The default layer mask is set to 0, meaning no layers will be ignored.
    public Vector3 GetShotDirection(Camera camera, Vector3 projectileHolderPosition, LayerMask ignoreLayers = default)
    {
        // Perform a raycast from the center of the camera to determine where you're aiming
        Ray ray = new Ray(camera.transform.position, camera.transform.forward);
        RaycastHit hit;

        // Default shoot direction is forward in case no hit is detected
        Vector3 shootDirection = camera.transform.forward;

        // Use raycast with a layer mask to ignore specific layers like the ball's layer
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, ~ignoreLayers))
        {
            shootDirection = (hit.point - projectileHolderPosition).normalized;
        }

        return shootDirection;
    }
}
