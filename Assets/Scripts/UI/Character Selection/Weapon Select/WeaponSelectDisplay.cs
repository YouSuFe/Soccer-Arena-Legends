using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSelectDisplay : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponDatabase weaponDatabase;
    [SerializeField] private Transform weaponsHolder;
    [SerializeField] private WeaponSelectButton selectButtonPrefab;
    [SerializeField] private PlayerCard[] playerCards;
    [SerializeField] private GameObject weaponInfoPanel;
    [SerializeField] private TMP_Text weaponNameText;
    [SerializeField] private Transform weaponPreviewSpawnPoint;

    private GameObject weaponPreviewInstance;
    private List<WeaponSelectButton> weaponButtons = new List<WeaponSelectButton>();

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            Weapon[] allWeapons = weaponDatabase.GetAllWeapons();

            foreach (var weapon in allWeapons)
            {
                var selectButtonInstance = Instantiate(selectButtonPrefab, weaponsHolder);
                selectButtonInstance.SetWeapon(this, weapon);
                weaponButtons.Add(selectButtonInstance);
            }

            SelectionNetwork.Instance.PlayerSelections.OnListChanged += HandlePlayersStateChanged;
            SelectionNetwork.Instance.PlayerStatuses.OnListChanged += HandlePlayersStateChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsClient)
        {
            SelectionNetwork.Instance.PlayerSelections.OnListChanged -= HandlePlayersStateChanged;
            SelectionNetwork.Instance.PlayerStatuses.OnListChanged -= HandlePlayersStateChanged;
        }
    }

    public void Select(Weapon weapon)
    {
        var playersSelection = SelectionNetwork.Instance.PlayerSelections;
        var playersStatus = SelectionNetwork.Instance.PlayerStatuses;

        int localPlayerTeam = -1;

        for (int i = 0; i < playersStatus.Count; i++)
        {
            if (playersSelection[i].ClientId == NetworkManager.Singleton.LocalClientId)
            {
                localPlayerTeam = playersStatus[i].TeamIndex;
                break;
            }
        }

        if (localPlayerTeam == -1)
        {
            Debug.LogWarning($"[Select] Could not find local player's team!");
            return;
        }

        for (int i = 0; i < playersStatus.Count; i++)
        {
            if (playersSelection[i].ClientId != NetworkManager.Singleton.LocalClientId) { continue; }
            if (playersStatus[i].IsLockedIn) { return; }
            if (playersSelection[i].WeaponId == weapon.Id) { return; }

            // ✅ Use team-based weapon availability check
            if (SelectionNetwork.Instance.IsWeaponTaken(weapon.Id, localPlayerTeam)) { return; }
        }

        weaponNameText.text = weapon.DisplayName;
        weaponInfoPanel.SetActive(true);

        if (weaponPreviewInstance != null)
        {
            Destroy(weaponPreviewInstance);
        }

        weaponPreviewInstance = Instantiate(weapon.ModelPrefab, weaponPreviewSpawnPoint);
        SelectWeaponServerRpc(weapon.Id);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SelectWeaponServerRpc(int weaponId, ServerRpcParams serverRpcParams = default)
    {
        var playersSelection = SelectionNetwork.Instance.PlayerSelections;
        var playersStatus = SelectionNetwork.Instance.PlayerStatuses;
        ulong clientId = serverRpcParams.Receive.SenderClientId;

        int playerIndex = -1;
        int teamIndex = -1;

        for (int i = 0; i < playersSelection.Count; i++)
        {
            if (playersSelection[i].ClientId == clientId)
            {
                playerIndex = i;
                teamIndex = playersStatus[i].TeamIndex;
                break;
            }
        }

        if (playerIndex == -1 || teamIndex == -1)
        {
            Debug.LogWarning($"[SelectWeaponServerRpc] Could not find player {clientId} or their team!");
            return;
        }

        if (!weaponDatabase.IsValidWeaponId(weaponId))
        {
            Debug.LogWarning($"[SelectWeaponServerRpc] Player {clientId} tried to select an invalid weapon ID {weaponId}!");
            return;
        }

        // ✅ Use team-based check
        if (SelectionNetwork.Instance.IsWeaponTaken(weaponId, teamIndex))
        {
            Debug.LogWarning($"[SelectWeaponServerRpc] Player {clientId} tried to select Weapon {weaponId}, but it's already locked by a teammate!");
            return;
        }

        var updatedPlayer = playersSelection[playerIndex];
        updatedPlayer.WeaponId = weaponId;
        playersSelection[playerIndex] = updatedPlayer;
    }

    private void HandlePlayersStateChanged<T>(NetworkListEvent<T> changeEvent)
    {
        StartCoroutine(WaitForSyncAndUpdateUI());
    }

    private IEnumerator WaitForSyncAndUpdateUI()
    {
        // Wait until both lists have the same count
        int attempts = 2; // Prevent infinite loops
        while (SelectionNetwork.Instance.PlayerSelections.Count != SelectionNetwork.Instance.PlayerStatuses.Count && attempts > 0)
        {
            yield return new WaitForEndOfFrame(); // Wait for the next frame
            attempts--;
        }

        if (SelectionNetwork.Instance.PlayerSelections.Count != SelectionNetwork.Instance.PlayerStatuses.Count)
        {
            Debug.LogWarning("Selection and Status lists are STILL not in sync after delay!");
            yield break; // Stop execution to prevent errors
        }

        // Get local player's team
        int localPlayerTeam = -1;
        foreach (var playerStatus in SelectionNetwork.Instance.PlayerStatuses)
        {
            if (playerStatus.ClientId == NetworkManager.Singleton.LocalClientId)
            {
                localPlayerTeam = playerStatus.TeamIndex;
                break;
            }
        }

        if (localPlayerTeam == -1)
        {
            Debug.LogWarning("Local player's team index not found!");
            yield break;
        }

        // Disable buttons for already locked characters
        foreach (var button in weaponButtons)
        {
            if (SelectionNetwork.Instance.IsCharacterTaken(button.Weapon.Id, localPlayerTeam))
            {
                button.SetDisabled();
            }
        }

        // Filter players by team
        List<PlayerSelectionState> teamSelections = new List<PlayerSelectionState>();
        List<PlayerStatusState> teamStatuses = new List<PlayerStatusState>();

        for (int i = 0; i < SelectionNetwork.Instance.PlayerStatuses.Count; i++)
        {
            if (SelectionNetwork.Instance.PlayerStatuses[i].TeamIndex == localPlayerTeam)
            {
                teamSelections.Add(SelectionNetwork.Instance.PlayerSelections[i]);
                teamStatuses.Add(SelectionNetwork.Instance.PlayerStatuses[i]);
            }
        }

        // Update only the UI for the local player's team
        for (int i = 0; i < playerCards.Length; i++)
        {
            if (i < teamSelections.Count)
            {
                playerCards[i].UpdateWeaponDisplay(teamSelections[i]);
            }
            else
            {
                playerCards[i].DisableDisplay();
            }
        }

        SelectionNetwork.Instance.NotifySelectionChanged();

    }

}
