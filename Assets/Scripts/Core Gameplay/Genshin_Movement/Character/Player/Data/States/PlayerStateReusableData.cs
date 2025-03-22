using UnityEngine;

public enum SpecialSkillReusableData
{
    None,
    IsDashing,
    IsBeingPulled,
}

public class PlayerStateReusableData
{
    public Vector2 MovementInput { get; set; }

    public SpecialSkillReusableData SpecialSkillReusableData { get; set; } = SpecialSkillReusableData.None;

    public float MovementSpeedModifier { get; set; } = 1f;
    public float MovementOnSlopeSpeedModifier { get; set; } = 1f;
    public float MovementDecelerationForce { get; set; } = 1f;

    public bool isDashing { get; set; }     // For Dash Weapon or Dash Related Skills
    public bool isGravityManBallSkillUsed { get; set; }

    public bool ShouldWalk { get; set; }
    public bool ShouldSprint { get; set; }
    public bool CanJumpOnAir { get; set; }
    public bool isFrozen { get; set; }
    public bool isStunned { get; set; }
    public bool isShocked { get; set; }

    private Vector3 currentTargetRotation;
    private Vector3 timeToReachTargetRotation;
    private Vector3 dampedTargetRotationCurrentVelocity;
    private Vector3 dampedTargetRotationPassedTime;


    public ref Vector3 CurrentTargetRotation
    {
        get
        {
            return ref currentTargetRotation;
        }
    }

    public ref Vector3 TimeToReachTargetRotation
    {
        get
        {
            return ref timeToReachTargetRotation;
        }
    }

    public ref Vector3 DampedTargetRotationCurrentVelocity
    {
        get
        {
            return ref dampedTargetRotationCurrentVelocity;
        }
    }

    public ref Vector3 DampedTargetRotationPassedTime
    {
        get
        {
            return ref dampedTargetRotationPassedTime;
        }
    }

    public Vector3 CurrentJumpForce { get; set; }

    public PlayerRotationData PlayerRotationData { get; set; }
}
