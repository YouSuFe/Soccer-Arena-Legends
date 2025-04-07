using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    #region 🔹 Fields & Properties

    [field: Header("Input")]
    [field: SerializeField] public InputReader InputReader { get; private set; }

    [field: Header("References")]
    [field: SerializeField] public PlayerSO PlayerData { get; private set; }

    [field: Header("Collisions")]
    [field: SerializeField] public PlayerCapsuleColliderUtility ColliderUtility { get; private set; }

    [field: Header("Layers")]
    [field: SerializeField] public PlayerLayerData LayerData { get; private set; }

    [field: Header("Animations")]
    [field: SerializeField] public PlayerAnimationData AnimationData { get; private set; }

    [field: Header("Sounds Data")]
    [field: SerializeField] public SoundData PlayerFallingSoundData { get; private set; }
    [field: SerializeField] public SoundData PlayerHardFallingSoundData { get; private set; }

    public bool IsPlayerOwner => IsOwner;
    public bool IsPlayerServer => IsServer;

    public Rigidbody Rigidbody { get; private set; }
    public Animator Animator { get; private set; }

    /// Only the local player should have access to their camera.
    public Transform MainCameraTransform { get; private set; }

    private CameraSwitchHandler cameraSwitchHandler;

    public PlayerMovementStateMachine MovementStateMachine { get; private set; }

    public PlayerAbstract Player { get; private set; }

    /// Networked variable to keep player state in sync across all clients.
    private NetworkVariable<PlayerState> currentState = new NetworkVariable<PlayerState>(PlayerState.Idle);

    #endregion

    private void OnValidate()
    {
        ColliderUtility.Initialize(gameObject);
        ColliderUtility.CalculateCapsuleColliderDimension();
    }

    #region 🔹 Unity & Network Lifecycle Methods

    /// <summary>
    /// Called when the player object is spawned into the network.
    /// Ensures all essential references are initialized.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Debug.Log("Player Controller On Network Spawn");
        // Assign references needed by ALL players
        Player = GetComponent<PlayerAbstract>();
        Rigidbody = GetComponent<Rigidbody>();
        if (Animator == null || Animator.Equals(null))
        {
            Debug.LogWarning("Animator is destroyed or missing. Attempting to reassign...");
            Animator = GetComponentInChildren<Animator>();
        }
        // Synchronize state changes across the network
        currentState.OnValueChanged += OnStateChanged;

        // Assign CameraSwitchHandler (prevents null errors)
        cameraSwitchHandler = GetComponentInChildren<CameraSwitchHandler>();

        // Only allow the owner to switch their own camera
        if (IsOwner)
        {
            MainCameraTransform = Camera.main.transform;

            cameraSwitchHandler?.SetOwnerControlled(true);
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        currentState.OnValueChanged -= OnStateChanged;

    }

    /// <summary>
    /// Initializes the movement state machine and sets up input handling for the local player.
    /// </summary>
    private void Start()
    {
        // ✅ Ensure ALL players have a valid movement state machine
        MovementStateMachine = new PlayerMovementStateMachine(this, cameraSwitchHandler);

        // ✅ Only the owner should process inputs and UI updates
        if (!IsOwner) return;

        AnimationData.Initialize();
        ColliderUtility.Initialize(gameObject);
        ColliderUtility.CalculateCapsuleColliderDimension();
        InputReader.EnableInputActions();

        // ✅ Set the initial movement state
        MovementStateMachine.ChangeState(PlayerState.Idle);
    }

#endregion

    #region 🔹 State Machine Methods

    private void Update()
    {
        if (!IsOwner) return; // Only local player processes inputs

        // IMPORTANT : MainCameraTransform is being null after second update call.
        // I think it is due to dynamic network object creation after scene change, it becomes null .
        if (MainCameraTransform == null)
        {
            Debug.LogWarning("MainCameraTransform is null! Trying to reassign from Update...");
            MainCameraTransform = Camera.main.transform;
        }

        MovementStateMachine.HandleInput();
        MovementStateMachine.Update();
    }

    private void LateUpdate()
    {
        if (!IsOwner) return; // Only local player processes inputs

        MovementStateMachine.LateUpdate();
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return; // Only local player processes inputs

        MovementStateMachine.FixedUpdate();
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (!IsOwner) return; // Only local player processes inputs

        MovementStateMachine.OnTriggerEnter(collider);
    }

    private void OnTriggerExit(Collider collider)
    {
        if (!IsOwner) return; // Only local player processes inputs

        MovementStateMachine.OnTriggerExit(collider);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsOwner) return; // Only local player processes inputs

        MovementStateMachine.OnCollisionEnter(collision);
    }

    public void OnMovementStateAnimationEnterEvent()
    {
        if (!IsOwner) return; // Only local player processes inputs

        MovementStateMachine.OnAnimationEnterEvent();
    }

    public void OnMovementStateAnimationExitEvent()
    {
        if (!IsOwner) return; // Only local player processes inputs

        MovementStateMachine.OnAnimationExitEvent();
    }

    public void OnMovementStateAnimationTransitionEvent()
    {
        if (!IsOwner) return; // Only local player processes inputs

        MovementStateMachine.OnAnimationTransitionEvent();
    }

    #endregion

    /// <summary>
    /// Called whenever the player's state changes over the network.
    /// Ensures only the owner updates their movement state.
    /// </summary>
    private void OnStateChanged(PlayerState previous, PlayerState current)
    {
        if (!IsOwner) return;

        Debug.Log($"[OnStateChanged] Player {Player?.name} state changed from {previous} to {current}");
        MovementStateMachine.ChangeState(current);
    }

    // ✅ Optimized ServerRPC
    [ServerRpc]
    public void RequestStateChangeServerRpc(PlayerState newState)
    {
        if (!IsServer) return;

        Debug.Log($"Client requested state change to {newState}");

        if (IsCriticalState(newState))
        {
            currentState.Value = newState; // Auto-syncs to clients!
        }
    }

    #region 🔹 Dash Synchronization

    /// <summary>
    /// Notifies all clients that a player's dash action has ended.
    /// This prevents unintended movement desynchronization.
    /// </summary>
    [ClientRpc]
    public void NotifyClientsEndDashClientRpc()
    {
        if (!IsOwner)
        {
            MovementStateMachine.ReusableData.isDashing = false;
            Debug.Log("Dash ended on client due to collision.");
        }
    }

    #endregion

    private bool IsCriticalState(PlayerState state)
    {
        return state == PlayerState.Stunned || state == PlayerState.Frozen || state == PlayerState.SpecialWeaponDashing;
    }

}
