using UnityEngine;
using UnityEngine.UI;

public class ControllerSettingsManager : MonoBehaviour, ISettingsTab
{
    [Header("UI Elements")]
    public Slider sensitivitySlider;
    public Toggle invertYToggle;

    [Header("Scene Reference (Optional in non-game scenes)")]
    [SerializeField]
    private CameraSensitivityApplier cameraSensitivityApplier;

    private ControllerSettingsData initial;
    private ControllerSettingsData temp;

    private void Start()
    {
        LoadInitialSettings();
        ApplyToUI(temp);
        BindUIEvents();
        gameObject.SetActive(false);
    }

    private void LoadInitialSettings()
    {
        initial = new ControllerSettingsData
        {
            sensitivity = 1f,
            invertYAxis = true
        };

        initial.LoadFromPlayerPrefs();
        temp = initial.Clone();
    }

    private void ApplyToUI(ControllerSettingsData data)
    {
        sensitivitySlider.value = data.sensitivity;
        invertYToggle.isOn = data.invertYAxis;

        ApplySensitivityLive(data.sensitivity);
        ApplyInvertYLive(data.invertYAxis);
    }

    private void BindUIEvents()
    {
        sensitivitySlider.onValueChanged.AddListener(v =>
        {
            temp.sensitivity = v;
            ApplySensitivityLive(v);
            NotifySettingsChanged();

        });

        invertYToggle.onValueChanged.AddListener(v =>
        {
            temp.invertYAxis = v;
            ApplyInvertYLive(v);
            NotifySettingsChanged();

        });
    }

    private void NotifySettingsChanged()
    {
        // You could animate or preview settings here later
        // But for now it's just a signal hook
        Debug.Log("[ControllerSettingsManager] Changed");

    }

    private void ApplySensitivityLive(float sensitivity)
    {
        if (cameraSensitivityApplier != null)
        {
            cameraSensitivityApplier.ApplySensitivity(sensitivity);
        }
    }

    private void ApplyInvertYLive(bool isInverted)
    {
        if (cameraSensitivityApplier != null)
        {
            cameraSensitivityApplier.SetInvertY(isInverted);
        }
    }

    public bool HasUnsavedChanges()
    {
        if (temp == null || initial == null)
        {
            Debug.LogWarning("[AudioSettingsManager] temp or initial is null");
            return false;
        }

        bool changed = !temp.Equals(initial);
        if (changed)
        {
            Debug.Log($"[ControllerSettingsManager] Changed:");
            Debug.Log($"Sensitivity: {temp.sensitivity} != {initial.sensitivity}");
            Debug.Log($"InvertY: {temp.invertYAxis} != {initial.invertYAxis}");
        }
        return changed;
    }

    public void ApplySettings()
    {
        initial = temp.Clone();
        initial.SaveToPlayerPrefs();

        ApplySensitivityLive(initial.sensitivity);
        ApplyInvertYLive(initial.invertYAxis);
    }

    public void RevertToInitial()
    {
        temp = initial.Clone();
        ApplyToUI(initial);
    }

    public void ResetToDefault()
    {
        temp = ControllerSettingsData.Default();
        ApplyToUI(temp);
    }

    public void SetInteractable(bool state)
    {
        sensitivitySlider.interactable = state;
        invertYToggle.interactable = state;
    }
}
