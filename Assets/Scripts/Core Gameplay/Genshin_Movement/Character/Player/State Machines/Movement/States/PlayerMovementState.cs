using System;
using UnityEngine;

public class PlayerMovementState : IState
{
    protected PlayerMovementStateMachine stateMachine;

    protected PlayerGroundedData groundedData;
    protected PlayerAirborneData airborneData;
    protected PlayerDebuffData debuffData;
    protected SpecialWeaponData specialWeaponData;

    // Camera switch handler
    protected CameraSwitchHandler cameraSwitchHandler;

    // Cache components to avoid repeated lookups
    protected Rigidbody playerRigidbody;
    protected InputReader inputReader;

    private Transform mainCameraTransform => stateMachine.PlayerController.MainCameraTransform;

    public PlayerMovementState(PlayerMovementStateMachine playerMovementStateMachine, CameraSwitchHandler cameraSwitcher)
    {
        stateMachine = playerMovementStateMachine;
        cameraSwitchHandler = cameraSwitcher;

        groundedData = stateMachine.PlayerController.PlayerData.GroundedData;
        airborneData = stateMachine.PlayerController.PlayerData.AirborneData;
        debuffData = stateMachine.PlayerController.PlayerData.DebuffData;
        specialWeaponData = stateMachine.PlayerController.PlayerData.SpecialWeaponData;

        InitializeData();

        cameraSwitchHandler.OnCameraSwitch += CameraSwitchHandler_OnSwitchFromFirstPerson;

    }

    private void InitializeData()
    {
        // Cache components
        playerRigidbody = stateMachine.PlayerController.Rigidbody;
        inputReader = stateMachine.PlayerController.InputReader;
        SetBasePlayerRotationData();
    }

    #region Input Methods

    public virtual void OnAnimationEnterEvent()
    {
    }

    public virtual void OnAnimationExitEvent()
    {
    }

    public virtual void OnAnimationTransitionEvent()
    {
    }



    #endregion

    #region IState Method
    public virtual void Enter()
    {
        //Debug.Log("State : " + GetType().Name);

        AddInputActionsCallBacks();
    }
    public virtual void Exit()
    {
        RemoveInputActionsCallBacks();
        cameraSwitchHandler.OnCameraSwitch -= CameraSwitchHandler_OnSwitchFromFirstPerson;

    }

    public virtual void CameraUpdate()
    {
        if (cameraSwitchHandler.IsFPSCameraActive())
        {
            RotateWithCamera();  // Continuously sync the player rotation with the camera
        }
    }

    public virtual void PhysicsUpdate()
    {
        Move();
    }

    public virtual void HandleInput()
    {
        if(!stateMachine.PlayerController.Player.IsPlayerAllowedToMove())
        {
            ClearMovementInput();
            return;
        }
        ReadMovementInput();
    }

    public virtual void Update()
    {

        if(cameraSwitchHandler.IsFPSCameraActive())
        {
            SetMovementAnimationParameters(stateMachine.ReusableData.MovementInput.x, stateMachine.ReusableData.MovementInput.y);
        }

        UpdateStamina();
    }

    public virtual void OnTriggerEnter(Collider collider)
    {

        if (stateMachine.PlayerController.LayerData.IsGroundLayer(collider.gameObject.layer))
        {
            OnContactWithGround(collider);

            return;
        }

    }

    public virtual void OnTriggerExit(Collider collider)
    {

        if (stateMachine.PlayerController.LayerData.IsGroundLayer(collider.gameObject.layer))
        {
            OnContactWithGroundExited(collider);

            return;
        }
    }

    public virtual void OnCollisionEnter(Collision collision)
    {
    }

    #endregion

    #region Main Methods

    void ReadMovementInput()
    {
        stateMachine.ReusableData.MovementInput = inputReader.GetPlayerMovement();
    }

    private void ClearMovementInput()
    {
        ResetVelocity();
        stateMachine.ReusableData.MovementInput = Vector2.zero; // Reset movement input
    }

