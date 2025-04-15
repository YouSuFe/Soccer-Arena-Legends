using UnityEngine;

[System.Serializable]
public class AudioSettingsData
{
    public float masterVolume;
    public float sfxVolume;
    public float uiVolume;
    public float envVolume;

    public AudioSettingsData Clone()
    {
        return (AudioSettingsData)this.MemberwiseClone();
    }

    public void SaveToPlayerPrefs()
    {
        PlayerPrefs.SetFloat("Audio_Master", masterVolume);
        PlayerPrefs.SetFloat("Audio_SFX", sfxVolume);
        PlayerPrefs.SetFloat("Audio_UI", uiVolume);
        PlayerPrefs.SetFloat("Audio_Env", envVolume);
    }

    public void LoadFromPlayerPrefs()
    {
        masterVolume = PlayerPrefs.GetFloat("Audio_Master", masterVolume);
        sfxVolume = PlayerPrefs.GetFloat("Audio_SFX", sfxVolume);
        uiVolume = PlayerPrefs.GetFloat("Audio_UI", uiVolume);
        envVolume = PlayerPrefs.GetFloat("Audio_Env", envVolume);
    }

    public override bool Equals(object obj)
    {
        if (obj is not AudioSettingsData other) return false;

        return Mathf.Approximately(masterVolume, other.masterVolume) &&
               Mathf.Approximately(sfxVolume, other.sfxVolume) &&
               Mathf.Approximately(uiVolume, other.uiVolume) &&
               Mathf.Approximately(envVolume, other.envVolume);
    }

    public override int GetHashCode()
    {
        return masterVolume.GetHashCode() ^
               sfxVolume.GetHashCode() ^
               uiVolume.GetHashCode() ^
               envVolume.GetHashCode();
    }
}
