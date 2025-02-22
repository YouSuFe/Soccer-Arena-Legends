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
    public static SelectionNetwork Instance { get; private set; }

    // Stores frequently updated data (Character and Weapon selection)
    public NetworkList<PlayerSelectionState> PlayerSelections { get; private set; }
    // Stores infrequently updated data (IsLockedIn and TeamIndex)
    public NetworkList<PlayerStatusState> PlayerStatuses { get; private set; }

    private NetworkVariable<float> selectionTimer = new NetworkVariable<float>();

    public Action OnSelectionStateChanged { get; internal set; }

    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private WeaponDatabase weaponDatabase;

    // If we want to start the game when max player is reached, we can use this.
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private float selectionTimeAmount = 90f;

    private Dictionary<int, HashSet<int>> lockedCharactersByTeam = new Dictionary<int, HashSet<int>>();
    private Dictionary<int, HashSet<int>> lockedWeaponsByTeam = new Dictionary<int, HashSet<int>>();
    private Dictionary<int, List<int>> availableCharactersByTeam = new Dictionary<int, List<int>>();
    private Dictionary<int, List<int>> availableWeaponsByTeam = new Dictionary<int, List<int>>();


    private Coroutine timerCoroutine;

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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Duplicate SelectionNetwork found! Destroying this instance.");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Initialize NetworkLists for players' selection and status
        PlayerSelections = new NetworkList<PlayerSelectionState>();
        PlayerStatuses = new NetworkList<PlayerStatusState>();
    }

    //private void Update()
    //{
    //    if (!IsServer) return; // Only the server should track this

    //    if (lockedCharacters.Count > 0)
    //    {
    //        Debug.Log("Locked Characters: " + string.Join(", ", lockedCharacters));
    //    }
    //    else
    //    {
    //        Debug.Log(" No characters locked yet.");
    //    }

    //    if (lockedWeapons.Count > 0)
    //    {
    //        Debug.Log(" Locked Weapons: " + string.Join(", ", lockedWeapons));
    //    }
    //    else
    //    {
    //        Debug.Log(" No weapons locked yet.");
    //    }
    //}


    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            selectionTimer.Value = selectionTimeAmount;

            InitializeTeamData();

            Debug.Log("Inside Selection Network Is Server " + IsServer);
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            ulong hostClientId = NetworkManager.Singleton.LocalClientId;
            if (!IsPlayerInList(hostClientId))
            {
                Debug.Log($"Adding Host Player (Client ID: {hostClientId}) to the selection list.");
                UserData userData = HostSingleton.Instance.GameManager.NetworkServer.GetUserDataByClientId(hostClientId);
                if (userData != null)
                {
                    // Adding the host player to both NetworkLists
                    PlayerSelections.Add(new PlayerSelectionState(hostClientId));
                    PlayerStatuses.Add(new PlayerStatusState(hostClientId, false, userData.teamIndex));
                }
                else
                {
                    Debug.LogError("[OnNetworkSpawn] Failed to get UserData for host!");
                }
            }
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

    /// ✅ **Now initializes all teams (0 and 1) at the start. No missing teams!**
    private void InitializeTeamData()
    {
        lockedCharactersByTeam.Clear();
        lockedWeaponsByTeam.Clear();
        availableCharactersByTeam.Clear();
        availableWeaponsByTeam.Clear();

        for (int teamIndex = 0; teamIndex <= 1; teamIndex++) // ✅ Always initialize Team 0 & 1
        {
            lockedCharactersByTeam[teamIndex] = new HashSet<int>();
            lockedWeaponsByTeam[teamIndex] = new HashSet<int>();
            availableCharactersByTeam[teamIndex] = new List<int>(characterDatabase.GetAllCharacterIds());
            availableWeaponsByTeam[teamIndex] = new List<int>(weaponDatabase.GetAllWeaponIds());

            Debug.Log($"[InitializeTeamData] Initialized all lists for Team {teamIndex}");
        }
    }


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
            PlayerStatuses.Add(new PlayerStatusState(clientId, false, userData.teamIndex));

            Debug.Log($"[OnClientConnected] Player {clientId} added to selections and status lists (Team {userData.teamIndex})");
        }

        // Start the timer only when the first player joins
        if (PlayerStatuses.Count == LobbyManager.Instance.GetJoinedLobby().MaxPlayers)
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
        for (int i = 0; i < PlayerStatuses.Count; i++)
        {
            if (PlayerStatuses[i].ClientId == clientId)
            {
                int teamIndex = PlayerStatuses[i].TeamIndex;

                if (PlayerStatuses[i].IsLockedIn)
                {
                    lockedCharactersByTeam[teamIndex].Remove(PlayerSelections[i].CharacterId);
                    lockedWeaponsByTeam[teamIndex].Remove(PlayerSelections[i].WeaponId);
                }

                availableCharactersByTeam[teamIndex] = GetAvailableCharacters(teamIndex);
                availableWeaponsByTeam[teamIndex] = GetAvailableWeapons(teamIndex);

                // ✅ Remove player from both NetworkLists
                PlayerStatuses.RemoveAt(i);
                PlayerSelections.RemoveAt(i);

                Debug.Log($"[OnClientDisconnected] Removed player {clientId} from selection lists.");

                NotifySelectionChanged();
                return; // ✅ Exit loop after finding and removing the player
            }
        }

        Debug.LogWarning($"[OnClientDisconnected] Player {clientId} was not found in the selection lists!");
    }


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
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestTeamLockDataServerRpc(ServerRpcParams serverRpcParams = default)
    {
        ulong clientId = serverRpcParams.Receive.SenderClientId;

        List<TeamLockData> teamLocks = new List<TeamLockData>();
        foreach (var team in lockedCharactersByTeam.Keys)
        {
            teamLocks.Add(new TeamLockData(team, lockedCharactersByTeam[team], lockedWeaponsByTeam[team]));
        }

        SyncTeamLocksToClientClientRpc(teamLocks.ToArray(), clientId);
    }

    [ClientRpc]
    private void SyncTeamLocksToClientClientRpc(TeamLockData[] teamLocks, ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

        lockedCharactersByTeam.Clear();
        lockedWeaponsByTeam.Clear();
        availableCharactersByTeam.Clear();
        availableWeaponsByTeam.Clear();

        foreach (var teamLock in teamLocks)
        {
            int teamIndex = teamLock.TeamIndex;
            lockedCharactersByTeam[teamIndex] = new HashSet<int>(teamLock.LockedCharacters);
            lockedWeaponsByTeam[teamIndex] = new HashSet<int>(teamLock.LockedWeapons);
            availableCharactersByTeam[teamIndex] = GetAvailableCharacters(teamIndex);
            availableWeaponsByTeam[teamIndex] = GetAvailableWeapons(teamIndex);
        }

        Debug.Log("[SyncTeamLocks] Client received updated team lock data.");
    }

    private void ForceLockInAllPlayers()
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




    /*
     * Determines if a player can lock in their selection.
     * Checks if the player has made valid selections that aren't taken.
     */
    public bool CanLockIn(ulong clientId)
    {
        foreach (var player in PlayerSelections)
        {
            if (player.ClientId != clientId) { continue; }

            int teamIndex = -1;
            foreach (var status in PlayerStatuses)
            {
                if (status.ClientId == clientId)
                {
                    teamIndex = status.TeamIndex;
                    break;
                }
            }

            if (teamIndex == -1)
            {
                Debug.LogError($"[CanLockIn] Failed to find team for Client {clientId}!");
                return false;
            }

            if (characterDatabase == null || weaponDatabase == null)
            {
                Debug.LogError("CharacterDatabase or WeaponDatabase is not set in SelectionNetwork!");
                return false;
            }

            if (!characterDatabase.IsValidCharacterId(player.CharacterId))
            {
                Debug.LogWarning($"CharacterId {player.CharacterId} is INVALID.");
                return false;
            }

            if (!weaponDatabase.IsValidWeaponId(player.WeaponId))
            {
                Debug.LogWarning($"WeaponId {player.WeaponId} is INVALID.");
                return false;
            }

            // ✅ Check only within the same team
            bool isCharacterValid = player.CharacterId != -1 && !IsCharacterTaken(player.CharacterId, teamIndex);
            bool isWeaponValid = player.WeaponId != -1 && !IsWeaponTaken(player.WeaponId, teamIndex);

            Debug.Log($"[CanLockIn] Client {clientId} → IsCharacterValid: {isCharacterValid}, IsWeaponValid: {isWeaponValid}");

            return isCharacterValid && isWeaponValid;
        }

        Debug.LogWarning($"[CanLockIn] Client {clientId} NOT found in PlayerSelections!");
        return false;
    }



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

        if (index == -1)
        {
            Debug.LogError($"[LockInSelection] Player {clientId} not found in selection/status lists!");
            return;
        }

        if (!CanLockIn(clientId))
        {
            Debug.LogError($"[LockInSelection] Client {clientId} has not selected both character & weapon!");
            return;
        }


        var updatedSelection = PlayerSelections[index];
        var updatedStatus = PlayerStatuses[index];
        int teamIndex = updatedStatus.TeamIndex;

        if (!lockedCharactersByTeam.ContainsKey(teamIndex))
        {
            Debug.LogWarning($"[LockInSelection] Team {teamIndex} not found in lockedCharactersByTeam! Initializing...");
            lockedCharactersByTeam[teamIndex] = new HashSet<int>();
        }

        if (!lockedWeaponsByTeam.ContainsKey(teamIndex))
        {
            Debug.LogWarning($"[LockInSelection] Team {teamIndex} not found in lockedWeaponsByTeam! Initializing...");
            lockedWeaponsByTeam[teamIndex] = new HashSet<int>();
        }

        if (!availableCharactersByTeam.ContainsKey(teamIndex))
        {
            Debug.LogWarning($"[LockInSelection] Team {teamIndex} not found in availableCharactersByTeam! Initializing...");
            availableCharactersByTeam[teamIndex] = new List<int>(characterDatabase.GetAllCharacterIds());
        }

        if (!availableWeaponsByTeam.ContainsKey(teamIndex))
        {
            Debug.LogWarning($"[LockInSelection] Team {teamIndex} not found in availableWeaponsByTeam! Initializing...");
            availableWeaponsByTeam[teamIndex] = new List<int>(weaponDatabase.GetAllWeaponIds());
        }

        Debug.Log($"[LockInSelectionServerRpc] Attempting to access teamIndex: {teamIndex}");

        // ✅ Retrieve team-based locks and available selections
        HashSet<int> teamLockedCharacters = lockedCharactersByTeam[teamIndex];
        HashSet<int> teamLockedWeapons = lockedWeaponsByTeam[teamIndex];
        List<int> teamAvailableCharacters = availableCharactersByTeam[teamIndex];
        List<int> teamAvailableWeapons = availableWeaponsByTeam[teamIndex];

        bool selectionInvalid = false;

        // ✅ Check team-based character locks
        if (teamLockedCharacters.Contains(updatedSelection.CharacterId))
        {
            Debug.LogWarning($"[LockInSelection] Player {clientId} tried to lock in Character {updatedSelection.CharacterId}, but it's already locked by a teammate!");
            updatedSelection.CharacterId = -1;
            selectionInvalid = true;
        }

        // ✅ Check team-based weapon locks
        if (teamLockedWeapons.Contains(updatedSelection.WeaponId))
        {
            Debug.LogWarning($"[LockInSelection] Player {clientId} tried to lock in Weapon {updatedSelection.WeaponId}, but it's already locked by a teammate!");
            updatedSelection.WeaponId = -1;
            selectionInvalid = true;
        }

        if (selectionInvalid)
        {
            Debug.LogError($"[LockInSelection] Player {clientId} has invalid selections and must reselect before locking in.");
            PlayerSelections[index] = updatedSelection;
            return;
        }

        // ✅ Add to team-based locks
        teamLockedCharacters.Add(updatedSelection.CharacterId);
        teamLockedWeapons.Add(updatedSelection.WeaponId);

        // ✅ Remove from team's available lists
        if (teamAvailableCharacters.Contains(updatedSelection.CharacterId))
        {
            teamAvailableCharacters.Remove(updatedSelection.CharacterId);
            Debug.Log($"[Character Removed] Character {updatedSelection.CharacterId} removed from Team {teamIndex}'s available characters.");
        }

        if (teamAvailableWeapons.Contains(updatedSelection.WeaponId))
        {
            teamAvailableWeapons.Remove(updatedSelection.WeaponId);
            Debug.Log($"[Weapon Removed] Weapon {updatedSelection.WeaponId} removed from Team {teamIndex}'s available weapons.");
        }

        // ✅ Finalize Lock-In
        updatedStatus.IsLockedIn = true;
        PlayerSelections[index] = updatedSelection;
        PlayerStatuses[index] = updatedStatus;

        Debug.Log($"[LockInSelection] Player {clientId} locked in with " +
                  $"CharacterId: {updatedSelection.CharacterId}, WeaponId: {updatedSelection.WeaponId}");

        // ✅ Sync with the server
        HostSingleton.Instance.GameManager.NetworkServer.SetCharacter(clientId, updatedSelection.CharacterId);
        HostSingleton.Instance.GameManager.NetworkServer.SetWeapon(clientId, updatedSelection.WeaponId);

        // ✅ Check teammates to reset invalid selections (ONLY for the same team)
        for (int i = 0; i < PlayerSelections.Count; i++)
        {
            if (i == index) continue; // Skip the locked-in player

            var otherSelection = PlayerSelections[i];
            var otherStatus = PlayerStatuses[i];

            if (otherStatus.TeamIndex != teamIndex) continue; // ✅ Only check players on the same team
            if (otherStatus.IsLockedIn) continue; // ✅ Skip already locked-in players

            if (otherSelection.CharacterId != -1 && teamLockedCharacters.Contains(otherSelection.CharacterId))
            {
                Debug.Log($"[Conflict Reset] Player {otherSelection.ClientId} (Team {teamIndex}) had Character {otherSelection.CharacterId}, but it's now locked. Resetting...");
                otherSelection.CharacterId = -1;
            }

            if (otherSelection.WeaponId != -1 && teamLockedWeapons.Contains(otherSelection.WeaponId))
            {
                Debug.Log($"[Conflict Reset] Player {otherSelection.ClientId} (Team {teamIndex}) had Weapon {otherSelection.WeaponId}, but it's now locked. Resetting...");
                otherSelection.WeaponId = -1;
            }

            PlayerSelections[i] = otherSelection;
        }

        NotifySelectionChanged();
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
        return lockedCharactersByTeam[teamIndex].Contains(characterId);
    }

    public bool IsWeaponTaken(int weaponId, int teamIndex)
    {
        return lockedWeaponsByTeam[teamIndex].Contains(weaponId);
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
        List<int> availableCharacters = new List<int>();
        foreach (int characterId in characterDatabase.GetAllCharacterIds())
        {
            if (!lockedCharactersByTeam.ContainsKey(teamIndex) || !lockedCharactersByTeam[teamIndex].Contains(characterId))
            {
                availableCharacters.Add(characterId);
            }
        }
        return availableCharacters;
    }

    private List<int> GetAvailableWeapons(int teamIndex)
    {
        List<int> availableWeapons = new List<int>();
        foreach (int weaponId in weaponDatabase.GetAllWeaponIds())
        {
            if (!lockedWeaponsByTeam.ContainsKey(teamIndex) || !lockedWeaponsByTeam[teamIndex].Contains(weaponId))
            {
                availableWeapons.Add(weaponId);
            }
        }
        return availableWeapons;
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
}
