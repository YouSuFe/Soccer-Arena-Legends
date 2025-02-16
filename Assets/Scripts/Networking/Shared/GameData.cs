using System;

public enum Map
{
    Default
}

public enum GameMode
{
    SKillGameMode,
    CoreGameMode,
    Training
}

public enum GameQueue
{
    SkillQueue,
    CoreQueue
}

[Serializable]
public class UserData
{
    public string userName;
    public string userAuthId;
    public ulong clientId;
    public GameInfo userGamePreferences = new GameInfo();

    public int teamIndex = -1;
    public int characterId = -1;
    public int weaponId = -1;
}

[Serializable]
public class GameInfo
{
    public Map map;
    public GameMode gameMode;
    public GameQueue gameQueue;

    public string ToMultiplayQueue()
    {
        return gameQueue switch
        {
            GameQueue.SkillQueue => "skill-game-mode",
            GameQueue.CoreQueue => "core-game-mode",
            _ => "skill-game-mode"
        };

    }
}
