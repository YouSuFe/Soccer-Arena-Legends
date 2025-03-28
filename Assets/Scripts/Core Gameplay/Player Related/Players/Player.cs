using System;
using Unity.Netcode;
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


    [Header("Input Reader")]
    [field: SerializeField] public InputReader InputReader { get; private set; }

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
    [SerializeField] protected float cooldownTime = 10f;
    [Tooltip("Current cooldown timer for ball skills.")]
    [SerializeField] protected float skillCooldownTimer = 0f;
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
        // ToDo: Move this to death function and disconnect function, When Player dies, remove the ball or related objects.
        if (activeBall != null)
        {
            ballOwnershipManager.NetworkObject.TryRemoveParent();
        }
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

            // Update the cooldown timer
            if (skillCooldownTimer > 0)
            {
                skillCooldownTimer -= Time.deltaTime;
            }
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

        if (!IsOwner) return; // Ensure only the owner triggers the skill, for double check

        if (!IsPlayerAllowedToMoveOrAction()) return;

        if (skillCooldownTimer <= 0 && activeBall != null)
        {
            Debug.Log("Requesting server to perform ball skill.");
            PerformBallSkillServerRpc(); // ✅ Call the RPC instead of direct method
        }
        else
        {
            Debug.LogWarning("Skill is on cooldown or no ball available.");
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
    private void PerformBallSkillServerRpc(ServerRpcParams rpcParams = default)
    {
        if (skillCooldownTimer > 0 || activeBall == null)
        {
            return; // Prevent skill usage if cooldown is active or no ball is available
        }

        bool canUseSkill = PerformBallSkill(); // Execute skill logic on the server

        if(canUseSkill)
        {
            // Set the cooldown if player can use it's skill
            skillCooldownTimer = cooldownTime;
        }
    }

    [ClientRpc]
    protected void PerformBallSkillEffectsClientRpc(ulong playerClientId, float cooldown)
    {
        // ✅ Play the skill effects for all clients
        PlaySkillEffects();

        // ✅ Only the player who activated the skill updates their UI
        if (NetworkManager.Singleton.LocalClientId == playerClientId)
        {
            OnSkillCooldownChanged?.Invoke(SkillType.BallSkill, cooldown);
        }
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
            if (specialWeaponSkill.CanExecuteSkill())
            {
                specialWeaponSkill.ExecuteSkill();

                OnSkillCooldownChanged?.Invoke(SkillType.WeaponSkill, specialWeaponSkill.GetCooldownTime());
            }
            else
            {
                Debug.Log("[Owner] Skill is on cooldown.");
            }
        }
        else
        {
            Debug.LogError("[Owner] It is not a SpecialWeaponSkill component!");
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

    protected virtual void Die()
    {
        // ToDo: For now, it works in server, when make this Client Rpc, move this logics into server part.
        IsPlayerDeath = true;

        // Invoke the OnDeath event
        OnDeath?.Invoke();

        // Handle other death-related logic here, like disabling player controls
        Debug.Log($"{gameObject.name} died.");
    }

    #endregion

    #region IDamagable Methods

    // It is for IPositionBasedDamageable damage dealers
    public void TakeDamage(int amount, Vector3 attackerPosition)
    {
        if (!IsServer) return; // Ensure only the server executes this

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
            Die(); // Trigger death if health is zero or below
        }
    }

    // It is for IDamageable damage dealers
    public void TakeDamage(int amount)
    {
        if (!IsServer) return; // Ensure only the server executes this

        DamageHandler damageHandler = new DamageHandler(Stats, Stats.Mediator);
        damageHandler.DealDamage(amount);

        // After dealing damage, retrieve the current health to see the adjusted value
        Health.Value = Stats.GetCurrentStat(StatType.Health);

        // Log the remaining health
        Debug.Log($"Enemy took {amount} damage. Remaining Health: {Health.Value}");

        // Check if health is zero or less
        if (Health.Value <= 0)
        {
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
        Weapon selectedWeapon = PlayerSpawnManager.Instance.weaponDatabase.GetWeaponById(weaponId);
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
        weapon = weaponNetworkObject.GetComponent<BaseWeapon>();
        if (weapon == null)
        {
            Debug.LogError($"Weapon does not inherit from BaseWeapon! Check prefab settings.");
            return;
        }
        else
        {
            weapon.Initialize(weaponHolder, playerCamera ?? Camera.main, projectileHolder);
            // Assign stats to weapon dynamically
            weapon.SetCurrentPlayer(player);
            weapon.SetPlayerStats(player.Stats);
            Debug.Log($"Assigned {weapon.GetType().Name} to player {player.name}");

            weapon.transform.position = weaponHolder.transform.position;
            weapon.transform.rotation = weaponHolder.transform.rotation;
        }
    }

    #endregion


}
