using System;
using System.Collections;
using System.Collections.Generic;
using QFSW.QC;
using Unity.Netcode;
using UnityEngine;

/*
 * This class (`SelectionNetwork`) manages player selections using a `NetworkList<PlayerSelectState>`. 
 * However, it is actually **redundant** because the same selection logic can be handled by 
 * the `UserData` stored in `NetworkServer`. 
 *
 * - **Why is it redundant?**  
 *   - `UserData` in `NetworkServer` already stores each player's `characterId` and `weaponId`.
 *   - We could update and retrieve selections directly from `UserData`, removing the need for `NetworkList<PlayerSelectState>`.
 * 
 * - **Why is this class still here?**  
 *   - It makes handling player selections easier and more **understandable**.
 *   - It allows real-time visibility of selections for all players.
 *   - It simplifies selection updates without modifying `NetworkServer`.
 * 
 * - **Potential Issues:**  
 *   - This approach **sends more network updates**, which may **waste bandwidth**.  
 *   - Every time a player selects a character/weapon, an update is sent to all clients.  
 *   - But it can be handled in 'Network Server' class.
 * 
 * - **Future Consideration:**  
 *   - This class might be **removed** in the future to **merge selection logic into `NetworkServer`**.  
 *   - Using only `NetworkServer.UserData` would reduce redundancy and optimize network traffic.
 */
public class SelectionNetwork : NetworkBehaviour
{

    [Command] // Required by third-party software
    public static void StartTheGame()
    {
        // ✅ Calls the instance method that contains actual logic
        HostSingleton.Instance.GameManager.NetworkServer.StartGame();
    }



    #region Singleton and Properties


    public static SelectionNetwork Instance { get; private set; }



    [Header("Databases")]
    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private WeaponDatabase weaponDatabase;

    [Header("Selection Settings")]
    // If we want to start the game when max player is reached, we can use this.
    [SerializeField] private int minPlayersToStart = 2;
    [SerializeField] private float selectionTimeAmount = 90f;
    [SerializeField] private float secondPhaseTime = 15f;

    // Stores frequently updated data (Character and Weapon selection)
    public NetworkList<PlayerSelectionState> PlayerSelections { get; private set; }
    // Stores infrequently updated data (IsLockedIn and TeamIndex)
    public NetworkList<PlayerStatusState> PlayerStatuses { get; private set; }

    private NetworkVariable<float> selectionTimer = new NetworkVariable<float>();

    public Action OnSelectionStateChanged { get; internal set; }

    private Dictionary<int, TeamSelectionData> teamData = new();

    private Coroutine timerCoroutine;
    private Coroutine secondTimerCoroutine;

    #endregion

