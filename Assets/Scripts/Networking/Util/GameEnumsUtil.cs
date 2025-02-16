using System;

public static class GameEnumsUtil
{
    // 🗺️ Map Enum
    public enum Map { StadiumMap, DesertMap, IceMap }

    // 🌍 Region Enum
    public enum Region { US_East, US_West, Europe, Asia }

    // 🎮 Game Mode Enum
    public enum GameMode { SkillGameMode, CoreGameMode, Training }

    // ⚽ Ball Type Enum
    public enum BallType { DefaultBall, FastBall, HeavyBall }

    // 🔵🔴 Team Enum (Previously in TeamUtils)
    public enum PlayerTeam { Blue, Red, Spectator }

    // Convert Enum to String
    public static string EnumToString<T>(T enumValue) where T : Enum
    {
        return enumValue.ToString();
    }

    // Convert String to Enum
    public static T StringToEnum<T>(string value, T defaultValue) where T : struct, Enum
    {
        if (Enum.TryParse(value, out T result))
        {
            return result;
        }
        return defaultValue;
    }
}

