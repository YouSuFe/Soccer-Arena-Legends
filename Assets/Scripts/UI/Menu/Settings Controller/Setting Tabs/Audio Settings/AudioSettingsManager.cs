using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class AudioSettingsManager : MonoBehaviour, ISettingsTab
{
    [Header("Mixer")]
    public AudioMixer audioMixer;

    [Header("UI Sliders")]
    public Slider masterSlider;
    public Slider sfxSlider;
    public Slider uiSlider;
    public Slider envSlider;

    private AudioSettingsData initial;
    private AudioSettingsData temp;

    private void Start()
    {
        LoadInitialSettings();
        ApplyToUI(temp);
        BindEvents();
    }

    private void LoadInitialSettings()
    {
        initial = new AudioSettingsData
        {
            masterVolume = GetMixerLinear("MasterVolume"),
            sfxVolume = GetMixerLinear("SFXVolume"),
            uiVolume = GetMixerLinear("UIVolume"),
            envVolume = GetMixerLinear("EnvironmentVolume")
        };

        initial.LoadFromPlayerPrefs();
        temp = initial.Clone();
    }

    private void ApplyToUI(AudioSettingsData data)
    {
        masterSlider.value = data.masterVolume;
        sfxSlider.value = data.sfxVolume;
        uiSlider.value = data.uiVolume;
        envSlider.value = data.envVolume;
    }

    private void BindEvents()
    {
        masterSlider.onValueChanged.AddListener(v =>
        {
            temp.masterVolume = v;
            SetMixerVolume("MasterVolume", v);
            NotifySettingsChanged();
        });

        sfxSlider.onValueChanged.AddListener(v =>
        {
            temp.sfxVolume = v;
            SetMixerVolume("SFXVolume", v);
            NotifySettingsChanged();

        });

        uiSlider.onValueChanged.AddListener(v =>
        {
            temp.uiVolume = v;
            SetMixerVolume("UIVolume", v);
            NotifySettingsChanged();

        });

        envSlider.onValueChanged.AddListener(v =>
        {
            temp.envVolume = v;
            SetMixerVolume("EnvironmentVolume", v);
            NotifySettingsChanged();

        });
    }

    private void NotifySettingsChanged()
    {
        Debug.Log("[AudioSettingsManager] Changed");
    }

    private float GetMixerLinear(string param)
    {
        if (audioMixer.GetFloat(param, out float db))
        {
            return db.ToLinearVolume(); // uses extension
        }
        return 1f;
    }

    private void SetMixerVolume(string param, float sliderValue)
    {
        float db = sliderValue.ToLogarithmicVolume(); // uses extension
        audioMixer.SetFloat(param, db);
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
            Debug.Log($"[AudioSettingsManager] Changed:");
            Debug.Log($"Master: {temp.masterVolume} != {initial.masterVolume}");
            Debug.Log($"SFX: {temp.sfxVolume} != {initial.sfxVolume}");
            Debug.Log($"UI: {temp.uiVolume} != {initial.uiVolume}");
            Debug.Log($"Env: {temp.envVolume} != {initial.envVolume}");
        }
        return changed;
    }


    public void ApplySettings()
    {
        initial = temp.Clone();
        initial.SaveToPlayerPrefs();
    }

    public void RevertToInitial()
    {
        temp = initial.Clone();
        ApplyToUI(initial);

        SetMixerVolume("MasterVolume", initial.masterVolume);
        SetMixerVolume("SFXVolume", initial.sfxVolume);
        SetMixerVolume("UIVolume", initial.uiVolume);
        SetMixerVolume("EnvironmentVolume", initial.envVolume);
    }

    public void SetInteractable(bool state)
    {
        masterSlider.interactable = state;
        sfxSlider.interactable = state;
        uiSlider.interactable = state;
        envSlider.interactable = state;
    }
}
