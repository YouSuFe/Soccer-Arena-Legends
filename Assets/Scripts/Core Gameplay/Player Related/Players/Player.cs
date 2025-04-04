using System;
using System.Collections;
using QFSW.QC;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public enum BallAttachmentStatus
{
    Attached,
    WhenShot
}

public enum SkillType
{
    WeaponSkill,
    BallSkill
}

public abstract class PlayerAbstract : Entity, IPositionBasedDamageable
{
    #region Fields

    [Header("Player Stamina")]
    private float _playerStamina; // Backing field for PlayerStamina
    public float PlayerStamina
    {
        get
        {
            return _playerStamina;
        }
        set
        {
            _playerStamina = Mathf.Clamp(value, 0, PlayerMaxStamina); // Clamp between 0 and max stamina

            // Notify listeners (e.g., UIManager) of the stamina change
            OnStaminaChanged?.Invoke(_playerStamina, PlayerMaxStamina);
        }
    }
    public float PlayerMaxStamina { get; set; }

    [Header("Referenes")]
    [SerializeField] private SkillCooldownManager skillCooldownManager;
    public SkillCooldownManager SkillCooldownManager => skillCooldownManager;

    [Header("Input Reader")]
    [field: SerializeField] public InputReader InputReader { get; private set; }

    [Space]

    [Header("Mesh Renderers")]
    [SerializeField] private SkinnedMeshRenderer[] meshes;

    [Header("Player Respawn")]
    [SerializeField] private float playerRespawnDelay = 10f;

    [Header("Game State Settings")]
    //[SerializeField] private GameStateEventChannel gameStateEventChannel;
    [SerializeField] private GameState CurrentGameState = GameState.WaitingForPlayers;

    [Header("Player HUD")]
    private PlayerUIManager playerUIManager;
    public PlayerUIManager PlayerUIManager => playerUIManager;

    [Header("Ball Skill Settings")]
    [Tooltip("Determines when the ball skill will trigger.")]
    [SerializeField] private BallAttachmentStatus ballSkillTrigger = BallAttachmentStatus.Attached;
    public BallAttachmentStatus BallAttachmentStatus { get { return ballSkillTrigger; } set { ballSkillTrigger = value; } }

    [Header("Weapon Settings")]
    [Tooltip("Base melee weapons for the player.")]
    protected BaseWeapon weapon = default;

    [Header("Ball Holder Settings")]
    [Tooltip("Transform that defines the position where the ball is held by the player.")]
    [SerializeField] protected Transform ballHolder;
    public Transform BallHolderPosition { get { return ballHolder; } }

    [Header("Player Camera Settings")]
    [Tooltip("Reference to the player's camera.")]
    [SerializeField] protected Camera playerCamera;
    [SerializeField] protected GameObject eyeTrackingPoint;
    public GameObject EyeTrackingPoint => eyeTrackingPoint;

    [SerializeField] protected GameObject followTrackingPoint;
    public GameObject FollowTrackingPoint => followTrackingPoint;

    [Tooltip("Transform where the player's weapon is held.")]
    [SerializeField] protected Transform weaponHolder;

    [SerializeField] protected Transform projectileHolder;    // Reference to the projectile holder in the Player (can be null)


    [Header("Ball Interaction Settings")]
    [Tooltip("Multiplier applied to ball speed when shooting.")]
    [SerializeField] protected float ballSpeedMultiplier = 2f;
    [Tooltip("Cooldown time before the player can use the ball skill again (in seconds).")]
    [SerializeField] protected float playerSkillCooldownTime = 10f;
    [Tooltip("Ignored layers when shooting the ball.")]
    [SerializeField] protected LayerMask IgnoredAimedLayers;


    public bool CanShoot { get; private set; }
    public bool IsPlayerDeath { get; private set; }

    // Event for when the player dies
    public event Action OnDeath;
    public event Action OnLevelUp;
    public event Action OnTakeBall;
    public event Action OnLoseBall;
    public event Action<SkillType, float> OnSkillCooldownChanged;
    public event Action<float, float> OnStaminaChanged; // Notify listeners when stamina changes

