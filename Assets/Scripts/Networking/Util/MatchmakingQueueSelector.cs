using System;

public static class MatchmakingQueueSelector
{
    public static GameQueue GetQueueFromMode(GameMode gameMode)
    {
        return gameMode switch
        {
            GameMode.SKillGameMode => GameQueue.SkillQueue,
            GameMode.CoreGameMode => GameQueue.CoreQueue,
            _ => throw new ArgumentException($"Unsupported GameMode: {gameMode}")
        };
    }
}