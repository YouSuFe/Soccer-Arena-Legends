using System;
using Unity.Collections;
using Unity.Netcode;

public class PlayerStatSync : NetworkBehaviour
{
    public event Action OnStatsChanged;

    public NetworkVariable<FixedString32Bytes> PlayerName = new();
    public NetworkVariable<int> Goals = new();
    public NetworkVariable<int> Kills = new();
    public NetworkVariable<int> Deaths = new();
    public NetworkVariable<int> Assists = new();
    public NetworkVariable<int> Saves = new();

    public void Initialize(string name)
    {
        if (IsServer)
        {
            PlayerName.Value = name;
        }
    }

    public override void OnNetworkSpawn()
    {
        // Only needed on clients — server doesn't handle UI
        if (!IsClient) return;

        Goals.OnValueChanged += (_, _) => OnStatsChanged?.Invoke();
        Kills.OnValueChanged += (_, _) => OnStatsChanged?.Invoke();
        Deaths.OnValueChanged += (_, _) => OnStatsChanged?.Invoke();
        Assists.OnValueChanged += (_, _) => OnStatsChanged?.Invoke();
        Saves.OnValueChanged += (_, _) => OnStatsChanged?.Invoke();
    }
}