    // The ball that can player interact with
    protected BallReference activeBall;
    public BallReference ActiveBall { get { return activeBall; } }

    private DeathType lastDeathType;
    private ulong lastKillerClientId;

    protected BallOwnershipManager ballOwnershipManager; // Reference to the instance-based BallOwnershipManager

    protected Animator animator;
    public Animator Animator => animator;

    protected Rigidbody ballRigidbody;

    public PlayerController PlayerController { get; private set; }

    protected TargetingSystem TargetingSystem { get; private set; }

    // Not sure to use it for weapons.
    //protected NetworkVariable<NetworkObjectReference> networkedWeapon = new NetworkVariable<NetworkObjectReference>();

    #endregion

    #region Behaviour Methods

    protected override void Awake()
    {
        base.Awake();
        playerUIManager = GetComponentInChildren<PlayerUIManager>();

        if (playerUIManager == null)
        {
            Debug.LogWarning("Player UI Manager is null");
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        TargetingSystem = new TargetingSystem();

        PlayerController = GetComponent<PlayerController>();

        SubscribeEvents();

        if(IsServer)
        {
            IsPlayerDeath = false;
        }

        if (IsOwner)
        {
            InputReader.EnableInputActions();

            CanShoot = false;
            activeBall = null;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        UnSubscribeEvents();

        if(IsOwner)
        {
            InputReader.DisableInputActions();

        }

        ResetBallStateOnDespawn();

        DestroyCurrentWeapon();

    }

    protected virtual void Start()
    {
        if (!IsOwner) return;

        PlayerMaxStamina = playerBaseStats.GetStamina();
        _playerStamina = PlayerMaxStamina;

        animator = GetComponentInChildren<Animator>();

        // Initialize the Player UI Manager
        playerUIManager.Initialize(this, Stats, Stats.Mediator);

        if (playerCamera == null)
        {
            playerCamera = Camera.main; // Finds the camera tagged as MainCamera
        }

    }

    public override void Update()
    {
        base.Update();

        if (IsOwner)
        {
            //Debug.Log($"Gameobject {gameObject} {NetworkManager.Singleton.LocalClientId} Health: {Health.Value}, Strength: {Strength.Value}, Speed: {Speed.Value}\n{Stats}");
        }
    }

    private void SubscribeEvents()
    {
        if (!IsOwner) return;

        InputReader.OnPlayerSkillUsed += InputManager_OnPlayerBallSkillUsed;
        InputReader.OnWeaponSkillUsed += InputManager_OnWeaponSkillUsed;
        InputReader.OnRegularAttackPerformed += InputManager_OnRegularAttack;
        InputReader.OnHeavyAttackPerformed += InputManager_OnHeavyAttack;
        InputReader.OnProjectilePerformed += InputManager_OnProjectile;


        InputReader.OnStatisticTabOpen += InputReader_OnStatisticTabVisibility;
        InputReader.OnStatisticTabClose += InputReader_OnStatisticTabVisibility;

        if(MultiplayerGameStateManager.Instance != null)
        {
            Debug.Log($"Multiplayer Server Game Manager is not null and subscribing the event");
            MultiplayerGameStateManager.Instance.OnGameStateChanged += GameStateManager_OnGameStateChanged;
        }
    }



    private void UnSubscribeEvents()
    {
        if (!IsOwner) return;

        InputReader.OnPlayerSkillUsed -= InputManager_OnPlayerBallSkillUsed;
        InputReader.OnWeaponSkillUsed -= InputManager_OnWeaponSkillUsed;
        InputReader.OnRegularAttackPerformed -= InputManager_OnRegularAttack;
        InputReader.OnHeavyAttackPerformed -= InputManager_OnHeavyAttack;
        InputReader.OnProjectilePerformed -= InputManager_OnProjectile;

        InputReader.OnStatisticTabOpen -= InputReader_OnStatisticTabVisibility;
        InputReader.OnStatisticTabClose -= InputReader_OnStatisticTabVisibility;

        if (MultiplayerGameStateManager.Instance != null)
        {
            Debug.Log($"Multiplayer Server Game Manager is not null and unsubscribing the event");
            MultiplayerGameStateManager.Instance.OnGameStateChanged -= GameStateManager_OnGameStateChanged;
        }
    }

    #endregion

    #region Input Methods

    private void InputManager_OnPlayerBallSkillUsed()
    {
        Debug.Log("InputManager_OnPlayerSkillUsed triggered.");

        if (!IsOwner || !IsPlayerAllowedToMoveOrAction()) return;

        if (activeBall == null)
        {
            Debug.LogWarning("Ball skill failed: No active ball.");
            return;
        }

        if (skillCooldownManager.TryUsePlayerSkill())
        {
            PerformBallSkillServerRpc();
        }
        else
        {
            Debug.LogWarning("Ball skill is on cooldown.");
        }
    }

    private void InputManager_OnWeaponSkillUsed()
    {

        if (!IsPlayerAllowedToMoveOrAction()) return;

        Debug.Log("Player Weapon Skill is Called from " + name);
        PerformPlayerWeaponSkill();
    }

    private void InputManager_OnRegularAttack()
    {
        if (!IsPlayerAllowedToMove()) return;

        PerformRegularAttack();
    }

    private void InputManager_OnHeavyAttack()
    {
        if (!IsPlayerAllowedToMove()) return;

        PerformHeavyAttack();
    }

    private void InputManager_OnProjectile()
    {
        if (!IsPlayerAllowedToMoveOrAction()) return;

        Debug.Log("Projectile is called from " + name + "if it can shoot : " + CanShoot);
        if(CanShoot)
        {
            Debug.Log("Shooting the Projectile from " + name);
            ShootBall();
        }
    }

    private void InputReader_OnStatisticTabVisibility(bool value)
    {
        ScoreboardManager.Instance.AdjustScoreboardVisibility(value);
    }

    private void GameStateManager_OnGameStateChanged(GameState newState)
    {
        this.CurrentGameState = newState;
    }

    #endregion

    #region Ball Ownership Methods


    public void SetBallOwnershipManagerAndEvents(BallOwnershipManager manager)
    {
        ballOwnershipManager = manager;

        // Subscribe to the events here if they weren’t added in OnEnable
        if (ballOwnershipManager != null)
        {
            Debug.Log($"{name} assigned BallOwnershipManager dynamically.");
            ballOwnershipManager.OnBallShot += BallOwnershipManager_OnBallShot;
            ballOwnershipManager.OnBallPickedUp += BallOwnershipManager_OnBallPickedUp;
        }

        else
        {
            Debug.LogError("Manager not assigned BallOwnershipManager dynamically.");
        }
    }

    protected virtual void BallOwnershipManager_OnBallPickedUp(PlayerAbstract picker)
    {
        if (picker != this)
        {
            activeBall = null;
            Debug.LogWarning($"{this.name} lost ball ownership as {picker.name} picked up the ball.");
        }
        else
        {
            Debug.LogWarning($"{this.name} picked up the ball and is now the owner.");
        }
    }

    private void BallOwnershipManager_OnBallShot(PlayerAbstract shooter)
    {
        if (shooter == this)
        {
            Debug.LogWarning($"{this.name} shot the ball and can now use their skill.");
        }
    }

    #endregion

    #region Reusable Methods

    [ServerRpc]
    private void PerformBallSkillServerRpc()
    {
        if (activeBall == null)
        {
            Debug.LogWarning("[Server] Cannot use ball skill - no ball attached.");
            return;
        }

        if (!skillCooldownManager.TryUsePlayerSkill())
        {
            Debug.LogWarning("[Server] Ball skill is on cooldown.");
            return;
        }

        bool canUseSkill = PerformBallSkill(); // your abstracted server-side logic

        if (canUseSkill)
        {
            // Optional: trigger client effects immediately or via logic inside PerformBallSkill()
            //PerformBallSkillEffectsClientRpc(OwnerClientId, skillCooldownManager.GetRemainingCooldown(SkillType.BallSkill));
        }
    }

    [ClientRpc]
    protected void PerformBallSkillEffectsClientRpc(ulong playerClientId, float cooldown)
    {
        // ✅ Play the skill effects for all clients
        PlaySkillEffects();
    }

    protected abstract bool PerformBallSkill();
    protected abstract void PlaySkillEffects();
    protected abstract void PerformRegularAttack();
    protected abstract void PerformHeavyAttack();

    protected void PerformPlayerWeaponSkill()
    {
        if (!IsOwner) return;

        Debug.Log($"[Owner] Player {gameObject.name} is attempting to use WeaponSkill.");

        if (weapon is ISpecialWeaponSkill specialWeaponSkill)
        {
            if (skillCooldownManager.TryUseWeaponSkill())
            {
                specialWeaponSkill.ExecuteSkill();
            }
            else
            {
                Debug.Log("[Owner] Weapon skill is on cooldown.");
            }
        }
    }

    #region Sounds
    protected void PlaySound(SoundData soundData, Vector3 position)
    {
        SoundManager.Instance.CreateSoundBuilder()
            .WithPosition(position)
            .WithRandomPitch()
            .Play(soundData);
    }

    protected void PlaySoundWithParent(SoundData soundData, Vector3 position, Transform parent)
    {
        SoundManager.Instance.CreateSoundBuilder()
            .WithPosition(position)
            .WithParent(parent)
            .WithRandomPitch()
            .Play(soundData);
    }

    protected void PlaySound(SoundData soundData, Vector3 position, float duration)
    {
        SoundManager.Instance.CreateSoundBuilder()
            .WithPosition(position)
            .WithRandomPitch()
            .WithLoopDuration(duration)
            .Play(soundData);
    }

    protected void PlaySoundWithParent(SoundData soundData, Vector3 position, float duration, Transform parent)
    {
        SoundManager.Instance.CreateSoundBuilder()
            .WithPosition(position)
            .WithParent(parent)
            .WithRandomPitch()
            .WithLoopDuration(duration)
            .Play(soundData);
    }

    protected SoundEmitter PlaySoundReturnSoundEmitter(SoundData soundData, Vector3 position)
    {
        return SoundManager.Instance.CreateSoundBuilder()
            .WithPosition(position)
            .WithRandomPitch()
            .Play(soundData);
    }

    protected SoundEmitter PlaySoundReturnSoundEmitter(SoundData soundData, Vector3 position, Transform parent)
    {
        return SoundManager.Instance.CreateSoundBuilder()
            .WithPosition(position)
            .WithParent(parent)
            .WithRandomPitch()
            .Play(soundData);
    }

    protected void StopSound(SoundEmitter soundEmitter)
    {
        if (soundEmitter != null)
        {
            soundEmitter.StopManually();
        }
    }

    #endregion

    protected virtual void ShootBall()
    {
        if (activeBall != null && BallAttachmentStatus != BallAttachmentStatus.WhenShot)
        {
            if (ballRigidbody == null)
            {
                ballRigidbody = activeBall.GetComponent<Rigidbody>();
            }

            // 🔹 Get shoot direction and force
            Vector3 shootDirection = TargetingSystem.GetShotDirection(playerCamera, activeBall.transform.position, IgnoredAimedLayers);

            Debug.DrawRay(activeBall.transform.position, shootDirection * 10f, Color.red, 2f);
            Debug.Log($"Shoot Magnitude: {shootDirection}");

            OnLoseBall?.Invoke();

            // 🔹 Send request to the server (server will validate and apply force)
            ballOwnershipManager.PlayerShootsBallServerRpc(shootDirection);
        }
    }

    /// <summary>
    /// Calculates total force which will be applied on ball for the server to use
    /// </summary>
    public float CalculateThrowForce()
    {
        // 🔹 This method should ONLY be called by the server
        return Mathf.Clamp(Strength.Value * ballSpeedMultiplier, 0f, 50f); // Ensure max force is reasonable
    }

    /// <summary>
    /// Changes shoot states for player, server does not care player shoot states in current implemenatation
    /// However, if I change implementation to only server validates player's shoot states, then I need to add server side change as well.
    /// Currently it only updates on client side with this RPC.
    /// </summary>
    [ClientRpc]
    public void UpdateShootStateClientRpc(ulong shooterClientId)
    {
        if (NetworkManager.Singleton.LocalClientId == shooterClientId) // ✅ Correctly checks if this client is the shooter
        {
            BallAttachmentStatus = BallAttachmentStatus.WhenShot;
            CanShoot = false;
        }
    }

    public bool CheckIfCurrentlyHasBall()
    {
        return ActiveBall != null && CanShoot;
    }


    #region Spawn-Despawn Logic

    /// <summary>
    /// Handles player death flow. Called when health reaches zero.
    /// </summary>
    protected virtual void Die()
    {
        HandleServerDeath();
    }

    /// <summary>
    /// Server-only logic when a player dies.  
    /// Determines whether to respawn instantly (via revive shield) or go into delayed queue.
    /// </summary>
    public void HandleServerDeath()
    {
        if (!IsServer) return;

        IsPlayerDeath = true;

        OnDeath?.Invoke();

        ResetBallStateOnDespawn();


        // To show UI
        string killerName = string.Empty;
        int killerTeamIndex = -1;

        if (lastKillerClientId != OwnerClientId && lastKillerClientId != ulong.MaxValue)
        {
            var killerData = PlayerSpawnManager.Instance.GetUserData(lastKillerClientId);
            if (killerData != null)
            {
                killerName = killerData.userName;
                killerTeamIndex = killerData.teamIndex;
            }
        }

        string victimName = PlayerSpawnManager.Instance.GetUserData(OwnerClientId)?.userName ?? "???";

        KillFeedManager.Instance?.ReportKill(killerName, victimName, lastDeathType, killerTeamIndex);




        // 👇 CHECK for revive shields
        bool reviveNow = false;

        if (PlayerSpawnManager.Instance.ShouldAutoRevivePlayer(OwnerClientId))
        {
            reviveNow = true;
            PlayerSpawnManager.Instance.RemovePlayerReviveShield(OwnerClientId);
        }
        else if (PlayerSpawnManager.Instance.GetUserData(OwnerClientId) is UserData userData &&
                 PlayerSpawnManager.Instance.ShouldAutoReviveTeam(userData.teamIndex))
        {
            reviveNow = true;
        }

        if (reviveNow)
        {
            // 🟢 Revive immediately
            PlayerSpawnManager.Instance.RespawnPlayer(OwnerClientId);
            return;
        }




        // 🔴 Otherwise go into respawn queue
        PlayerSpawnManager.Instance.QueueRespawn(OwnerClientId, playerRespawnDelay);


        // 🔄 Disable full player functionality
        SetPlayerSimulationState(false);

        // 🔄 Notify client to disable controls
        NotifyOwnerOfDeathClientRpc(OwnerClientId, playerRespawnDelay);

        Debug.Log("[Server] Player is dead due to death.");

        // ❌ Do NOT destroy the object
        NetworkObject.Despawn(false);
    }

    /// <summary>
    /// Client-side reaction to death: disables controls and UI
    /// </summary>
    [ClientRpc]
    private void NotifyOwnerOfDeathClientRpc(ulong clientId, float respawnDelay)
    {
        if (NetworkManager.Singleton.LocalClientId != clientId)
        {
            Debug.Log($"Server]: client id {clientId} is not Local client id {NetworkManager.Singleton.LocalClientId}");
            return;
        }

        SetPlayerSimulationState(false);

        CanShoot = false;

        activeBall = null;
           
        playerUIManager?.StartRespawnCountdown(respawnDelay); // ✅ Show panel + timer

        Debug.Log("[Client] Player input/UI disabled due to death.");
    }

    /// <summary>
    /// Resets the player's server state and prepares it for a respawn.
    /// </summary>
    public void ResetAndRespawnPlayer(Vector3 spawnPosition)
    {
        if (!IsServer) return;

        TeleportToSpawn(spawnPosition);

        IsPlayerDeath = false;

        // 🔄 Re-enable the player fully
        SetPlayerSimulationState(true);

        playerUIManager?.HideDeathScreen();

        // 👇 Tell client to re-enable movement/input/UI
        NotifyClientOfRespawnClientRpc(OwnerClientId);

        Debug.Log($"{name} has been respawned at {spawnPosition}.");
    }

    /// <summary>
    /// Called on the owner client after they are respawned.
    /// </summary>
    [ClientRpc]
    private void NotifyClientOfRespawnClientRpc(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId != clientId) return;

        SetPlayerSimulationState(true);

        playerUIManager?.HideDeathScreen();

        // Animator?.SetTrigger("Respawn");

        Debug.Log("[Client] Player respawned and input re-enabled.");
    }

    /// <summary>
    /// Teleports player to a spawn point without resetting stats.  
    /// Used when player is alive but should be moved (e.g., start of round).
    /// </summary>
    public void TeleportToSpawn(Vector3 newPosition)
    {
        if (!IsServer) return;

        Debug.Log($"[Server] TeleportToSpawn() called for {OwnerClientId} to {newPosition}");

        // 👇 Client does the visual/transform teleport
        TeleportClientRpc(newPosition);

        // 👇 Server handles stat resets
        ResetStatsForNextRound(false);

        Debug.Log($"[Server] Reset stats and requested client to teleport {name} to {newPosition}");
    }

    [ClientRpc]
    private void TeleportClientRpc(Vector3 newPosition)
    {
        Debug.Log($"[ClientRpc] TeleportClientRpc called on client {NetworkManager.Singleton.LocalClientId}, IsOwner: {IsOwner}");

        if (!IsOwner)
        {
            Debug.Log($"[Client] Skipping teleport. This client does not own this player.");
            return;
        }

        StartCoroutine(DelayedTeleport(newPosition));
    }
    
    private IEnumerator DelayedTeleport(Vector3 newPosition)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.None;
        }