    protected void SetMovementAnimationParameters(float inputX, float inputZ)
    {
        Animator animator = stateMachine.PlayerController.Animator;
        
        // Set float parameters for movement
        animator.SetFloat(stateMachine.PlayerController.AnimationData.InputXParameterHash, inputX);
        animator.SetFloat(stateMachine.PlayerController.AnimationData.InputZParameterHash, inputZ);
    }

    void Move()
    {
        if (stateMachine.ReusableData.MovementInput == Vector2.zero
            || stateMachine.ReusableData.MovementSpeedModifier == 0f
            || stateMachine.ReusableData.isDashing) return;

        Vector3 movementDirection = GetMovementInputDirection();

        float targetRotationYAngle = RotatePlayer(movementDirection);

        Vector3 targetRotationDirection;

        if (cameraSwitchHandler.IsFPSCameraActive())
        {
            // In first-person mode, use the input-relative movement direction directly
            targetRotationDirection = movementDirection;
        }
        else
        {
            // In third-person mode, calculate movement direction based on player's rotation
            targetRotationDirection = GetTargetRotationDirection(targetRotationYAngle);
        }


        float movementSpeed = GetMovementSpeed();

        Vector3 currentPlayerHorizontalVelocity = GetPlayerHorizontalVelocity();

        playerRigidbody.AddForce(targetRotationDirection * movementSpeed - currentPlayerHorizontalVelocity, ForceMode.VelocityChange);
    }

    private void UpdateStamina()
    {
        if (stateMachine.CurrentState == stateMachine.GetState(PlayerState.SprintStarting)) return;

        // Reduce stamina when sprinting
        if (stateMachine.CurrentState == stateMachine.GetState(PlayerState.Sprinting) || stateMachine.ReusableData.ShouldSprint)
        {
            stateMachine.PlayerController.Player.PlayerStamina -= groundedData.PlayerSprintData.StaminaConsumptionPerSecond * Time.deltaTime;
        }
        else
        {
            if(stateMachine.PlayerController.Player == null)
            {
                Debug.LogWarning("Player is null inside PlayerController");
                return;
            }


            // Regenerate stamina if not sprinting
            stateMachine.PlayerController.Player.PlayerStamina = Mathf.Min(
                stateMachine.PlayerController.Player.PlayerMaxStamina,
                stateMachine.PlayerController.Player.PlayerStamina + groundedData.PlayerSprintData.StaminaRegenRate * Time.deltaTime
            );
        }
    }


    float RotatePlayer(Vector3 direction)
    {
        if (cameraSwitchHandler.IsFPSCameraActive())
        {
            // In first-person mode, rotate the player instantly with the camera
            return stateMachine.PlayerController.MainCameraTransform.eulerAngles.y;
        }
        else
        {
            // Third-person mode uses smooth rotation
            float directionAngle = UpdateTargetRotation(direction);
            RotateTowardsTargetRotation();
            return directionAngle;
        }
    }

    protected void RotateWithCamera()
    {
        // Get camera's Y rotation
        float cameraYRotation = mainCameraTransform.eulerAngles.y;

        // Create a new rotation for the player based on the camera's Y rotation
        Quaternion targetRotation = Quaternion.Euler(0f, cameraYRotation, 0f);

        // Directly apply the rotation to the player’s Rigidbody
        playerRigidbody.MoveRotation(targetRotation);
    }

    private float AddCameraRotationToAngle(float angle)
    {
        angle += mainCameraTransform.eulerAngles.y;

        if (angle > 360f) angle -= 360f;
        return angle;
    }

    private float GetDirectionAngle(Vector3 direction)
    {
        float directionAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

        if (directionAngle < 0f) directionAngle += 360f;
        return directionAngle;
    }

    private void UpdateTargetRotationData(float targetAngle)
    {
        stateMachine.ReusableData.CurrentTargetRotation.y = targetAngle;

        stateMachine.ReusableData.DampedTargetRotationPassedTime.y = 0f;
    }

    #endregion

    #region Reusable Methods

    protected void StartAnimation(int animationHash)
    {
        stateMachine.PlayerController.Animator.SetBool(animationHash, true);
    }

