using Unity.Netcode;
using UnityEngine;

public abstract class BaseWeapon : NetworkBehaviour, IWeapon, IDamageDealer, ISpecialWeaponSkill
{
    #region Weapon Configuration

    private const string REGULAR_ATTACK_ANIM_TRIGGER = "regularAttack";
    private const string HEAVY_ATTACK_ANIM_TRIGGER = "heavyAttack";

    [Header("Input Reader")]
    [field: SerializeField] public InputReader InputReader { get; private set; }

    [Header("Weapon Data")]
    [Tooltip("Scriptable object containing data about the weapon such as length and hit radius.")]
    [SerializeField] protected WeaponDataSO weaponData;
    [Tooltip("The player's hand or attachment point for the weapon.")]
    protected Transform weaponHolder;

    [Header("Cooldown Settings")]
    [Tooltip("Cooldown time for executing the weapon's special skill.")]
    [SerializeField] private float skillCooldownTime = 1f; // Cooldown time for the skill

    [Tooltip("Cooldown time between regular attacks.")]
    [SerializeField] private float regularAttackCooldown = 0.3f;
    [Tooltip("Cooldown time between heavy attacks.")]
    [SerializeField] private float heavyAttackCooldown = 0.8f;

    // 🔄 Networked cooldown timer (prevents attack spam)
    private NetworkVariable<float> nextAttackTime = new NetworkVariable<float>(0f);

    [Header("Damage Settings")]
    [Tooltip("Base damage dealt by the weapon.")]
    [SerializeField] private int baseDamage = 10;
    [Tooltip("Multiplier for the damage dealt by heavy attacks.")]
    [SerializeField] private int heavyDamageMultiplier = 3;

    // ToDo: Maybe move the sounds data to weapon data
    [Header("Sounds Settings")]
    [Tooltip("Regular Attacks Sounds Data")]
    [SerializeField] protected SoundData regularAttackSoundData;
    [Tooltip("Heavy Attacks Sounds Data")]
    [SerializeField] protected SoundData heavyAttackSoundData;
    [Tooltip("Regular Attacks Sounds Data On Hit")]
    [SerializeField] protected SoundData regularAttackSoundDataOnHit;
    [Tooltip("Heavy Attacks Sounds Data On Hit")]
    [SerializeField] protected SoundData heavyAttackSoundDataOnHit;

    #endregion

    #region Optional Debugging

    [Header("Debugging")]
    [Tooltip("Optional: Transform used for raycasting to visualize or assist with targeting.")]
    [SerializeField] protected Transform raycastHolder;

    #endregion

    #region Internal References

    protected IProjectileBehaviour projectileBehaviour;

    [Header("Player Reference")]
    [Tooltip("Reference to the player's stats.")]
    protected Stats playerStats;
    protected PlayerAbstract player;
    protected Animator playerAnimator;

    #endregion

    #region MonoBehaviour Methods

