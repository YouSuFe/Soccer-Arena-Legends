using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Central controller for all pickup zones. Syncs to game state.
/// </summary>
public class PickupSpawnManager : NetworkBehaviour
{
    [Header("Spawner Points")]
    [SerializeField] private Transform[] healthZoneBlue;
    [SerializeField] private Transform[] healthZoneRed;
    [SerializeField] private Transform[] buffZoneBlue;
    [SerializeField] private Transform[] buffZoneRed;
    [SerializeField] private Transform mysteryPoint;

    [Header("Prefabs")]
    [SerializeField] private GameObject[] healthPrefabs;
    [SerializeField] private GameObject[] speedPrefabs;
    [SerializeField] private GameObject[] strengthPrefabs;
    [SerializeField] private GameObject[] mysteryBuffPrefabs;
    [SerializeField] private GameObject[] mysteryDebuffPrefabs;

    [Header("Cooldown Durations")]
    [SerializeField] private float healthCooldown = 10f;
    [SerializeField] private float buffCooldown = 10f;
    [SerializeField] private float mysteryCooldown = 12f;

    private PickupZoneGroup healthA, healthB, buffA, buffB, mystery;
    private int buffIndexA = 0, buffIndexB = 1;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        // Initialize all zone groups
        healthA = new PickupZoneGroup(healthZoneBlue, this, healthCooldown, () => RandomPrefab(healthPrefabs));
        healthB = new PickupZoneGroup(healthZoneRed, this, healthCooldown, () => RandomPrefab(healthPrefabs));

        buffA = new PickupZoneGroup(buffZoneBlue, this, buffCooldown, GetNextBuffA);
        buffB = new PickupZoneGroup(buffZoneRed, this, buffCooldown, GetNextBuffB);

        mystery = new PickupZoneGroup(new[] { mysteryPoint }, this, mysteryCooldown, GetRandomMystery);


        MultiplayerGameStateManager.Instance.OnGameStateChanged += HandleGameStateChange;
    }

    /// <summary>
    /// Syncs zones to the current GameState.
    /// </summary>
    private void HandleGameStateChange(GameState state)
    {
        if (state == GameState.InGame)
        {
            healthA.StartZone();
            healthB.StartZone();
            buffA.StartZone();
            buffB.StartZone();
            mystery.StartZone();
        }
        else
        {
            healthA.StopZone();
            healthB.StopZone();
            buffA.StopZone();
            buffB.StopZone();
            mystery.StopZone();
        }
    }

    private GameObject RandomPrefab(GameObject[] pool)
        => pool[Random.Range(0, pool.Length)];

    private GameObject GetNextBuffA()
    {
        var pool = buffIndexA == 0 ? speedPrefabs : strengthPrefabs;
        buffIndexA = (buffIndexA + 1) % 2;
        return RandomPrefab(pool);
    }

    private GameObject GetNextBuffB()
    {
        var pool = buffIndexB == 0 ? speedPrefabs : strengthPrefabs;
        buffIndexB = (buffIndexB + 1) % 2;
        return RandomPrefab(pool);
    }

    private GameObject GetRandomMystery()
    {
        bool isBuff = Random.value < 0.5f;
        var pool = isBuff ? mysteryBuffPrefabs : mysteryDebuffPrefabs;
        return RandomPrefab(pool);
    }
}
