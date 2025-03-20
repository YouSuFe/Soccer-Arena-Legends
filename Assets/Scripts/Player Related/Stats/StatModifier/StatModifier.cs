using System;

// ToDo: Find a way to vary the debuffs and player skills
// also find a way to use them together
public enum ModifierSourceTag
{
    Buff,
    Debuff,
    BallSkillUsed,
    GravitySpeedModifier,
    SlowFieldWeaponSkill,
}

public class StatModifier : IDisposable
{
    public StatType StatType { get; }
    public IOperationStrategy OperationStrategy { get;  set; } // Change from read-only to private set
    public bool MarkedForRemoval { get; set; }
    public ModifierSourceTag SourceTag { get; } 

    public event Action<StatModifier> OnDispose;

    readonly CountdownTimer timer;

    public StatModifier(StatType type, IOperationStrategy operationStrategy, float duration, ModifierSourceTag sourceTag)
    {
        StatType = type;
        OperationStrategy = operationStrategy;
        SourceTag = sourceTag;  // Initialize with enum
        if (duration <= 0) return;

        timer = new CountdownTimer(duration);
        timer.OnTimerStop += () => MarkedForRemoval = true;
        timer.Start();
    }

    public void Update(float deltaTime) => timer?.Tick(deltaTime);

    public void Dispose()
    {
        OnDispose.Invoke(this);
    }
}
