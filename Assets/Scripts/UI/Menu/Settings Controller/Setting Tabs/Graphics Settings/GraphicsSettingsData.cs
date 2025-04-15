using UnityEngine;

[System.Serializable]
public class GraphicsSettingsData
{
    public int resolutionIndex;
    public int qualityLevel;
    public bool fullscreen;
    public bool vsync;

    public GraphicsSettingsData Clone()
    {
        return (GraphicsSettingsData)MemberwiseClone();
    }

    public void SaveToPlayerPrefs()
    {
        PlayerPrefs.SetInt("Graphics_ResolutionIndex", resolutionIndex);
        PlayerPrefs.SetInt("Graphics_QualityLevel", qualityLevel);
        PlayerPrefs.SetInt("Graphics_Fullscreen", fullscreen ? 1 : 0);
        PlayerPrefs.SetInt("Graphics_VSync", vsync ? 1 : 0);
    }

    public void LoadFromPlayerPrefs()
    {
        resolutionIndex = PlayerPrefs.GetInt("Graphics_ResolutionIndex", resolutionIndex);
        qualityLevel = PlayerPrefs.GetInt("Graphics_QualityLevel", qualityLevel);
        fullscreen = PlayerPrefs.GetInt("Graphics_Fullscreen", fullscreen ? 1 : 0) == 1;
        vsync = PlayerPrefs.GetInt("Graphics_VSync", vsync ? 1 : 0) == 1;
    }

    public override bool Equals(object obj)
    {
        if (obj is not GraphicsSettingsData other) return false;
        return resolutionIndex == other.resolutionIndex &&
               qualityLevel == other.qualityLevel &&
               fullscreen == other.fullscreen &&
               vsync == other.vsync;
    }

    public override int GetHashCode()
    {
        return resolutionIndex ^ qualityLevel ^ fullscreen.GetHashCode() ^ vsync.GetHashCode();
    }
}
