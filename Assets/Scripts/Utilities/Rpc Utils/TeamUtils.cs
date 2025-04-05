using UnityEngine;

public static class TeamUtils
{
    public static bool AreOpponents(ulong attackerClientId, ulong targetClientId)
    {
        var attackerData = PlayerSpawnManager.Instance.GetUserData(attackerClientId);
        var targetData = PlayerSpawnManager.Instance.GetUserData(targetClientId);

        if (attackerData == null || targetData == null)
        {
            Debug.LogWarning($"[TeamUtils] Missing user data. Attacker: {attackerClientId}, Target: {targetClientId}");
            return false; // Safer to not deal damage
        }

        return attackerData.teamIndex != targetData.teamIndex;
    }
}

