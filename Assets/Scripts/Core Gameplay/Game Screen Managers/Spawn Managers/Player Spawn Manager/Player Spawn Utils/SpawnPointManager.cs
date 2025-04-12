using System.Collections.Generic;
using UnityEngine;

public class SpawnPointManager
{
    private List<Transform> availableBlueTeamSpawns = new List<Transform>();
    private List<Transform> availableRedTeamSpawns = new List<Transform>();

    private Transform[] allBlueTeamSpawns;
    private Transform[] allRedTeamSpawns;

    public SpawnPointManager(Transform[] blueTeamSpawns, Transform[] redTeamSpawns)
    {
        allBlueTeamSpawns = blueTeamSpawns;
        allRedTeamSpawns = redTeamSpawns;
        ResetSpawnPoints();
    }

    /// <summary>
    /// Resets all spawn points, making them available again.
    /// </summary>
    public void ResetSpawnPoints()
    {
        availableBlueTeamSpawns.Clear();
        availableRedTeamSpawns.Clear();
        availableBlueTeamSpawns.AddRange(allBlueTeamSpawns);
        availableRedTeamSpawns.AddRange(allRedTeamSpawns);
    }

    /// <summary>
    /// Gets a **random spawn point for a single player** (used for new connections).
    /// </summary>
    public Transform GetSingleSpawnPoint(int teamIndex)
    {
        Transform[] spawnPoints = teamIndex == 0 ? allBlueTeamSpawns : allRedTeamSpawns;
        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }

    /// <summary>
    /// Gets a **unique spawn point for bulk player spawning** (prevents overlap).
    /// </summary>
    public Transform GetUniqueSpawnPoint(int teamIndex)
    {
        List<Transform> availableSpawns = teamIndex == 0 ? availableBlueTeamSpawns : availableRedTeamSpawns;

        if (availableSpawns.Count == 0)
        {
            Debug.LogWarning($"No available spawn points for team {teamIndex}! Resetting spawn points.");
            ResetSpawnPoints(); // Reset if all are used.
            availableSpawns = teamIndex == 0 ? availableBlueTeamSpawns : availableRedTeamSpawns;
        }

        int randomIndex = Random.Range(0, availableSpawns.Count);
        Transform spawnPoint = availableSpawns[randomIndex];
        availableSpawns.RemoveAt(randomIndex); // Mark it as used.

        return spawnPoint;
    }
}