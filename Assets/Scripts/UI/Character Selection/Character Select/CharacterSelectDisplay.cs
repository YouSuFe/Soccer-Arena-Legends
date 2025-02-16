using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectDisplay : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private Transform charactersHolder;
    [SerializeField] private CharacterSelectButton selectButtonPrefab;
    [SerializeField] private PlayerCard[] playerCards;
    [SerializeField] private GameObject characterInfoPanel;
    [SerializeField] private TMP_Text characterNameText;
    [SerializeField] private Transform introSpawnPoint;

    private GameObject introInstance;
    private List<CharacterSelectButton> characterButtons = new List<CharacterSelectButton>();

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            Character[] allCharacters = characterDatabase.GetAllCharacters();

            foreach (var character in allCharacters)
            {
                var selectButtonInstance = Instantiate(selectButtonPrefab, charactersHolder);
                selectButtonInstance.SetCharacter(this, character);
                characterButtons.Add(selectButtonInstance);
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

    public void Select(Character character)
    {
        var players = SelectionNetwork.Instance.Players; // Cached reference

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId != NetworkManager.Singleton.LocalClientId) { continue; }

            if (players[i].IsLockedIn) { return; }

            if (players[i].CharacterId == character.Id) { return; }

            if (SelectionNetwork.Instance.IsCharacterTaken(character.Id, false)) { return; }
        }

        characterNameText.text = character.DisplayName;
        characterInfoPanel.SetActive(true);

        if (introInstance != null)
        {
            Destroy(introInstance);
        }

        introInstance = Instantiate(character.IntroPrefab, introSpawnPoint);
        SelectServerRpc(character.Id);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SelectServerRpc(int characterId, ServerRpcParams serverRpcParams = default)
    {
        var players = SelectionNetwork.Instance.Players; // Cached reference

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].ClientId != serverRpcParams.Receive.SenderClientId) { continue; }

            if (!characterDatabase.IsValidCharacterId(characterId)) { return; }

            if (SelectionNetwork.Instance.IsCharacterTaken(characterId, true)) { return; }

            var updatedPlayer = players[i];
            updatedPlayer.CharacterId = characterId;
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
                playerCards[i].UpdateCharacterDisplay(players[i]);
            }
            else
            {
                playerCards[i].DisableDisplay();
            }
        }

        foreach (var button in characterButtons)
        {
            if (button.IsDisabled) { continue; }

            // Disabling all the buttons after lock in, we leave it like this for now
            if (SelectionNetwork.Instance.IsCharacterTaken(button.Character.Id, false) ||
                SelectionNetwork.Instance.IsPlayerLockedIn(NetworkManager.Singleton.LocalClientId))
            {
                button.SetDisabled();
            }
        }

        SelectionNetwork.Instance.NotifySelectionChanged();
    }



}
