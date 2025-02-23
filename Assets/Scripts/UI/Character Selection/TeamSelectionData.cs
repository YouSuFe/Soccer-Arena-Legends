using System.Collections.Generic;

public class TeamSelectionData
{
    public HashSet<int> LockedCharacters { get; private set; } = new HashSet<int>();
    public HashSet<int> LockedWeapons { get; private set; } = new HashSet<int>();
    public List<int> AvailableCharacters { get; private set; }
    public List<int> AvailableWeapons { get; private set; }

    public TeamSelectionData(CharacterDatabase characterDatabase, WeaponDatabase weaponDatabase)
    {
        AvailableCharacters = new List<int>(characterDatabase.GetAllCharacterIds());
        AvailableWeapons = new List<int>(weaponDatabase.GetAllWeaponIds());
    }

    public void LockSelection(int id, HashSet<int> lockSet, List<int> availableList)
    {
        lockSet.Add(id);
        availableList.Remove(id);
    }

    public void UnlockSelection(int id, HashSet<int> lockSet, List<int> availableList)
    {
        lockSet.Remove(id);
        availableList.Add(id);
    }
}