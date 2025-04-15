using UnityEngine;

[System.Serializable]
public class ControllerSettingsData
{
    public float sensitivity;
    public bool invertYAxis;

    public ControllerSettingsData Clone()
    {
        return (ControllerSettingsData)this.MemberwiseClone();
    }

    public void SaveToPlayerPrefs()
    {
        PlayerPrefs.SetFloat("Controller_Sensitivity", sensitivity);
        PlayerPrefs.SetInt("Controller_InvertY", invertYAxis ? 1 : 0);
    }

    public void LoadFromPlayerPrefs()
    {
        sensitivity = PlayerPrefs.GetFloat("Controller_Sensitivity", 1f);
        invertYAxis = PlayerPrefs.GetInt("Controller_InvertY", 0) == 1;
    }

    public override bool Equals(object obj)
    {
        if (obj is not ControllerSettingsData other) return false;
        return Mathf.Approximately(sensitivity, other.sensitivity) &&
               invertYAxis == other.invertYAxis;
    }

    public override int GetHashCode()
    {
        return sensitivity.GetHashCode() ^ invertYAxis.GetHashCode();
    }

    public static ControllerSettingsData Default()
    {
        return new ControllerSettingsData
        {
            sensitivity = 1f,
            invertYAxis = true
        };
    }
}
