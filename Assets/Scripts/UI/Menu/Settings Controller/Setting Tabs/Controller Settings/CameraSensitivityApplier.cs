using UnityEngine;
using Unity.Cinemachine;

public class CameraSensitivityApplier : MonoBehaviour
{
    [Header("Only assign FPS Camera controller")]
    public CinemachineInputAxisController fpsCameraController;

    [Tooltip("Apply separate Y-axis sensitivity if needed")]
    public bool useSeparateYGain = false;

    [Tooltip("Invert vertical Y-axis (mouse up = look up)")]
    public bool invertY;

    [Tooltip("Multiplier for Y gain when useSeparateYGain is true")]
    public float verticalGainMultiplier = 1f;

    private float currentSensitivity = 40f;

    public void ApplySensitivity(float sensitivity)
    {
        currentSensitivity = sensitivity;

        if (fpsCameraController == null || fpsCameraController.Controllers == null)
            return;

        foreach (var controller in fpsCameraController.Controllers)
        {
            if (controller == null || controller.Input == null)
                continue;

            if (controller.Name == "Look X (Pan)")
            {
                controller.Input.Gain = sensitivity;
            }
            else if (controller.Name == "Look Y (Tilt)")
            {
                float yGain = useSeparateYGain
                    ? sensitivity * verticalGainMultiplier
                    : sensitivity;

                controller.Input.Gain = invertY ? -yGain : yGain;
            }
        }
    }

    public float GetCurrentSensitivity() => currentSensitivity;

    public void SetInvertY(bool isInverted)
    {
        invertY = isInverted;

        // Re-apply current sensitivity to update Y gain direction
        ApplySensitivity(currentSensitivity);
    }
}
