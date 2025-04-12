using System.Collections;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Manages a pickup zone with 2 spawn points and shared cooldown logic.
/// Ensures only one cooldown runs at a time, and refills up to 2 pickups.
/// </summary>
public class PickupZoneGroup
{
    /// <summary>
    /// Represents a single pickup spawner point in the zone.
    /// Tracks the transform and current pickup instance.
    /// </summary>
    private class SpawnerSlot
    {
        public Transform Point;
        public GameObject Pickup;
        public bool IsOccupied => Pickup != null;
    }

    private readonly SpawnerSlot[] slots;
    private readonly MonoBehaviour host;
    private readonly float cooldownTime;
    private readonly System.Func<GameObject> getPrefab;

    private Coroutine cooldownCoroutine;
    private bool isActive;

    /// <summary>
    /// Constructs a pickup zone with spawn points, logic handler, cooldown, and prefab selection logic.
    /// </summary>
    public PickupZoneGroup(Transform[] spawnPoints, MonoBehaviour host, float cooldownTime, System.Func<GameObject> prefabSelector)
    {
        this.host = host;
        this.cooldownTime = cooldownTime;
        this.getPrefab = prefabSelector;
        this.slots = spawnPoints.Select(p => new SpawnerSlot { Point = p }).ToArray();
    }

    /// <summary>
    /// Starts the zone spawning logic (typically when game enters InGame state).
    /// </summary>
    public void StartZone()
    {
        isActive = true;
        TryStartCooldown(); // Start first pickup spawn
    }

    /// <summary>
    /// Stops all zone activity, cancels cooldowns, and despawns any existing pickups.
    /// </summary>
    public void StopZone()
    {
        isActive = false;

        if (cooldownCoroutine != null)
            host.StopCoroutine(cooldownCoroutine);

        cooldownCoroutine = null;

        foreach (var slot in slots)
        {
            if (slot.Pickup != null)
            {
                var netObj = slot.Pickup.GetComponent<NetworkObject>();
                if (netObj != null && netObj.IsSpawned)
                    netObj.Despawn(true);
            }

            slot.Pickup = null;
        }
    }

    /// <summary>
    /// Called when a pickup is collected from a slot.
    /// Clears the slot and starts cooldown to refill if needed.
    /// </summary>
    private void OnPickupCollected(SpawnerSlot slot)
    {
        slot.Pickup = null;
        Debug.Log($"Pickup collected from {slot.Point.name}");

        // Trigger new cooldown if there's now room
        TryStartCooldown();
    }

    /// <summary>
    /// Starts the cooldown timer if conditions allow (active, not already running, slot is free).
    /// </summary>
    private void TryStartCooldown()
    {
        if (!isActive || cooldownCoroutine != null || !HasFreeSlot()) return;

        cooldownCoroutine = host.StartCoroutine(SpawnAfterCooldown());
    }

    /// <summary>
    /// Waits for cooldown time, then spawns a pickup into one available slot.
    /// Automatically checks if another cooldown should start.
    /// </summary>
    private IEnumerator SpawnAfterCooldown()
    {
        yield return new WaitForSeconds(cooldownTime);
        cooldownCoroutine = null;

        TrySpawn();         // Spawn 1 pickup
        TryStartCooldown(); // Try to queue up another if room remains
    }

    /// <summary>
    /// Spawns a pickup prefab into one of the available, unoccupied spawn points.
    /// </summary>
    private void TrySpawn()
    {
        if (!isActive) return;

        var freeSlot = slots.FirstOrDefault(s => !s.IsOccupied);
        if (freeSlot == null)
        {
            Debug.Log("No available slot to spawn.");
            return;
        }

        var prefab = getPrefab();
        var instance = Object.Instantiate(prefab, freeSlot.Point.position, Quaternion.identity);
        var netObj = instance.GetComponent<NetworkObject>();
        netObj.Spawn(true);

        freeSlot.Pickup = instance;

        Debug.Log($"Spawned {prefab.name} at {freeSlot.Point.name}");

        var pickup = instance.GetComponent<Pickup>();
        pickup.OnCollected += () => OnPickupCollected(freeSlot);
    }

    /// <summary>
    /// Checks if at least one spawn slot is free (not currently holding a pickup).
    /// </summary>
    private bool HasFreeSlot()
    {
        return slots.Any(s => !s.IsOccupied);
    }
}
