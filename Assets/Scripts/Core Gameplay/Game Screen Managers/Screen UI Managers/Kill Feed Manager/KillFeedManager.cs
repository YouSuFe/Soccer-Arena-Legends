using System;
using Unity.Netcode;

public class KillFeedManager : NetworkBehaviour
{
    public static KillFeedManager Instance;

    public event Action<KillFeedEntry> OnKillFeedReceived;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    public void ReportKill(string killerName, string victimName, DeathType type, int killerTeamIndex)
    {
        var entry = new KillFeedEntry
        {
            KillerName = killerName,
            VictimName = victimName,
            DeathType = type,
            KillerTeamIndex = killerTeamIndex
        };

        SendKillFeedClientRpc(entry);
    }

    [ClientRpc]
    private void SendKillFeedClientRpc(KillFeedEntry entry)
    {
        OnKillFeedReceived?.Invoke(entry);
    }
}

