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
    [SerializeField] private Transform previewSpawnPoint;

    private GameObject characterPreviewInstance;
    private List<CharacterSelectButton> characterButtons = new List<CharacterSelectButton>();

    private Dictionary<ulong, (int playerIndex, int teamIndex)> playerInfoCache = new Dictionary<ulong, (int, int)>();
    private List<PlayerSelectionState> reusableTeamSelections = new List<PlayerSelectionState>();
    private List<PlayerStatusState> reusableTeamStatuses = new List<PlayerStatusState>();

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
        var selectionNetwork = SelectionNetwork.Instance;
        var playersSelection = selectionNetwork.PlayerSelections;
        var playersStatus = selectionNetwork.PlayerStatuses;

        (int localPlayerIndex, int localPlayerTeam) = GetLocalPlayerInfo();

        if (localPlayerIndex == -1 || localPlayerTeam == -1)
        {
            Debug.LogWarning($"[Select] Could not find local player's index or team!");
            return;
        }

        if (playersStatus[localPlayerIndex].IsLockedIn || playersSelection[localPlayerIndex].CharacterId == character.Id)
            return;

        if (selectionNetwork.IsCharacterTaken(character.Id, localPlayerTeam))
            return;

        characterNameText.text = character.DisplayName;
        characterInfoPanel.SetActive(true);

        if (characterPreviewInstance != null)
        {
            Destroy(characterPreviewInstance);
        }

        characterPreviewInstance = Instantiate(character.IntroPrefab, previewSpawnPoint);

        //  Now we pass the cached playerIndex and teamIndex to the server
        SelectServerRpc(character.Id, localPlayerIndex, localPlayerTeam);
    }


    [ServerRpc(RequireOwnership = false)]
    private void SelectServerRpc(int characterId, int playerIndex, int teamIndex, ServerRpcParams serverRpcParams = default)
    {
        var selectionNetwork = SelectionNetwork.Instance;
        var playersSelection = selectionNetwork.PlayerSelections;

        ulong clientId = serverRpcParams.Receive.SenderClientId;

        // ✅ No need to loop through lists anymore!
        if (playerIndex < 0 || playerIndex >= playersSelection.Count || teamIndex < 0)
        {
            Debug.LogWarning($"[SelectServerRpc] Invalid playerIndex {playerIndex} or teamIndex {teamIndex} for client {clientId}.");
            return;
        }

        if (!characterDatabase.IsValidCharacterId(characterId))
        {
            Debug.LogWarning($"[SelectServerRpc] Invalid character ID {characterId} selected by {clientId}!");
            return;
        }

        if (selectionNetwork.IsCharacterTaken(characterId, teamIndex))
        {
            Debug.LogWarning($"[SelectServerRpc] Character {characterId} already taken by a teammate!");
            return;
        }

        var updatedPlayer = playersSelection[playerIndex];
        updatedPlayer.CharacterId = characterId;
        playersSelection[playerIndex] = updatedPlayer;
    }


    private void HandlePlayersStateChanged<T>(NetworkListEvent<T> changeEvent)
    {
        playerInfoCache.Clear(); // ✅ Clear cache when player data updates

        // ✅ Only use Coroutine if necessary
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

        // Get local player team instantly from cache
        (int _, int localPlayerTeam) = GetLocalPlayerInfo();
        if (localPlayerTeam == -1) return;

        // Disable buttons for characters that are already taken in this team
        foreach (var button in characterButtons)
        {
            if (selectionNetwork.IsCharacterTaken(button.Character.Id, localPlayerTeam))
            {
                button.SetDisabled();
            }
        }

        // Clear reusable lists instead of creating new ones
        reusableTeamSelections.Clear();
        reusableTeamStatuses.Clear();

        // Filter only the players from the same team (fast O(n) loop)
        for (int i = 0; i < playerStatuses.Count; i++)
        {
            if (playerStatuses[i].TeamIndex == localPlayerTeam)
            {
                reusableTeamSelections.Add(playerSelections[i]);
                reusableTeamStatuses.Add(playerStatuses[i]);
            }
        }

        // Update player card UI efficiently
        int teamCount = reusableTeamSelections.Count;
        for (int i = 0; i < playerCards.Length; i++)
        {
            if (i < teamCount)
            {
                playerCards[i].UpdateCharacterDisplay(reusableTeamSelections[i], reusableTeamStatuses[i]);
            }
            else
            {
                playerCards[i].DisableDisplay();
            }
        }

        // Notify UI updates only once at the end
        selectionNetwork.NotifySelectionChanged();
    }

    private (int playerIndex, int teamIndex) GetLocalPlayerInfo()
    {
        ulong localId = NetworkManager.Singleton.LocalClientId;

        if (playerInfoCache.TryGetValue(localId, out var cachedInfo))
        {
            return cachedInfo;  // ✅ Return cached data (O(1) lookup)
        }

        var playerSelections = SelectionNetwork.Instance.PlayerSelections;
        var playerStatuses = SelectionNetwork.Instance.PlayerStatuses;

        Dictionary<ulong, (int playerIndex, int teamIndex)> tempCache = new Dictionary<ulong, (int, int)>();

        for (int i = 0; i < playerStatuses.Count; i++)
        {
            tempCache[playerSelections[i].ClientId] = (i, playerStatuses[i].TeamIndex);
        }

        if (tempCache.TryGetValue(localId, out var playerInfo))
        {
            playerInfoCache[localId] = playerInfo; // ✅ Cache result
            return playerInfo;
        }

        return (-1, -1);
    }

}
