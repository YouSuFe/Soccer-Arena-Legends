using System.Collections.Generic;
using UnityEngine;

public static class LateJoinerUtility
{
    /// <summary>
    /// Assigns a random available character and weapon to the user, avoiding duplicates based on currently used data.
    /// </summary>
    /// <param name="userData">The user data to modify and assign to.</param>
    /// <param name="allUsers">List or dictionary of all current players' data.</param>
    /// <param name="characterDB">The character database.</param>
    /// <param name="weaponDB">The weapon database.</param>
    /// <returns>True if something was assigned, false if everything was already valid.</returns>
    public static bool AssignRandomCharacterAndWeapon(
        UserData userData,
        IEnumerable<UserData> allUsers,
        CharacterDatabase characterDB,
        WeaponDatabase weaponDB)
    {
        bool didAssign = false;

        HashSet<int> lockedCharacters = new();
        HashSet<int> lockedWeapons = new();

        foreach (var other in allUsers)
        {
            if (other.characterId != -1) lockedCharacters.Add(other.characterId);
            if (other.weaponId != -1) lockedWeapons.Add(other.weaponId);
        }

        List<int> availableCharacters = characterDB.GetAllCharacterIds().FindAll(id => !lockedCharacters.Contains(id));
        List<int> availableWeapons = weaponDB.GetAllWeaponIds().FindAll(id => !lockedWeapons.Contains(id));

        if (userData.characterId == -1 && availableCharacters.Count > 0)
        {
            userData.characterId = availableCharacters[Random.Range(0, availableCharacters.Count)];
            didAssign = true;
            Debug.Log($"[LateJoinerUtility] Assigned characterId {userData.characterId} to client {userData.clientId}");
        }

        if (userData.weaponId == -1 && availableWeapons.Count > 0)
        {
            userData.weaponId = availableWeapons[Random.Range(0, availableWeapons.Count)];
            didAssign = true;
            Debug.Log($"[LateJoinerUtility] Assigned weaponId {userData.weaponId} to client {userData.clientId}");
        }

        return didAssign;
    }
}
