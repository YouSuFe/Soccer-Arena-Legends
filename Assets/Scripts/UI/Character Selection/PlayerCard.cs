using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCard : MonoBehaviour
{
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private WeaponDatabase weaponDatabase;
    [SerializeField] private GameObject visuals;

    [Header("Character UI")]
    [SerializeField] private Image characterIconImage;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text characterNameText;

    [Header("Weapon UI")]
    [SerializeField] private Image weaponIconImage;
    [SerializeField] private TMP_Text weaponNameText;

    // ✅ Updates character UI, now using `PlayerSelectionState`
    public void UpdateCharacterDisplay(PlayerSelectionState selection, PlayerStatusState status)
    {
        if (selection.CharacterId != -1)
        {
            var character = characterDatabase.GetCharacterById(selection.CharacterId);
            characterIconImage.sprite = character.Icon;
            characterIconImage.enabled = true;
            characterNameText.text = character.DisplayName;
        }
        else
        {
            characterIconImage.enabled = false;
            characterNameText.text = "No Character Selected";
        }

        // ✅ Player name is now based on `PlayerStatusState`
        playerNameText.text = status.IsLockedIn ? $"Player {selection.ClientId}" : $"Player {selection.ClientId} (Picking...)";

        if (!visuals.activeSelf)
        {
            visuals.SetActive(true);
        }
    }

    // ✅ Updates weapon UI, now using `PlayerSelectionState`
    public void UpdateWeaponDisplay(PlayerSelectionState selection)
    {
        if (selection.WeaponId != -1)
        {
            var weapon = weaponDatabase.GetWeaponById(selection.WeaponId);
            weaponIconImage.sprite = weapon.Icon;
            weaponIconImage.enabled = true;
            weaponNameText.text = weapon.DisplayName;
        }
        else
        {
            weaponIconImage.enabled = false;
            weaponNameText.text = "No Weapon Selected";
        }
    }

    public void DisableDisplay()
    {
        if (visuals.activeSelf)
        {
            visuals.SetActive(false);
        }
    }
}

