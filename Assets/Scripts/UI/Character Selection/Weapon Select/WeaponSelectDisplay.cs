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

            SelectionNetwork.Instance.Players.OnListChanged += HandlePlayersStateChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsClient)
        {
            SelectionNetwork.Instance.Players.OnListChanged -= HandlePlayersStateChanged;
        }
    }

    public void Select(Weapon weapon)
    {
        var players = SelectionNetwork.Instance.Players; // Cached reference

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId != NetworkManager.Singleton.LocalClientId) { continue; }

            if (players[i].IsLockedIn) { return; }

            if (players[i].WeaponId == weapon.Id) { return; }

            if (SelectionNetwork.Instance.IsWeaponTaken(weapon.Id, false)) { return; }
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
        var players = SelectionNetwork.Instance.Players; // Cached reference

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId != serverRpcParams.Receive.SenderClientId) { continue; }

            if (!weaponDatabase.IsValidWeaponId(weaponId)) { return; }

            if (SelectionNetwork.Instance.IsWeaponTaken(weaponId, true)) { return; }

            var updatedPlayer = players[i];
            updatedPlayer.WeaponId = weaponId;
            players[i] = updatedPlayer;
        }
    }

    private void HandlePlayersStateChanged(NetworkListEvent<PlayerSelectState> changeEvent)
    {
        var players = SelectionNetwork.Instance.Players; // Cached reference

        for (int i = 0; i < playerCards.Length; i++)
        {
            if (players.Count > i)
            {
                playerCards[i].UpdateWeaponDisplay(players[i]);
            }
            else
            {
                playerCards[i].DisableDisplay();
            }
        }

        foreach (var button in weaponButtons)
        {
            if (button.IsDisabled) { continue; }

            if (SelectionNetwork.Instance.IsWeaponTaken(button.Weapon.Id, false))
            {
                button.SetDisabled();
            }
        }

        SelectionNetwork.Instance.NotifySelectionChanged();
    }

}
