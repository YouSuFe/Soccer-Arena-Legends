using System;
using UnityEngine;

[Serializable]
public class PlayerAnimationData
{
    [Header("State Group Parameter Names")]
    [SerializeField] private string groundedParameterName = "Grounded";
    [SerializeField] private string movingParameterName = "Moving";
    [SerializeField] private string stoppingParameterName = "Stopping";
    [SerializeField] private string landingParameterName = "Landing";
    [SerializeField] private string airborneParameterName = "Airborne";
    [SerializeField] private string debuffParameterName = "Debuff";
    [SerializeField] private string weaponSkillParameterName = "WeaponSkill";

    [Header("Input Parameters")]
    [SerializeField] private string inputXParameterName = "InputX";
    [SerializeField] private string inputZParameterName = "InputZ";

    [Header("Grounded Parameter Names")]
    [SerializeField] private string idleParameterName = "isIdling";
    [SerializeField] private string dashParameterName = "isDashing";
    [SerializeField] private string walkParameterName = "isWalking";
    [SerializeField] private string runParameterName = "isRunning";
    [SerializeField] private string sprintParameterName = "isSprinting";

    [Header("Airborne Parameter Names")]
    [SerializeField] private string fallParameterName = "isFalling";

    [Header("Debuff Parameter Names")]
    [SerializeField] private string stunnedParameterName = "isStunned";
    [SerializeField] private string frozenParameterName = "isFrozen";
    [SerializeField] private string shockedParameterName = "isShocked";

    [Header("Special Weapon Parameter Names")]
    [SerializeField] private string speacialDashinParameterName = "isDashWeaponUsed";

    [Header("Attack Trigger Names")]
    [SerializeField] private string regularAttackTriggerName = "regularAttack";
    [SerializeField] private string heavyAttackTriggerName = "heavyAttack";


    public int GroundedParameterHash { get; private set; }
    public int MovingParameterHash { get; private set; }
    public int StoppingParameterHash { get; private set; }
    public int LandingParameterHash { get; private set; }
    public int AirborneParameterHash { get; private set; }
    public int DebuffParamaterHash { get; private set; }
    public int WeaponSkillParamaterHash { get; private set; }

    public int InputXParameterHash { get; private set; }
    public int InputZParameterHash { get; private set; }

    public int IdleParameterHash { get; private set; }
    public int DashParameterHash { get; private set; }
    public int WalkParameterHash { get; private set; }
    public int RunParameterHash { get; private set; }
    public int SprintParameterHash { get; private set; }

    public int FallParameterHash { get; private set; }

    public int StunnedParameterHash { get; private set; }
    public int FrozenParameterHash { get; private set; }
    public int ShockedParameterHash { get; private set; }

    public int SpeacialDashinParameterHash { get; private set; }

    public int RegularAttackTriggerHash { get; private set; }
    public int HeavyAttackTriggerHash { get; private set; }

    public void Initialize()
    {
        GroundedParameterHash = Animator.StringToHash(groundedParameterName);
        MovingParameterHash = Animator.StringToHash(movingParameterName);
        StoppingParameterHash = Animator.StringToHash(stoppingParameterName);
        LandingParameterHash = Animator.StringToHash(landingParameterName);
        AirborneParameterHash = Animator.StringToHash(airborneParameterName);
        DebuffParamaterHash = Animator.StringToHash(debuffParameterName);
        WeaponSkillParamaterHash = Animator.StringToHash(weaponSkillParameterName);

        InputXParameterHash = Animator.StringToHash(inputXParameterName);
        InputZParameterHash = Animator.StringToHash(inputZParameterName);

        IdleParameterHash = Animator.StringToHash(idleParameterName);
        DashParameterHash = Animator.StringToHash(dashParameterName);
        WalkParameterHash = Animator.StringToHash(walkParameterName);
        RunParameterHash = Animator.StringToHash(runParameterName);
        SprintParameterHash = Animator.StringToHash(sprintParameterName);

        FallParameterHash = Animator.StringToHash(fallParameterName);

        StunnedParameterHash = Animator.StringToHash(stunnedParameterName);
        FrozenParameterHash = Animator.StringToHash(frozenParameterName);
        ShockedParameterHash = Animator.StringToHash(shockedParameterName);

        SpeacialDashinParameterHash = Animator.StringToHash(speacialDashinParameterName);

        RegularAttackTriggerHash = Animator.StringToHash(regularAttackTriggerName);
        HeavyAttackTriggerHash = Animator.StringToHash(heavyAttackTriggerName);
    }
}
