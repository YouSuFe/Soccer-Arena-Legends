using System.Collections.Generic;
using UnityEngine;

public class SpawnPointManager
{
    private List<Transform> originalBlueTeamSpawns;
    private List<Transform> originalRedTeamSpawns;

    private List<Transform> availableBlueTeamSpawns;
    private List<Transform> availableRedTeamSpawns;

    public SpawnPointManager(Transform[] blueTeamSpawns, Transform[] redTeamSpawns)
    {
        originalBlueTeamSpawns = new List<Transform>(blueTeamSpawns);
        originalRedTeamSpawns = new List<Transform>(redTeamSpawns);

        availableBlueTeamSpawns = new List<Transform>(originalBlueTeamSpawns);
        availableRedTeamSpawns = new List<Transform>(originalRedTeamSpawns);
    }

    /// <summary>
    /// Resets all spawn points, making them available again.
    /// </summary>
    public void ResetSpawnPoints()
    {
        availableBlueTeamSpawns.Clear();
        availableRedTeamSpawns.Clear();
        availableBlueTeamSpawns.AddRange(originalBlueTeamSpawns);
        availableRedTeamSpawns.AddRange(originalRedTeamSpawns);
    }

    /// <summary>
    /// Gets a random available spawn point and reserves it (removes it from list).
    /// </summary>
    public Transform GetRandomAvailableSpawnPoint(int teamIndex)
    {
        List<Transform> availableSpawns = teamIndex == 0 ? availableBlueTeamSpawns : availableRedTeamSpawns;

        if (availableSpawns.Count == 0)
        {
            Debug.LogWarning($"All spawn points used for team {teamIndex}, resetting.");
            ResetSpawnPoints();
            availableSpawns = teamIndex == 0 ? availableBlueTeamSpawns : availableRedTeamSpawns;
        }

        int randomIndex = Random.Range(0, availableSpawns.Count);
        Transform spawnPoint = availableSpawns[randomIndex];
        availableSpawns.RemoveAt(randomIndex);
        return spawnPoint;
    }
}
