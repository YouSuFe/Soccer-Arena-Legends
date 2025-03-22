using System.Collections.Generic;
using System.Linq;

public class NormalStatModifierOrder : IStatModifierApplicationOrder
{
    // Maybe I should change it int to float beacuse I am setting it to integer on stats Get method
    public float Apply(IEnumerable<StatModifier> statModifiers, float baseValue)
    {
        var allStatModifiers = statModifiers.ToList();
        var addModifiers = allStatModifiers.Where(modifier => modifier.OperationStrategy is AddOperation);
        var multiplyModifiers = allStatModifiers.Where(modifier => modifier.OperationStrategy is MultiplyOperation);
        var multiplyByPercentageModifiers = allStatModifiers.Where(modifier => modifier.OperationStrategy is MultiplyByPercentageOperation);


        foreach (var modifier in addModifiers)
        {
            baseValue = modifier.OperationStrategy.Calculate(baseValue);
        }

        foreach (var modifier in multiplyModifiers)
        {
            baseValue = modifier.OperationStrategy.Calculate(baseValue);
        }

        foreach (var modifier in multiplyByPercentageModifiers)
        {
            baseValue = modifier.OperationStrategy.Calculate(baseValue);
        }

        // Returning int value to get rif of potential precisions
        return (baseValue);
    }
}