using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class GraphicsSettingsManager : MonoBehaviour, ISettingsTab
{
    [Header("UI Elements")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown qualityDropdown;
    public Toggle fullscreenToggle;
    public Toggle vsyncToggle;

    private Resolution[] resolutions;
    private GraphicsSettingsData initialSettings;
    private GraphicsSettingsData tempSettings;

    void Start()
    {
        resolutions = Screen.resolutions;
        LoadResolutionOptions();
        LoadQualityOptions();
        LoadInitialSettings();
        ApplySettingsToUI(tempSettings);
        BindUIEvents();
    }

    void LoadResolutionOptions()
    {
        resolutionDropdown.ClearOptions();
        var options = new List<string>();
        int defaultIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string label = $"{resolutions[i].width} x {resolutions[i].height}";
            options.Add(label);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                defaultIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
    }

    void LoadQualityOptions()
    {
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
    }

    void LoadInitialSettings()
    {
        initialSettings = GetCurrentSystemSettings();
        initialSettings.LoadFromPlayerPrefs();
        tempSettings = initialSettings.Clone();
    }

    void ApplySettingsToUI(GraphicsSettingsData s)
    {
        resolutionDropdown.value = s.resolutionIndex;
        qualityDropdown.value = s.qualityLevel;
        fullscreenToggle.isOn = s.fullscreen;
        vsyncToggle.isOn = s.vsync;
    }

    void BindUIEvents()
    {
        resolutionDropdown.onValueChanged.AddListener(val =>
        {
            tempSettings.resolutionIndex = val;
            NotifySettingsChanged();
        });

        qualityDropdown.onValueChanged.AddListener(val =>
        {
            tempSettings.qualityLevel = val;
            NotifySettingsChanged();
        });

        fullscreenToggle.onValueChanged.AddListener(val =>
        {
            tempSettings.fullscreen = val;
            NotifySettingsChanged();
        });

        vsyncToggle.onValueChanged.AddListener(val =>
        {
            tempSettings.vsync = val;
            NotifySettingsChanged();
        });
    }

    private void NotifySettingsChanged()
    {
        // You could animate or preview settings here later
        // But for now it's just a signal hook
        Debug.Log("[Graphic Manager] Changed");

    }

    GraphicsSettingsData GetCurrentSystemSettings()
    {
        return new GraphicsSettingsData
        {
            resolutionIndex = GetCurrentResolutionIndex(),
            qualityLevel = QualitySettings.GetQualityLevel(),
            fullscreen = Screen.fullScreen,
            vsync = QualitySettings.vSyncCount > 0
        };
    }

    int GetCurrentResolutionIndex()
    {
        for (int i = 0; i < resolutions.Length; i++)
        {
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                return i;
            }
        }
        return 0;
    }

    public bool HasUnsavedChanges()
    {
        if (tempSettings == null || initialSettings == null)
        {
            Debug.LogWarning("[AudioSettingsManager] temp or initial is null");
            return false;
        }

        bool changed = !tempSettings.Equals(initialSettings);
        if (changed)
        {
            Debug.Log($"[GraphicsSettingsManager] Changed:");
            Debug.Log($"Resolution: {tempSettings.resolutionIndex} != {initialSettings.resolutionIndex}");
            Debug.Log($"Quality: {tempSettings.qualityLevel} != {initialSettings.qualityLevel}");
            Debug.Log($"Fullscreen: {tempSettings.fullscreen} != {initialSettings.fullscreen}");
            Debug.Log($"VSync: {tempSettings.vsync} != {initialSettings.vsync}");
        }
        return changed;
    }

    public void ApplySettings()
    {
        Resolution res = resolutions[tempSettings.resolutionIndex];
        Screen.SetResolution(res.width, res.height, tempSettings.fullscreen);
        QualitySettings.SetQualityLevel(tempSettings.qualityLevel, true);
        QualitySettings.vSyncCount = tempSettings.vsync ? 1 : 0;

        initialSettings = tempSettings.Clone();
        initialSettings.SaveToPlayerPrefs();
    }

    public void RevertToInitial()
    {
        tempSettings = initialSettings.Clone();
        ApplySettingsToUI(initialSettings);
    }

    public void SetInteractable(bool state)
    {
        resolutionDropdown.interactable = state;
        qualityDropdown.interactable = state;
        fullscreenToggle.interactable = state;
        vsyncToggle.interactable = state;
    }
}