    #region Unity Callbacks

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Duplicate SelectionNetwork found! Destroying this instance.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        PlayerSelections = new NetworkList<PlayerSelectionState>();
        PlayerStatuses = new NetworkList<PlayerStatusState>();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            selectionTimer.Value = selectionTimeAmount;
            InitializeTeamData();

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            AddHostPlayer();
        }

        if (IsClient && !IsServer)
        {
            RequestTeamLockDataServerRpc();
        }

        if (IsHost)
        {
            NotifySelectionChanged();
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    #endregion


    #region Initialization

    /// <summary>
    /// Initializes team data at the start of the game.
    /// </summary>
    private void InitializeTeamData()
    {
        teamData.Clear();
        teamData = new Dictionary<int, TeamSelectionData>
        {
            { 0, new TeamSelectionData(characterDatabase, weaponDatabase) },
            { 1, new TeamSelectionData(characterDatabase, weaponDatabase) }
        };
    }

    private void AddHostPlayer()
    {
        ulong hostClientId = NetworkManager.Singleton.LocalClientId;
        if (!IsPlayerInList(hostClientId))
        {
            UserData userData = HostSingleton.Instance.GameManager.NetworkServer.GetUserDataByClientId(hostClientId);
            if (userData != null)
            {
                PlayerSelections.Add(new PlayerSelectionState(hostClientId));
                PlayerStatuses.Add(new PlayerStatusState(hostClientId, false, userData.teamIndex, userData.userName));
            }
            else
            {
                Debug.LogError("[OnNetworkSpawn] Failed to get UserData for host!");
            }
        }
    }

    #endregion


    #region Player Connection Management

    /*
     * Called when a new client connects to the server.
     * We add the player to the selection list and status list.
     */
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client connected: {clientId}");

        // Get UserData from NetworkServer (contains teamIndex)
        UserData userData = HostSingleton.Instance.GameManager.NetworkServer.GetUserDataByClientId(clientId);
        if (userData == null)
        {
            Debug.LogError($"[OnClientConnected] Could not find UserData for client {clientId}");
            return;
        }

        if (!IsPlayerInList(clientId))
        {
            // ✅ Add player to selection and status lists
            PlayerSelections.Add(new PlayerSelectionState(clientId));
            PlayerStatuses.Add(new PlayerStatusState(clientId, false, userData.teamIndex, userData.userName));

            Debug.Log($"[OnClientConnected] Player {clientId} added to selections and status lists (Team {userData.teamIndex})");
        }

        // Start the timer only when the first player joins
        if (PlayerStatuses.Count == minPlayersToStart /*LobbyManager.Instance.GetMaxPlayersInLobby()*/)
        {
            StartSelectionTimer();
        }
    }

    /*
     * Called when a client disconnects.
     * Removes the player from selection and status lists, unlocking their choices.
     */
    private void OnClientDisconnected(ulong clientId)
    {
        int index = GetPlayerStatusIndex(clientId);
        if (index == -1) return;

        int teamIndex = PlayerStatuses[index].TeamIndex;
        if (teamData.TryGetValue(teamIndex, out var team))
        {
            if (PlayerStatuses[index].IsLockedIn)
            {
                team.UnlockSelection(PlayerSelections[index].CharacterId, team.LockedCharacters, team.AvailableCharacters);
                team.UnlockSelection(PlayerSelections[index].WeaponId, team.LockedWeapons, team.AvailableWeapons);
            }
        }

        PlayerSelections.RemoveAt(index);
        PlayerStatuses.RemoveAt(index);

        NotifySelectionChanged();

        Debug.LogWarning($"[OnClientDisconnected] Player {clientId} was not found in the selection lists!");
    }

    #endregion

    #region Selection Timer

    /// <summary>
    /// Starts the selection timer.
    /// </summary>
    private void StartSelectionTimer()
    {
        if (timerCoroutine != null) { StopCoroutine(timerCoroutine); }
        timerCoroutine = StartCoroutine(SelectionTimerRoutine());
    }

    // Timer Countdown Logic
    private IEnumerator SelectionTimerRoutine()
    {
        while (selectionTimer.Value > 0)
        {
            yield return new WaitForSeconds(1f);
            selectionTimer.Value -= 1f; 
        }

        Debug.Log("Timer Expired! Forcing Lock-In.");
        ForceLockInAllPlayers();

        // Set the selectionTimer value for all players before starting second phase
        StartSecondPhaseTimer();
    }

    // Second Timer (Pre-Game Phase)
    private void StartSecondPhaseTimer()
    {
        selectionTimer.Value = secondPhaseTime; // Assign the selection timer value for second phase of selection

        if (secondTimerCoroutine != null) { StopCoroutine(secondTimerCoroutine); }
        secondTimerCoroutine = StartCoroutine(SecondPhaseTimerRoutine());
    }

    private IEnumerator SecondPhaseTimerRoutine()
    {
        while (selectionTimer.Value > 0)
        {
            yield return new WaitForSeconds(1f);
            selectionTimer.Value -= 1f;
        }

        Debug.Log("Second Phase Timer Expired! Starting Game.");
        HostSingleton.Instance.GameManager.NetworkServer.StartGame();
    }

    #endregion

    #region Lock-In and Team Management

    public void LockInSelection()
    {
        LockInSelectionServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void LockInSelectionServerRpc(ServerRpcParams serverRpcParams = default)
    {

        Debug.Log("LockInSelectionServerRpc is called");

        ulong clientId = serverRpcParams.Receive.SenderClientId;
        int index = GetPlayerSelectionIndex(clientId);
        if (index == -1) return;

        if (!CanLockIn(clientId))
        {
            Debug.LogWarning($"[LockInSelection] Client {clientId} cannot lock in due to invalid selection.");
            return;
        }

        // ✅ Retrieve the structs from NetworkLists
        PlayerSelectionState selection = PlayerSelections[index];
        PlayerStatusState status = PlayerStatuses[index];

        // ✅ Lock in selection
        LockInPlayer(clientId, ref selection, ref status);

        // ✅ Assign modified structs back to NetworkLists
        PlayerSelections[index] = selection;
        PlayerStatuses[index] = status;

        // ✅ Resolve conflicts
        ResolveSelectionConflicts(status.TeamIndex);

        NotifySelectionChanged();

        SyncTeamLocksToAllClientsClientRpc(GetTeamLocksArray());
    }

    private void LockInPlayer(ulong clientId, ref PlayerSelectionState selection, ref PlayerStatusState status)
    {
        int teamIndex = status.TeamIndex;
        if (!teamData.TryGetValue(teamIndex, out var team)) return;

        Debug.Log($"[LockInPlayer] Locking in Player {clientId} for Team {teamIndex}");

        // Handle Character Selection
        if (team.LockedCharacters.Contains(selection.CharacterId))
        {
            selection.CharacterId = -1;
        }
        else if (selection.CharacterId != -1)
        {
            team.LockSelection(selection.CharacterId, team.LockedCharacters, team.AvailableCharacters);
        }

        // Handle Weapon Selection
        if (team.LockedWeapons.Contains(selection.WeaponId))
        {
            selection.WeaponId = -1;
        }
        else if (selection.WeaponId != -1)
        {
            team.LockSelection(selection.WeaponId, team.LockedWeapons, team.AvailableWeapons);
        }

        // Assign Random Character if needed
        if (selection.CharacterId == -1 && team.AvailableCharacters.Count > 0)
        {
            selection.CharacterId = team.AvailableCharacters[UnityEngine.Random.Range(0, team.AvailableCharacters.Count)];
            team.LockSelection(selection.CharacterId, team.LockedCharacters, team.AvailableCharacters);
        }

        // Assign Random Weapon if needed
        if (selection.WeaponId == -1 && team.AvailableWeapons.Count > 0)
        {
            selection.WeaponId = team.AvailableWeapons[UnityEngine.Random.Range(0, team.AvailableWeapons.Count)];
            team.LockSelection(selection.WeaponId, team.LockedWeapons, team.AvailableWeapons);
        }

        // Finalize Lock-In
        status.IsLockedIn = true;

        // Sync with server
        HostSingleton.Instance.GameManager.NetworkServer.SetCharacter(clientId, selection.CharacterId);
        HostSingleton.Instance.GameManager.NetworkServer.SetWeapon(clientId, selection.WeaponId);
    }

    private void ResolveSelectionConflicts(int teamIndex)
    {
        if (!teamData.TryGetValue(teamIndex, out var team)) return;

        HashSet<int> teamLockedCharacters = team.LockedCharacters;
        HashSet<int> teamLockedWeapons = team.LockedWeapons;

        for (int i = 0; i < PlayerSelections.Count; i++)
        {
            var otherSelection = PlayerSelections[i];
            var otherStatus = PlayerStatuses[i];

            if (otherStatus.TeamIndex != teamIndex) continue; // ✅ Only check players on the same team
            if (otherStatus.IsLockedIn) continue; // ✅ Skip already locked-in players

            // Reset character if conflict exists
            if (otherSelection.CharacterId != -1 && teamLockedCharacters.Contains(otherSelection.CharacterId))
            {
                Debug.Log($"[Conflict Reset] Player {otherSelection.ClientId} (Team {teamIndex}) had Character {otherSelection.CharacterId}, but it's now locked. Resetting...");
                otherSelection.CharacterId = -1;
            }

            // Reset weapon if conflict exists
            if (otherSelection.WeaponId != -1 && teamLockedWeapons.Contains(otherSelection.WeaponId))
            {
                Debug.Log($"[Conflict Reset] Player {otherSelection.ClientId} (Team {teamIndex}) had Weapon {otherSelection.WeaponId}, but it's now locked. Resetting...");
                otherSelection.WeaponId = -1;
            }

            PlayerSelections[i] = otherSelection;
        }
    }

    /// <summary>
    /// Forces all players to lock in their selections.
    /// </summary>
    private void ForceLockInAllPlayers()
    {
        Debug.Log($"[ForceLockIn] Starting lock-in process for {PlayerSelections.Count} players.");

        for (int i = 0; i < PlayerSelections.Count; i++)
        {
            PlayerSelectionState selection = PlayerSelections[i];
            PlayerStatusState status = PlayerStatuses[i];
            int teamIndex = status.TeamIndex;

            if (status.IsLockedIn) continue; // Skip already locked-in players

            if (!teamData.TryGetValue(teamIndex, out var team))
            {
                Debug.LogError($"[ForceLockIn] Team {teamIndex} not found in teamData!");
                continue;
            }

            Debug.Log($"[Lock-In Process] Checking Player {selection.ClientId} (Team {teamIndex}): " +
                      $"Character {selection.CharacterId}, Weapon {selection.WeaponId}, LockedIn: {status.IsLockedIn}");

            // ✅ Lock in the player using our centralized method
            LockInPlayer(selection.ClientId, ref selection, ref status);

            // ✅ Store the updated selections in NetworkList
            PlayerSelections[i] = selection;
            PlayerStatuses[i] = status;
        }

        // Notify selection changes
        NotifySelectionChanged();
        Debug.Log("[ForceLockIn] Lock-in process complete.");

        SyncTeamLocksToAllClientsClientRpc(GetTeamLocksArray());

    }

    #region Sync Methods (Server <--> Clients)

    [ServerRpc(RequireOwnership = false)]
    private void RequestTeamLockDataServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong clientId = serverRpcParams.Receive.SenderClientId;

        List<TeamLockData> teamLocks = new List<TeamLockData>();
        foreach (var kvp in teamData) // ✅ Iterating over teamData instead of separate dictionaries
        {
            int teamIndex = kvp.Key;
            TeamSelectionData team = kvp.Value;

            // ✅ Creating a data packet for each team
            teamLocks.Add(new TeamLockData(teamIndex, team.LockedCharacters, team.LockedWeapons));
        }

        Debug.Log("Server : Try to sync team lock datas to players");
        SyncTeamLocksToClientClientRpc(teamLocks.ToArray(), RpcUtils.ToClient(clientId));
    }

    // For individual player to inform when it is first connected if there is any locked character or weapon
    [ClientRpc]
    private void SyncTeamLocksToClientClientRpc(TeamLockData[] teamLocks, ClientRpcParams clientRpcParams = default)
    {
        teamData.Clear(); // ✅ Clearing teamData before updating

        foreach (var teamLock in teamLocks)
        {
            TeamSelectionData team = new TeamSelectionData(characterDatabase, weaponDatabase);

            // ✅ Copying locked characters and weapons from the server's data
            foreach (var charId in teamLock.LockedCharacters)
                team.LockedCharacters.Add(charId);

            foreach (var weaponId in teamLock.LockedWeapons)
                team.LockedWeapons.Add(weaponId);

            teamData[teamLock.TeamIndex] = team;
        }

        Debug.Log("[SyncTeamLocks] Client received updated team lock data.");
    }

    // All player to inform about team data
    [ClientRpc]
    private void SyncTeamLocksToAllClientsClientRpc(TeamLockData[] teamLocks)
    {
        teamData.Clear();
        foreach (var teamLock in teamLocks)
        {
            TeamSelectionData team = new TeamSelectionData(characterDatabase, weaponDatabase);
            foreach (var charId in teamLock.LockedCharacters)
                team.LockedCharacters.Add(charId);
            foreach (var weaponId in teamLock.LockedWeapons)
                team.LockedWeapons.Add(weaponId);

            teamData[teamLock.TeamIndex] = team;
        }

        Debug.Log("[SyncTeamLocks] All clients updated their team lock data.");
    }

    #endregion


    #endregion


    #region Utility Methods

    /*
     * Determines if a player can lock in their selection.
     * Checks if the player has made valid selections that aren't taken.
     */
    public bool CanLockIn(ulong clientId)
    {
        int index = GetPlayerSelectionIndex(clientId);
        if (index == -1) return false;

        var selection = PlayerSelections[index];
        var status = PlayerStatuses[index];
        int teamIndex = status.TeamIndex;

        if (characterDatabase == null || weaponDatabase == null)
        {
            Debug.LogError("[CanLockIn] CharacterDatabase or WeaponDatabase is not set!");
            return false;
        }

        if (!characterDatabase.IsValidCharacterId(selection.CharacterId))
        {
            Debug.LogWarning($"[CanLockIn] CharacterId {selection.CharacterId} is INVALID.");
            return false;
        }

        if (!weaponDatabase.IsValidWeaponId(selection.WeaponId))
        {
            Debug.LogWarning($"[CanLockIn] WeaponId {selection.WeaponId} is INVALID.");
            return false;
        }

        // ✅ Use `teamData` for quick lookup instead of checking lists
        if (!teamData.TryGetValue(teamIndex, out var team)) return false;

        bool isCharacterValid = selection.CharacterId != -1 && !team.LockedCharacters.Contains(selection.CharacterId);
        bool isWeaponValid = selection.WeaponId != -1 && !team.LockedWeapons.Contains(selection.WeaponId);

        Debug.Log($"[CanLockIn] Client {clientId} → IsCharacterValid: {isCharacterValid}, IsWeaponValid: {isWeaponValid}");

        return isCharacterValid && isWeaponValid;
    }




    private int GetPlayerSelectionIndex(ulong clientId)
    {
        for (int i = 0; i < PlayerSelections.Count; i++)
        {
            if (PlayerSelections[i].ClientId == clientId) return i;
        }
        return -1;
    }

    private int GetPlayerStatusIndex(ulong clientId)
    {
        for (int i = 0; i < PlayerStatuses.Count; i++)
        {
            if (PlayerStatuses[i].ClientId == clientId) return i;
        }
        return -1;
    }

    public bool IsCharacterTaken(int characterId, int teamIndex)
    {
        return teamData.ContainsKey(teamIndex) && teamData[teamIndex].LockedCharacters.Contains(characterId);
    }

    public bool IsWeaponTaken(int weaponId, int teamIndex)
    {
        return teamData.ContainsKey(teamIndex) && teamData[teamIndex].LockedWeapons.Contains(weaponId);
    }


    public bool IsPlayerLockedIn(ulong clientId)
    {
        foreach (var player in PlayerStatuses)
        {
            if (player.ClientId == clientId)
            {
                return player.IsLockedIn;
            }
        }
        return false;
    }

    public bool HasPlayerMadeSelection(ulong clientId)
    {
        foreach (var player in PlayerSelections)
        {
            if (player.ClientId == clientId)
            {
                return player.CharacterId != -1 && player.WeaponId != -1;
            }
        }
        return false;
    }

    public PlayerStatusState? GetPlayerState(ulong clientId)
    {
        foreach (var player in PlayerStatuses)
        {
            if (player.ClientId == clientId) return player;
        }
        return null;
    }

    public List<PlayerStatusState> GetPlayersByTeam(GameEnumsUtil.PlayerTeam team)
    {
        List<PlayerStatusState> teamPlayers = new List<PlayerStatusState>();

        foreach (var player in PlayerStatuses)
        {
            UserData userData = HostSingleton.Instance.GameManager.NetworkServer.GetUserDataByClientId(player.ClientId);
            if (userData != null && userData.teamIndex == (int)team)
            {
                teamPlayers.Add(player);
            }
        }

        return teamPlayers;
    }

    //  Allow UI to Access the Timer Value
    public float GetRemainingTime()
    {
        return selectionTimer.Value;
    }

    private List<int> GetAvailableCharacters(int teamIndex)
    {
        if (!teamData.TryGetValue(teamIndex, out var team))
            return new List<int>(); // Return empty list if team data doesn't exist

        List<int> availableCharacters = new List<int>();
        foreach (int characterId in characterDatabase.GetAllCharacterIds())
        {
            if (!team.LockedCharacters.Contains(characterId))
            {
                availableCharacters.Add(characterId);
            }
        }
        return availableCharacters;
    }

    private List<int> GetAvailableWeapons(int teamIndex)
    {
        if (!teamData.TryGetValue(teamIndex, out var team))
            return new List<int>(); // Return empty list if team data doesn't exist

        List<int> availableWeapons = new List<int>();
        foreach (int weaponId in weaponDatabase.GetAllWeaponIds())
        {
            if (!team.LockedWeapons.Contains(weaponId))
            {
                availableWeapons.Add(weaponId);
            }
        }
        return availableWeapons;
    }

    private TeamLockData[] GetTeamLocksArray()
    {
        List<TeamLockData> teamLocks = new List<TeamLockData>();
        foreach (var kvp in teamData)
        {
            teamLocks.Add(new TeamLockData(kvp.Key, kvp.Value.LockedCharacters, kvp.Value.LockedWeapons));
        }
        return teamLocks.ToArray();
    }

    private bool IsPlayerInList(ulong clientId)
    {
        foreach (var player in PlayerStatuses)
        {
            if (player.ClientId == clientId)
            {
                return true;
            }
        }
        return false;
    }

    public void NotifySelectionChanged()
    {
        OnSelectionStateChanged?.Invoke();
    }

    #endregion

}
