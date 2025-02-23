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

    private Dictionary<ulong, (int playerIndex, int teamIndex)> playerInfoCache = new Dictionary<ulong, (int, int)>();
    private List<PlayerSelectionState> reusableTeamSelections = new List<PlayerSelectionState>();
    private List<PlayerStatusState> reusableTeamStatuses = new List<PlayerStatusState>();

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
        var selectionNetwork = SelectionNetwork.Instance;
        var playersSelection = selectionNetwork.PlayerSelections;
        var playersStatus = selectionNetwork.PlayerStatuses;

        (int localPlayerIndex, int localPlayerTeam) = GetLocalPlayerInfo();

        if (localPlayerIndex == -1 || localPlayerTeam == -1)
        {
            Debug.LogWarning($"[Select] Could not find local player's index or team!");
            return;
        }

        if (playersStatus[localPlayerIndex].IsLockedIn || playersSelection[localPlayerIndex].WeaponId == weapon.Id)
            return;

        if (selectionNetwork.IsWeaponTaken(weapon.Id, localPlayerTeam))
            return;

        weaponNameText.text = weapon.DisplayName;
        weaponInfoPanel.SetActive(true);

        if (weaponPreviewInstance != null)
        {
            Destroy(weaponPreviewInstance);
        }

        weaponPreviewInstance = Instantiate(weapon.ModelPrefab, weaponPreviewSpawnPoint);

        // ✅ Pass cached playerIndex and teamIndex
        SelectWeaponServerRpc(weapon.Id, localPlayerIndex, localPlayerTeam);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SelectWeaponServerRpc(int weaponId, int playerIndex, int teamIndex, ServerRpcParams serverRpcParams = default)
    {
        var selectionNetwork = SelectionNetwork.Instance;
        var playersSelection = selectionNetwork.PlayerSelections;

        ulong clientId = serverRpcParams.Receive.SenderClientId;

        if (playerIndex < 0 || playerIndex >= playersSelection.Count || teamIndex < 0)
        {
            Debug.LogWarning($"[SelectServerRpc] Invalid playerIndex {playerIndex} or teamIndex {teamIndex} for client {clientId}.");
            return;
        }

        if (!weaponDatabase.IsValidWeaponId(weaponId))
        {
            Debug.LogWarning($"[SelectServerRpc] Invalid weapon ID {weaponId} selected by {clientId}!");
            return;
        }

        if (selectionNetwork.IsWeaponTaken(weaponId, teamIndex))
        {
            Debug.LogWarning($"[SelectServerRpc] Weapon {weaponId} already taken by a teammate!");
            return;
        }

        var updatedPlayer = playersSelection[playerIndex];
        updatedPlayer.WeaponId = weaponId;
        playersSelection[playerIndex] = updatedPlayer;
    }

    private void HandlePlayersStateChanged<T>(NetworkListEvent<T> changeEvent)
    {
        playerInfoCache.Clear(); // ✅ Ensure cached data is refreshed

        if (SelectionNetwork.Instance.PlayerSelections.Count == SelectionNetwork.Instance.PlayerStatuses.Count)
        {
            UpdateUI();
        }
        else
        {
            StartCoroutine(WaitForSyncAndUpdateUI());
        }
    }

    private IEnumerator WaitForSyncAndUpdateUI()
    {
        int attempts = 3;
        while (SelectionNetwork.Instance.PlayerSelections.Count != SelectionNetwork.Instance.PlayerStatuses.Count && attempts > 0)
        {
            yield return new WaitForEndOfFrame();
            attempts--;
        }

        if (SelectionNetwork.Instance.PlayerSelections.Count != SelectionNetwork.Instance.PlayerStatuses.Count)
        {
            Debug.LogWarning("Selection and Status lists are STILL not in sync after delay!");
            yield break;
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        var selectionNetwork = SelectionNetwork.Instance;
        var playerStatuses = selectionNetwork.PlayerStatuses;
        var playerSelections = selectionNetwork.PlayerSelections;

        // ✅ Get local player team instantly from cache
        (int _, int localPlayerTeam) = GetLocalPlayerInfo();
        if (localPlayerTeam == -1) return;

        // ✅ Reduce redundant method calls inside loop
        foreach (var button in weaponButtons)
        {
            if (selectionNetwork.IsWeaponTaken(button.Weapon.Id, localPlayerTeam))
            {
                button.SetDisabled();
            }
        }

        reusableTeamSelections.Clear();
        reusableTeamStatuses.Clear();

        for (int i = 0; i < playerStatuses.Count; i++)
        {
            if (playerStatuses[i].TeamIndex == localPlayerTeam)
            {
                reusableTeamSelections.Add(playerSelections[i]);
                reusableTeamStatuses.Add(playerStatuses[i]);
            }
        }

        int teamCount = reusableTeamSelections.Count;
        for (int i = 0; i < playerCards.Length; i++)
        {
            if (i < teamCount)
            {
                playerCards[i].UpdateWeaponDisplay(reusableTeamSelections[i]);
            }
            else
            {
                playerCards[i].DisableDisplay();
            }
        }

        selectionNetwork.NotifySelectionChanged();
    }

    private (int playerIndex, int teamIndex) GetLocalPlayerInfo()
    {
        ulong localId = NetworkManager.Singleton.LocalClientId;
        if (playerInfoCache.TryGetValue(localId, out var cachedInfo)) return cachedInfo;

        var playerSelections = SelectionNetwork.Instance.PlayerSelections;
        var playerStatuses = SelectionNetwork.Instance.PlayerStatuses;
        Dictionary<ulong, (int, int)> tempCache = new Dictionary<ulong, (int, int)>();

        for (int i = 0; i < playerStatuses.Count; i++)
            tempCache[playerSelections[i].ClientId] = (i, playerStatuses[i].TeamIndex);

        if (tempCache.TryGetValue(localId, out var playerInfo))
        {
            playerInfoCache[localId] = playerInfo;
            return playerInfo;
        }

        return (-1, -1);
    }
}
