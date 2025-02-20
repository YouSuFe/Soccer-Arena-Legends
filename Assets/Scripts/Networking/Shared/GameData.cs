using System;

public enum Map
{
    Default
}

public enum BallType
{
    DefaultBall,
    FastBall,
    HeavyBall
}

public enum GameMode
{
    SKillGameMode,
    CoreGameMode,
    LevelUpMode
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
    public BallType ballType;
}