        yield return null;

        var netTransform = GetComponent<OwnerNetworkTransform>();
        if (netTransform != null)
        {
            netTransform.Teleport(newPosition, Quaternion.identity, transform.localScale);
            Physics.SyncTransforms(); // ✅ Force Unity to apply changes to physics system
            Debug.Log($"[Client] Delayed teleport with Physics.SyncTransforms() to {newPosition}");
        }

        yield return null; // wait another frame

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    private void ResetStatsForNextRound(bool isSpawnCall)
    {
        Stats.ResetStatsToBaseValues();
        Debug.Log($"[Reset Stats For Next Round] stats being resetted to {Stats.GetBaseStat(StatType.Health)} {Stats.GetBaseStat(StatType.Strength)} {Stats.GetBaseStat(StatType.Speed)}");
        Stats.Mediator.RemoveAllModifiers();

        Health.Value = Stats.GetBaseStat(StatType.Health);
        Strength.Value = Stats.GetBaseStat(StatType.Strength);
        Speed.Value = Stats.GetBaseStat(StatType.Speed);

        if(isSpawnCall)
        {
            PlayerStamina = PlayerMaxStamina;
        }
        else
        {
            PlayerStamina = Math.Clamp(PlayerStamina + 10, PlayerStamina, PlayerMaxStamina);
        }
    }