    protected void StopAnimation(int animationHash)
    {
        stateMachine.PlayerController.Animator.SetBool(animationHash, false);
    }

    #region Movement Related
    protected Vector3 GetMovementInputDirection()
    {
        Vector3 movementDirection;

        if(cameraSwitchHandler.IsFPSCameraActive())
        {
            if (mainCameraTransform == null)
            {
                Debug.LogWarning("mainCameraTransform is null! From GetDirection Trying to reassign...");

            }

            // Get the camera's forward and right directions, but ignore the y-axis to keep the movement on a horizontal plane.
            Vector3 cameraForward = mainCameraTransform.forward;
            Vector3 cameraRight = mainCameraTransform.right;

            cameraForward.y = 0f; // Ignore vertical component for movement
            cameraRight.y = 0f;   // Ignore vertical component for movement

            cameraForward.Normalize();
            cameraRight.Normalize();

            // Combine the forward and right directions with the player's input (WASD or controller).
            movementDirection = cameraForward * stateMachine.ReusableData.MovementInput.y +
                                        cameraRight * stateMachine.ReusableData.MovementInput.x;
        }

        else
        {
            return new Vector3(stateMachine.ReusableData.MovementInput.x, 0f, stateMachine.ReusableData.MovementInput.y);
        }


        return movementDirection;
    }

    protected float GetMovementSpeed(bool shouldConsiderSlopes = true)
    {
        float movementSpeed = stateMachine.PlayerController.Player.Stats.GetCurrentStat(StatType.Speed) * stateMachine.ReusableData.MovementSpeedModifier;

        if(shouldConsiderSlopes)
        {
            movementSpeed *= stateMachine.ReusableData.MovementOnSlopeSpeedModifier;
        }
        return movementSpeed;
    }

    protected void SetBasePlayerRotationData()
    {
        stateMachine.ReusableData.PlayerRotationData = groundedData.PlayerRotationData;

        stateMachine.ReusableData.TimeToReachTargetRotation = stateMachine.ReusableData.PlayerRotationData.TargetRotationReachTime;
    }

    protected Vector3 GetPlayerHorizontalVelocity()
    {
        Vector3 playerHorizontalVelocity = playerRigidbody.linearVelocity;

        playerHorizontalVelocity.y = 0f;

        return playerHorizontalVelocity;
    }

    protected Vector3 GetPlayerVerticalVelocity()
    {
        return new Vector3(0f, playerRigidbody.linearVelocity.y, 0f);
    }

    protected void DecelerateHorizontally()
    {
        Vector3 playerHorizontalVelocity = GetPlayerHorizontalVelocity();

        playerRigidbody.AddForce(-playerHorizontalVelocity * stateMachine.ReusableData.MovementDecelerationForce, ForceMode.Acceleration);
    }

    protected void DecelerateVertically()
    {
        Vector3 playerVerticalVelocity = GetPlayerVerticalVelocity();

        playerRigidbody.AddForce(-playerVerticalVelocity * stateMachine.ReusableData.MovementDecelerationForce, ForceMode.Acceleration);
    }

    protected bool IsMovingHorizontally(float minimumMagnitude = 0.1f)
    {
        Vector3 playerHorizontalVelocity = GetPlayerHorizontalVelocity();

        Vector2 playerHorizontalMovement = new Vector2(playerHorizontalVelocity.x, playerHorizontalVelocity.z);

        return playerHorizontalMovement.magnitude > minimumMagnitude;
    }

    protected bool IsMovingUp(float minimumVelocity = 0.1f)
    {
        return GetPlayerVerticalVelocity().y < minimumVelocity;
    }

    protected bool IsMovingDown(float minimumVelocity = 0.1f)
    {
        return GetPlayerVerticalVelocity().y < -minimumVelocity;
    }

    protected void ResetVelocity()
    {
        if(!playerRigidbody.isKinematic)
            playerRigidbody.linearVelocity = Vector3.zero;
    }

    protected void ResetVerticalVelocity()
    {
        Vector3 playerHorizontalVelocity = GetPlayerHorizontalVelocity();

        playerRigidbody.linearVelocity = playerHorizontalVelocity;
    }

