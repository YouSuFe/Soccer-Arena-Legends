using System;
using Unity.Collections;
using Unity.Netcode;

public class PlayerStatSync : NetworkBehaviour
{
    public event Action OnStatsChanged;

    public NetworkVariable<ulong> BoundClientId = new();
    public NetworkVariable<int> TeamIndex = new();
    public NetworkVariable<int> CharacterId = new();

    public NetworkVariable<FixedString32Bytes> PlayerName = new();
    public NetworkVariable<int> Goals = new();
    public NetworkVariable<int> Kills = new();
    public NetworkVariable<int> Deaths = new();
    public NetworkVariable<int> Assists = new();
    public NetworkVariable<int> Saves = new();

    public void Initialize(string playerName, ulong clientId, int teamIndex, int characterId)
    {
        if (!IsServer) return;

        PlayerName.Value = playerName;
        BoundClientId.Value = clientId;
        TeamIndex.Value = teamIndex;
        CharacterId.Value = characterId;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsClient) return;

        ScoreboardManager.Instance?.RegisterClientStat(this);

        Goals.OnValueChanged += OnStatsChange;
        Kills.OnValueChanged += OnStatsChange;
        Deaths.OnValueChanged += OnStatsChange;
        Assists.OnValueChanged += OnStatsChange;
        Saves.OnValueChanged += OnStatsChange;
    }

    private void OnStatsChange(int previousValue, int newValue)
    {
        OnStatsChanged?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        ScoreboardManager.Instance?.UnregisterClientStat(this); 

        Goals.OnValueChanged -= OnStatsChange;
        Kills.OnValueChanged -= OnStatsChange;
        Deaths.OnValueChanged -= OnStatsChange;
        Assists.OnValueChanged -= OnStatsChange;
        Saves.OnValueChanged -= OnStatsChange;
    }
}