    private void ResetBallStateOnDespawn()
    {
        if (activeBall != null)
        {
            if (IsServer)
            {
                ballOwnershipManager?.NetworkObject?.TryRemoveParent();
                ballOwnershipManager?.ResetCurrentOwnershipId();
            }

            activeBall = null;
            OnLoseBall?.Invoke(); // Notify that we lost the ball
        }
    }

    private void SetPlayerSimulationState(bool isEnabled)
    {
        Debug.Log($"[SetPlayerSimulationState] isEnabled = {isEnabled} on object {gameObject.name}");

        // 🔹 Server-side logic
        if (IsServer)
        {
            Rigidbody rb = PlayerController?.Rigidbody;
            if (rb != null)
            {
                rb.isKinematic = !isEnabled;
                Debug.Log($"[Server] Rigidbody set to isKinematic = {!isEnabled}");
            }

            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                col.enabled = isEnabled;
                Debug.Log($"[Server] Collider enabled = {isEnabled}");
            }

            if (PlayerController != null)
            {
                PlayerController.enabled = isEnabled;
                Debug.Log($"[Server] PlayerController enabled = {isEnabled}");
            }

            if (!isEnabled)
            {
                DestroyCurrentWeapon(); // Only destroy weapon when disabling simulation
                Debug.Log($"[Server] Destroyed current weapon on death.");
            }
        }

