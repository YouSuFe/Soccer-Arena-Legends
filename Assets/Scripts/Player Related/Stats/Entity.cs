using Unity.Netcode;
using UnityEngine;

public abstract class Entity : NetworkBehaviour
{
    [SerializeField] BaseStats baseStats;
    public Stats Stats { get; private set; }
    protected PlayerBaseStats playerBaseStats;

    public NetworkVariable<int> Health = new NetworkVariable<int>(100);
    public NetworkVariable<int> Strength = new NetworkVariable<int>(10);
    public NetworkVariable<int> Speed = new NetworkVariable<int>(5);

    protected virtual void Awake()
    {

    }

    public override void OnNetworkSpawn()
    {
        // Initialize Stats for both Owner and Server
        playerBaseStats = new PlayerBaseStats(baseStats);
        Stats = new Stats(new StatsMediator(), playerBaseStats);

        if (Stats == null)
        {
            Debug.LogWarning($"Stats is null on {gameObject.name}" + (IsServer ? "Server" : "Client"));
        }
        else
        {
            Debug.Log($"Stats initialized on  {gameObject.name} " + (IsServer ? "Server" : "Client"));
        }

        if (IsOwner)  // Only the owner initializes their local stats
        {

            Stats.Mediator.OnStatChanged += OnLocalStatChanged;

            Debug.Log($"[Client {OwnerClientId}] Stats Initialized: {Stats}");
        }

        if (IsServer) // Server listens for stat updates but doesn't overwrite client stats
        {
            Debug.Log($"[Server] Player {OwnerClientId} Spawned.");
            Health.Value = Stats.GetCurrentStat(StatType.Health);
            Strength.Value = Stats.GetCurrentStat(StatType.Strength);
            Speed.Value = Stats.GetCurrentStat(StatType.Speed);
            Debug.Log($"[Server {OwnerClientId}] Stats Initialized: {Stats} and Assigned into Network Variables.");
        }
    }

    // This method will be triggered on all clients when a stat is changed
    private void OnLocalStatChanged(StatType statType)
    {
        if (!IsOwner) return;

        Debug.Log($"[Client {OwnerClientId}] Stat {statType} Changed. Syncing with Server...");

        UpdateStatServerRpc(statType, Stats.GetCurrentStat(statType));
    }

    // Clients tell the server when a stat changes
    [ServerRpc]
    private void UpdateStatServerRpc(StatType statType, int newValue)
    {
        if (!IsServer) return;

        switch (statType)
        {
            case StatType.Health:
                Health.Value = newValue;
                break;
            case StatType.Strength:
                Strength.Value = newValue;
                break;
            case StatType.Speed:
                Speed.Value = newValue;
                break;
        }

        Debug.Log($"[Server] Updated {statType} to {newValue} for Player {OwnerClientId}");
    }

    public virtual void Update()
    {
        if(IsOwner)
        {
            Stats.Mediator.Update(Time.deltaTime);
        }
    }
}