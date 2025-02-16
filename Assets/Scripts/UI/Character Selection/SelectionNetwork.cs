using System;
using System.Collections;
using System.Collections.Generic;
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

    public NetworkList<PlayerSelectState> Players { get; private set; }

    public Action OnSelectionStateChanged { get; internal set; }

    [SerializeField] private CharacterDatabase characterDatabase;
    [SerializeField] private WeaponDatabase weaponDatabase;

    // If we want to start the game when max player is reached, we can use this.
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private float selectionTimeAmount = 90f;

    private HashSet<int> lockedCharacters = new HashSet<int>();
    private HashSet<int> lockedWeapons = new HashSet<int>();
    private HashSet<int> availableCharactersCache = new HashSet<int>();
    private HashSet<int> availableWeaponsCache = new HashSet<int>();

    private NetworkVariable<float> selectionTimer = new NetworkVariable<float>();
    private Coroutine timerCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("Duplicate SelectionNetwork found! Destroying this instance.");
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Players = new NetworkList<PlayerSelectState>();

    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            selectionTimer.Value = selectionTimeAmount;

            RefreshAvailableSelections();
            lockedCharacters.Clear();
            lockedWeapons.Clear();

            Debug.Log("Inside Selection Network Is Server " + IsServer);
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
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

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client connected: {clientId}");
        Players.Add(new PlayerSelectState(clientId));

        // Start the timer only when the first player joins
        if (Players.Count == 1)
        {
            StartSelectionTimer();
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId == clientId)
            {
                if (Players[i].IsLockedIn)
                {
                    // ✅ Remove from locked lists
                    lockedWeapons.Remove(Players[i].WeaponId);
                    lockedCharacters.Remove(Players[i].CharacterId);
                }

                Players.RemoveAt(i);
                break;
            }
        }

        NotifySelectionChanged();
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

    // Force All Players to Lock-In with Their Current Selections
    private void ForceLockInAllPlayers()
    {
        List<int> availableCharacters = GetAvailableCharacters();
        List<int> availableWeapons = GetAvailableWeapons();

        Debug.Log($"[ForceLockIn] Starting lock-in process for {Players.Count} players.");

        for (int i = 0; i < Players.Count; i++)
        {
            var updatedPlayer = Players[i];

            if (!updatedPlayer.IsLockedIn)
            {
                // 🚨 Ensure uniqueness before assigning final choices
                if (lockedCharacters.Contains(updatedPlayer.CharacterId))
                {
                    Debug.Log($"[Conflict] Player {updatedPlayer.ClientId} selected Character {updatedPlayer.CharacterId}, but it's already taken! Resetting...");
                    updatedPlayer.CharacterId = -1;
                }
                else if (updatedPlayer.CharacterId != -1)
                {
                    lockedCharacters.Add(updatedPlayer.CharacterId);
                }

                if (lockedWeapons.Contains(updatedPlayer.WeaponId))
                {
                    Debug.Log($"[Conflict] Player {updatedPlayer.ClientId} selected Weapon {updatedPlayer.WeaponId}, but it's already taken! Resetting...");
                    updatedPlayer.WeaponId = -1;
                }
                else if (updatedPlayer.WeaponId != -1)
                {
                    lockedWeapons.Add(updatedPlayer.WeaponId);
                }

                // ✅ Assign random choices if selections are invalid
                if (updatedPlayer.CharacterId == -1 && availableCharacters.Count > 0)
                {
                    int randomIndex = UnityEngine.Random.Range(0, availableCharacters.Count);
                    updatedPlayer.CharacterId = availableCharacters[randomIndex];
                    availableCharacters.RemoveAt(randomIndex);
                    Debug.Log($"[Random Assign] Player {updatedPlayer.ClientId} gets Character {updatedPlayer.CharacterId}.");
                    lockedCharacters.Add(updatedPlayer.CharacterId);
                }

                if (updatedPlayer.WeaponId == -1 && availableWeapons.Count > 0)
                {
                    int randomIndex = UnityEngine.Random.Range(0, availableWeapons.Count);
                    updatedPlayer.WeaponId = availableWeapons[randomIndex];
                    availableWeapons.RemoveAt(randomIndex);
                    Debug.Log($"[Random Assign] Player {updatedPlayer.ClientId} gets Weapon {updatedPlayer.WeaponId}.");
                    lockedWeapons.Add(updatedPlayer.WeaponId);
                }

                updatedPlayer.IsLockedIn = true;
                Players[i] = updatedPlayer;

                Debug.Log($"[Final Lock-In] Player {Players[i].ClientId} locked in with " +
                          $"Character {Players[i].CharacterId}, Weapon {Players[i].WeaponId}.");

                // ✅ Finalize selection in NetworkServer
                HostSingleton.Instance.GameManager.NetworkServer.SetCharacter(Players[i].ClientId, Players[i].CharacterId);
                HostSingleton.Instance.GameManager.NetworkServer.SetWeapon(Players[i].ClientId, Players[i].WeaponId);
            }
        }

        NotifySelectionChanged();
        Debug.Log("[ForceLockIn] Lock-in process complete.");
    }


    public bool CanLockIn(ulong clientId)
    {
        foreach (var player in Players)
        {
            if (player.ClientId != clientId) { continue; }

            if (characterDatabase == null || weaponDatabase == null)
            {
                Debug.LogError("CharacterDatabase or WeaponDatabase is not set in SelectionNetwork!");
                return false;
            }

            if (!characterDatabase.IsValidCharacterId(player.CharacterId)) { return false; }
            if (!weaponDatabase.IsValidWeaponId(player.WeaponId)) { return false; }

           return player.CharacterId != -1 &&
                player.WeaponId != -1 &&
                !IsCharacterTaken(player.CharacterId,false) &&
                !IsWeaponTaken(player.WeaponId, false); ;
        }

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

        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId != clientId) { continue; }

            if (!CanLockIn(clientId))
            {
                Debug.LogError($"[LockInSelection] Client {clientId} has not selected both character & weapon!");
                return;
            }

            var updatedPlayer = Players[i];

            // ✅ If the character or weapon is taken, reset selection and prevent lock-in
            bool selectionInvalid = false;

            if (lockedCharacters.Contains(updatedPlayer.CharacterId))
            {
                Debug.LogWarning($"[LockInSelection] Player {clientId} tried to lock in Character {updatedPlayer.CharacterId}, but it's already locked!");
                updatedPlayer.CharacterId = -1; // Reset selection
                selectionInvalid = true;
            }

            if (lockedWeapons.Contains(updatedPlayer.WeaponId))
            {
                Debug.LogWarning($"[LockInSelection] Player {clientId} tried to lock in Weapon {updatedPlayer.WeaponId}, but it's already locked!");
                updatedPlayer.WeaponId = -1; // Reset selection
                selectionInvalid = true;
            }

            // ✅ If selection was invalid, prevent the player from locking in
            if (selectionInvalid)
            {
                Debug.LogError($"[LockInSelection] Player {clientId} has invalid selections and must reselect before locking in.");
                Players[i] = updatedPlayer; // Save the reset selections
                return;
            }

            // ✅ If everything is valid, allow the player to lock in
            updatedPlayer.IsLockedIn = true;
            Players[i] = updatedPlayer;

            // ✅ 🔥 ADD player's selection to the locked HashSet immediately
            lockedCharacters.Add(updatedPlayer.CharacterId);
            lockedWeapons.Add(updatedPlayer.WeaponId);

            Debug.Log($"[LockInSelection] Player {updatedPlayer.ClientId} locked in with " +
                      $"CharacterId: {updatedPlayer.CharacterId}, WeaponId: {updatedPlayer.WeaponId}");

            HostSingleton.Instance.GameManager.NetworkServer.SetCharacter(Players[i].ClientId, Players[i].CharacterId);
            HostSingleton.Instance.GameManager.NetworkServer.SetWeapon(Players[i].ClientId, Players[i].WeaponId);
        }

        // ✅ Reset conflicts for other players dynamically
        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].ClientId == clientId || Players[i].IsLockedIn) { continue; }

            var otherPlayer = Players[i];

            if (otherPlayer.CharacterId != -1 && lockedCharacters.Contains(otherPlayer.CharacterId))
            {
                Debug.Log($"[Conflict Reset] Player {otherPlayer.ClientId} had Character {otherPlayer.CharacterId}, but it's now locked. Resetting...");
                otherPlayer.CharacterId = -1;
            }

            if (otherPlayer.WeaponId != -1 && lockedWeapons.Contains(otherPlayer.WeaponId))
            {
                Debug.Log($"[Conflict Reset] Player {otherPlayer.ClientId} had Weapon {otherPlayer.WeaponId}, but it's now locked. Resetting...");
                otherPlayer.WeaponId = -1;
            }

            Players[i] = otherPlayer;
        }

        NotifySelectionChanged();
    }

    private void RefreshAvailableSelections()
    {
        availableCharactersCache.Clear();
        availableWeaponsCache.Clear();

        foreach (var character in characterDatabase.GetAllCharacters())
        {
            if (!lockedCharacters.Contains(character.Id))
            {
                availableCharactersCache.Add(character.Id);
            }
        }

        foreach (var weapon in weaponDatabase.GetAllWeapons())
        {
            if (!lockedWeapons.Contains(weapon.Id))
            {
                availableWeaponsCache.Add(weapon.Id);
            }
        }
    }


    public bool IsCharacterTaken(int characterId, bool checkAll)
    {
        for (int i = 0; i < Players.Count; i++)
        {
            if (!checkAll && Players[i].ClientId == NetworkManager.Singleton.LocalClientId) { continue; }

            if (Players[i].IsLockedIn && Players[i].CharacterId == characterId)
            {
                return true;
            }
        }

        return false;
    }


    public bool IsWeaponTaken(int weaponId, bool checkAll)
    {
        for (int i = 0; i < Players.Count; i++)
        {
            if (!checkAll && Players[i].ClientId == NetworkManager.Singleton.LocalClientId) { continue; }

            if (Players[i].IsLockedIn && Players[i].WeaponId == weaponId)
            {
                return true;
            }
        }

        return false;
    }

    public bool IsPlayerLockedIn(ulong clientId)
    {
        foreach (var player in Players)
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
        foreach (var player in Players)
        {
            if (player.ClientId == clientId)
            {
                return player.CharacterId != -1 && player.WeaponId != -1;
            }
        }
        return false;
    }

    public PlayerSelectState? GetPlayerState(ulong clientId)
    {
        foreach (var player in Players)
        {
            if (player.ClientId == clientId) return player;
        }
        return null;
    }

    //  Allow UI to Access the Timer Value
    public float GetRemainingTime()
    {
        return selectionTimer.Value;
    }

    // Convert HashSet to List when needed
    private List<int> GetAvailableCharacters()
    {
        return new List<int>(availableCharactersCache);
    }

    private List<int> GetAvailableWeapons()
    {
        return new List<int>(availableWeaponsCache);
    }

    public void NotifySelectionChanged()
    {
        RefreshAvailableSelections();
        OnSelectionStateChanged?.Invoke();
    }
}
