using System;

public static class TeamUtils
{
    public enum PlayerTeam
    {
        Blue,
        Red,
        Spectator
    }

    public static string TeamToString(PlayerTeam team)
    {
        return team.ToString(); // Converts enum to string
    }

    public static PlayerTeam StringToTeam(string teamString)
    {
        if (Enum.TryParse(teamString, out PlayerTeam team))
        {
            return team;
        }
        return PlayerTeam.Spectator; // Default fallback
    }
}