        // 🔹 Client-side logic (owner only)
        if (IsOwner)
        {
            if (InputReader != null)
            {
                if (isEnabled)
                {
                    InputReader.EnableInputActions();
                    Debug.Log("[Client] Input enabled.");
                }
                else
                {
                    InputReader.DisableInputActions();
                    Debug.Log("[Client] Input disabled.");
                }
            }
        }

        // 🔹 Shared (visuals, animator)
        if (animator != null)
        {
            animator.enabled = isEnabled;
            Debug.Log($"Animator enabled = {isEnabled}");
        }

        foreach (var mesh in meshes)
        {
            mesh.enabled = isEnabled;
            Debug.Log($"Mesh {mesh.name} enabled = {isEnabled}");
        }
    }


    // 🔄 RPC to sync respawn UI updates (called from server)
    [ClientRpc]
    public void UpdateRespawnTimerClientRpc(float newTime)
    {
        if (!IsOwner) return;
        playerUIManager?.StartRespawnCountdown(newTime);
    }

    #endregion

    #endregion

    #region IDamagable Methods

    // It is for IPositionBasedDamageable damage dealers
    public void TakeDamage(int amount, Vector3 attackerPosition, DeathType type, ulong attackerClientId = ulong.MaxValue)
    {
        if (!IsServer || IsPlayerDeath) return; // Ensure only the server executes this

        int damageMultiplier = DamageUtils.CalculateBackstabMultiplier(transform, attackerPosition);
        amount *= damageMultiplier;

        DamageHandler damageHandler = new DamageHandler(Stats, Stats.Mediator);
        damageHandler.DealDamage(amount);

        // After dealing damage, retrieve the current health to see the adjusted value
        Health.Value = Stats.GetCurrentStat(StatType.Health);

        // Log the remaining health
        Debug.Log($"Enemy took {amount} damage. Remaining Health: {Health.Value}");

        // Check if health is zero or less
        if (Health.Value <= 0)
        {
            lastKillerClientId = attackerClientId;
            lastDeathType = type;
            Die(); // Trigger death if health is zero or below
        }
    }

    // It is for IDamageable damage dealers
    public void TakeDamage(int amount, DeathType type, ulong attackerClientId = ulong.MaxValue)
    {
        if (!IsServer || IsPlayerDeath) return; // Ensure only the server executes this

        DamageHandler damageHandler = new DamageHandler(Stats, Stats.Mediator);
        damageHandler.DealDamage(amount);

        // After dealing damage, retrieve the current health to see the adjusted value
        Health.Value = Stats.GetCurrentStat(StatType.Health);

        // Log the remaining health
        Debug.Log($"{name} took {amount} damage. Remaining Health: {Health.Value}");

        // Check if health is zero or less
        if (Health.Value <= 0)
        {
            lastKillerClientId = attackerClientId;
            lastDeathType = type;
            // ToDo: Make it Client RPC for player to react it. 
            Die(); // Trigger death if health is zero or below
        }
    }

    #endregion

    #region Main Methods
    public void RegisterBall(BallReference ball)
    {
        if (!IsOwner) return;

        activeBall = ball;

        BallAttachmentStatus = BallAttachmentStatus.Attached;

        if (ballOwnershipManager == null)
        {
            Debug.LogError("Ballownership is null ");
        }

        Debug.LogWarning($"Inside a player {name} we are calling Take Ball invoke");
        OnTakeBall?.Invoke();

        CanShoot = true;
    }

    public GameState GetPlayerCurrentGameState()
    {
        return CurrentGameState;
    }

    public float GetBallSkillCooldownTime()
    {
        return playerSkillCooldownTime;
    }

    public bool IsPlayerAllowedToMove()
    {
        return (CurrentGameState == GameState.InGame || CurrentGameState == GameState.WaitingForPlayers || CurrentGameState == GameState.PostGame);
    }

    public bool IsPlayerAllowedToMoveOrAction()
    {
        return (CurrentGameState == GameState.InGame || CurrentGameState == GameState.WaitingForPlayers);
    }

    public void DistributeStatPoint(StatType statType)
    {
        if (playerBaseStats.AllocatePoint(statType))
        {
            // Optionally, trigger an update to the Stats class if needed
            Debug.Log($"Point allocated to {statType}. Current Stats: {Stats}");
        }
    }

    public void CreateAndAssignWeapon(int weaponId)
    {
        if (!IsServer) return;

        // Get Weapon
        Weapon selectedWeapon = PlayerSpawnManager.Instance.WeaponDatabase.GetWeaponById(weaponId);
        if (selectedWeapon == null) return;

        // Spawn Weapon on Server
        NetworkObject weaponPrefab = selectedWeapon.GameplayPrefab;
        NetworkObject spawnedWeapon = Instantiate(weaponPrefab, weaponHolder.position, weaponHolder.rotation);
        spawnedWeapon.SpawnWithOwnership(OwnerClientId);
        spawnedWeapon.TrySetParent(this.transform, false);

        // Sync Weapon with Clients
        EquipWeaponClientRpc(spawnedWeapon.NetworkObjectId, NetworkObjectId);
    }

    [ClientRpc]
    private void EquipWeaponClientRpc(ulong weaponNetworkId, ulong playerNetworkId)
    {
        Debug.Log($"[EquipWeaponClientRpc] Called on Client: {NetworkManager.Singleton.LocalClientId}");

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(weaponNetworkId, out NetworkObject weaponNetworkObject))
        {
            Debug.LogError("Failed to find spawned weapon.");
            return;
        }

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkId, out NetworkObject playerNetworkObject))
        {
            Debug.LogError("Failed to find player.");
            return;
        }

        // Get Player reference
        PlayerAbstract player = playerNetworkObject.GetComponent<PlayerAbstract>();

        if (player == null)
        {
            Debug.LogError($"[Client {NetworkManager.Singleton.LocalClientId}] Player reference is null! Player Network ID: {playerNetworkId}");
            return;
        }

        Debug.Log($"[Client {NetworkManager.Singleton.LocalClientId}] Found Player: {player.name}, OwnerClientId: {playerNetworkObject.OwnerClientId}");

        // Automatically detect correct weapon class (e.g., AuraWeapon, SwordWeapon)
        BaseWeapon weapon = weaponNetworkObject.GetComponent<BaseWeapon>();
        if (weapon == null)
        {
            Debug.LogError($"Weapon does not inherit from BaseWeapon! Check prefab settings.");
            return;
        }

        // 🧠 Initialize Weapon
        // player.playerCamera is null for the first call, So, give Camera.main instead
        weapon.Initialize(player.weaponHolder, player.playerCamera ?? Camera.main, player.projectileHolder);
        weapon.SetCurrentPlayer(player);
        weapon.SetPlayerStats(player.Stats);
        weapon.transform.position = player.weaponHolder.position;
        weapon.transform.rotation = player.weaponHolder.rotation;

        // ✅ Set to player's weapon field
        player.weapon = weapon;

        // ✅ Initialize the skill cooldown manager with the weapon
        if (player.SkillCooldownManager != null)
        {
            player.SkillCooldownManager.Initialize(player, weapon);

            // ✅ Forward cooldown event to Player for UI & other systems
            player.SkillCooldownManager.OnSkillCooldownChanged += (type, time) =>
            {
                player.OnSkillCooldownChanged?.Invoke(type, time);
            };

            Debug.Log("[SkillCooldownManager] Initialized with weapon inside EquipWeaponClientRpc");
        }
        else
        {
            Debug.LogError("SkillCooldownManager is not assigned on player!");
        }
    }

    public void DestroyCurrentWeapon()
    {
        if (!IsServer) return;

        if (weapon != null && weapon.NetworkObject != null)
        {
            weapon.NetworkObject.Despawn(true);
            weapon = null;
        }
    }

    #endregion
}
