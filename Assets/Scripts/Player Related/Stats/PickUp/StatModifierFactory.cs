public class StatModifierFactory : IStatModifierFactory
{
    public StatModifier Create(OperatorType operatorType, StatType statType, float value, float duration, ModifierSourceTag sourceTag)
    {
        IOperationStrategy operationStrategy = operatorType switch
        {
            OperatorType.Add => new AddOperation(value),
            OperatorType.Multiply => new MultiplyOperation(value),
            OperatorType.MuliplyByPercentage => new MultiplyByPercentageOperation(value),
            _ => throw new System.NotImplementedException(),
        };

        return new StatModifier(statType, operationStrategy, duration, sourceTag);

    }
}