    protected virtual void Awake()
    {
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            Debug.Log($"[Netcode] {weaponData?.name} spawned for Player {OwnerClientId}");
        }
    }

    private void Start()
    {
        if (!IsOwner) return;

        playerAnimator = player.Animator;
        InputReader.EnableInputActions();
    }

    protected virtual void Update()
    {
        if (IsOwner && weaponHolder != null)
        {
            UpdateWeaponTransform();
        }
    }

    private void UpdateWeaponTransform()
    {
        if (transform.position != weaponHolder.transform.position)
            transform.position = weaponHolder.transform.position;
        if (transform.rotation != weaponHolder.transform.rotation)
            transform.rotation = weaponHolder.transform.rotation;
    }

    private void OnEnable()
    {
        if (!IsOwner) return;

        InputReader.OnRegularAttackPerformed += InputManager_OnRegularAttack;
        InputReader.OnHeavyAttackPerformed += InputManager_OnHeavyAttack;
    }

    private void OnDisable()
    {
        if (!IsOwner) return;

        InputReader.OnRegularAttackPerformed -= InputManager_OnRegularAttack;
        InputReader.OnHeavyAttackPerformed -= InputManager_OnHeavyAttack;
    }

    #endregion

    #region Abstract Methods

    public abstract void Initialize(Transform weaponHolder, Transform aimTransform, Transform projectileHolder = null);
    protected abstract void HeavyAttackBehaviour();
    protected abstract void RegularAttackBehaviour();

    #endregion

    #region Input Handling

    private void InputManager_OnRegularAttack()
    {
        if (!IsOwner) return;

        Debug.Log($"[Client-{OwnerClientId}] Regular Attack Input recieved.");
        PerformRegularAttack();
    }

    private void InputManager_OnHeavyAttack()
    {
        if (!IsOwner) return;

        Debug.Log($"[Client-{OwnerClientId}] Heavy Attack Input recieved.");
        PerformHeavyAttack();
    }

    #endregion

    #region Damage Calculation and Dealing

    public PlayerAbstract GetCurrentPlayer()
    {
        return this.player;
    }

    public void SetCurrentPlayer(PlayerAbstract player)
    {
        this.player = player;
    }

    // Set the player's stats for the weapon to use during damage calculation
    public void SetPlayerStats(Stats stats)
    {
        if (stats == null)
        {
            Debug.LogError("SetPlayerStats called with a null Stats object!");
        }
        else
        {
            Debug.Log("SetPlayerStats called successfully with a valid Stats object. " + stats.ToString());
            playerStats = stats;
        }
    }

    // Method to calculate total damage based on base damage and player's strength
    public int CalculateDamage()
    {
        int strength = playerStats.GetCurrentStat(StatType.Strength);
        return baseDamage + strength;
    }

    // Returns the calculated damage value
    public int ReturnCalculatedDamage()
    {
        return CalculateDamage();
    }

    #region Networked Dealing Damage Logic

    [ServerRpc]
    private void DealDamageServerRpc(ulong targetNetworkId, int damage, Vector3 attackerPosition, bool isPositionBased)
    {
        Debug.Log($"[Server] Received damage request: Target-{targetNetworkId} | Damage-{damage}");

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(targetNetworkId)) return;

        var targetObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[targetNetworkId];
        var target = targetObject.GetComponent<IDamageable>();

        if (target != null)
        {
            // ❌ Block friendly fire (same team)
            if (!TeamUtils.AreOpponents(OwnerClientId, targetNetworkId))
            {
                Debug.Log($"{name} Skipping damage: target is same team.");
                return;
            }

            Debug.Log($"[Server] Applying {damage} damage to Target-{targetNetworkId}");

            // ✅ Apply damage based on interface type
            if (isPositionBased && target is IPositionBasedDamageable positionBasedDamageable)
            {
                positionBasedDamageable.TakeDamage(damage, attackerPosition, DeathType.Knife, OwnerClientId);
            }
            else
            {
                target.TakeDamage(damage, DeathType.Knife, OwnerClientId);
            }

            // ✅ Efficient: only notify attacker to show floating damage
            ShowFloatingDamageClientRpc(damage, RpcUtils.SendRpcToOwner(this));
        }
    }

    [ClientRpc]
    private void ShowFloatingDamageClientRpc(int damage, ClientRpcParams rpcParams = default)
    {
        if (player?.PlayerUIController != null)
        {
            player.PlayerUIController.ShowFloatingDamage(Vector3.zero, damage);
        }
        else
        {
            Debug.LogError("PlayerUIManager is missing!");
        }
    }

    public void DealDamage(IDamageable target)
    {
        int damage = CalculateDamage();
        Vector3 weaponPositionY = new Vector3(0f, transform.position.y, 0f);
        Vector3 attackerPosition = transform.root.position + weaponPositionY;

        if (target is NetworkBehaviour networkTarget)
        {
            bool isPositionBased = target is IPositionBasedDamageable;

            Debug.Log($"[Client-{OwnerClientId}] Dealing normal damage to Target-{networkTarget.NetworkObjectId}");

            // 🔄 Server will handle validation and feedback
            DealDamageServerRpc(networkTarget.NetworkObjectId, damage, attackerPosition, isPositionBased);
        }
        else
        {
            Debug.LogError("Target is not a networked object and cannot take networked damage.");
        }
    }

    public void DealHeavyDamage(IDamageable target)
    {
        int damage = CalculateDamage() * heavyDamageMultiplier;
        Vector3 weaponPositionY = new Vector3(0f, transform.position.y, 0f);
        Vector3 attackerPosition = transform.root.position + weaponPositionY;

        if (target is NetworkBehaviour networkTarget)
        {
            bool isPositionBased = target is IPositionBasedDamageable;

            Debug.Log($"[Client-{OwnerClientId}] Dealing heavy damage to Target-{networkTarget.NetworkObjectId}");

            DealDamageServerRpc(networkTarget.NetworkObjectId, damage, attackerPosition, isPositionBased);
        }
        else
        {
            Debug.LogError("Target is not a networked object and cannot take networked damage.");
        }
    }
    #endregion

    #endregion


    #region Special Skill Interface Method


    public void ExecuteSkill(Vector3 rayOrigin, Vector3 direction)
    {
        ExecuteSkillServerRpc(rayOrigin, direction);
    }

    public float GetCooldownTime()
    {
        return GetSkillCooldownTime();
    }
    // Special Skill Implementation Method
    protected abstract void ExecuteSpecialSkill(Vector3 rayOrigin, Vector3 direction);
    // Sound and Visual Effects Abstract Method
    protected abstract void PlaySkillEffects();

    [ServerRpc(RequireOwnership = false)]
    private void ExecuteSkillServerRpc(Vector3 rayOrigin, Vector3 direction)
    {
        ExecuteSpecialSkill(rayOrigin, direction); //  New overload with ray

        // Notify clients to play skill effects
        NotifySkillEffectsClientRpc();
    }

    [ClientRpc]
    private void NotifySkillEffectsClientRpc()
    {
        PlaySkillEffects(); // Each weapon handles its own effects
    }

    #endregion


    #region Weapon Actions and Cooldowns

    protected float GetSkillCooldownTime()
    {
        return skillCooldownTime;
    }

    // Check if the regular attack can be performed based on cooldown
    protected bool CanAttack()
    {
        return Time.time >= nextAttackTime.Value;
    }

    #region Networked Attack Methods
    // Perform the regular attack if the cooldown allows
    public virtual void PerformRegularAttack()
    {
        if (CanAttack())
        {
            Debug.Log($"[Client-{OwnerClientId}] Performing regular attack locally.");
            //  Play animation instantly on local attacker
            TriggerAttackAnimation(REGULAR_ATTACK_ANIM_TRIGGER);
            //  Send attack request to the server
            PerformRegularAttackServerRpc();
        }
    }

    [ServerRpc]
    private void PerformRegularAttackServerRpc()
    {
        Debug.Log($"[Server] Received RegularAttackServerRpc from Client-{OwnerClientId}");

        if (Time.time < nextAttackTime.Value) return; // Validate cooldown on server

        // ✅ Server updates nextAttackTime (authoritative control)
        SetAttackCooldown(regularAttackCooldown);

        Debug.Log($"[Server] Executing regular attack logic for Client-{OwnerClientId}");

        // ✅ Execute attack logic
        RegularAttackBehaviour();

        // Execute sound for all clients
        PlayAttackSoundClientRpc(false, OwnerClientId);
    }

    // Perform the heavy attack if the cooldown allows
    public virtual void PerformHeavyAttack()
    {
        if (CanAttack())
        {
            Debug.Log($"[Client-{OwnerClientId}] Performing heavy attack locally.");

            //  Play animation instantly on local attacker
            TriggerAttackAnimation(HEAVY_ATTACK_ANIM_TRIGGER);
            //  Send attack request to the server
            PerformHeavyAttackServerRpc();
        }
    }

    [ServerRpc]
    private void PerformHeavyAttackServerRpc()
    {
        Debug.Log($"[Server] Received HeavyAttackServerRpc from Client-{OwnerClientId}");

        if (Time.time < nextAttackTime.Value) return;

        // ✅ Server updates nextAttackTime
        SetAttackCooldown(heavyAttackCooldown);

        Debug.Log($"[Server] Executing heavy attack logic for Client-{OwnerClientId}");

        // ✅ Execute attack logic
        HeavyAttackBehaviour();

        PlayAttackSoundClientRpc(true, OwnerClientId);
    }

    #endregion

    // Set the shared cooldown timer
    protected void SetAttackCooldown(float cooldownDuration)
    {
        nextAttackTime.Value = Time.time + cooldownDuration;

        Debug.Log($"[Server] Attack cooldown set to {nextAttackTime.Value}");
    }

    // Trigger animations based on attack type
    private void TriggerAttackAnimation(string attackType)
    {
        // Ensure Animator is assigned
        if (playerAnimator == null)
        {
            Debug.LogWarning("Animator not set for weapon!");
            return;
        }

        playerAnimator.SetTrigger(attackType);
    }

    #endregion

    #region Sound Methods

    [ClientRpc]
    private void PlayAttackSoundClientRpc(bool isHeavy, ulong attackerClientId)
    {
        if (OwnerClientId == attackerClientId) return;

        SoundData soundData = isHeavy ? heavyAttackSoundData : regularAttackSoundData;

        if (soundData != null)
        {
            SoundManager.Instance.CreateSoundBuilder()
                .WithPosition(transform.position)
                .WithRandomPitch()
                .Play(soundData);
        }
    }

    [ClientRpc]
    protected void PlayAttackHitSoundClientRpc(bool isHeavy, ulong attackerClientId, Vector3 position)
    {
        // Skip the attacker to avoid double-playing (they already hear it locally)
        if (NetworkManager.Singleton.LocalClientId == attackerClientId) return;

        SoundData soundData = isHeavy ? heavyAttackSoundDataOnHit : regularAttackSoundDataOnHit;

        if (soundData != null)
        {
            SoundManager.Instance.CreateSoundBuilder()
                .WithPosition(position)
                .WithRandomPitch()
                .Play(soundData);
        }
    }

    // Play sound for regular attack without hit
    protected void PlayRegularAttackSound()
    {
        SoundManager.Instance.CreateSoundBuilder()
            .WithPosition(transform.position)
            .WithRandomPitch()
            .Play(regularAttackSoundData);
    }

    // Play sound for regular attack on hit
    protected void PlayRegularAttackHitSound()
    {
        SoundManager.Instance.CreateSoundBuilder()
            .WithPosition(transform.position)
            .WithRandomPitch()
            .Play(regularAttackSoundDataOnHit);
    }

    // Play sound for heavy attack without hit
    protected void PlayHeavyAttackSound()
    {
        SoundManager.Instance.CreateSoundBuilder()
            .WithPosition(transform.position)
            .WithRandomPitch()
            .Play(heavyAttackSoundData);
    }

    // Play sound for heavy attack on hit
    protected void PlayHeavyAttackHitSound()
    {
        SoundManager.Instance.CreateSoundBuilder()
            .WithPosition(transform.position)
            .WithRandomPitch()
            .Play(heavyAttackSoundDataOnHit);
    }

    #endregion

    #region Projectile Behavior and Gizmos

    // Set a new shooting behavior for the projectile
    public void SetShootingBehaviour(IProjectileBehaviour newProjectileBehaviour)
    {
        this.projectileBehaviour = newProjectileBehaviour;
    }

    // Debugging method to visualize weapon range or targeting with gizmos
    protected void OnDrawGizmos()
    {
        if (raycastHolder != null)
        {
            if(weaponData != null)
            {

                float weaponLength = weaponData.WeaponLength;
                float hitRadius = weaponData.HitRadius;
                Vector3 rayOrigin = raycastHolder.position;
                Vector3 rayDirection = -raycastHolder.forward;

                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(rayOrigin, hitRadius);

                Vector3 rayEndPoint = rayOrigin + rayDirection * weaponLength;
                Gizmos.DrawWireSphere(rayEndPoint, hitRadius);

                Gizmos.DrawLine(rayOrigin, rayEndPoint);
            }
        }
    }


    #endregion


}