    protected bool CanStartSprinting()
    {
        bool canStartSprinting = stateMachine.PlayerController.Player.IsPlayerAllowedToMove() &&
            stateMachine.PlayerController.Player.PlayerStamina > 20;

        return canStartSprinting;
    }

    protected bool CanKeepSprinting()
    {
        return stateMachine.PlayerController.Player.PlayerStamina > 0;
    }
    #endregion

    #region Rotation Related
    protected void RotateTowardsTargetRotation()
    {
        float currentAngle = playerRigidbody.rotation.eulerAngles.y;

        if (currentAngle == stateMachine.ReusableData.CurrentTargetRotation.y) return;

        float smoothedYAngle = Mathf.SmoothDampAngle(currentAngle, stateMachine.ReusableData.CurrentTargetRotation.y,
            ref stateMachine.ReusableData.DampedTargetRotationCurrentVelocity.y,
            stateMachine.ReusableData.TimeToReachTargetRotation.y - stateMachine.ReusableData.DampedTargetRotationPassedTime.y);

        stateMachine.ReusableData.DampedTargetRotationPassedTime.y += Time.deltaTime;

        Quaternion targetRotation = Quaternion.Euler(0f, smoothedYAngle, 0f);

        playerRigidbody.MoveRotation(targetRotation);
    }

    protected float UpdateTargetRotation(Vector3 direction, bool shouldConsiderCameraRotation = true)
    {
        float directionAngle = GetDirectionAngle(direction);
        if (shouldConsiderCameraRotation) directionAngle = AddCameraRotationToAngle(directionAngle);

        if (directionAngle != stateMachine.ReusableData.CurrentTargetRotation.y) UpdateTargetRotationData(directionAngle);
        return directionAngle;
    }

    protected Vector3 GetTargetRotationDirection(float targetAngle)
    {
        // Order Matters
        return Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
    }

    #endregion


    protected virtual void AddInputActionsCallBacks()
    {
        inputReader.OnWalkTogglePerformed += InputManager_OnWalkToggleStarted;
        inputReader.OnWeaponSkillUsed += InputManager_OnWeaponSkillUsed;
    }

    protected virtual void RemoveInputActionsCallBacks()
    {
        inputReader.OnWalkTogglePerformed -= InputManager_OnWalkToggleStarted;
        inputReader.OnWeaponSkillUsed -= InputManager_OnWeaponSkillUsed;
    }


    protected virtual void OnContactWithGround(Collider collider)
    {

    }

    protected virtual void OnContactWithGroundExited(Collider collider)
    {
    }

    #endregion

    #region Input Methods

    protected virtual void InputManager_OnWalkToggleStarted()
    {
        stateMachine.ReusableData.ShouldWalk = !stateMachine.ReusableData.ShouldWalk;
        Debug.Log("Should walk   " + stateMachine.ReusableData.ShouldWalk);
    }

    protected void CameraSwitchHandler_OnSwitchFromFirstPerson()
    {
        if (cameraSwitchHandler.currentCameraMode != CameraMode.FirstPerson)
        {
            SetMovementAnimationParameters(0f, 0f);
        }
    }


    protected void InputManager_OnWeaponSkillUsed()
    {
        if (stateMachine.ReusableData.SpecialSkillReusableData == SpecialSkillReusableData.None) return;

        // Switch statement to handle different special skill states
        switch (stateMachine.ReusableData.SpecialSkillReusableData)
        {
            case SpecialSkillReusableData.IsDashing:
                // Client side immediate State Change
                stateMachine.ChangeState(PlayerState.SpecialWeaponDashing);

                // Server side State Change call
                stateMachine.PlayerController.RequestStateChangeServerRpc(PlayerState.SpecialWeaponDashing);
                break;

            // Add more cases as needed for additional states

            default:
                Debug.LogWarning("Unhandled special skill state: " + stateMachine.ReusableData.SpecialSkillReusableData);
                break;
        }

    }

    #endregion

    #region Sounds Settings
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
}
