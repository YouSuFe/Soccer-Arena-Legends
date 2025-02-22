using System.Collections;
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

            SelectionNetwork.Instance.PlayerSelections.OnListChanged += HandlePlayersStateChanged;
            SelectionNetwork.Instance.PlayerStatuses.OnListChanged += HandlePlayersStateChanged;
        }

        if(IsHost)
        {
            HandlePlayersStateChanged(new NetworkListEvent<PlayerSelectionState>());
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

    public void Select(Character character)
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
            if (playersSelection[i].CharacterId == character.Id) { return; }

            // ✅ Use team-based character availability check
            if (SelectionNetwork.Instance.IsCharacterTaken(character.Id, localPlayerTeam)) { return; }
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
            Debug.LogWarning($"[SelectServerRpc] Could not find player {clientId} or their team!");
            return;
        }

        if (!characterDatabase.IsValidCharacterId(characterId))
        {
            Debug.LogWarning($"[SelectServerRpc] Player {clientId} tried to select an invalid character ID {characterId}!");
            return;
        }

        // ✅ Use team-based check
        if (SelectionNetwork.Instance.IsCharacterTaken(characterId, teamIndex))
        {
            Debug.LogWarning($"[SelectServerRpc] Player {clientId} tried to select Character {characterId}, but it's already locked by a teammate!");
            return;
        }

        var updatedPlayer = playersSelection[playerIndex];
        updatedPlayer.CharacterId = characterId;
        playersSelection[playerIndex] = updatedPlayer;
    }

    private void HandlePlayersStateChanged<T>(NetworkListEvent<T> changeEvent)
    {
        StartCoroutine(WaitForSyncAndUpdateUI());
    }

    private IEnumerator WaitForSyncAndUpdateUI()
    {
        // Wait until both lists have the same count
        int attempts = 3; // Prevent infinite loops
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

        for (int i = 0; i < playerCards.Length; i++)
        {
            if (i < teamSelections.Count)
            {
                playerCards[i].UpdateCharacterDisplay(teamSelections[i], teamStatuses[i]);

            }
            else
            {
                playerCards[i].DisableDisplay();
            }
        }
        SelectionNetwork.Instance.NotifySelectionChanged();

    }



}