/*
  /// <summary>
    /// ✅ Called from external third-party software. This method is STATIC.
    /// </summary>
    [Command] // Required by third-party software
    public static void ForceLockInAllPlayersStatic()
    {
        if (Instance == null)
        {
            Debug.LogError("SelectionNetwork Instance is null! Cannot force lock-in.");
            return;
        }

        // ✅ Calls the instance method that contains actual logic
        Instance.ExecuteForceLockIn();
    }

    /// <summary>
    /// 🚀 The actual lock-in logic. This method is NOT static, so it can modify NetworkLists.
    /// </summary>
    private void ExecuteForceLockIn()
    {
        Debug.Log($"[ForceLockIn] Starting lock-in process for {PlayerSelections.Count} players.");

        for (int i = 0; i < PlayerSelections.Count; i++)
        {
            var updatedSelection = PlayerSelections[i];
            var updatedStatus = PlayerStatuses[i];
            int teamIndex = updatedStatus.TeamIndex;

            Debug.Log($"[Lock-In Process] Checking Player {updatedSelection.ClientId} (Team {teamIndex}): " +
                      $"Character {updatedSelection.CharacterId}, Weapon {updatedSelection.WeaponId}, LockedIn: {updatedStatus.IsLockedIn}");

            if (!updatedStatus.IsLockedIn)
            {
                // ✅ Now we just retrieve the team-specific lists, no need to check if they exist
                HashSet<int> teamLockedCharacters = lockedCharactersByTeam[teamIndex];
                HashSet<int> teamLockedWeapons = lockedWeaponsByTeam[teamIndex];
                List<int> teamAvailableCharacters = availableCharactersByTeam[teamIndex];
                List<int> teamAvailableWeapons = availableWeaponsByTeam[teamIndex];

                // Handle Character Selection
                if (teamLockedCharacters.Contains(updatedSelection.CharacterId))
                {
                    Debug.LogWarning($"[Character Conflict] Player {updatedSelection.ClientId} (Team {teamIndex}) selected Character {updatedSelection.CharacterId}, but it's already locked by a teammate. Resetting.");
                    updatedSelection.CharacterId = -1;
                }
                else if (updatedSelection.CharacterId != -1)
                {
                    Debug.Log($"[Character Assigned] Player {updatedSelection.ClientId} (Team {teamIndex}) locks in Character {updatedSelection.CharacterId}.");
                    teamLockedCharacters.Add(updatedSelection.CharacterId);
                    if (teamAvailableCharacters.Contains(updatedSelection.CharacterId))
                    {
                        teamAvailableCharacters.Remove(updatedSelection.CharacterId);
                        Debug.Log($"[Character Removed] Character {updatedSelection.CharacterId} removed from Team {teamIndex}'s available characters.");
                    }
                }

                // Handle Weapon Selection
                if (teamLockedWeapons.Contains(updatedSelection.WeaponId))
                {
                    Debug.LogWarning($"[Weapon Conflict] Player {updatedSelection.ClientId} (Team {teamIndex}) selected Weapon {updatedSelection.WeaponId}, but it's already locked by a teammate. Resetting.");
                    updatedSelection.WeaponId = -1;
                }
                else if (updatedSelection.WeaponId != -1)
                {
                    Debug.Log($"[Weapon Assigned] Player {updatedSelection.ClientId} (Team {teamIndex}) locks in Weapon {updatedSelection.WeaponId}.");
                    teamLockedWeapons.Add(updatedSelection.WeaponId);
                    if (teamAvailableWeapons.Contains(updatedSelection.WeaponId))
                    {
                        teamAvailableWeapons.Remove(updatedSelection.WeaponId);
                        Debug.Log($"[Weapon Removed] Weapon {updatedSelection.WeaponId} removed from Team {teamIndex}'s available weapons.");
                    }
                }

                // Assign Random Character if not selected
                if (updatedSelection.CharacterId == -1 && teamAvailableCharacters.Count > 0)
                {
                    int randomIndex = UnityEngine.Random.Range(0, teamAvailableCharacters.Count);
                    updatedSelection.CharacterId = teamAvailableCharacters[randomIndex];
                    teamAvailableCharacters.RemoveAt(randomIndex);
                    teamLockedCharacters.Add(updatedSelection.CharacterId);
                    Debug.Log($"[Auto-Assign Character] Player {updatedSelection.ClientId} (Team {teamIndex}) gets random Character {updatedSelection.CharacterId}.");
                }

                // Assign Random Weapon if not selected
                if (updatedSelection.WeaponId == -1 && teamAvailableWeapons.Count > 0)
                {
                    int randomIndex = UnityEngine.Random.Range(0, teamAvailableWeapons.Count);
                    updatedSelection.WeaponId = teamAvailableWeapons[randomIndex];
                    teamAvailableWeapons.RemoveAt(randomIndex);
                    teamLockedWeapons.Add(updatedSelection.WeaponId);
                    Debug.Log($"[Auto-Assign Weapon] Player {updatedSelection.ClientId} (Team {teamIndex}) gets random Weapon {updatedSelection.WeaponId}.");
                }

                // Finalize Lock-In
                updatedStatus.IsLockedIn = true;
                PlayerSelections[i] = updatedSelection;
                PlayerStatuses[i] = updatedStatus;

                Debug.Log($"[Final Lock-In] Player {PlayerSelections[i].ClientId} locked in with " +
                          $"Character {PlayerSelections[i].CharacterId}, Weapon {PlayerSelections[i].WeaponId}.");

                // Sync with the server
                HostSingleton.Instance.GameManager.NetworkServer.SetCharacter(updatedSelection.ClientId, updatedSelection.CharacterId);
                HostSingleton.Instance.GameManager.NetworkServer.SetWeapon(updatedSelection.ClientId, updatedSelection.WeaponId);
            }
        }

        // Notify that selections have changed
        NotifySelectionChanged();
        Debug.Log("[ForceLockIn] Lock-in process complete.");
    }
*/
